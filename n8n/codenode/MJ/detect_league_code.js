// Code-based League Code Detection
// Replaces AI-based detection with a mapping approach

const input = $input.all();

// Extract matches and pair configurations from input
const matches = [];
const pairConfig = [];

for (const item of input) {
  const data = item.json;

  // Check if this is a Major match (betType = MJ)
  if (data.nodeType === 'mismatch' && data.betType === 'MJ') {
    matches.push({
      rowNumber: data.rowNumber,
      matchNo: data.no,
      league: data.league,
      start: data.matchDate,
      end: data.endDate
    });
  }

  // Check if this is pair trader config
  else if (data.nodeType === 'pairTrader') {
    const leaguePriority = {
      '1st': null,
      '2nd': null,
      '3rd': null
    };

    // Extract league priorities from league_priority object
    if (data.league_priority) {
      if (data.league_priority[1] && Array.isArray(data.league_priority[1]) && data.league_priority[1].length > 0) {
        leaguePriority['1st'] = data.league_priority[1][0];
      }
      if (data.league_priority[2] && Array.isArray(data.league_priority[2]) && data.league_priority[2].length > 0) {
        leaguePriority['2nd'] = data.league_priority[2][0];
      }
      if (data.league_priority[3] && Array.isArray(data.league_priority[3]) && data.league_priority[3].length > 0) {
        leaguePriority['3rd'] = data.league_priority[3][0];
      }
    }

    pairConfig.push({
      hdp: data.hdp,
      ou: data.ou,
      tier: data.tier,
      '1st': leaguePriority['1st'],
      '2nd': leaguePriority['2nd'],
      '3rd': leaguePriority['3rd']
    });
  }
}

// Extract all unique league codes from pair configurations
const leagueCodesSet = new Set();

for (const config of pairConfig) {
  if (config['1st']) leagueCodesSet.add(config['1st']);
  if (config['2nd']) leagueCodesSet.add(config['2nd']);
  if (config['3rd']) leagueCodesSet.add(config['3rd']);
}

// Convert to sorted array
const availableLeagueCodes = Array.from(leagueCodesSet).sort();

/**
 * League name to code mapping
 * Maps common league name patterns to their standard codes
 * Only supports top 5 major European leagues
 */
const LEAGUE_MAPPINGS = {
  // English Premier League
  'EPL': [
    'ENGLISH PREMIER LEAGUE',
    'EPL',
    'PREMIER LEAGUE',
    'ENGLAND PREMIER LEAGUE',
    '*ENGLISH PREMIER LEAGUE'
  ],

  // Spanish La Liga
  'SPA': [
    'SPAIN PRIMERA LALIGA',
    'LA LIGA',
    'LALIGA',
    'SPAIN LA LIGA',
    'SPANISH PRIMERA',
    '*SPAIN PRIMERA LALIGA',
    'SPAIN PRIMERA DIVISION'
  ],

  // Italian Serie A
  'ITA': [
    'ITALY SERIE A',
    'SERIE A',
    'ITALIAN SERIE A',
    '*ITALY SERIE A',
    'ITALY SERIE-A'
  ],

  // German Bundesliga
  'GER': [
    'GERMANY-BUNDESLIGA I',
    'BUNDESLIGA',
    'GERMANY BUNDESLIGA',
    'GERMAN BUNDESLIGA',
    '*GERMANY-BUNDESLIGA I',
    'GERMANY BUNDESLIGA 1'
  ],

  // French Ligue 1
  'FRA': [
    'FRANCE LIGUE 1',
    'LIGUE 1',
    'FRENCH LIGUE 1',
    '*FRANCE LIGUE 1',
    'FRANCE LIGUE-1'
  ]
};

/**
 * Normalize league name for comparison
 * Removes special characters, extra spaces, and converts to uppercase
 */
function normalizeLeagueName(name) {
  if (!name) return '';

  return name
    .toUpperCase()
    .replace(/[^\w\s-]/g, ' ')  // Replace special chars with space
    .replace(/\s+/g, ' ')        // Collapse multiple spaces
    .trim();
}

/**
 * Match league name to league code
 * Returns the matching code or null if no match found
 */
function matchLeagueCode(leagueName, availableCodes) {
  if (!leagueName) return null;

  const normalized = normalizeLeagueName(leagueName);

  // First, try exact match with available codes (for efficiency)
  for (const code of availableCodes) {
    const patterns = LEAGUE_MAPPINGS[code];
    if (patterns) {
      for (const pattern of patterns) {
        const normalizedPattern = normalizeLeagueName(pattern);

        // Check for exact match
        if (normalized === normalizedPattern) {
          return code;
        }

        // Check if league name contains the pattern (for cases like "*ITALY SERIE A" in data)
        if (normalized.includes(normalizedPattern) || normalizedPattern.includes(normalized)) {
          return code;
        }
      }
    }
  }

  // Second pass: Try all mappings even if not in available codes
  // This helps catch leagues that should be mapped but aren't in pair config yet
  for (const [code, patterns] of Object.entries(LEAGUE_MAPPINGS)) {
    for (const pattern of patterns) {
      const normalizedPattern = normalizeLeagueName(pattern);

      if (normalized === normalizedPattern ||
          normalized.includes(normalizedPattern) ||
          normalizedPattern.includes(normalized)) {
        return code;
      }
    }
  }

  // No match found
  return null;
}

/**
 * Detect league codes for all matches
 */
const matchLeagueDetections = matches.map(match => {
  const leagueCode = matchLeagueCode(match.league, availableLeagueCodes);

  return {
    matchNo: match.matchNo,
    leagueName: match.league,
    leagueCode: leagueCode
  };
});

// Return result in the same format as AI node output
return [{
  json: {
    matchLeagueDetections: matchLeagueDetections
  }
}];
