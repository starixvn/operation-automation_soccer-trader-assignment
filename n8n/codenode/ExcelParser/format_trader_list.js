/**
* Map staff level values to normalized format
*/
function mapStaffLevel(level) {
    if (!level) return null;

    const levelMap = {
        'elite': 'elite',
        'member': 'member',
        '(1st)': 'first',
        '(2nd)': 'second',
        '(3rd)': 'third',
        '(4th)': 'fourth',
        'op(1st)': 'op_first',
        'op(2nd)': 'op_second'
    };

    const lowerLevel = level.toLowerCase();
    return levelMap[lowerLevel] || level;
}

/**
 * Parse M1 and M2 capacity from bettype string
 * Default capacity is 1 for M1, 2 for M2 if not specified
 * Examples:
 * - "HDP/M1*2" → { m1Capacity: 2, m2Capacity: 1 }
 * - "OU/M2*3" → { m1Capacity: 1, m2Capacity: 3 }
 * - "2/M1*2/M2*1" → { m1Capacity: 2, m2Capacity: 1 }
 * - "HDP/OU" → { m1Capacity: 1, m2Capacity: 1 } (default)
 */
function parseCapacity(bettype) {
    if (!bettype || typeof bettype !== 'string') {
        return { m1Capacity: 1, m2Capacity: 2 };
    }

    // Default capacity
    let m1Capacity = 1;
    let m2Capacity = 2;

    // Match M1*<number>
    const m1Match = bettype.match(/M1\*(\d+)/i);
    if (m1Match) {
        m1Capacity = parseInt(m1Match[1], 10);
    }

    // Match M2*<number>
    const m2Match = bettype.match(/M2\*(\d+)/i);
    if (m2Match) {
        m2Capacity = parseInt(m2Match[1], 10);
    }

    return { m1Capacity, m2Capacity };
}

/**
 * Parse ignore league string into array
 * Examples:
 * - "league 1, league 2" → ["league 1", "league 2"]
 * - "EPL, LaLiga, SerieA" → ["EPL", "LaLiga", "SerieA"]
 * - "" or null → []
 */
function parseIgnoreLeague(ignoreLeagueStr) {
    if (!ignoreLeagueStr || typeof ignoreLeagueStr !== 'string') {
        return [];
    }

    // Split by comma and trim whitespace
    return ignoreLeagueStr
        .split(',')
        .map(league => league.trim())
        .filter(league => league.length > 0);
}

/**
 * Parse non-solo league string into array
 * Traders cannot trade solo matches in these leagues
 * Examples:
 * - "EPL, LaLiga" → ["EPL", "LaLiga"]
 * - "JAPAN J-LEAGUE DIVISION 1, AUSTRALIA A-LEAGUE" → ["JAPAN J-LEAGUE DIVISION 1", "AUSTRALIA A-LEAGUE"]
 * - "" or null → []
 */
function parseNonSoloLeague(nonSoloLeagueStr) {
    if (!nonSoloLeagueStr || typeof nonSoloLeagueStr !== 'string') {
        return [];
    }

    // Split by comma and trim whitespace
    return nonSoloLeagueStr
        .split(',')
        .map(league => league.trim())
        .filter(league => league.length > 0);
}

/**
 * Check if trader can be assigned to EXPT matches
 * Default is true, unless bettype contains "[No Expt]" (case-insensitive)
 * Examples:
 * - "HDP/M1*2" → true
 * - "2/M1*2[No Expt]" → false
 * - "OU/M2*3 [no expt]" → false
 * - "" or null → true
 */
function parseIsEXPTAssignable(bettype) {
    if (!bettype || typeof bettype !== 'string') {
        return true;
    }

    // Check if bettype contains "[No Expt]" (case-insensitive)
    return !bettype.match(/\[no\s*expt\]/i);
}

/**
 * Parse MaxBet from various formats and convert to integer
 * Examples:
 * - "14k" or "14K" → 14000
 * - "14 k" or "14 K" → 14000
 * - "14000" → 14000
 * - 14000 → 14000
 * - "" or null → 1000000 (default no limit)
 */
function parseMaxBet(maxBetValue) {
    const DEFAULT_MAX_BET = 1000000; // Default: 1 million (no MaxBet limit)

    if (!maxBetValue) {
        return DEFAULT_MAX_BET;
    }

    // Convert to string and trim whitespace
    const maxBetStr = String(maxBetValue).trim().toLowerCase();

    // Match patterns like "14k", "14 k", or plain numbers
    const match = maxBetStr.match(/^(\d+(?:\.\d+)?)\s*k?$/);

    if (!match) {
        return DEFAULT_MAX_BET; // Invalid format defaults to no limit
    }

    const numericValue = parseFloat(match[1]);

    // If the original string contained 'k', multiply by 1000
    if (maxBetStr.includes('k')) {
        return Math.round(numericValue * 1000);
    }

    // Otherwise, just return the numeric value as integer
    return Math.round(numericValue);
}

const result = [];

for (const item of $input.all()) {
    // Extract data from input
    const traderName = item.json.NAME;
    const traderLevel = item.json.LEVEL;
    const bettype = item.json['MAIN BET TYPE'];
    const group = item.json.GROUP;
    const ignoreLeague = item.json['Ignore League'];
    const nonSoloLeague = item.json['Non-Solo league'];
    const prioritySkill = item.json['Priority skill'];
    const rowNumber = item.json.row_number;

    // Initialize level objects
    let managementLevel = null;
    let staffLevel = null;
    let isManager = false;

    // Check if trader has PIC (People In Charge) - management level
    if (traderLevel && traderLevel.includes('[PIC]')) {
        isManager = true;
        managementLevel = '[PIC]';

        // Extract staff level from the remaining part
        const levelParts = traderLevel.split('/');

        if (levelParts.length > 1) {
            // Has both management and staff level (e.g., "[PIC]/Elite", "[PIC]/(1st)")
            staffLevel = levelParts[1];
        } else {
            // Only management level (e.g., "[PIC]")
            staffLevel = null;
        }
    } else {
        // No management level, only staff level
        managementLevel = null;
        staffLevel = traderLevel;
    }

    // Parse M1 and M2 capacity from bettype
    const { m1Capacity, m2Capacity } = parseCapacity(bettype);

    // Parse ignore league into array
    const ignoreLeagueArray = parseIgnoreLeague(ignoreLeague);

    // Parse non-solo league into array
    const nonSoloLeagueArray = parseNonSoloLeague(nonSoloLeague);

    // Parse EXPT assignability from bettype
    const isEXPTAssignable = parseIsEXPTAssignable(bettype);

    // Parse MaxBet from Excel (optional field, supports formats like "14k", "14K", "14 K", "14000")
    const maxBet = parseMaxBet(item.json.MaxBet);

    // Add to result array in n8n format
    result.push({
        json: {
            id: rowNumber,
            group: group,
            traderName,
            originalLevel: traderLevel,
            managementLevel: managementLevel,
            staffLevel: mapStaffLevel(staffLevel),
            isManager: isManager,
            bettype,
            ignoreLeague: ignoreLeagueArray,
            nonSoloLeague: nonSoloLeagueArray,
            m1Capacity: m1Capacity,
            m2Capacity: m2Capacity,
            isEXPTAssignable: isEXPTAssignable,
            maxBet: maxBet,
            prioritySkill: prioritySkill || null,
            nodeType: 'trader'
        }
    });
}

return result;
