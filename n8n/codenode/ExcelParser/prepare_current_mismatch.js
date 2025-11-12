// Add nodeType to each mismatch item
// Filter: only get matches that have a value in the "No" column (not empty)
const items = $input.all();

return items
  .filter(item => item.json.No) // Only keep items where "No" column has a value
  .map(item => ({
    json: {
      ...item.json,
      nodeType: 'mismatch'
    },
    pairedItem: item.pairedItem
  }));
