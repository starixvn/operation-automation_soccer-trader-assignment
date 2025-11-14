# Soccer LST Excel Sheets User Manual

This manual provides step-by-step instructions on how to use each sheet in the Soccer LST Google Sheets document for trader assignment.

---

## Table of Contents

1. [TimeTable Sheet](#1-timetable-sheet) - Managing trader working shifts
2. [Grade Sheet](#2-grade-sheet) - Setting up trader profiles and capabilities
3. [Mismatch Sheet](#3-mismatch-sheet) - Inputting matches to be assigned
4. [Config Sheet](#4-config-sheet) - System configuration
5. [Pair Trader Sheet](#5-pair-trader-sheet) - Configuring preferred trader pairs

---

## 1. TimeTable Sheet

### Purpose
Record when each trader is available to work and any breaks/busy periods during their shift.

### Sheet Configuration
- **Sheet Name:** TimeTable
- **Header Row:** Row 1
- **Data Starts:** Row 2
- **Required Columns:** Date, Trader, MYT
- **Optional Columns:** LST Note, Busy

### Columns

#### Date
**What to enter:** The date of the working shift
**Format:** Any recognizable date format (e.g., "2024-08-16", "16/08/2024", "Aug 16, 2024")
**Example:** `2024-08-16`

#### Trader
**What to enter:** The trader's name (must match exactly with the name in Grade sheet)
**Format:** Text
**Example:** `CLGKelven`, `VNMHung`, `THAPatrick`
**Important:** Name must be consistent across all sheets

#### MYT
**What to enter:** The shift time range in Malaysian Time (MYT)
**Format:** `H>H` or `HH:MM>HH:MM` (using `>` as separator, not `-`)
**Examples:**
- `1>9` (1 AM to 9 AM)
- `01:00>09:00` (1 AM to 9 AM with minutes)
- `13>21` (1 PM to 9 PM)
- `13:30>21:30` (1:30 PM to 9:30 PM)

**Special notation:**
- Add `(REAL)` for crossing shifts (shifts that span across two calendar days)
- Example: `22>6 (REAL)` - shift from 10 PM today to 6 AM tomorrow
- Without `(REAL)`, the system treats it as same-day (22 > 6 would mean error or wrap around)

**Time format rules:**
- Hours can be 1 or 2 digits (1-12, 13-23)
- Minutes are optional (default to :00 if not specified)
- Always use `>` symbol between start and end times

#### LST Note
**What to enter:** Early in (E) or Late out (L) time adjustments (optional)
**Format:** `E<hours>` for earlier start, `L<hours>` for later end
**Examples:**
- `E1` - Start 1 hour EARLIER (shift entire shift 1 hour earlier)
- `E0.5` - Start 30 minutes earlier (shift entire shift 30 min earlier)
- `L1` - End 1 hour LATER (shift entire shift 1 hour later)
- `L1.5` - End 1.5 hours later (shift entire shift 1.5 hours later)
- `E1 L0.5` - Both: 1 hour earlier AND 0.5 hour later (combined)
- Leave empty if no adjustment needed

**How it works:**
- **E (Early in)**: Subtracts hours from BOTH start and end times
  - MYT `1>9` + LST Note `E1` → Actual shift: `00:00>08:00`
- **L (Late out)**: Adds hours to BOTH start and end times
  - MYT `1>9` + LST Note `L1` → Actual shift: `02:00>10:00`
- **Combined**: Can use both E and L together
  - MYT `1>9` + LST Note `E1 L0.5` → First E1 (`00:00>08:00`), then L0.5 (`00:30>08:30`)

#### Busy
**What to enter:** Time ranges when trader is busy/unavailable during their shift (optional)
**Format:** `H>H` or `HH:MM>HH:MM`, multiple ranges separated by commas
**Examples:**
- `3>4` (busy from 3 AM to 4 AM)
- `03:00>04:00` (busy from 3:00 AM to 4:00 AM)
- `2>3, 5>6` (busy for two separate periods)
- `14:00>15:30, 17:00>17:30` (multiple busy times)
- Leave empty if trader is available for entire shift

**Use cases:**
- Lunch breaks: `12>13`
- Training sessions: `14:00>15:30`
- Meetings: `10:00>10:30`

**Important:** Busy times use the SAME format as MYT (with `>` separator)

### Example Rows

#### Example 1: Basic shift with busy period
| Date | Trader | MYT | LST Note | Busy |
|------|--------|-----|----------|------|
| 2024-08-16 | CLGKelven | 1>9 | L1 | 3>4 |

**Interpretation:**
- Base shift: 1 AM to 9 AM
- LST Note L1: Shift 1 hour later → 2:00 AM to 10:00 AM
- Busy: 3 AM to 4 AM
- CLGKelven works 2 AM to 10 AM but is busy from 3 AM to 4 AM

#### Example 2: Crossing shift (spans two calendar days)
| Date | Trader | MYT | LST Note | Busy |
|------|--------|-----|----------|------|
| 2024-08-16 | VNMHung | 22>6 (REAL) | E1 | 1>2 |

**Interpretation:**
- Base shift: 10 PM (Aug 16) to 6 AM (Aug 17) - crossing shift
- LST Note E1: Shift 1 hour earlier → 9:00 PM to 5:00 AM
- Busy: 1 AM to 2 AM
- VNMHung works 9 PM to 5 AM (spanning two days), busy 1-2 AM

#### Example 3: Shift with minutes and multiple busy periods
| Date | Trader | MYT | LST Note | Busy |
|------|--------|-----|----------|------|
| 2024-08-16 | THAPatrick | 13:30>21:30 | | 15>16, 18:30>19:00 |

**Interpretation:**
- Shift: 1:30 PM to 9:30 PM (no LST adjustment)
- Busy: 3 PM to 4 PM, and 6:30 PM to 7:00 PM
- THAPatrick works 1:30 PM to 9:30 PM with two busy periods

### Tips
- ✅ Always use `>` symbol (greater than) as the time separator, NOT `-` (dash)
- ✅ Use 24-hour format for times (0-23 hours)
- ✅ Make sure trader names match exactly with Grade sheet
- ✅ Add `(REAL)` notation for crossing shifts (shifts spanning two calendar days)
- ✅ LST Note E = Earlier (subtract from both times), L = Later (add to both times)
- ✅ Busy periods use the same `>` format as MYT
- ⚠️ Empty rows are ignored automatically
- ⚠️ Minutes are optional - `1>9` is the same as `01:00>09:00`

---

## 2. Grade Sheet

### Purpose
Define each trader's profile, skills, capacity, and restrictions.

### Sheet Configuration
- **Sheet Name:** Grade
- **Header Row:** Row 1
- **Data Starts:** Row 2
- **Required Columns:** NAME, LEVEL, MAIN BET TYPE
- **Optional Columns:** GROUP, Ignore League, Ignore League Type, Non-Solo league, Priority skill, Priority league, MaxBet

### Columns

#### NAME
**What to enter:** Trader's full name
**Example:** `CLGKelven`, `VNMHung`, `THAPatrick`, `IDNJoko`
**Important:** This name will be used across all sheets - be consistent!

#### GROUP
**What to enter:** Team or department identifier (optional)
**Format:** Text
**Example:** `SLG`, `CLG`

#### LEVEL
**What to enter:** Trader's skill level and management status

**Format:** `[Management Level]/Staff Level`

**Staff Levels (actual skill level):**
- `Elite` - Highest skill level (mapped internally as "elite")
- `Member` - Mid-level trader (mapped internally as "member")
- `(1st)`, `(2nd)`, `(3rd)`, `(4th)` - First/Second/Third/Fourth tier (mapped as "first", "second", "third", "fourth")
- `OP(1st)`, `OP(2nd)` - Operational first/second tier (mapped as "first", "second")

**Management Levels (optional prefix):**
- `[PIC]/` - People In Charge (manager/supervisor)
- If no `[PIC]/` prefix, trader is not a manager

**Complete Examples:**
- `Elite` → Staff Level: Elite, Not a manager
- `[PIC]/Elite` → Staff Level: Elite, IS a manager
- `Member` → Staff Level: Member, Not a manager
- `[PIC]/Member` → Staff Level: Member, IS a manager
- `(1st)` → Staff Level: First tier, Not a manager
- `[PIC]/(1st)` → Staff Level: First tier, IS a manager
- `OP(1st)` → Staff Level: First tier (operational), Not a manager

#### MAIN BET TYPE
**What to enter:** Which bet types trader can handle and their capacity

**Format:** `[BetType]/[M1Capacity]/[M2Capacity]/[Flags]`

**Basic Bet Types:**
- `HDP` - Handicap only
- `OU` - Over/Under only
- `2` - Both HDP and OU (versatile trader)

**Capacity Modifiers:**
- `M1*X` - Can handle X M1 matches simultaneously
- `M2*X` - Can handle X M2 matches simultaneously
- **Default if not specified:** M1*1, M2*2

**Special Flags:**
- `[No Expt]` - Cannot be assigned to EXPT matches (case-insensitive)

**Format Examples:**
- `HDP/M1*2` - Only HDP, can handle 2 M1 matches
- `OU/M2*3` - Only OU, can handle 3 M2 matches
- `2/M1*2/M2*1` - Both HDP and OU, can handle 2 M1 and 1 M2
- `HDP/M1*1[No Expt]` - Only HDP, 1 M1 match, cannot do EXPT
- `2` - Both HDP and OU, default capacity (M1*1, M2*2)
- `HDP` - Only HDP, default capacity (M1*1, M2*2)
- `2/M1*3` - Both HDP and OU, 3 M1 matches, default M2 (2)

**How the system parses this:**
1. Extracts M1 capacity: Finds `M1*X` pattern → If not found, defaults to 1
2. Extracts M2 capacity: Finds `M2*X` pattern → If not found, defaults to 2
3. Checks for `[No Expt]` flag anywhere in the string (case-insensitive)
4. Remaining text indicates bet type (HDP, OU, or 2)

#### Ignore League
**What to enter:** Specific leagues this trader should NOT be assigned to
**Format:** Comma-separated league names
**Examples:**
- `JAPAN J-LEAGUE DIVISION 1, AUSTRALIA A-LEAGUE` - Multiple leagues
- Leave empty if no restrictions

#### Ignore League Type
**What to enter:** Which match types (M2, M3, M4) this trader should NOT be assigned to
**Format:** Comma-separated match types
**Examples:**
- `M2` - Don't assign to M2 matches
- Leave empty if trader can handle all match types

#### Non-Solo league
**What to enter:** Leagues where this trader CANNOT work solo matches
**Format:** Comma-separated league names
**Examples:**
- `JAPAN J-LEAGUE DIVISION 1, AUSTRALIA A-LEAGUE` - Cannot work these leagues alone
- Leave empty if trader can work all leagues solo

#### Priority skill
**What to enter:** Trader's specialized skill (HDP or OU)
**Format:** `HDP` or `OU`
**Examples:**
- `HDP` - Trader is better at Handicap
- `OU` - Trader is better at Over/Under
- Leave empty if no specialization

**Effect:** Trader gets priority bonus when assigned to their specialty role

#### Priority league
**What to enter:** Leagues where this trader performs best
**Format:** Comma-separated league names
**Examples:**
- `England Premier League` - Best at English Premier League
- `England Premier League, Spain La Liga` - Best at England Premier League and Spain La Liga
- Leave empty if no league preference

**Effect:** Trader gets bonus when assigned to their priority leagues

#### MaxBet
**What to enter:** Maximum bet amount this trader can handle

**Format:** Number with optional "k" or "K" suffix for thousands

**How the system parses this:**
1. Looks for pattern: `[number]k` or `[number]K` (with optional space)
2. Multiplies the number by 1,000
3. If no "k" found, returns 0 (meaning no MaxBet restriction)
4. Empty value = no limit (default: 1,000,000 internally)

**Examples:**
- `14k` or `14K` → Parsed as 14,000
- `14 k` → Parsed as 14,000
- `50k` → Parsed as 50,000
- `100K` → Parsed as 100,000
- `50000` → Parsed as 0 (no "k" suffix, treated as no restriction)
- Empty → No limit (defaults to 1,000,000)

**Important:** Always use the "k" suffix! Without it, the system treats it as no restriction.

**Recommended values:**
- Junior traders: `10k` to `20k`
- Mid-level traders: `30k` to `50k`
- Senior traders: `70k` or leave empty (no limit)

### Example Row

| NAME | GROUP | LEVEL | MAIN BET TYPE | Ignore League | Ignore League Type | Non-Solo league | Priority skill | Priority league | MaxBet |
|------|-------|-------|---------------|---------------|-------------------|----------------|----------------|----------------|--------|
| CLGKelven | CLG | [PIC]/Elite | 2/M1*2/M2*3 | JAPAN J-LEAGUE | M2 | England Premier League | HDP | England Premier League, Spain La Liga | 50k |

**Interpretation:**
- CLGKelven is from Group (CLG)
- Is a Manager with Elite skills
- Can handle both HDP and OU
- Can handle 2 M1 matches and 3 M2 matches simultaneously
- Should NOT be assigned to Japan J-League matches
- Should NOT be assigned to M2 match type
- Cannot work England Premier League matches alone (needs a pair)
- Better at HDP role
- Performs best in England Premier League and Spain La Liga
- Can handle matches up to 50,000 maxbet

### Tips
- ✅ Be consistent with trader names across all sheets
- ✅ Use `[PIC]/` prefix for managers
- ✅ Specify capacity if different from defaults (M1*1, M2*2)
- ✅ List all leagues and restrictions clearly
- ⚠️ Don't use conflicting settings (e.g., Ignore League Type: M1 but MAIN BET TYPE: M1*2)

---

## 3. Mismatch Sheet

### Purpose
Record all matches that need trader assignments.

### Sheet Configuration
- **Sheet Name:** Mismatch
- **Header Row:** Row 2
- **Data Starts:** Row 3
- **Required Columns:** No, Time, League, Maxbet
- **Optional Columns:** League Code, Provider, HDP, OU, ET_PK, Link EAL, Unchanged, L.Group

### Columns

#### No
**What to enter:** Unique match number/identifier
**Format:** Number or text with optional suffix for special bet types
**Important:** Each match should have a unique number

**Basic Formats:**
- Simple numbers: `1`, `2`, `3`, `100`
- Alphanumeric: `1A`, `1A1`, `2B3`

**Special Suffix Formats (triggers automatic bet type detection):**

1. **Booking Bet Types:**
   - `TB` suffix = Booking (Total Bookings)
     - Example: `123TB`, `5TB`
   - `TB1` suffix = Next Booking
     - Example: `123TB1`, `5TB1`

2. **Corner Bet Types:**
   - `C` suffix = Corner (Total Corners)
     - Example: `123C`, `5C`
   - `C1` suffix = Next Corner
     - Example: `123C1`, `5C1`

3. **Child Product Format (Other Sports):**
   - Pattern: Numbers + Letters (+ optional Numbers)
   - Example: `12B`, `13FS`, `12B1`, `13FS2`
   - Used for non-soccer sports or special markets

**Examples:**
- `1` - Regular match #1
- `123TB` - Match #123 Booking market
- `5C1` - Match #5 Next Corner market
- `12FS` - Child product (Futsal or other sport)

**Note:** The No suffix has the highest priority in bet type detection, overriding league and maxbet-based classification

#### Time
**What to enter:** Match date and time
**Format:** Date and time using 12-hour format with AM/PM

**Time Format:**
- **12-hour format with AM/PM** (required):
  - `HH:MM AM` or `HH:MM PM`
  - Examples: `03:00 AM`, `11:30 PM`, `12:00 PM`, `09:45 AM`
  - Space between time and AM/PM is required

**Important Notes:**
- Always use AM/PM format (12-hour format)
- `12:00 PM` is noon, `12:00 AM` is midnight
- All times are processed in UTC timezone by the system

#### League
**What to enter:** Full league name
**Format:** Text (case-insensitive, will be uppercased)
**Examples:**
- `England Premier League` 
- `JAPAN J-LEAGUE DIVISION 1`
- `AUSTRALIA A-LEAGUE`
- `ENGLAND PREMIER LEAGUE`

**Important:** Use consistent league names that match those in Grade sheet

#### Maxbet
**What to enter:** Maximum bet amount for this match
**Format:** Number with "k" or "K" suffix (for thousands), or whole number

**Accepted Formats:**
- With "k" suffix: `30k`, `50K`, `100k` (system processes these as thousands)
- Whole numbers: `50000`, `100000` (displayed but NOT processed by system)

**How the system processes Maxbet:**
1. Looks for pattern: `[number]k` or `[number]K` (with optional space)
2. If found: Multiplies by 1,000 to get actual value
3. If no "k" found: Returns 0 (treated as no maxbet restriction)

**Examples with processing:**
- `30k` or `30K` → Processed as 30,000
- `50k` → Processed as 50,000
- `100K` → Processed as 100,000
- `50000` (no "k") → Processed as 0 (no restriction)
- Empty → No maxbet (processed as 0)

**⚠️ Important:** Always use the "k" suffix! Without it, the system treats the value as 0 (no restriction) and won't use it for bet type detection.

**Bet Type Detection (uses processed Maxbet value):**
The system uses Maxbet for automatic bet type classification (after processing with "k"):
- ≥ 70k → MJ (Major)
- 40k-69k → M1 (Minor 1)
- 20k-39k → M2 (Minor 2)
- 8k-19k → M3 (Minor 3)
- 1.5k-7k → M4 (Minor 4)
- < 1.5k → Unknown

**Examples with bet type detection:**
- Maxbet = `80k` → Processed as 80,000 → MJ (Major)
- Maxbet = `50k` → Processed as 50,000 → M1 (Minor 1)
- Maxbet = `25k` → Processed as 25,000 → M2 (Minor 2)
- Maxbet = `50000` (no "k") → Processed as 0 → Unknown (no bet type from maxbet)

**Note:** Maxbet is used for:
1. Bet type classification (fallback when other rules don't apply)
2. Match prioritization (higher maxbet matches get better traders)
3. Optimization scoring (matches weighted by maxbet value)

#### ET_PK
**What to enter:** Extra Time and Penalty Kick information for matches (optional)
**Format:** Text containing `ET` and/or `PK` (case-sensitive)

**Purpose:** This column tells the system how long a match will last, which is critical for:
- Preventing trader overlap (trader can't be assigned to two matches at the same time)
- Calculating when a trader becomes available for the next match
- Ensuring traders have enough time to complete their current match before being assigned to another

**How the system processes ET_PK:**
1. Checks if the value contains the text "ET" (case-sensitive)
2. Checks if the value contains the text "PK" (case-sensitive)
3. Calculates match end time based on what it finds

**Match Duration Rules (for real soccer matches):**

| ET_PK Value | Contains "ET"? | Contains "PK"? | Duration | Calculation |
|-------------|----------------|----------------|----------|-------------|
| Empty | No | No | 2 hours | Normal match |
| `ET` | Yes | No | 3 hours | Extra time match |
| `PK` | No | Yes | 2.5 hours | Penalty shootout |
| `ET PK` | Yes | Yes | 3 hours | Both (uses ET duration) |
| `ETPK` | Yes | Yes | 3 hours | Both (uses ET duration) |
| `PK ET` | Yes | Yes | 3 hours | Both (uses ET duration) |

**Accepted Formats:**
- `ET` - Extra Time only
- `PK` - Penalty Kicks only
- `ET PK` - Both (with space)
- `ETPK` - Both (no space)
- `PK ET` - Both (reversed order)
- Any text containing "ET" and/or "PK" will work

**Examples with Time Calculation:**
1. Match starts at `08:00 AM`, ET_PK is empty
   - Match ends at: `10:00 AM` (2 hours later)

2. Match starts at `08:00 AM`, ET_PK = `ET`
   - Match ends at: `11:00 AM` (3 hours later)

3. Match starts at `08:00 AM`, ET_PK = `PK`
   - Match ends at: `10:30 AM` (2 hours 30 minutes later)

4. Match starts at `08:00 AM`, ET_PK = `ET PK`
   - Match ends at: `11:00 AM` (3 hours later)

**Why This Matters:**
If a trader is assigned to a match from 8:00 AM to 10:00 AM, they cannot be assigned to another match that starts at 9:00 AM (overlap). The system uses ET_PK to calculate these overlaps accurately.

**Note:** This only applies to real soccer matches. Virtual matches (PES games, etc.) use fixed durations based on bet type and ignore the ET_PK column.

#### Link EAL
**What to enter:** Extra PT (EXPT) and EAL match classification (optional)
**Format:** Text (case-insensitive)

**Purpose:** This column controls two important match characteristics:
1. **EAL Classification**: Whether this is an EAL match
2. **EXPT Restriction**: Whether traders with `[No Expt]` flag can be assigned to this Extra PT match

**How the system processes Link EAL:**

The system performs two separate checks on this column:

**1. EAL Detection (checks if text contains "EAL"):**
- Process: Converts value to uppercase → Checks if it contains "EAL"
- Case-insensitive: `eal`, `EAL`, `Eal` all work the same

**2. EXPT Detection (checks if text equals "NO EXPT"):**
- Process: Converts value to uppercase → Checks if it exactly equals "NO EXPT"
- Case-insensitive: `no expt`, `NO EXPT`, `No Expt` all work the same
- Must be exact match: "NO EXPT" only (not "NOEXPT" or "NO_EXPT")

**Processing Logic Table:**

| Link EAL Value | Contains "EAL"? | Equals "NO EXPT"? | Result |
|----------------|-----------------|-------------------|--------|
| Empty | No | No | isEXPT = true, isEAL = false (Normal Extra PT match) |
| `EAL` | Yes | No | isEXPT = true, isEAL = true (EAL match, Extra PT assignable) |
| `NO EXPT` | No | Yes | isEXPT = false, isEAL = false (Not Extra PT assignable) |
| `EAL NO EXPT` | Yes | No | isEXPT = true, isEAL = true (EAL takes priority) |
| `Something else` | No | No | isEXPT = true, isEAL = false (Default behavior) |

**Common Values:**

1. **Empty (most common)**
   - Match is Extra PT assignable (any trader can be assigned)
   - Match is NOT EAL
   - Use for: Normal matches

2. **`EAL`**
   - Match is marked as EAL type
   - Match is Extra PT assignable
   - Use for: EAL matches that any trader can handle

3. **`NO EXPT`**
   - Match is NOT Extra PT assignable
   - Traders with `[No Expt]` flag in their MAIN BET TYPE cannot be assigned
   - Match is NOT EAL
   - Use for: Matches that require experienced traders only (exclude traders with [No Expt] flag)

**How This Affects Trader Assignment:**

When a match has `Link EAL = "NO EXPT"`:
- System sets `isEXPT = false`
- Only traders with `isEXPTAssignable = true` can be assigned
- Traders with `[No Expt]` in MAIN BET TYPE are excluded

**Examples:**

1. **Normal Match (Empty)**
   ```
   Link EAL: (empty)
   Result: Any trader can be assigned
   ```

2. **EAL Match**
   ```
   Link EAL: EAL
   Result: Marked as EAL, any trader can be assigned
   ```

3. **Expert-Only Match**
   ```
   Link EAL: NO EXPT
   Result: Only expert traders (without [No Expt] flag) can be assigned
   ```

**Related Trader Setting:**

In Grade sheet, traders can have `[No Expt]` in their MAIN BET TYPE:
- `HDP/M1*2[No Expt]` - Trader cannot be assigned to Extra PT matches
- When match has `Link EAL = "NO EXPT"`, these traders are excluded

**Important Notes:**
- Case-insensitive: "eal", "EAL", "Eal" all work the same
- "NO EXPT" must be exact (with space): "NOEXPT" won't work
- Empty value defaults to EXPT = true, EAL = false
- The two flags (EAL and EXPT) are independent checks

#### Unchanged
**What to enter:** Mark to preserve existing assignments (optional)
**Format:** `x` or `X` to mark as unchanged (case-insensitive)
**Example:** `x`

**What it does:**
- When set to "x", the system keeps the current HDP and OU trader assignments
- Match will not be reassigned during optimization
- The assigned traders are blocked from being assigned to other matches during this time

**When to use:**
- Match is already in progress (don't change traders mid-match)
- You manually assigned specific traders and want to keep them
- Previous assignments were good and you want to maintain consistency

**Important:**
- Only "x" works (not "yes", "y", etc.)
- HDP and/or OU columns must have trader names when Unchanged = "x"
- Empty value or any other text means match goes through normal optimization

**Example:**

| No | Time | HDP | OU | Unchanged |
|----|------|-----|-----|-----------|
| 1 | 08/16/2024 03:00 AM | CLGKelven | VNMHung | x |

Result: System keeps CLGKelven as HDP and VNMHung as OU, blocks them from other matches during this time

#### L.Group
**What to enter:** Manual bet type override (optional)
**Format:** One of: `MJ`, `M1`, `M2`, `M3`, `M4`
**Examples:**
- `MJ` - Force this match to be Major type
- `M2` - Force this match to be Minor 2 type
- Leave empty - Use automatic detection

**Priority:** This column has the highest priority in bet type detection
- If L.Group is set, it overrides all automatic detection (time-based, league-based, maxbet-based)
- Use when automatic detection doesn't classify matches correctly
- Useful for special cases or exceptions to normal classification rules

#### HDP
**What to enter:** Assigned Handicap trader name (if already assigned)
**Format:** Trader name
**Examples:**
- `CLGKelven` - Match already has HDP trader
- Leave empty if needs assignment

**When to fill:**
- Match is already handled and shouldn't change
- You want to keep existing assignment

#### OU
**What to enter:** Assigned Over/Under trader name (if already assigned)
**Format:** Trader name
**Examples:**
- `VNMHung` - Match already has OU trader
- Leave empty if needs assignment

### How to Check if a Match is Solo

The system automatically determines if a match should be treated as a solo match (one trader handles both HDP and OU roles) based on time proximity to **future** matches in the same league.

**Rule:** A match is considered **solo** if there is NO other match in the same league starting within 45 minutes **AFTER** it (forward direction only).

**How to Check:**

1. **Find all matches in the same league** from the Mismatch sheet
2. **Look only at matches that start AFTER your match** (ignore matches that start before)
3. **Apply the 45-minute rule:**
   - If ANY other match in the same league starts within 45 minutes **after** your match → **NOT solo** (paired)
   - If NO other matches in the same league within 45 minutes after → **Solo match**

**Examples:**

| Your Match Time | Same League Matches | Time Difference | Result | Reason |
|----------------|---------------------|-----------------|--------|--------|
| 3:00 PM | 3:30 PM (after) | +30 minutes | NOT solo | Within 45 min after current match |
| 3:00 PM | 3:45 PM (after) | +45 minutes | NOT solo | Exactly 45 min after = still within window (≤ 45) |
| 3:00 PM | 3:46 PM (after) | +46 minutes | Solo | Just over 45 min = outside window |
| 3:00 PM | 5:00 PM (after) | +120 minutes | Solo | Well outside 45 min window |
| 3:00 PM | 2:30 PM (before) | N/A | Solo | Match before is ignored (backward direction) |
| 3:00 PM | 2:30 PM (before), 4:00 PM (after) | +60 min | Solo | Only future match counts, and it's > 45 min |
| 3:00 PM | 2:30 PM (before), 3:30 PM (after) | +30 min | NOT solo | Future match is within 45 min |
| 3:00 PM | None | N/A | Solo | No other matches in league |
| 3:00 PM | 3:00 PM (different league) | 0 minutes | Solo | Different league doesn't count |

**Important Notes:**
- The system compares ONLY matches within the SAME league
- Matches in different leagues do NOT affect solo detection
- The 45-minute window applies **ONLY in the forward direction** (checks matches that start after, not before)
- Matches that start before the current match are **completely ignored**
- If the match has no league specified, it's automatically treated as solo
- Solo matches require traders who can handle BOTH HDP and OU roles (versatile traders)

### Auto-Detection: Bet Type

The system automatically detects bet type using multiple rules in priority order. The L.Group column has the highest priority and overrides all automatic detection.

**Detection Priority (highest to lowest):**

1. **No Suffix Patterns** (Special bet types based on match No)
   - Ends with `TB1` → TB1 (Next Booking)
   - Ends with `TB` → TB (Booking)
   - Ends with `C1` → C1 (Next Corner)
   - Ends with `C` → CR (Corner)
   - Example: No = "123TB1" → TB1 type

2. **Child Product Pattern** (Other sports)
   - Numbers followed by letters, optionally followed by numbers
   - Pattern: `12B`, `13FS`, `12B1`, `13FS2`
   - Example: No = "12B" → CP (Child Product)

3. **League-Based Detection**
   - League contains `FUTSAL` → FS (Futsal)
   - League contains `SABA CLUB FRIENDLY` and `PES 21` → PESCF
   - League contains `SABA INTERNATIONAL FRIENDLY` and `PES 21` → PESIF
   - League contains `SABA VIETNAM V LEAGUE 1` and `PES 24` → PES24
   - League contains `SABA ELITE FRIENDLY` and `PES 21` → PES5MIN
   - League contains `PENALTY SHOOTOUTS` and `PES 23`:
     - Contains `INTERNATIONAL` → PKIF
     - Contains `CLUB` → PKCF

4. **MJLeagueInclude Configuration** (Highest priority for real soccer matches)
   - If league is in MJLeagueInclude list from Config sheet → MJ
   - Example: "England Premier League" in MJLeagueInclude → MJ
   - This overrides time-based and maxbet-based detection

5. **M1LeagueInclude Configuration**
   - If league is in M1LeagueInclude list from Config sheet → M1
   - Example: "JAPAN J-LEAGUE" in M1LeagueInclude → M1
   - Always M1 regardless of time or maxbet

6. **L.Group Column Override** (User manual override)
   - If L.Group = `MJ`, `M1`, `M2`, `M3`, or `M4` → Use that value
   - This has higher priority than maxbet-based detection
   - Use when automatic detection doesn't classify correctly

7. **Maxbet-Based Classification** (Final fallback)
   - Maxbet ≥ 70,000 → MJ (Major)
   - Maxbet 40,000-69,999 → M1 (Minor 1)
   - Maxbet 20,000-39,999 → M2 (Minor 2)
   - Maxbet 8,000-19,999 → M3 (Minor 3)
   - Maxbet 1,500-7,999 → M4 (Minor 4)
   - Maxbet < 1,500 → Unknown

**Examples:**

1. Match No = "123TB", League = "England Premier League", Maxbet = 80000
   - Result: **TB** (No suffix pattern has highest priority)

2. Match No = "1", League = "England Premier League", Maxbet = 50000, L.Group = empty
   - MJLeagueInclude contains "England Premier League" → **MJ**

3. Match No = "2", League = "JAPAN J-LEAGUE", Maxbet = 30000, L.Group = empty
   - M1LeagueInclude contains "JAPAN J-LEAGUE" → **M1**

4. Match No = "3", League = "THAILAND LEAGUE", Maxbet = 25000, L.Group = "MJ"
   - L.Group override → **MJ** (overrides maxbet-based M2)

5. Match No = "4", League = "THAILAND LEAGUE", Maxbet = 25000, L.Group = empty
   - Not in MJLeagueInclude or M1LeagueInclude
   - Maxbet 25000 is in range 20,000-39,999 → **M2**

### Example Rows

#### Example 1: Basic Match (Needs Assignment)

| No | Time | League | Maxbet | Provider | ET_PK | Link EAL | Unchanged | L.Group | HDP | OU |
|----|------|--------|--------|----------|-------|----------|-----------|---------|-----|-----|
| 1 | 08/16/2024 03:00 PM | England Premier League | 80k | SABA | | | | | | |

**Interpretation:**
- Match #1 in England Premier League, starts 3:00 PM
- Maxbet: 80k → Detected as **MJ** (≥70k AND in MJLeagueInclude)
- Normal match duration (2 hours)
- HDP and OU empty → Needs assignment from system

#### Example 2: Unchanged Match with Existing Traders

| No | Time | League | Maxbet | Provider | ET_PK | Link EAL | Unchanged | L.Group | HDP | OU |
|----|------|--------|--------|----------|-------|----------|-----------|---------|-----|-----|
| 2 | 08/16/2024 02:00 AM | Japan J-League | 30k | SABA | ET PK | | x | | CLGKelven | VNMHung |

**Interpretation:**
- Match #2 in Japan J-League, starts 2:00 AM
- Maxbet: 30k → Detected as **M1** (40k-69k range)
- ET PK → Match duration 3 hours
- Unchanged = "x" → System keeps CLGKelven (HDP) and VNMHung (OU)
- These traders are blocked from 2:00 AM to 5:00 AM

### Tips
- ✅ Always include Time, League, and Maxbet
- ✅ Use consistent league names
- ✅ Leave HDP/OU empty if you want the system to assign
- ✅ Set Solo = Yes only if ONE trader should handle both roles
- ⚠️ If HDP and OU have same trader name, Solo is auto-detected as Yes

---

## 4. Config Sheet

### Purpose
Set system-wide configuration values that affect match type detection and assignment rules.

### Columns

The Config sheet has two columns: **Type** and **Value**

#### Row 1: CurrentDate
**Type:** `CurrentDate`
**Value:** The reference date for calculations
**Format:** `YYYY-MM-DD` or `MM/DD/YYYY` or any recognizable date format
**Example:** `2024-08-16` or `8/17/2025`

**What it does:**
- Used as the base date for relative time calculations
- Helps the system interpret dates correctly

**How to set:**
- Usually set to today's date or the date you're planning assignments for
- Example: If planning for August 16, 2024, set to `2024-08-16` or `8/16/2024`

#### Row 2: MJLeagueInclude
**Type:** `MJLeagueInclude`
**Value:** Comma-separated list of leagues that should be treated as MJ (Major) matches
**Format:** League names separated by commas
**Example:** `England Premier League, SPAIN LA LIGA, ITALY SERIE A, GERMANY BUNDESLIGA, FRANCE LIGUE 1`

**What it does:**
- Any match in these leagues will be classified as **MJ** type
- Overrides maxbet-based detection
- These are typically top-tier, high-stakes leagues

**Common leagues to include:**
- `England Premier League` - English Premier League
- `SPAIN LA LIGA` - Spanish La Liga
- `ITALY SERIE A` - Italian Serie A
- `GERMANY BUNDESLIGA` - German Germany Bundesliga
- `FRANCE LIGUE 1` - French Ligue 1

#### Row 3: M1LeagueInclude
**Type:** `M1LeagueInclude`
**Value:** Comma-separated list of leagues that should ALWAYS be treated as M1 matches
**Format:** League names separated by commas
**Example:** `JAPAN J-LEAGUE, AUSTRALIA A-LEAGUE`

**What it does:**

- Any match in these leagues will be classified as **M1** type
- Overrides maxbet-based detection
- These are typically lower-tier or regional leagues

**When to use:**

- Leagues that should always be M1 regardless of maxbet
- Regional leagues or lower divisions

#### Row 4: MJLeagueAlternativeAssignRule
**Type:** `MJLeagueAlternativeAssignRule`
**Value:** Comma-separated list of leagues that use alternative MJ assignment rules
**Format:** League names separated by commas
**Example:** `England Premier League, SPAIN LA LIGA`

**What it does:**
- Allows ONE trader to handle BOTH HDP and OU roles 

#### Row 5: MJIncludeBettype (Optional)
**Type:** `MJIncludeBettype`
**Value:** Comma-separated list of bet types that should be included when assigning traders for MJ workflow
**Format:** Bet type codes separated by commas
**Example:** `FS, PES24, TB`

**What it does:**

- Tells the MJ workflow which OTHER bet types to check when assigning traders to MJ matches
- When assigning MJ matches, the system looks at ALL matches with these bet types to see which traders are already busy
- This prevents trader overlap - ensures traders already handling these bet types won't be assigned to conflicting MJ matches
- **Leave empty = Only check MJ matches** (system won't check other bet types for trader availability)

**When to use:**

- When you want to prevent MJ assignments from conflicting with specific bet types
- Example: If set to `M1, FS`, the MJ workflow will check traders already assigned to M1 and FS matches
- This ensures no trader gets assigned to overlapping MJ + M1 matches or MJ + FS matches

**Example - Preventing Trader Overlap:**

Scenario: You have `MJIncludeBettype` set to `M1, FS`

**Match Schedule:**

- Match A: England Premier League (bet type **MJ**), starts 3:00 PM, needs assignment
- Match B: Corner match (bet type **M1**), starts 3:15 PM, already assigned to SarahHDP
- Match C: Futsal match (bet type **FS**), starts 3:20 PM, already assigned to MikeHDP

**When the MJ workflow runs to assign traders for Match A:**

1. System needs to assign traders to Match A (MJ match)
2. System checks `MJIncludeBettype` config = `M1, FS`
3. System looks at ALL matches with bet types **M1** and **FS** to find traders already assigned:
   - Match B (M1 type) → SarahHDP is handling this from 3:15-5:15 PM
   - Match C (FS type) → MikeHDP is handling this from 3:20-5:20 PM
4. When assigning Match A (3:00-5:00 PM):
   - **SarahHDP** is NOT available (busy with M1 Match B from 3:15-5:15 PM - overlaps!)
   - **MikeHDP** is NOT available (busy with FS Match C from 3:20-5:20 PM - overlaps!)
   - System must choose different traders who don't have M1 or FS matches at overlapping times

**Result:** By including `M1` and `FS` in `MJIncludeBettype`, the MJ workflow knows to check traders already assigned to M1 and FS matches. This prevents assigning SarahHDP or MikeHDP to Match A because they're already busy with other matches at overlapping times. Without this configuration, the system might incorrectly assign them to Match A, creating conflicts.

#### Row 6: M1IncludeBettype (Optional)
**Type:** `M1IncludeBettype`
**Value:** Comma-separated list of bet types that should be included when assigning traders for M1 workflow
**Format:** Bet type codes separated by commas
**Example:** `MJ, CR, C1`

**What it does:**

- Tells the M1 workflow which OTHER bet types to check when assigning traders to M1 matches
- When assigning M1 matches, the system looks at ALL matches with these bet types to see which traders are already busy
- This prevents trader overlap - ensures traders already handling these bet types won't be assigned to conflicting M1 matches
- **Leave empty = Only check M1 matches** (system won't check other bet types for trader availability)

**When to use:**

- When you want to prevent M1 assignments from conflicting with specific bet types
- Example: If set to `MJ, CR`, the M1 workflow will check traders already assigned to MJ and Corner matches
- This ensures no trader gets assigned to overlapping M1 + MJ matches or M1 + CR matches

#### Row 7: M2IncludeBettype (Optional)
**Type:** `M2IncludeBettype`
**Value:** Comma-separated list of bet types that should be included when assigning traders for M2 workflow
**Format:** Bet type codes separated by commas
**Example:** `MJ, M1, PESCF, PESIF`

**What it does:**

- Tells the M2 workflow which OTHER bet types to check when assigning traders to M2 matches
- When assigning M2 matches, the system looks at ALL matches with these bet types to see which traders are already busy
- This prevents trader overlap - ensures traders already handling these bet types won't be assigned to conflicting M2 matches
- **Leave empty = Only check M2 matches** (system won't check other bet types for trader availability)

**When to use:**

- When you want to prevent M2 assignments from conflicting with specific bet types
- Example: If set to `MJ, M1, PESCF`, the M2 workflow will check traders already assigned to MJ, M1, and PESCF matches
- This ensures no trader gets assigned to overlapping M2 + MJ matches, M2 + M1 matches, or M2 + PESCF matches

### Example Config Sheet

| Type | Value |
|------|-------|
| CurrentDate | 2024-08-16 |
| MJLeagueInclude | England Premier League, SPAIN LA LIGA, ITALY SERIE A, GERMANY BUNDESLIGA |
| M1LeagueInclude | JAPAN J-LEAGUE, AUSTRALIA A-LEAGUE |
| MJLeagueAlternativeAssignRule | England Premier League, SPAIN LA LIGA |
| MJIncludeBettype | FS, PES24 |
| M1IncludeBettype | CR, C1 |
| M2IncludeBettype | PESCF, PESIF |

**Interpretation:**
- System reference date: August 16, 2024
- England Premier League, Spain La Liga, Italy Serie A, Germany Bundesliga → **MJ** type
- Japan J-League, Australia A-League → Always **M1** type
- England Premier League, Spain La Liga → Allow single-trader assignments
- When MJ workflow runs: check traders already assigned to FS and PES24 matches to prevent overlaps
- When M1 workflow runs: check traders already assigned to CR and C1 matches to prevent overlaps
- When M2 workflow runs: check traders already assigned to PESCF and PESIF matches to prevent overlaps

### Match Type Detection Flow

```
For each match:
1. Is No suffix TB1/TB/C1/C? → Special bet type (TB1/TB/C1/CR)
2. Is No child product pattern? → CP
3. Is League FUTSAL/PES/etc? → FS/PESCF/PESIF/etc
4. Is League in MJLeagueInclude? → MJ
5. Is League in M1LeagueInclude? → M1
6. Is L.Group set to MJ/M1/M2/M3/M4? → Use that value
7. Use Maxbet ranges → MJ/M1/M2/M3/M4
```

### Tips
- ✅ Update CurrentDate regularly to match your planning date
- ✅ Keep league names consistent with Mismatch sheet
- ✅ Use UPPERCASE for league names (system auto-converts anyway)
- ⚠️ BetTypeInclude configs are for trader overlap prevention, not for bet type classification
- ⚠️ Alternative rule only applies to MJ leagues

---

## 5. Pair Trader Sheet

### Purpose
Define preferred trader pairs with priority tiers and league preferences. The system will try to assign these pairs together when possible and give them scoring bonuses.

### Columns

#### HDP
**What to enter:** Trader name for Handicap role
**Format:** Text (must match name in Grade sheet)
**Example:** `CLGKelven`

#### OU1
**What to enter:** First preferred Over/Under trader to pair with HDP
**Format:** Text (must match name in Grade sheet)
**Example:** `VNMHung`

**Tier:** This is the **Primary** pair (highest priority - most preferred pairing)

#### OU2
**What to enter:** Second preferred Over/Under trader to pair with HDP (if OU1 is unavailable)
**Format:** Text (must match name in Grade sheet)
**Example:** `THAPatrick`
**Leave empty if no second preference**

**Tier:** This is the **Secondary** pair (medium priority - second choice)

#### HDPBK
**What to enter:** Backup Handicap trader (used with OUBK)
**Format:** Text (must match name in Grade sheet)
**Example:** `IDNJoko`
**Leave empty if no backup needed**

**Important:** HDPBK must be used together with OUBK to create a backup pair

#### OUBK
**What to enter:** Backup Over/Under trader (used with HDPBK)
**Format:** Text (must match name in Grade sheet)
**Example:** `MYSAlex`
**Leave empty if no backup needed**

**Important:** OUBK must be used together with HDPBK to create a backup pair

**Note:** If both HDPBK and OUBK are filled, it creates one backup tier pair: HDPBK + OUBK

#### 1st
**What to enter:** Highest priority league code for this pair
**Format:** Single league code (NOT full name, NOT comma-separated)
**Example:** `EPL`
**Priority:** Most priority league (best match for this pair)

**⚠️ IMPORTANT: The system automatically prioritizes EPL (English Premier League) above all other leagues**

**Supported League Codes:**

- `EPL` - English Premier League **(Automatically prioritized by the system)**
- `GER` - Germany Bundesliga
- `ITA` - Italy Serie A
- `SPA` - Spain Primera LaLiga
- `FRA` - France Ligue 1
- `JPN` - Japan J-League Division 1
- `AUS` - Australia A-League

#### 2nd
**What to enter:** Second priority league code for this pair
**Format:** Single league code
**Example:** `ITA`
**Priority:** Second priority league (good match for this pair)

#### 3rd
**What to enter:** Third priority league code for this pair
**Format:** Single league code
**Example:** `FRA`
**Priority:** Third priority league (acceptable match for this pair)

**How league priorities work:**

- When this pair is assigned to their 1st priority league → Gets highest priority
- When this pair is assigned to their 2nd priority league → Gets medium priority
- When this pair is assigned to their 3rd priority league → Gets lower priority
- Other leagues → No special priority

### Understanding Tiers

**Primary Tier (HDP + OU1):** Most preferred pairing

- The highest priority combination
- System will strongly prefer this combination
- Created when HDP and OU1 are both filled

**Secondary Tier (HDP + OU2):** Second preferred pairing

- Second choice if you want an alternative to OU1
- Still good pairing but lower priority than Primary
- Created when HDP and OU2 are both filled

**Backup Tier (HDPBK + OUBK):** Lowest priority pairing

- Alternative pair using backup traders
- Created ONLY when BOTH HDPBK and OUBK are filled
- If only one is filled, no backup pair is created

**Combined Priorities:**

When a pair tier matches a league priority, the system gives even higher preference:

- Primary tier + 1st priority league → Highest possible priority (best combination!)
- Secondary tier + 2nd priority league → High priority
- Backup tier + 3rd priority league → Medium priority

**Important:** League priorities (1st, 2nd, 3rd) apply to ALL pairs in that row

### Example Row

| HDP | OU1 | OU2 | HDPBK | OUBK | 1st | 2nd | 3rd |
|-----|-----|-----|-------|------|-----|-----|-----|
| CLGKelven | VNMHung | THAPatrick | IDNJoko | MYSAlex | EPL | ITA | FRA |

**Interpretation:**

**Pair Combinations Created:**

1. **Primary:** CLGKelven (HDP) + VNMHung (OU) - Highest priority
2. **Secondary:** CLGKelven (HDP) + THAPatrick (OU) - Medium priority
3. **Backup:** IDNJoko (HDP) + MYSAlex (OU) - Lower priority

**Total:** 3 pairs created from this single row

**League Priorities:**

- EPL (English Premier League) matches → Most priority (best for this pair)
- ITA (Italy Serie A) matches → Second priority
- FRA (France Ligue 1) matches → Third priority
- Other leagues → No special priority

**Example Scenario:**
- EPL (English Premier League) match needs assignment
- CLGKelven + VNMHung are both available
- They get highest possible priority (primary tier + 1st priority league EPL)
- This makes them highly likely to be assigned together for this match
  
---

## Quick Start Workflow

### Daily Assignment Process

1. **Update TimeTable Sheet**
   - Add today's trader shifts
   - Mark any busy periods (breaks, meetings)
   - Adjust shift times if needed

2. **Update Mismatch Sheet**
   - Add all matches that need assignments
   - Fill in Time, League, Maxbet
   - Mark any already-handled matches (fill HDP/OU columns)

3. **Review Grade Sheet**
   - Ensure all active traders are listed
   - Update any restrictions (Ignore League, Ignore League Type)
   - Check capacity settings are correct

4. **Check Config Sheet**
   - Update CurrentDate to today
   - Verify league lists are current

5. **Run Assignment Workflow (Sequential Execution)**

   **Step 1: Trigger MJ Workflow**
   - Fill any character in the MJ trigger column in Google Sheets
   - Wait maximum 2 minutes for MJ assignment to complete
   - Results will be written back to Google Sheets

   **Step 2: Trigger M1 Workflow**
   - Fill any character in the M1 trigger column
   - Wait maximum 2 minutes for M1 assignment to complete
   - Results will be written back to Google Sheets

   **Step 3: Trigger M2 Workflow**
   - Fill any character in the M2 trigger column
   - Wait maximum 2 minutes for M2 assignment to complete
   - Results will be written back to Google Sheets

   **Important:**
   - Execute workflows in order (MJ → M1 → M2) to ensure proper trader capacity allocation
   - Maximum waiting time for each workflow is 2 minutes

6. **Review Results**
   - Check assigned traders for each match
   - Verify no conflicts or overlaps
   - Confirm high-stakes matches have best traders

---

## Support and Questions

If you encounter issues:
1. Check all trader names are consistent across sheets
2. Verify date and time formats are correct
3. Ensure required columns (Time, League, Maxbet) are filled
4. Review Config sheet for correct league classifications
5. Check for conflicting restrictions in Grade sheet

For technical issues with the system, consult the technical documentation or contact your system administrator.
