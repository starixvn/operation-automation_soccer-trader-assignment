# Minor 2 (M2) Match Assignment Rules

## Overview

The Minor 2 assignment system assigns traders to lower-value soccer matches. Like M1, M2 matches are handled by a single trader who covers both Handicap and Over/Under betting. The unique feature of M2 is that traders can handle multiple concurrent matches (with the same start time) based on their capacity settings.

## What Makes M2 Different

### Single Trader Model

Like M1 matches:
- **One trader handles everything:** Both HDP and OU betting for the entire match
- **No skill requirements:** Any qualified trader can work any M2 match
- **Simpler assignments:** No need to match pairs or balance roles

### Concurrent Match Capability

Unlike M1 (where traders work only one match at a time), M2 allows:
- **Multiple matches simultaneously:** Traders can handle several M2 matches at the same time
- **Same start time required:** Concurrent matches must start at exactly the same time
- **Capacity controlled:** Limited by trader's M2 capacity setting

### Match Classification

**M2 Match Criteria:**
- MaxBet value: 20,000 to 40,000 (exclusive of 40,000)
- Or explicitly marked as BetType "M2"

**Match Type Comparison:**
- **MJ (Major):** MaxBet ≥ 70,000 - Requires two traders (HDP + OU)
- **M1 (Minor 1):** MaxBet 40,000-70,000 - Single trader, one match at a time
- **M2 (Minor 2):** MaxBet 20,000-40,000 - Single trader, can handle multiple concurrent matches

## Who Can Work M2 Matches

### Trader Requirements

To work M2 matches, traders need:
- Appropriate level (Elite, Member, First, Second, or Third)
- Available shift time that covers the entire match
- Sufficient MaxBet capacity for the match value
- M2 capacity available (not overloaded with concurrent matches)

### Trader Levels

M2 matches can be assigned to:
- **Elite:** Highest skill level
- **Member:** Second tier
- **First:** Third tier
- **Second:** Fourth tier
- **Third:** Fifth tier

All levels can work M2 matches - no level restrictions.

## Assignment Rules

### Core Principle: Concurrent Capacity-Based Assignment

Each trader has an M2 Capacity setting (typically 2) that controls concurrent workload:

**M2 Capacity Rules:**
- Defines maximum number of M2/M3/M4 matches trader can handle at the same time
- Default capacity: 2 matches
- Concurrent M2/M3/M4 jobs count toward this capacity
- **Important:** Only applies to matches with the EXACT same start time
- Example: Trader with capacity 2 can handle 2 M2 matches that both start at 10:00

**Key Difference from M1:**
- M1: Trader can only work ONE M1 match at a time (plus M3/M4 jobs)
- M2: Trader can work MULTIPLE M2 matches at the same time (if same start time and within capacity)

### Match Requirements

Every M2 match needs:
- Exactly one trader to handle both HDP and OU roles
- Trader available for the complete match duration

### Trader Qualifications

Traders can only work matches when they meet all these conditions:

**Time Coverage:**
- Match must fall completely within the trader's shift
- No partial coverage allowed

**Same Start Time for Concurrent Matches:**
- Multiple M2 matches can only be assigned if they start at the exact same time
- Matches with different start times are treated as conflicts (even if within capacity)

**League Preferences:**
- Match league cannot be on trader's ignore list
- Traders can opt out of leagues they don't want to work

**EXPT Capability:**
- Extra PT matches require specially qualified traders
- Only traders marked as EXPT-capable can work these matches

**No Schedule Conflicts:**
- Cannot overlap with existing assigned work (except concurrent M2/M3/M4 with same start)
- Must have 15-minute gap after virtual matches (PES, PK types)

**Value Capacity:**
- Trader's MaxBet limit must meet or exceed match value
- Protects traders from high-stakes situations beyond their level

**Available M2 Capacity:**
- Must have available capacity slot after counting concurrent M2/M3/M4 jobs
- Cannot exceed trader's M2 capacity limit

## How Assignments Work

### Assignment Process

The system assigns traders to matches following these steps:

#### Step 1: Sort Matches by Priority

Matches are prioritized in this order:
1. **Higher MaxBet matches first** - More valuable matches get priority
2. **Within same value, by time** - Earlier matches assigned first

#### Step 2: Find Best Trader for Each Match

For each match (starting with highest priority):

**Trader Selection:**
- Consider all eligible traders (meeting all qualifications above)
- Check concurrent capacity for traders with same-start-time matches
- Select trader with highest score and available capacity
- Balance workload across traders when possible

### Priority Considerations

When choosing between available traders:

1. **Higher level** traders before lower level
2. **Non-managers** strongly preferred over managers (keeps managers available for supervision)
3. **Fair distribution** when multiple traders have capacity for concurrent matches
4. **Priority league expertise** gives small advantage

### Manager vs Non-Manager Distinction

For M2 matches, the system strongly prefers non-managers:

**Why Non-Managers First:**
- Keeps managers available for supervisory duties
- Develops non-manager skills on lower-complexity matches
- Reduces manager workload on routine operations
- Even a Third non-manager is preferred over an Elite manager

**Manager Assignment:**
- Only when no non-managers available
- EXPT matches may require manager expertise
- Elite managers take priority over lower-level managers when needed

## Summary

The M2 Match assignment system handles lower-value matches efficiently with single-trader assignments and concurrent match capability. The system:

- Assigns one trader per match to handle both HDP and OU roles
- Works with matches valued 20,000 to 40,000 MaxBet
- Accepts all trader levels (Elite through Third)
- Allows multiple concurrent matches (with same start time) based on M2 capacity
- Respects all scheduling, value, and preference constraints
- Optimizes for highest-level available traders
- Strongly prefers non-managers to keep managers available for supervision
- Balances workload across traders when multiple can handle concurrent matches

Success depends on:
- Accurate trader availability data
- Proper M2 capacity configuration
- Understanding same-start-time requirement for concurrent matches
- Up-to-date league preference settings
- Reasonable shift coverage
- Balanced trader level distribution
