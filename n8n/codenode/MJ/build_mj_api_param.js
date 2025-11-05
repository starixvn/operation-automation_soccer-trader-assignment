/**
 * Transform n8n data to Trader Assignment API format
 *
 * This code node transforms the input data from three different node types:
 * - mismatch (matches to assign)
 * - workingShift (trader availability)
 * - pairTrader (preconfigured trader pairs)
 *
 * Into the API format with proper field mappings and skill parsing.
 */

const allItems = $input.all();

/**
 * Parse bettype string into skills array (HDP and/or OU only)
 *
 * Rules:
 * - "2" means trader can handle BOTH HDP and OU
 * - "2[OP]" is ignored (not relevant for MJ assignment)
 * - "R-FS" is ignored (not relevant for MJ assignment)
 * - Only HDP and OU skills are extracted
 *
 * Examples:
 * - "HDP/M1*2" → ["HDP"]
 * - "OU/M1*2" → ["OU"]
 * - "2/M1*2" → ["HDP", "OU"]
 * - "2[OP]" → []
 * - "OU/R-FS/M1*2" → ["OU"]
 */
function parseBetTypeToSkills(bettype) {
  if (!bettype) return [];

  const parts = bettype.split('/');
  const skills = new Set();

  for (const part of parts) {
    const trimmed = part.trim();

    // Skip multiplier patterns (M1*2, M2*1, etc.)
    if (trimmed.match(/^M\d+\*\d+/)) continue;

    // Skip irrelevant skills
    if (trimmed.startsWith('2[OP]') || trimmed.startsWith('R-FS')) continue;

    // "2" means can handle both HDP and OU
    if (trimmed === '2' || trimmed.startsWith('2 ')) {
      skills.add('HDP');
      skills.add('OU');
    }
    // Direct HDP or OU
    else if (trimmed.startsWith('HDP')) {
      skills.add('HDP');
    }
    else if (trimmed.startsWith('OU')) {
      skills.add('OU');
    }
    // Handle patterns like "Solo HDP", "Priority HDP"
    else if (trimmed.match(/^[A-Z]/)) {
      if (trimmed.includes('HDP')) skills.add('HDP');
      if (trimmed.includes('OU')) skills.add('OU');
    }
  }

  return Array.from(skills);
}

/**
 * Capitalize first letter of a string
 * Used for level transformation: "elite" → "Elite", "first" → "First"
 */
function capitalizeFirst(str) {
  if (!str) return str;
  return str.charAt(0).toUpperCase() + str.slice(1).toLowerCase();
}

// Initialize result arrays
const matches = [];
const traderShifts = [];
const preconfiguredPairs = [];

// Define transformation handlers for each nodeType
const nodeTypeHandlers = {
  mismatch: (data) => {
    // Only include MJ matches
    if (data.betType === 'MJ') {
      matches.push({
        id: data.rowNumber?.toString() || '',
        matchNo: data.no?.toString() || '',
        start: data.matchDate,
        end: data.endDate,
        maxBet: data.maxBet || 0,
        leagueCode: data.leagueCode || null,
        leagueName: data.league || null,
        isAlreadyHandled: data.isUnchanged || false,
        isEXPT: data.isEXPT !== undefined ? data.isEXPT : true,
        isEAL: data.isEAL || false
      });
    }
  },

  workingShift: (data) => {
    traderShifts.push({
      id: data.rowNumber?.toString() || '',
      name: data.traderName || '',
      start: data.startTime,
      end: data.endTime,
      skills: parseBetTypeToSkills(data.bettype),
      level: capitalizeFirst(data.staffLevel),
      isManager: data.isManager || false,
      isEXPTMatchAssignable: data.isEXPTAssignable !== undefined ? data.isEXPTAssignable : true,
      ignoreLeague: data.ignoreLeague || [],
      assignedJobs: data.assignedJobs?.map(job => ({
        type: job.type,
        start: job.start,
        end: job.end
      })) || []
    });
  },

  pairTrader: (data) => {
    preconfiguredPairs.push({
      hdp: data.hdp,
      ou: data.ou,
      tier: data.tier,
      leaguePriority: data.league_priority || {}
    });
  }
};

// Process each item using the appropriate handler
for (const item of allItems) {
  const data = item.json;
  const handler = nodeTypeHandlers[data.nodeType];

  if (handler) {
    handler(data);
  }
}

// Return transformed data in API format
return [{
  json: {
    traderShifts: traderShifts,
    matches: matches,
    preconfiguredPairs: preconfiguredPairs
  }
}];
