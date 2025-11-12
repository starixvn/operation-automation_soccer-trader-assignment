/**
 * Transform n8n data to Trader Assignment API format for M1 matches
 *
 * This code node transforms the input data from two different node types:
 * - mismatch (matches to assign)
 * - workingShift (trader availability)
 *
 * M1 matches don't require skills or preconfigured pairs since one trader handles both HDP and OU.
 */

const allItems = $input.all();

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

// Define transformation handlers for each nodeType
const nodeTypeHandlers = {
  mismatch: (data) => {
    // Only include M1 matches
    if (data.betType === 'M1') {
      matches.push({
        id: data.rowNumber?.toString() || '',
        matchNo: data.no?.toString() || '',
        start: data.matchDate,
        end: data.endDate,
        maxBet: data.maxBet || 0,
        leagueCode: data.leagueCode || null,
        leagueName: data.leagueName || null,
        betType: data.betType || null,
        isSoloMatch: data.isSoloMatch || false,
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
      level: capitalizeFirst(data.staffLevel),
      isManager: data.isManager || false,
      isEXPTMatchAssignable: data.isEXPTAssignable !== undefined ? data.isEXPTAssignable : true,
      m1Capacity: data.m1Capacity || 1,
      ignoreLeague: data.ignoreLeague || [],
      nonSoloLeagues: data.nonSoloLeague || [],
      maxBet: data.maxBet !== undefined ? data.maxBet : 1000000, // Default: 1 million (no limit)
      assignedJobs: data.assignedJobs?.map(job => ({
        type: job.type,
        start: job.start,
        end: job.end
      })) || []
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
    preconfiguredPairs: []
  }
}];
