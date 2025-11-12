// Extract config values from Type-Value pairs
const allItems = $input.all();

// Filter items where Type = "CurrentDate" (case insensitive)
const currentDateItems = allItems.filter(item => {
  const type = item.json.Type;
  return type && type.toString().toLowerCase() === 'currentdate';
});

// Filter items where Type = "MJLeagueInclude" (case insensitive)
const mjLeagueIncludeItems = allItems.filter(item => {
  const type = item.json.Type;
  return type && type.toString().toLowerCase() === 'mjleagueinclude';
});

// Filter items where Type = "MJLeagueAlternativeAssignRule" (case insensitive)
const mjLeagueAltRuleItems = allItems.filter(item => {
  const type = item.json.Type;
  return type && type.toString().toLowerCase() === 'mjleaguealternativeassignrule';
});

// Take first item (Limit behavior)
if (currentDateItems.length === 0) {
  return [];
}

const currentDate = currentDateItems[0].json.Value;

// Parse MJLeagueInclude - could be comma-separated string or array
let mjLeagueInclude = [];
if (mjLeagueIncludeItems.length > 0) {
  const value = mjLeagueIncludeItems[0].json.Value;
  if (typeof value === 'string') {
    // Split by comma and trim whitespace
    mjLeagueInclude = value.split(',').map(league => league.trim()).filter(league => league);
  } else if (Array.isArray(value)) {
    mjLeagueInclude = value;
  }
}

// Parse MJLeagueAlternativeAssignRule - could be comma-separated string or array
let mjLeagueAlternativeAssignRule = [];
if (mjLeagueAltRuleItems.length > 0) {
  const value = mjLeagueAltRuleItems[0].json.Value;
  if (typeof value === 'string') {
    // Split by comma and trim whitespace
    mjLeagueAlternativeAssignRule = value.split(',').map(league => league.trim()).filter(league => league);
  } else if (Array.isArray(value)) {
    mjLeagueAlternativeAssignRule = value;
  }
}

// Return config item with nodeType
return [{
  json: {
    currentDate: currentDate,
    mjLeagueInclude: mjLeagueInclude,
    mjLeagueAlternativeAssignRule: mjLeagueAlternativeAssignRule,
    nodeType: 'config'
  }
}];
