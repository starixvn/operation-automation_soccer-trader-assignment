// Match League Names to League Codes
// This node matches each match league name to its corresponding league code

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
const leagueCodes = Array.from(leagueCodesSet).sort();

// Build the system prompt for LEAGUE DETECTION
const systemPrompt = `You are a league code matching system.

PURPOSE:
- Match each match league name to its corresponding league code
- Compare match league names against provided league codes
- Use AI intelligence to determine semantic matches

================================================================================
1. DETECTION RULES
================================================================================

Match league names to codes:
- For each match, compare match.league against LEAGUE_CODES
- Use semantic understanding to determine matches:
  * "ITALY SERIE A" or "*ITALY SERIE A" should match "ITA"
  * "SPAIN PRIMERA LALIGA" or "*SPAIN PRIMERA LALIGA" should match "SPA"
  * "ENGLISH PREMIER LEAGUE" or "*ENGLISH PREMIER LEAGUE" should match "EPL"
  * "GERMANY-BUNDESLIGA I" or "*GERMANY-BUNDESLIGA I" should match "GER"
- If no semantic match found → matchedLeagueCode = null

================================================================================
2. OUTPUT FORMAT
================================================================================

Return ONLY valid JSON (no markdown):
{
  "matchLeagueDetections": [
    {
      "matchNo": "<string>",
      "leagueName": "<string>",
      "leagueCode": "<code from LEAGUE_CODES or null>"
    }
  ]
}

RULES:
- Include ALL matches
- Match league name to a league code semantically
- Use semantic matching (not just substring)
- If no match found, leagueCode should be null`;

// Build the user prompt
const userPrompt = `INPUT DATA:

MATCHES (${matches.length} total):
${JSON.stringify(matches, null, 2)}

LEAGUE_CODES (${leagueCodes.length} distinct codes extracted from pair configurations):
${JSON.stringify(leagueCodes, null, 2)}

PAIR_CONFIGURATIONS (${pairConfig.length} pairs with priorities):
${JSON.stringify(pairConfig, null, 2)}

TASK:
For each match, match the match league name to one of the LEAGUE_CODES semantically. Return JSON with matchLeagueDetections array.`;

// Output the prompts
return [{
  json: {
    systemPrompt: systemPrompt,
    userPrompt: userPrompt,
    matchCount: matches.length,
    leagueCodesCount: leagueCodes.length,
    pairCount: pairConfig.length,
    matches: matches,
    leagueCodes: leagueCodes,
    pairConfigurations: pairConfig
  }
}];
