# Major (MJ) Match Assignment Rules

## Overview

The Major Match assignment system assigns Elite, Member, and First level traders to high-value soccer matches. Each match requires two different traders working together - one handling Handicap betting and one handling Over/Under betting. The system finds the best possible assignments by considering match values, trader skills, team chemistry, and operational constraints.

## What Makes MJ Different

### Two Traders Required

Unlike other match types where one trader handles everything, Major matches need two specialists:
- **HDP Trader:** Focuses on Handicap betting
- **OU Trader:** Focuses on Over/Under betting

These must be different people - you cannot assign the same trader to both roles.

### Match Classification

**Major Match Criteria:**
- Minimum MaxBet: 70,000
- All matches must meet this threshold to be considered Major

## Who Can Work Major Matches

### Trader Requirements

To work Major matches, traders need:
- Required skill (either HDP or OU ability)
- Appropriate level (Elite, Member, or First)
- Available shift time that covers the entire match
- No conflicts with existing assignments

### Manager vs Non-Manager Distinction

Traders are classified by their management responsibilities:
- **Non-Managers:** Dedicated traders who focus full-time on trading
- **Managers:** Have trading skills but also supervise teams and handle escalations

## Assignment Rules

### Core Principle: One MJ Match Per Trader

Each trader can only be assigned to one MJ match at a time. This means:

- A trader working Match A (08:00-10:00) cannot also work Match B (09:00-11:00)
- This applies regardless of role - even if they work HDP in one match and OU in another
- Traders need full focus on their assigned match
- No multitasking across overlapping matches

### Match Requirements

Every MJ match needs:
- Exactly one HDP trader
- Exactly one OU trader
- These must be two different people

### Trader Qualifications

Traders can only work matches when they meet all these conditions:

**Skill Match:**
- Trader must have the required skill (HDP or OU)
- Versatile traders with both skills can work either role

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
- Protects junior traders from high-stakes situations

## How Assignments Work

### Assignment Process

The system assigns traders to matches following these steps:

#### Step 1: Sort Matches by Priority

Matches are prioritized in this order:
1. **EPL matches first** - Always highest priority regardless of value
2. **Then by MaxBet** - Higher value matches before lower value

#### Step 2: For Each Match, Find Best Trader Pair

For each match (starting with highest priority):

**First Choice: Preconfigured Pairs**
- Check if any configured pair (Primary, Secondary, or Backup) is available
- Use the best available configured pair for this match

**Second Choice: Ad-hoc Elite Pairs**
- If no configured pairs available, try to pair two Elite traders
- Prefer Elite Non-Managers over Elite Managers

**Third Choice: Mixed Skill Pairs**
- If only one Elite available, pair Elite with Member
- If no Elite available, pair two Members
- If only one Member available, pair Member with First
- Continue down skill levels as needed

**Manager Consideration:**
- Non-managers are always preferred over managers at same skill level
- Elite Non-Manager > Elite Manager
- Member Non-Manager > Member Manager
- First Non-Manager > First Manager

### EPL Priority Rule

EPL (English Premier League) matches always get first priority:
- All EPL matches assigned before any other league
- Applies regardless of MaxBet value
- Best available pairs always go to EPL first

### Trader Selection Priority

When choosing between available traders:

1. **Configured pairs** beat ad-hoc pairings
2. **Elite** traders before Member traders
3. **Member** traders before First traders
4. **Non-managers** before managers at same skill level
5. **League expertise** helps when skill levels are equal

### Option 2: First-Half Assignment (Fallback)

Some leagues support Option 2 assignment when there are not enough traders for full match coverage:

**Leagues with Option 2:**
- Japan J-League Division 1

**How Option 2 Works:**
- If cannot find two traders for full match (both HDP and OU for entire match)
- Assign 1 trader for full match (HDP or OU)
- Assign 1 trader for first half only (the other role)
- First-half trader can be any level (Elite, Member, First, Second, Third, or Other products team)
- This provides partial coverage when full coverage is impossible

**When Option 2 is Used:**
- Only after trying all normal assignment choices fail
- Only for leagues that have Option 2 enabled
- Ensures at least partial match coverage instead of leaving match completely unassigned

## Summary

The Major Match assignment system ensures high-value matches get the best available trader pairs while respecting operational constraints and team dynamics. The system:

- Requires two different traders per match (HDP and OU specialists)
- Prioritizes match value as the primary decision factor
- Considers trader skill level, manager status, and pair traders
- Respects all scheduling, skill, and preference constraints
- Encourages league expertise and proven partnerships
- Maintains operational balance between trading and supervision
- Provides optimal assignments within real-world limitations

Success depends on:
- Accurate trader availability data
- Up-to-date preconfigured pair information
- Proper league preference configuration
- Reasonable shift coverage
- Balanced HDP and OU skill availability
