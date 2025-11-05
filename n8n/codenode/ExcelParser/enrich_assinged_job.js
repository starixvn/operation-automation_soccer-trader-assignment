/**
 * Create an assigned job object from a match
 */
function createAssignedJob(traderMatch) {
  return {
    type: traderMatch.betType,
    start: traderMatch.matchDate,
    end: traderMatch.endDate
  };
}

/**
 * Parse existing assignedJobs from shift data
 */
function parseAssignedJobs(shift) {
  if (!shift.assignedJobs) {
    return [];
  }
  
  if (typeof shift.assignedJobs === 'string') {
    try {
      return JSON.parse(shift.assignedJobs);
    } catch (e) {
      return [];
    }
  }
  
  if (Array.isArray(shift.assignedJobs)) {
    return [...shift.assignedJobs];
  }
  
  return [];
}

/**
 * Check if shift is a crossing shift for previous day
 */
function isPreviousDayCrossingShift(shift, previousDate) {
  if (!shift.isCrossingShift || !previousDate) {
    return false;
  }
  
  const shiftDate = new Date(shift.shiftDate);
  shiftDate.setUTCHours(0, 0, 0, 0);
  
  return shiftDate.getTime() === previousDate.getTime();
}

/**
 * Get previous date from matches
 */
function getPreviousDate(previousMatches) {
  if (previousMatches.length === 0) {
    return null;
  }
  
  const firstMatch = previousMatches[0].json;
  if (!firstMatch.matchDate) {
    return null;
  }
  
  const matchDate = new Date(firstMatch.matchDate);
  const previousDate = new Date(matchDate);
  previousDate.setUTCHours(0, 0, 0, 0);
  
  return previousDate;
}

/**
 * Find all matches for a trader
 */
function findTraderMatches(previousMatches, traderName) {
  return previousMatches.filter(matchItem => {
    const match = matchItem.json;
    return match.hdpTrader === traderName || match.ouTrader === traderName;
  });
}

/**
 * Enrich shift with assigned jobs from previous day matches
 */
function enrichShiftWithPreviousJobs(shift, previousMatches, previousDate) {
  let assignedJobs = parseAssignedJobs(shift);
  
  if (isPreviousDayCrossingShift(shift, previousDate)) {
    const traderMatchItems = findTraderMatches(previousMatches, shift.traderName);
    
    // Add all matched trader's jobs
    for (const traderMatchItem of traderMatchItems) {
      const job = createAssignedJob(traderMatchItem.json);
      assignedJobs.push(job);
    }
  }
  
  return assignedJobs;
}

// Main execution
const allItems = $input.all();

// Filter items by nodeType
const workingShifts = allItems.filter(item => item.json.nodeType === 'workingShift');
const previousMatches = allItems
  .filter(item => item.json.nodeType === 'mismatch')
  .sort((a, b) => {
    const dateA = new Date(a.json.matchDate);
    const dateB = new Date(b.json.matchDate);
    return dateA - dateB; // ascending order
  });

if (workingShifts.length === 0) {
  return [];
}

// Get previous date from first match
const previousDate = getPreviousDate(previousMatches);

// Enrich each working shift
const enrichedShifts = workingShifts.map(item => {
  const shift = item.json;
  const assignedJobs = enrichShiftWithPreviousJobs(shift, previousMatches, previousDate);
  
  return {
    json: {
      ...shift,
      assignedJobs: assignedJobs,
      nodeType: 'workingShift'
    },
    pairedItem: item.pairedItem
  };
});

return enrichedShifts;
