const allItems = $input.all();
const result = [];

for (const item of allItems) {
  const data = item.json;

  // Build league priority object
  const leaguePriority = {};
  if (data['1st']) {
    leaguePriority[1] = [data['1st']];
  }
  if (data['2nd']) {
    leaguePriority[2] = [data['2nd']];
  }
  if (data['3rd']) {
    leaguePriority[3] = [data['3rd']];
  }

  // Primary tier: HDP + OU1 (only if OU1 exists and is not empty)
  if (data.HDP && data.OU1 && data.OU1.trim() !== '') {
    result.push({
      json: {
        hdp: data.HDP,
        ou: data.OU1,
        tier: "primary",
        league_priority: leaguePriority,
        nodeType: "pairTrader"
      }
    });
  }

  // Secondary tier: HDP + OU2 (only if OU2 exists and is not empty)
  if (data.HDP && data.OU2 && data.OU2.trim() !== '') {
    result.push({
      json: {
        hdp: data.HDP,
        ou: data.OU2,
        tier: "secondary",
        league_priority: leaguePriority,
        nodeType: "pairTrader"
      }
    });
  }

  // Backup tier: HDPBK + OUBK (only if both exist and are not empty)
  if (data.HDPBK && data.HDPBK.trim() !== '' && data.OUBK && data.OUBK.trim() !== '') {
    result.push({
      json: {
        hdp: data.HDPBK,
        ou: data.OUBK,
        tier: "backup",
        league_priority: leaguePriority,
        nodeType: "pairTrader"
      }
    });
  }
}

return result;
