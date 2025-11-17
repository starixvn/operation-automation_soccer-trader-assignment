# M3/M4 (Virtual Matches) Assignment Rules

## Overview

The M3/M4 assignment system handles virtual soccer matches with lower values and complexity. These are automated matches where a single trader can handle one or two matches depending on the match value and pairing rules. The system uses special pairing strategies to optimize trader utilization.

## What Makes M3/M4 Different

### Virtual Match Model

Unlike real matches (MJ, M1, M2):
- **Automated virtual matches:** Not real soccer games
- **Lower complexity:** Simpler betting patterns
- **All traders eligible:** No skill requirements
- **Can pair matches:** One trader can handle two matches simultaneously

### Match Classification

**M3 Match Criteria:**
- MaxBet value: Less than 20,000
- Virtual automated matches

**M4 Match Criteria:**
- MaxBet value: Less than 12,000
- Subset of M3 matches

**Match Type Comparison:**
- **MJ (Major):** MaxBet ≥ 70,000 - Two traders required
- **M1 (Minor 1):** MaxBet 40,000-70,000 - Single trader
- **M2 (Minor 2):** MaxBet 20,000-40,000 - Single trader, can handle multiple concurrent
- **M3/M4 (Virtual):** MaxBet < 20,000 - Single trader, can pair matches

## Who Can Work M3/M4 Matches

### Trader Requirements

All traders can work M3/M4 matches:
- **No skill requirements:** Any trader can work virtual matches
- **No level restrictions:** All levels (Elite through Third) can work
- **Available shift time** that covers the match
- **EXPT capability** only required for EXPT matches

This makes M3/M4 matches ideal for training junior traders.

## Assignment Rules

### Core Principle: Pairing-Based Assignment

M3/M4 assignments follow different rules based on match value:

**For M3 matches with MaxBet > 12,000:**
- Trader must handle TWO matches simultaneously
- Primary match (the high-value match)
- Paired match (must be EAL or EXPT with MaxBet ≤ 12,000)
- Both matches must start at exactly the same time

**For M3/M4 matches with MaxBet ≤ 12,000:**
- Trader handles ONE match only
- No pairing required
- Simple single-match assignment

### Pairing Requirements

When pairing is required (MaxBet > 12,000):

**Paired Match Options (in priority order):**
1. **EAL (Early Assignment League) matches** - Highest priority, any MaxBet
2. **EXPT matches with MaxBet ≤ 12,000** - Second priority, requires EXPT-capable trader

**Same Start Time Required:**
- Both matches must start at exactly the same time
- Matches with different start times cannot be paired
- Example: Both must start at 10:00 (not 10:00 and 10:30)

**EXPT Match Restrictions:**
- EXPT matches can only be paired if MaxBet ≤ 12,000
- Trader must have EXPT capability (isEXPTMatchAssignable: true)
- EXPT matches with MaxBet > 12,000 cannot be used for pairing

### Special League Rules

Some leagues have special rules that prevent pairing:

**Leagues with No Pairing Allowed:**
- **KOREA K LEAGUE 1** - Each match requires dedicated trader
- **NETHERLANDS EERSTE DIVISIE (PLAY OFF)** - Each match requires dedicated trader

For these leagues:
- Cannot pair with any other match (EAL or EXPT)
- One trader per match only
- Applies regardless of MaxBet value

### Trader Qualifications

Traders can work M3/M4 matches when they meet these conditions:

**Time Coverage:**
- Match must fall completely within the trader's shift
- No partial coverage allowed

**EXPT Capability (for EXPT matches only):**
- Extra PT matches require specially qualified traders
- Only traders marked as EXPT-capable can work EXPT matches

**No Schedule Conflicts:**
- Cannot overlap with existing assigned work
- Can run alongside M1/M2 matches (using M1/M2 capacity rules)

## How Assignments Work

### Assignment Process

The system assigns traders to M3/M4 matches following these steps:

#### Step 1: Identify Match Type

For each match:
1. Check if MaxBet > 12,000 (requires pairing)
2. Check if special league (K League 1 or Eerste Divisie Play-Off - no pairing)
3. Check if EXPT match (requires EXPT-capable trader)

#### Step 2: Find Pairing Match (if needed)

For high-value matches (MaxBet > 12,000):

**First: Look for EAL matches**
- Check for EAL matches with same start time
- Any MaxBet value acceptable
- EAL has highest priority

**Second: Look for EXPT matches**
- Check for EXPT matches with same start time AND MaxBet ≤ 12,000
- Requires EXPT-capable trader
- EXPT has second priority

**No Pairing Available:**
- If no suitable pairing found, match goes UNASSIGNED
- High-value matches cannot be assigned alone

#### Step 3: Assign Trader

**For paired matches:**
- One trader handles both matches
- Both matches assigned to same trader
- Trader must be available for both match durations

**For single matches:**
- One trader handles one match
- Standard assignment

### Priority Considerations

When choosing between available traders:

1. **All traders equal** - No level or manager preferences for M3/M4
2. **EXPT capability** required for EXPT matches
3. **Availability** - Must cover match time

## Special Cases

### Unpaired High-Value Matches

If a high-value M3 match (MaxBet > 12,000) cannot find a pairing match:
- Match remains UNASSIGNED
- Need to add EAL or suitable EXPT matches at same start time
- Or manually override pairing requirement

### Special League Handling

K League 1 and Netherlands Eerste Divisie Play-Off matches:
- Never paired with other matches
- Always get dedicated trader
- Special rule enforced automatically

### Concurrent with M1/M2

M3/M4 matches can run alongside M1/M2 matches:
- Uses M1 capacity (for M1 matches)
- Uses M2 capacity (for M2 matches)
- All matches must start at same time
- Counted toward capacity limits

## Summary

The M3/M4 Match assignment system handles virtual matches with special pairing rules. The system:

- Works with virtual matches valued less than 20,000 MaxBet
- Accepts all trader levels (no skill requirements)
- Pairs high-value matches (>12K) with EAL or EXPT matches
- Enforces same-start-time requirement for pairing
- Respects special league rules (K League 1, Eerste Divisie Play-Off - no pairing)
- Requires EXPT capability only for EXPT matches
- Can run alongside M1/M2 matches using capacity rules
- Leaves high-value matches unassigned if no pairing available

Success depends on:
- Accurate trader availability data
- EAL matches scheduled at common start times
- EXPT matches with MaxBet ≤ 12,000 available for pairing
- EXPT-capable traders for EXPT match handling
- Understanding special league restrictions
