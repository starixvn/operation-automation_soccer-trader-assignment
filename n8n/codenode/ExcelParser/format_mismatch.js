function convertTimeToDate(currentDate, timeString) {
  // Clean the time string - remove extra spaces
  const cleanTime = timeString.trim().replace(/\s+/g, ' ');

  // Parse the time string
  const timeMatch = cleanTime.match(/^(\d{1,2}):(\d{2})\s*(AM|PM)$/i);

  if (!timeMatch) {
    throw new Error(`Invalid time format: ${timeString}. Expected format: "12:00PM", "12:00 PM", "01:00 AM", "01:00AM"`);
  }

  let hours = parseInt(timeMatch[1], 10);
  const minutes = parseInt(timeMatch[2], 10);
  const period = timeMatch[3].toUpperCase();

  // Validate hours and minutes
  if (hours < 1 || hours > 12) {
    throw new Error(`Invalid hours: ${hours}. Hours must be between 1 and 12`);
  }

  if (minutes < 0 || minutes > 59) {
    throw new Error(`Invalid minutes: ${minutes}. Minutes must be between 0 and 59`);
  }

  // Create a new date object based on currentDate
  const resultDate = new Date(currentDate);

  // Handle AM/PM logic
  if (period === 'PM') {
    // PM: Use currentDate with the specified time
    if (hours === 12) {
      // 12:xx PM = 12:xx (noon)
      resultDate.setUTCHours(12, minutes, 0, 0);
    } else {
      // 1:xx PM to 11:xx PM = 13:xx to 23:xx
      resultDate.setUTCHours(hours + 12, minutes, 0, 0);
    }
  } else { // AM
    // AM: Add one day to currentDate with the specified time
    resultDate.setUTCDate(resultDate.getUTCDate() + 1);

    if (hours === 12) {
      // 12:xx AM = 00:xx (midnight)
      resultDate.setUTCHours(0, minutes, 0, 0);
    } else {
      // 1:xx AM to 11:xx AM = 01:xx to 11:xx
      resultDate.setUTCHours(hours, minutes, 0, 0);
    }
  }

  return resultDate;
}

function convertMaxBet(valueString) {
  if (!valueString || typeof valueString !== 'string') {
    return 0;
  }
  // Clean the string - remove extra spaces
  const cleanString = valueString.trim();

  // Find number followed by 'k' or 'K' anywhere in the string
  // Examples: "30K", "123Kxxx", "abc 30Kxxx"
  const kMatch = cleanString.match(/(\d+(?:\.\d+)?)\s*[kK]/i);

  if (!kMatch) {
    return 0;
  }

  // Extract the numeric part
  const numericValue = parseFloat(kMatch[1]);

  // Validate the numeric value
  if (isNaN(numericValue)) {
    return 0;
  }

  // Convert to number by multiplying by 1000
  return numericValue * 1000;
}

function detectBetType(match, mjLeagueInclude = []) {
  const league = (match.League || '').toUpperCase();
  const no = (match.No || '').toString().toUpperCase();
  const maxBet = convertMaxBet(match.Maxbet);
  const lGroup = (match['L.Group'] || '').toUpperCase().trim();

  // 1. Check special bet types by No suffix (order matters!)
  if (no.endsWith('TB1')) return 'TB1';  // Next Booking
  if (no.endsWith('TB')) return 'TB';    // Booking
  if (no.endsWith('C1')) return 'C1';    // Next Corner
  if (no.endsWith('C')) return 'CR';     // Corner

  // 2. Check for child product format: numbers followed by letters, optionally followed by numbers
  // Examples: 12B, 13FS, 12B1, 13FS2 - This indicates OtherSport category
  const childProductPattern = /^\d+[A-Z]+\d*$/;
  if (childProductPattern.test(no)) return 'CP'; //child product

  // 3. Check by League name
  if (league.includes('FUTSAL')) return 'FS';

  // 4. Check Virtual PES games
  if (league.includes('SABA CLUB FRIENDLY') && league.includes('PES 21')) return 'PESCF';
  if (league.includes('SABA INTERNATIONAL FRIENDLY') && league.includes('PES 21')) return 'PESIF';
  if (league.includes('SABA VIETNAM V LEAGUE 1') && league.includes('PES 24')) return 'PES24';
  if (league.includes('SABA ELITE FRIENDLY') && league.includes('PES 21')) return 'PES5MIN';
  if (league.includes('PENALTY SHOOTOUTS') && league.includes('PES 23')) {
    if (league.includes('INTERNATIONAL')) return 'PKIF';
    if (league.includes('CLUB')) return 'PKCF';
  }

  // 5. Check MJLeagueInclude configuration - leagues explicitly marked as MJ (HIGHEST PRIORITY)
  if (mjLeagueInclude && mjLeagueInclude.length > 0) {
    const isInMJIncludeList = mjLeagueInclude.some(includedLeague => {
      const normalizedIncluded = includedLeague.toUpperCase().trim();
      return league.includes(normalizedIncluded) || normalizedIncluded.includes(league);
    });
    if (isInMJIncludeList) {
      return 'MJ';
    }
  }

  // 6. Priority: L.Group column for MJ/M1/M2/M3/M4 league groups
  if (lGroup) {
    const validGroups = ['MJ', 'M1', 'M2', 'M3', 'M4'];
    if (validGroups.includes(lGroup)) {
      return lGroup;
    }
  }

  // 7. Fall back to MaxBet-based classification
  if (maxBet >= 70000) return 'MJ';   // Major
  if (maxBet >= 40000 && maxBet < 70000) return 'M1';    // Minor 1
  if (maxBet >= 20000 && maxBet < 40000) return 'M2';    // Minor 2
  if (maxBet >= 8000 && maxBet < 20000) return 'M3';     // Minor 3
  if (maxBet >= 1500 && maxBet < 8000) return 'M4';      // Minor 4

  return 'Unknown';
}

function isRealGame(betType) {
  const virtualBetTypes = ['PESCF', 'PESIF', 'PES24', 'PES5MIN', 'PKIF', 'PKCF'];
  return !virtualBetTypes.includes(betType);
}

/**
 * Calculate end time for a real match
 * Rules:
 * - Both ET and PK: 3 hours
 * - Only PK: 2.5 hours
 * - Only ET: 3 hours
 * - Neither: 2 hours
 */
function calculateRealMatchEndTime(matchDate, etpkValue) {
  const endDate = new Date(matchDate);

  if (!etpkValue) {
    // No ET/PK: 2 hours
    endDate.setUTCHours(endDate.getUTCHours() + 2);
  } else if (etpkValue.includes("ET") && etpkValue.includes("PK")) {
    // Both ET and PK: 3 hours
    endDate.setUTCHours(endDate.getUTCHours() + 3);
  } else if (etpkValue.includes("PK")) {
    // Only PK: 2.5 hours (2 hours 30 minutes)
    endDate.setUTCHours(endDate.getUTCHours() + 2);
    endDate.setUTCMinutes(endDate.getUTCMinutes() + 30);
  } else if (etpkValue.includes("ET")) {
    // Only ET: 3 hours
    endDate.setUTCHours(endDate.getUTCHours() + 3);
  } else {
    // Default: 2 hours
    endDate.setUTCHours(endDate.getUTCHours() + 2);
  }

  return endDate;
}

/**
 * Calculate end time for a virtual match based on bet type
 */
function calculateVirtualMatchEndTime(matchDate, betType) {
  const endDate = new Date(matchDate);

  const durationMap = {
    'PES5MIN': 15,
    'PESCF': 20,
    'PESIF': 20,
    'PKIF': 5,
    'PKCF': 5,
    'PES24': 15
  };

  const minutesToAdd = durationMap[betType] || 15; // default 15 minutes
  endDate.setUTCMinutes(endDate.getUTCMinutes() + minutesToAdd);
  return endDate;
}

/**
 * Calculate match end date
 */
function calculateMatchEndDate(matchDate, isReal, etpkValue, betType) {
  return isReal
    ? calculateRealMatchEndTime(matchDate, etpkValue)
    : calculateVirtualMatchEndTime(matchDate, betType);
}

/**
 * Check if a match is a solo match
 * A match is solo if there's no other match in the same league within 45 minutes from its start time
 *
 * Rules:
 * - Matches at the exact same time in same league are NOT solo
 * - Matches within 45 minutes in same league are NOT solo
 * - Matches > 45 minutes apart or in different leagues are solo
 *
 * @param {Date} matchDate - The start time of the match
 * @param {string} league - The league name
 * @param {number} currentIndex - The index of the current match in allMatches
 * @param {Array} allMatches - All matches to compare against
 * @returns {boolean} - True if the match is solo
 */
function isSoloMatch(matchDate, league, currentIndex, allMatches) {
  if (!league) return true; // If no league, consider it solo

  const matchTime = matchDate.getTime();
  const fortyFiveMinutes = 45 * 60 * 1000; // 45 minutes in milliseconds

  // Check if any other match in the same league starts within 45 minutes (including same time)
  const hasNearbyMatch = allMatches.some((otherMatch, idx) => {
    // Skip self (same array index)
    if (idx === currentIndex) return false;

    // Skip if different league
    if (otherMatch.league !== league) return false;

    // Check if other match starts within 45 minutes (or at the same time)
    const timeDiff = Math.abs(otherMatch.matchDate.getTime() - matchTime);
    return timeDiff <= fortyFiveMinutes;
  });

  return !hasNearbyMatch;
}

const allItems = $input.all();

// Find config item with nodeType
const currentDateItem = allItems.find(_ => _.json.nodeType === 'config');
// Find mismatch items with nodeType
const mismatchItems = allItems.filter(_ => _.json.nodeType === 'mismatch');
// Find previous mismatch items with nodeType
const previousMismatchItems = allItems.filter(_ => _.json.nodeType === 'previousMismatch');

const currentDate = currentDateItem.json.currentDate;
const mjLeagueInclude = currentDateItem.json.mjLeagueInclude || [];
const mjLeagueAlternativeAssignRule = currentDateItem.json.mjLeagueAlternativeAssignRule || [];
const tempMatches = [];

/**
 * Check if a league matches the alternative MJ rule list
 */
function isLeagueInAlternativeRule(league, altRuleList) {
  if (!league || !altRuleList || altRuleList.length === 0) return false;

  const normalizedLeague = league.toUpperCase().trim();
  return altRuleList.some(altLeague => {
    const normalizedAlt = altLeague.toUpperCase().trim();
    return normalizedLeague.includes(normalizedAlt) || normalizedAlt.includes(normalizedLeague);
  });
}

// First pass: Process current mismatch items and collect basic match data
for (const item of mismatchItems) {
  const value = item.json;
  const betType = detectBetType(value, mjLeagueInclude);
  const etpkValue = value.ET_PK || "";
  const isETPK = !!value.ET_PK && (value.ET_PK.includes("ET") || value.ET_PK.includes("PK"));
  const isReal = isRealGame(betType);
  const matchDate = convertTimeToDate(currentDate, value.Time);
  const endDate = calculateMatchEndDate(matchDate, isReal, etpkValue, betType);
  const isUseAlternativeMJRule = isLeagueInAlternativeRule(value.League, mjLeagueAlternativeAssignRule);

  tempMatches.push({
    matchDate: matchDate,
    endDate: endDate,
    league: value.League,
    isETPK: isETPK,
    maxBet: convertMaxBet(value.Maxbet),
    provider: value.Provider,
    rowNumber: value.row_number,
    no: value.No,
    isEAL: !!value["Link EAL"] && value["Link EAL"].toUpperCase().includes("EAL"),
    isEXPT: !(value["Link EAL"] && value["Link EAL"].toUpperCase() === "NO EXPT"),
    betType: betType,
    isReal: isReal,
    isUseAlternativeMJRule: isUseAlternativeMJRule,
    hdpTrader: value.HDP || null,
    ouTrader: value.OU || null,
    isUnchanged: !!value.Unchanged && value.Unchanged.toLowerCase() === "x",
    isPreviousMatch: false
  });
}

// Process previous mismatch items
for (const item of previousMismatchItems) {
  const value = item.json;
  const matchDate = convertTimeToDate(currentDate, value.Time);
  // Subtract 1 day from matchDate
  matchDate.setUTCDate(matchDate.getUTCDate() - 1);

  const betType = detectBetType(value, mjLeagueInclude);
  const etpkValue = value.ET_PK || "";
  const isETPK = !!value.ET_PK && (value.ET_PK.includes("ET") || value.ET_PK.includes("PK"));
  const isReal = isRealGame(betType);
  const endDate = calculateMatchEndDate(matchDate, isReal, etpkValue, betType);
  const isUseAlternativeMJRule = isLeagueInAlternativeRule(value.League, mjLeagueAlternativeAssignRule);

  tempMatches.push({
    matchDate: matchDate,
    endDate: endDate,
    league: value.League,
    isETPK: isETPK,
    maxBet: convertMaxBet(value.Maxbet),
    provider: value.Provider,
    rowNumber: value.row_number,
    no: value.No,
    isEAL: !!value["Link EAL"] && value["Link EAL"].toUpperCase().includes("EAL"),
    isEXPT: !(value["Link EAL"] && value["Link EAL"].toUpperCase() === "NO EXPT"),
    betType: betType,
    isReal: isReal,
    isUseAlternativeMJRule: isUseAlternativeMJRule,
    hdpTrader: value.HDP,
    ouTrader: value.OU,
    isUnchanged: !!value.Unchanged && value.Unchanged.toLowerCase() === "x",
    isPreviousMatch: true
  });
}

// Second pass: Calculate isSoloMatch for each match and build final result
const result = [];
for (let i = 0; i < tempMatches.length; i++) {
  const match = tempMatches[i];
  const soloMatch = isSoloMatch(match.matchDate, match.league, i, tempMatches);

  result.push({
    json: {
      ...match,
      isSoloMatch: soloMatch,
      isUseAlternativeMJRule: match.isUseAlternativeMJRule,
      nodeType: 'mismatch'
    }
  });
}

return result;
