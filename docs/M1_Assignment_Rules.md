# Minor 1 (M1) Match Assignment Rules

## Overview

The Minor 1 assignment system assigns traders to medium-value soccer matches. Unlike Major matches that require two traders, M1 matches are handled by a single trader who covers both Handicap and Over/Under betting. The system finds the best assignments by considering match values, trader levels, capacity limits, and operational constraints.

## What Makes M1 Different

### Single Trader Model

Unlike Major matches where two traders work together:
- **One trader handles everything:** Both HDP and OU betting for the entire match
- **No skill requirements:** Any qualified trader can work any M1 match
- **Simpler assignments:** No need to match pairs or balance roles

### Match Classification

**M1 Match Criteria:**
- MaxBet value: 40,000 to 70,000 (exclusive of 70,000)
- Or explicitly marked as BetType "M1"

**Match Type Comparison:**
- **MJ (Major):** MaxBet ≥ 70,000 - Requires two traders (HDP + OU)
- **M1 (Minor 1):** MaxBet 40,000-70,000 - Single trader
- **M2 (Minor 2):** MaxBet 20,000-40,000 - Single trader

## Who Can Work M1 Matches

### Trader Requirements

To work M1 matches, traders need:
- Appropriate level (Elite, Member, First, Second, or Third)
- Available shift time that covers the entire match
- Sufficient MaxBet capacity for the match value
- M1 capacity available (not overloaded with other matches)

### Trader Levels

M1 matches can be assigned to:
- **Elite:** Highest skill level
- **Member:** Second tier
- **First:** Third tier
- **Second:** Fourth tier
- **Third:** Fifth tier

All levels can work M1 matches - no level restrictions.

## Assignment Rules

### Core Principle: Capacity-Based Assignment

Each trader has an M1 Capacity setting (typically 1) that controls workload:

**M1 Capacity Rules:**
- Defines maximum number of M1 matches trader can handle at the same time
- Default capacity: 1 match
- Concurrent M3/M4 jobs count toward this capacity
- Example: Trader with capacity 2 and 1 concurrent M3 job can take 1 more M1 match

**Important:** While capacity allows multiple matches, M1 matches themselves cannot overlap in time. Each trader works at most one M1 match at any given moment.

### Match Requirements

Every M1 match needs:
- Exactly one trader to handle both HDP and OU roles
- Trader available for the complete match duration

### Trader Qualifications

Traders can only work matches when they meet all these conditions:

**Time Coverage:**
- Match must fall completely within the trader's shift
- No partial coverage allowed

**League Preferences:**
- Match league cannot be on trader's ignore list
- Traders can opt out of leagues they don't want to work

**EXPT Capability:**
- Extra PT matches require specially qualified traders
- Only traders marked as EXPT-capable can work these matches

**No Schedule Conflicts:**
- Cannot overlap with existing assigned work
- Must have 15-minute gap after virtual matches (PES, PK types)

**Value Capacity:**
- Trader's MaxBet limit must meet or exceed match value
- Protects traders from high-stakes situations beyond their level

**Available M1 Capacity:**
- Must have available capacity slot after counting concurrent M3/M4 jobs
- Cannot exceed trader's M1 capacity limit

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
- Score each trader based on their level and league expertise
- Select trader with highest score
- If multiple traders have same score, system optimizes for overall best solution

### Priority Considerations

When choosing between available traders:

1. **Higher level** traders before lower level
2. **Non-managers** strongly preferred over managers (keeps managers available for supervision)
3. **Priority league expertise** gives small advantage
4. **Consecutive work** preferred (matches that connect to existing jobs)
5. **Shift alignment** preferred (matches at shift start/end)

### Manager vs Non-Manager Distinction

For M1 matches, the system strongly prefers non-managers:

**Why Non-Managers First:**
- Keeps managers available for supervisory duties
- Develops non-manager skills on moderate-complexity matches
- Reduces manager workload on routine operations
- Even a Third non-manager is preferred over an Elite manager

**Manager Assignment:**
- Only when no non-managers available
- EXPT matches may require manager expertise
- Elite managers take priority over lower-level managers when needed

## Summary

The M1 Match assignment system handles medium-value matches efficiently with single-trader assignments. The system:

- Assigns one trader per match to handle both HDP and OU roles
- Works with matches valued 40,000 to 70,000 MaxBet
- Accepts all trader levels (Elite through Third)
- Uses capacity limits to prevent trader overload
- Respects all scheduling, value, and preference constraints
- Optimizes for highest-level available traders
- Strongly prefers non-managers to keep managers available for supervision
- Encourages consecutive work blocks and shift efficiency

Success depends on:
- Accurate trader availability data
- Proper M1 capacity configuration
- Up-to-date league preference settings
- Reasonable shift coverage
- Balanced trader level distribution
