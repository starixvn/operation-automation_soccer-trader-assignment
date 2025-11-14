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
 * Create an assigned job object from a match
 */
function createAssignedJob(match, traderName) {
  // Check if this trader is assigned as first half trader
  const isFirstHalfTrader = match['1st Half'] === traderName || match.firstHalf === traderName;

  return {
    type: isFirstHalfTrader ? 'FirstHalf' : match.betType,
    start: match.matchDate,
    end: match.endDate
  };
}

/**
 * Check if match starts during or after shift start
 */
function matchStartsAfterShift(shift, match) {
  const shiftStart = new Date(shift.startTime);
  const matchStart = new Date(match.matchDate);

  // Match must start at or after shift start
  return shiftStart <= matchStart;
}

/**
 * Find matches for a trader that start at or after shift start
 */
function findTraderMatchesInShift(matches, traderName, shift) {
  return matches.filter(match => {
    const isTraderMatch = match.hdpTrader === traderName ||
      match.ouTrader === traderName ||
      match['1st Half'] === traderName ||
      match.firstHalf === traderName;
    const matchStartsInShift = matchStartsAfterShift(shift, match);
    return isTraderMatch && matchStartsInShift;
  });
}

// Main execution
const allItems = $input.all();

// Get config
const configItem = allItems.find(item => item.json.nodeType === 'config');
const mjIncludeBettype = configItem?.json.mjIncludeBettype || [];

// Filter Major unchanged matches (for enrichment)
const matchesToConsider = allItems
  .filter(item => {
    if (item.json.nodeType !== 'mismatch') {
      return false;
    }

    // If mjIncludeBettype is configured, filter by betType
    if (mjIncludeBettype.length > 0) {
      const betType = (item.json.betType || '').toUpperCase();
      return mjIncludeBettype.some(includedBettype => includedBettype.toUpperCase() === betType);
    }

    // If no config, default to MJ only
    return item.json.betType === 'MJ' && item.json.isUnchanged;
  })
  .sort((a, b) => {
    const dateA = new Date(a.json.matchDate);
    const dateB = new Date(b.json.matchDate);
    return dateA - dateB; // ascending order
  });

// Process all items - enrich working shifts, pass through others
const enrichedItems = allItems.map(item => {
  // Only enrich working shifts
  if (item.json.nodeType === 'workingShift') {
    const shift = item.json;
    let assignedJobs = parseAssignedJobs(shift);

    // Find all MJ unchanged matches for this trader that start at or after shift start
    const traderMatches = findTraderMatchesInShift(
      matchesToConsider.map(m => m.json),
      shift.traderName,
      shift
    );

    // Append matched jobs to assignedJobs
    for (const match of traderMatches) {
      const job = createAssignedJob(match, shift.traderName);
      assignedJobs.push(job);
    }

    // Return enriched working shift
    return {
      json: {
        ...shift,
        assignedJobs: assignedJobs,
        nodeType: 'workingShift'
      },
      pairedItem: item.pairedItem
    };
  }

  // Pass through all other items unchanged
  return item;
});

return enrichedItems;
