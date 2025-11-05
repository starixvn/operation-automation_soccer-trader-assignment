// Add nodeType to each mismatch item
const items = $input.all();

return items.map(item => ({
  json: {
    ...item.json,
    nodeType: 'mismatch'
  },
  pairedItem: item.pairedItem
}));
