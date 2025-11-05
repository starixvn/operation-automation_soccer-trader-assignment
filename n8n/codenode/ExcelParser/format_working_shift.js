/**
 * Convert Excel date (days since Dec 30, 1899) to JavaScript Date
 */
function excelDateToJSDate(excelDate) {
    const excelEpoch = new Date(Date.UTC(1899, 11, 30));
    const msPerDay = 24 * 60 * 60 * 1000;
    return new Date(excelEpoch.getTime() + excelDate * msPerDay);
}

/**
 * Parse LST Note for early in (E) and late out (L) adjustments
 */
function parseLSTNote(lstNote) {
    if (!lstNote || typeof lstNote !== 'string') {
        return { earlyInHours: 0, lateOutHours: 0 };
    }

    const cleanNote = lstNote.trim().toUpperCase();
    let earlyInHours = 0;
    let lateOutHours = 0;

    // Match E pattern (Early in): E1, E0.5, E2.5, etc.
    const earlyMatch = cleanNote.match(/E(\d+(?:\.\d+)?)/);
    if (earlyMatch) {
        earlyInHours = parseFloat(earlyMatch[1]);
    }

    // Match L pattern (Late out): L1, L0.5, L1.5, etc.
    const lateMatch = cleanNote.match(/L(\d+(?:\.\d+)?)/);
    if (lateMatch) {
        lateOutHours = parseFloat(lateMatch[1]);
    }

    return { earlyInHours, lateOutHours };
}

/**
 * Create a time range from start/end hour and minute
 * @param {Date} baseDate - Base date to use for creating the time range
 * @param {number} startHour - Start hour (0-23)
 * @param {number} startMinute - Start minute (0-59)
 * @param {number} endHour - End hour (0-23)
 * @param {number} endMinute - End minute (0-59)
 * @returns {Object} Object with startTime and endTime Date objects
 */
function createTimeRange(baseDate, startHour, startMinute, endHour, endMinute) {
    // Create startTime using UTC to avoid timezone issues
    const startTime = new Date(Date.UTC(
        baseDate.getUTCFullYear(),
        baseDate.getUTCMonth(),
        baseDate.getUTCDate(),
        startHour,
        startMinute,
        0,
        0
    ));

    // Create endTime
    const endTime = new Date(Date.UTC(
        baseDate.getUTCFullYear(),
        baseDate.getUTCMonth(),
        baseDate.getUTCDate(),
        endHour,
        endMinute,
        0,
        0
    ));

    // If end time is before or equal to start time, add 1 day to endTime (midnight crossing)
    if (endTime <= startTime) {
        endTime.setUTCDate(endTime.getUTCDate() + 1);
    }

    return { startTime, endTime };
}

/**
 * Parse MYT time format and create Date objects for start and end times
 */
function parseMYTTime(mytString, shiftDate, lstNote) {
    if (!mytString || typeof mytString !== 'string') {
        throw new Error(`Invalid MYT format: ${mytString}`);
    }

    const cleanString = mytString.trim();
    const isCrossingShift = cleanString.toUpperCase().includes('(REAL)');
    const timeString = cleanString.replace(/\(REAL\)/i, '').trim();

    const timeMatch = timeString.match(/^(\d{1,2})(?::(\d{2}))?\s*>\s*(\d{1,2})(?::(\d{2}))?$/);

    if (!timeMatch) {
        throw new Error(`Invalid MYT time format: ${mytString}`);
    }

    const startHour = parseInt(timeMatch[1], 10);
    const startMinute = parseInt(timeMatch[2] || '0', 10);
    const endHour = parseInt(timeMatch[3], 10);
    const endMinute = parseInt(timeMatch[4] || '0', 10);

    // Use the extracted createTimeRange function
    const { startTime, endTime } = createTimeRange(shiftDate, startHour, startMinute, endHour, endMinute);

    // Apply LST Note adjustments
    // E (Early in) = both start and end earlier (subtract hours from both)
    // L (Late out) = both start and end later (add hours to both)
    const { earlyInHours, lateOutHours } = parseLSTNote(lstNote);

    if (earlyInHours > 0) {
        // Convert hours to milliseconds for accurate decimal handling
        const msToSubtract = earlyInHours * 60 * 60 * 1000;
        startTime.setTime(startTime.getTime() - msToSubtract);
        endTime.setTime(endTime.getTime() - msToSubtract);
    }

    if (lateOutHours > 0) {
        // Convert hours to milliseconds for accurate decimal handling
        const msToAdd = lateOutHours * 60 * 60 * 1000;
        startTime.setTime(startTime.getTime() + msToAdd);
        endTime.setTime(endTime.getTime() + msToAdd);
    }

    return {
        startTime,
        endTime,
        isCrossingShift
    };
}

/**
 * Parse BUSY column format and create assignedJobs array
 * Format: "start > end, start > end, ..."
 * @param {string} busyString - BUSY times from Excel
 * @param {Date} shiftStartTime - Shift start time
 * @param {Date} shiftEndTime - Shift end time
 */
function parseBusyColumn(busyString, shiftStartTime, shiftEndTime) {
    const assignedJobs = [];

    if (!busyString || typeof busyString !== 'string' || busyString.trim() === '') {
        return assignedJobs;
    }

    // Get the shift start hour to determine which date to use for BUSY times
    const shiftStartHour = shiftStartTime.getUTCHours();

    // Split by comma to get individual time ranges
    const timeRanges = busyString.split(',');

    for (const range of timeRanges) {
        const trimmedRange = range.trim();
        if (!trimmedRange) continue;

        // Match pattern: "start > end" or "start>end"
        const rangeMatch = trimmedRange.match(/^(\d{1,2})(?::(\d{2}))?\s*>\s*(\d{1,2})(?::(\d{2}))?$/);

        if (rangeMatch) {
            const startHour = parseInt(rangeMatch[1], 10);
            const startMinute = parseInt(rangeMatch[2] || '0', 10);
            const endHour = parseInt(rangeMatch[3], 10);
            const endMinute = parseInt(rangeMatch[4] || '0', 10);

            // Determine which date to use based on the BUSY start hour
            // If BUSY hour >= shift start hour, it's on the start date
            // If BUSY hour < shift start hour, it's on the end date (next day portion)
            const baseDate = startHour >= shiftStartHour ? shiftStartTime : shiftEndTime;

            // Use the extracted createTimeRange function
            const { startTime, endTime } = createTimeRange(baseDate, startHour, startMinute, endHour, endMinute);

            assignedJobs.push({
                type: "IC",
                start: startTime.toISOString(),
                end: endTime.toISOString()
            });
        }
    }

    return assignedJobs;
}

// Transform timeline data
const result = [];

for (const item of $input.all()) {
    const shiftDate = excelDateToJSDate(item.json.Date);
    const { startTime, endTime, isCrossingShift } = parseMYTTime(item.json.MYT, shiftDate, item.json['LST Note']);

    // Parse BUSY column to get assignedJobs
    // Pass both start and end times to handle crossing shifts correctly
    const assignedJobs = parseBusyColumn(item.json.Busy, startTime, endTime);

    result.push({
        json: {
            id: item.json.row_number,
            rowNumber: item.json.row_number,
            shiftDate: shiftDate,
            startTime,
            endTime,
            traderName: item.json.Trader,
            isCrossingShift,
            assignedJobs,
            nodeType: 'workingShift'
        }
    });
}

return result;
