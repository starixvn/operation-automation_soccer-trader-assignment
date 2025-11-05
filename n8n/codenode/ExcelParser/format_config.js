// Filter for CurrentDate type, take first, extract value
const allItems = $input.all();

// Filter items where Type = "CurrentDate" (case insensitive)
const currentDateItems = allItems.filter(item => {
  const type = item.json.Type;
  return type && type.toString().toLowerCase() === 'currentdate';
});

// Take first item (Limit behavior)
if (currentDateItems.length === 0) {
  return [];
}

const firstItem = currentDateItems[0];

// Extract currentDate from Value field
const currentDate = firstItem.json.Value;

// Return config item with nodeType
return [{
  json: {
    currentDate: currentDate,
    nodeType: 'config'
  }
}];
