// Filter items from 10:00 AM to 12:00 PM (not including 12:00 PM)
function parseTime(timeString) {
  const cleanTime = timeString.trim().replace(/\s+/g, ' ');
  const timeMatch = cleanTime.match(/^(\d{1,2}):(\d{2})\s*(AM|PM)$/i);

  if (!timeMatch) return null;

  let hours = parseInt(timeMatch[1], 10);
  const minutes = parseInt(timeMatch[2], 10);
  const period = timeMatch[3].toUpperCase();

  // Convert to 24-hour format for comparison
  if (period === 'PM' && hours !== 12) {
    hours += 12;
  } else if (period === 'AM' && hours === 12) {
    hours = 0;
  }

  return hours * 60 + minutes; // Return total minutes from midnight
}

const filtered = [];

for (const item of $input.all()) {
  const timeString = item.json.Time;
  const timeInMinutes = parseTime(timeString);

  // 10:00 AM = 600 minutes, 12:00 PM = 720 minutes
  if (timeInMinutes !== null && timeInMinutes >= 600 && timeInMinutes < 720) {
    filtered.push({
      json: {
        ...item.json,
        nodeType: 'previousMismatch'
      },
      pairedItem: item.pairedItem
    });
  }
}

return filtered;
