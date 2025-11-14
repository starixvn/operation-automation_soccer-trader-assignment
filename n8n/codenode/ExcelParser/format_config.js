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

// Filter items where Type = "M1LeagueInclude" (case insensitive)
const m1LeagueIncludeItems = allItems.filter(item => {
  const type = item.json.Type;
  return type && type.toString().toLowerCase() === 'm1leagueinclude';
});

// Filter items where Type = "MJLeagueAlternativeAssignRule" (case insensitive)
const mjLeagueAltRuleItems = allItems.filter(item => {
  const type = item.json.Type;
  return type && type.toString().toLowerCase() === 'mjleaguealternativeassignrule';
});

// Filter items where Type = "MJIncludeBettype" (case insensitive)
const mjIncludeBettypeItems = allItems.filter(item => {
  const type = item.json.Type;
  return type && type.toString().toLowerCase() === 'mjincludebettype';
});

// Filter items where Type = "M1IncludeBettype" (case insensitive)
const m1IncludeBettypeItems = allItems.filter(item => {
  const type = item.json.Type;
  return type && type.toString().toLowerCase() === 'm1includebettype';
});

// Filter items where Type = "M2IncludeBettype" (case insensitive)
const m2IncludeBettypeItems = allItems.filter(item => {
  const type = item.json.Type;
  return type && type.toString().toLowerCase() === 'm2includebettype';
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

// Parse M1LeagueInclude - could be comma-separated string or array
let m1LeagueInclude = [];
if (m1LeagueIncludeItems.length > 0) {
  const value = m1LeagueIncludeItems[0].json.Value;
  if (typeof value === 'string') {
    // Split by comma and trim whitespace
    m1LeagueInclude = value.split(',').map(league => league.trim()).filter(league => league);
  } else if (Array.isArray(value)) {
    m1LeagueInclude = value;
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

// Parse MJIncludeBettype - could be comma-separated string or array
let mjIncludeBettype = [];
if (mjIncludeBettypeItems.length > 0) {
  const value = mjIncludeBettypeItems[0].json.Value;
  if (typeof value === 'string') {
    // Split by comma and trim whitespace
    mjIncludeBettype = value.split(',').map(bettype => bettype.trim()).filter(bettype => bettype);
  } else if (Array.isArray(value)) {
    mjIncludeBettype = value;
  }
}

// Parse M1IncludeBettype - could be comma-separated string or array
let m1IncludeBettype = [];
if (m1IncludeBettypeItems.length > 0) {
  const value = m1IncludeBettypeItems[0].json.Value;
  if (typeof value === 'string') {
    // Split by comma and trim whitespace
    m1IncludeBettype = value.split(',').map(bettype => bettype.trim()).filter(bettype => bettype);
  } else if (Array.isArray(value)) {
    m1IncludeBettype = value;
  }
}

// Parse M2IncludeBettype - could be comma-separated string or array
let m2IncludeBettype = [];
if (m2IncludeBettypeItems.length > 0) {
  const value = m2IncludeBettypeItems[0].json.Value;
  if (typeof value === 'string') {
    // Split by comma and trim whitespace
    m2IncludeBettype = value.split(',').map(bettype => bettype.trim()).filter(bettype => bettype);
  } else if (Array.isArray(value)) {
    m2IncludeBettype = value;
  }
}

// Return config item with nodeType
return [{
  json: {
    currentDate: currentDate,
    mjLeagueInclude: mjLeagueInclude,
    m1LeagueInclude: m1LeagueInclude,
    mjLeagueAlternativeAssignRule: mjLeagueAlternativeAssignRule,
    mjIncludeBettype: mjIncludeBettype,
    m1IncludeBettype: m1IncludeBettype,
    m2IncludeBettype: m2IncludeBettype,
    nodeType: 'config'
  }
}];
