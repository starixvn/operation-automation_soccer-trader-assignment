// Add nodeType to each working shift item
const items = $input.all();

return items.map(item => ({
  json: {
    ...item.json,
    nodeType: 'workingShift'
  },
  pairedItem: item.pairedItem
}));
