# M2 (Minor 2) Match Assignment Rules

## Document Overview

This document explains how the system assigns traders to **Minor 2 (M2) matches** using constraint-based optimization. M2 matches are the lowest-priority real matches, and the system has unique **concurrent match handling** capabilities for this category.

**Target Audience:** Operations managers, trading supervisors, and system administrators responsible for M2 match coverage.

---

## Table of Contents

1. [Match Classification](#match-classification)
2. [Assignment Model](#assignment-model)
3. [Core Constraints](#core-constraints)
4. [M2 Capacity: Concurrent Match Handling](#m2-capacity-concurrent-match-handling)
5. [Trader Work Priority System](#trader-work-priority-system)
6. [Virtual Match Gap Rules](#virtual-match-gap-rules)
7. [League Filtering](#league-filtering)
8. [EXPT Match Restrictions](#expt-match-restrictions)
9. [Workload Balancing](#workload-balancing)
10. [Real-World Scenarios](#real-world-scenarios)
11. [Operational Guidelines](#operational-guidelines)
12. [Troubleshooting Decision Tree](#troubleshooting-decision-tree)
13. [API Usage](#api-usage)
14. [FAQ](#faq)
15. [Glossary](#glossary)

---

## Match Classification

### M2 Match Criteria

**MaxBet Range:** 20,000 - 39,999

**Key Characteristics:**
- **Single trader** handles both HDP and OU roles
- **Lowest priority** among real matches (MJ > M1 > M2)
- **No skill-specific requirements** (any trader can work M2)
- **Unique concurrent capability**: Traders can handle MULTIPLE M2 matches simultaneously (subject to M2 Capacity)

**Valid MaxBet Examples:**
- 20,000 ✓ (minimum)
- 30,000 ✓
- 39,999 ✓ (maximum)
- 19,999 ✗ (too low - below M2 range)
- 40,000 ✗ (too high - enters M1 range)

### Match Hierarchy

```
Priority Order:
1. MJ Matches (70K+) - Highest Priority
2. M1 Matches (40K-70K)
3. M2 Matches (20K-40K) - Lowest Priority
```

---

## Assignment Model

### Single Trader Model

Like M1, **M2 matches are handled by a single versatile trader**:

```
M2 Match (30K MaxBet)
├─ HDP Role → TraderA
└─ OU Role → TraderA (same trader)
```

### Decision Variables

```
x[traderId, matchId] ∈ {0, 1}

Where:
  1 = trader is assigned to this M2 match (handles both roles)
  0 = trader is not assigned to this M2 match
```

---

## Core Constraints

### Constraint 1: One Trader Per Match (Maximum)

**Rule:** Each M2 match can have at most 1 trader assigned.

**Formula:**
```
For each match m:
  Σ x[trader, m] ≤ 1
```

### Constraint 2: M2 Capacity Respects Concurrent Workload

**Rule:** A trader can handle up to `M2Capacity` overlapping matches simultaneously.

**This is M2's UNIQUE feature** - unlike M1 (which only allows ONE match at a time), M2 allows MULTIPLE concurrent matches based on trader capacity.

**Formula:**
```
For each trader t and overlapping match group G:
  Σ x[t, match] ≤ (M2Capacity - existingM2/M3/M4Jobs)
  (where matches in G overlap with each other)
```

**Key Points:**
- M2Capacity includes: M2 matches + M2/M3/M4 assigned jobs
- Does NOT include: MJ matches or M1 matches (these always block)
- Applies to BOTH exact same-time starts AND time-overlapping matches

---

## M2 Capacity: Concurrent Match Handling

### What is M2 Capacity?

**M2 Capacity** is a trader-specific property that determines **how many M2-level jobs can run concurrently** (overlapping in time).

**Default Value:** `m2Capacity: 2` (if not specified)

**Valid Values:**
- `m2Capacity: 1` - Can handle 1 M2 match at a time
- `m2Capacity: 2` - Can handle 2 concurrent M2 matches (default)

### Critical Difference from M1 Capacity

| Feature | M1 Capacity | M2 Capacity |
|---------|-------------|-------------|
| **Applies to** | Only M3/M4 jobs + ONE M1 match | M2/M3/M4 jobs + MULTIPLE M2 matches |
| **Multiple M1/M2 Matches** | ✗ Cannot handle multiple M1 matches | ✓ **CAN handle multiple M2 matches** (same start time) |
| **Same Start Time Required** | ✓ Yes (strict) | ✓ **Yes (strict)** - same as M1 |
| **Different Start Times** | ✗ Treated as overlap conflict | ✗ Treated as overlap conflict |
| **Key Use Case** | Virtual jobs alongside real match | **Multiple low-priority matches concurrently (same start)** |

### M2 Capacity Rules

#### Rule 1: Same Start Time Required

**Like M1 Capacity**, M2 Capacity **ONLY applies to matches/jobs with EXACT same start time**.

**Valid:**
```
TraderA (m2Capacity: 2)
├─ M2 Match 1: 10:00 - 11:30 ✓
└─ M2 Match 2: 10:00 - 11:30 ✓ (same start time, within capacity)
```

**Invalid:**
```
TraderB (m2Capacity: 2)
├─ M2 Match 1: 10:00 - 11:30 ✓
└─ M2 Match 2: 10:30 - 12:00 ✗ (different start time - treated as overlap conflict)
```

#### Rule 2: Includes M2/M3/M4 Jobs in Capacity Count

**Formula:**
```
Used Capacity = (M2 assigned jobs) + (M3 assigned jobs) + (M4 assigned jobs) + (New M2 matches)
Available Capacity = M2Capacity - (M2+M3+M4 assigned jobs that overlap)
```

**Example:**
```
TraderB (m2Capacity: 2, has 1 M3 job at 10:00)
├─ Available capacity for M2 matches: 2 - 1 = 1
└─ Can assign 1 M2 match at 10:00
```

#### Rule 3: MJ and M1 Jobs Always Block

**M2 Capacity does NOT apply** to MJ or M1 jobs. These always block M2 matches.

| Job Type | Blocks M2? | Counts Toward M2 Capacity? |
|----------|------------|---------------------------|
| **MJ** | Yes (always) | No (doesn't apply) |
| **M1** | Yes (always) | No (doesn't apply) |
| **M2** | Only if capacity exceeded | Yes |
| **M3** | Only if capacity exceeded | Yes |
| **M4** | Only if capacity exceeded | Yes |

### Concurrent Assignment Examples

#### Example 1: Two M2 Matches, Capacity Allows

```
TraderA Properties:
- m2Capacity: 2
- Shift: 08:00 - 18:00

New M2 Matches:
- Match M2-100: 10:00 - 11:30 (25K)
- Match M2-101: 10:00 - 11:30 (30K)

Capacity Check:
- Concurrent M2/M3/M4 jobs: 0
- Required capacity: 2 (both matches)
- Trader's M2Capacity: 2
- Result: ✓ Both matches assigned to TraderA
```

#### Example 2: Three M2 Matches, Capacity Blocks Third

```
TraderB Properties:
- m2Capacity: 2
- Shift: 08:00 - 18:00

New M2 Matches:
- Match M2-200: 14:00 - 15:30 (22K)
- Match M2-201: 14:00 - 15:30 (28K)
- Match M2-202: 14:00 - 15:30 (35K)

Capacity Check:
- Concurrent M2/M3/M4 jobs: 0
- Available capacity: 2
- System assigns: 2 highest-value matches (35K, 28K)
- Result: ✓ M2-202 and M2-201 assigned, ✗ M2-200 unassigned
```

#### Example 3: M2 Match + M3 Job

```
TraderC Properties:
- m2Capacity: 2
- Shift: 08:00 - 18:00
- Assigned Jobs: M3 job at 10:00 - 11:30

New M2 Match:
- Match M2-300: 10:00 - 11:30 (30K)

Capacity Check:
- Existing M3 job: 1
- Available capacity: 2 - 1 = 1
- Required for M2 match: 1
- Result: ✓ M2 match assigned (1 + 1 = 2, within capacity)
```

#### Example 4: Different Start Times = Overlap Conflict

```
TraderD Properties:
- m2Capacity: 2
- Shift: 08:00 - 18:00

New M2 Matches:
- Match M2-400: 10:00 - 11:30
- Match M2-401: 10:30 - 12:00 (different start time)

Capacity Check:
- M2-401 has different start time than M2-400
- M2 Capacity does NOT apply (only works with same start time)
- These are treated as time overlaps (conflicts)
- Result: ✗ Can only assign ONE match (not both)

Correct Assignment:
- TraderD → M2-400 (10:00-11:30) ✓
- M2-401 → UNASSIGNED (overlap conflict)
```

### Why M2 Capacity is More Flexible

**Operational Rationale:**

1. **Lower Stakes:** M2 matches (20K-40K) have lower financial risk than M1/MJ
2. **Simpler Betting:** Lower MaxBet indicates less complex betting patterns
3. **Training Opportunities:** Junior traders (Second/Third level) can monitor multiple low-stakes matches
4. **Resource Efficiency:** Maximizes trader utilization during high-volume, low-complexity periods

---

## Trader Work Priority System

M2 uses the **same priority philosophy as M1**: Non-manager traders strongly preferred over managers.

### Trader Level Points

| Level | Points | Typical Role |
|-------|--------|--------------|
| **Elite** | 64 | Senior trader |
| **Member** | 16 | Intermediate trader |
| **First** | 4 | Junior trader |
| **Second** | 2 | Entry-level trader |
| **Third** | 1 | Trainee |

### M2 Work Priority Multipliers

#### Non-Manager Priority

- **Elite Non-Manager:** 2.0x (highest)
- **Member Non-Manager:** 1.3x
- **First Non-Manager:** 1.2x
- **Second Non-Manager:** 1.0x
- **Third Non-Manager:** 0.9x

#### Manager Priority (Very Low)

- **Elite Manager:** 0.010x → Score: 0.64
- **Member Manager:** 0.009x → Score: 0.144
- **First Manager:** 0.008x → Score: 0.032
- **Second Manager:** 0.007x → Score: 0.014
- **Third Manager:** 0.006x → Score: 0.006

**Key Principle:** Even Third non-manager (0.9) > Elite manager (0.64)

### Work Priority Rankings (High to Low)

1. **Elite Non-Manager** (128.0)
2. **Member Non-Manager** (20.8)
3. **First Non-Manager** (4.8)
4. **Second Non-Manager** (2.0)
5. **Third Non-Manager** (0.9)
6. **Elite Manager** (0.64) ← Managers start here
7. **Member Manager** (0.144)
8. **First Manager** (0.032)
9. **Second Manager** (0.014)
10. **Third Manager** (0.006)

---

## Virtual Match Gap Rules

**Same as M1 and MJ:** 15-minute mandatory gap after virtual matches.

**Affected Job Types:**
- PES5MIN
- PES24
- PKIF
- PKCF

**Example:**
```
PES5MIN ends: 10:00
M2 Match starts: 10:15 ✓ (15-minute gap)
M2 Match starts: 10:10 ✗ (only 10 minutes)
```

---

## League Filtering

**Same as M1/MJ:** Traders can specify leagues to avoid via `ignoreLeague` property.

**Rules:**
- Case-insensitive substring matching
- Multiple patterns supported
- Empty/null = works all leagues

---

## EXPT Match Restrictions

**Same as M1/MJ:** EXPT matches require `isEXPTMatchAssignable: true` traders.

**Rules:**
1. EXPT match + non-EXPT trader = Blocked
2. EXPT match + EXPT trader = Allowed
3. Non-EXPT match + any trader = Allowed
4. EXPT traders can work non-EXPT matches

---

## Workload Balancing

### Fair Distribution Logic

M2 strategy includes **workload balancing** to distribute concurrent matches fairly among traders.

#### Scenario 1: Multiple Traders with Capacity

**Rule:** When multiple traders can handle all concurrent matches, distribute fairly rather than assigning all to highest-priority trader.

**Example:**
```
2 M2 Matches at 10:00:
- Match M2-100: 25K
- Match M2-101: 30K

2 Traders:
- EliteTrader (m2Capacity: 2) - Could handle both
- ThirdTrader (m2Capacity: 2) - Could handle both

System Result:
- EliteTrader → M2-101 (30K) ✓ (1 match)
- ThirdTrader → M2-100 (25K) ✓ (1 match)

Why? Fair distribution (1 match each) rather than overloading Elite
```

#### Scenario 2: Only One Trader Can Handle All

**Rule:** When only one trader has sufficient capacity for all concurrent matches, assign all matches to that trader.

**Example:**
```
2 M2 Matches at 10:00:
- Match M2-200: 25K
- Match M2-201: 30K

2 Traders:
- EliteTrader (m2Capacity: 1) - Can only handle 1
- ThirdTrader (m2Capacity: 2) - Can handle both

System Result:
- ThirdTrader → Both matches ✓ (only trader with sufficient capacity)
- EliteTrader → Assigned to other time slots
```

### Workload Balancing Penalties

**System adds penalties for:**
- Traders exceeding "fair share" of concurrent matches
- Uneven distribution when multiple traders have capacity

**Benefits:**
- Prevents single-trader overload
- Improves coverage resilience (if one trader disconnects)
- More balanced workload across team

---

## Real-World Scenarios

### Scenario 1: Peak Hour with Capacity Advantage

**Situation:**
- 3 M2 matches at 10:00 (all overlapping): 22K, 28K, 35K
- 2 traders: Elite (m2Capacity: 2), Third (m2Capacity: 2)

**System Behavior:**

1. **Elite trader** → Assigned 2 matches (35K, 28K) - capacity limit
2. **Third trader** → Assigned 1 match (22K) - remaining match

**Why?**
- Elite handles highest-value matches up to capacity
- Fair distribution applied: Elite gets 2, Third gets 1
- All matches covered efficiently

### Scenario 2: Perfect Capacity Match

**Situation:**

- 3 M2 matches at 14:00: 25K, 30K, 38K
- 2 traders: Elite (m2Capacity: 1), Member (m2Capacity: 2)

**System Behavior:**

1. System calculates: Total capacity = 1 + 2 = 3
2. Exactly enough capacity for all 3 matches
3. **Elite** → 38K match (highest value)
4. **Member** → 30K and 25K matches (next two highest)
5. **All matches covered**

**Key Learning:**

- Total capacity matches number of concurrent matches perfectly
- Higher-priority trader (Elite) assigned to highest-value match
- System optimizes assignment for best overall value coverage

### Scenario 3: M3 Jobs Reduce Available Capacity

**Situation:**

- TraderA (m2Capacity: 2) has 1 M3 job at 10:00
- 2 M2 matches arrive at 10:00: 28K, 35K

**System Behavior:**

1. Calculate available capacity: 2 - 1 (M3 job) = 1
2. Can assign only 1 M2 match
3. **TraderA** → 35K match (highest value)
4. **28K match** → UNASSIGNED

**Key Learning:**
- M3/M4 jobs consume M2 capacity
- Always account for existing jobs when planning M2 coverage

### Scenario 4: Workload Balancing in Action

**Situation:**
- 2 M2 matches at 10:00: 30K, 35K
- 2 traders: Elite (m2Capacity: 2), Third (m2Capacity: 2)

**System Behavior (Fair Distribution):**

1. Both traders can handle both matches
2. System applies workload balancing
3. **Elite** → 35K match (1 match)
4. **Third** → 30K match (1 match)

**Why Not Assign Both to Elite?**
- Fair distribution prevents overload
- If Elite disconnects, only 1 match affected (not both)
- Balanced team workload improves morale

---

## Operational Guidelines

### Before Each Assignment Run

**1. Verify Trader Capacity Settings**

- Default m2Capacity is 2 (if not explicitly set)
- Confirm m2Capacity values are accurate for each trader
- Junior traders (Second/Third): typically m2Capacity = 2
- Experienced traders (Elite/Member): typically m2Capacity = 1-2 (reserved for higher-priority matches)

**2. Review M2 Match Schedule**
- Identify peak concurrent match periods
- Calculate total required capacity: count overlapping matches
- Ensure sufficient total capacity across all traders

**3. Check Assigned Jobs**
- Verify MJ/M1 assignments complete (higher priority)
- Count M2/M3/M4 jobs that reduce available capacity
- Update any changed match times or cancellations

### During Assignment Process

**4. Monitor Optimization Results**
- Check for unassigned M2 matches
- Verify workload distribution is balanced
- Confirm manager workload is minimal

**5. Handle Capacity Warnings**
- **"Insufficient M2 Capacity"** → Add traders or increase capacity settings
- **"Unbalanced workload"** → Review fair distribution (may be acceptable if intended)

### After Assignment

**6. Review Coverage and Capacity Utilization**
- Calculate coverage rate: Assigned / Total Matches
- Check capacity utilization: Concurrent matches per trader
- Identify traders at or near capacity limits

**7. Communicate to Traders**
- Highlight concurrent match assignments clearly
- Flag if trader handling 2+ matches simultaneously
- Confirm trader has adequate monitoring setup for multiple matches

### Best Practices

**M2 Capacity Planning**

- **Junior traders:** m2Capacity = 2 (default, suitable for training)
- **Senior traders:** m2Capacity = 1-2 (reserve capacity for M1/MJ)
- **Monitor stress:** Concurrent matches increase cognitive load

**Workload Balancing**
- Trust the system's fair distribution logic
- Override only when necessary (e.g., specific trader expertise)
- Balance training opportunities with operational needs

---

## Troubleshooting Decision Tree

### Problem: M2 Match Unassigned

**Check 1: Capacity Sufficient?**

→ **NO:** Total trader M2Capacity < concurrent matches
   - **Solution:** Add traders or increase m2Capacity settings

→ **YES:** Continue to Check 2

**Check 2: Traders blocked by higher-priority jobs?**

→ **YES:** MJ/M1 jobs overlap with M2 match
   - **Solution:** Accept (MJ/M1 has priority) or add more traders

→ **NO:** Continue to Check 3

**Check 3: EXPT restriction?**

→ **YES:** Match is EXPT, no EXPT-capable traders available
   - **Solution:** Bring in EXPT-capable trader or remove EXPT flag

→ **NO:** Continue to Check 4

**Check 4: League filter blocking?**

→ **YES:** All traders have match league in ignoreLeague
   - **Solution:** Update trader preferences or add non-filtering traders

→ **NO:** Continue to Check 5

**Check 5: Gap rule violation?**

→ **YES:** All traders have recent virtual jobs (< 15 min)
   - **Solution:** Wait for gap period or reassign virtual jobs

### Problem: Trader Handling Too Many Concurrent Matches

**Check 1: Within m2Capacity?**

→ **YES:** System working as designed
   - **Action:** Monitor trader stress, consider reducing capacity if overloaded

→ **NO:** Bug - report immediately
   - **Action:** Manual reassignment to fix immediate issue

**Check 2: Is workload balanced?**

→ **NO:** Other traders available but underutilized
   - **Possible Cause:** Other traders blocked by constraints
   - **Solution:** Check why other traders not eligible (shift time, league filter, etc.)

---

## API Usage

### Endpoint

```
POST /api/TraderAssignment/optimize
Content-Type: application/json
```

### Request Format

```json
{
  "traderShifts": [
    {
      "id": "1",
      "name": "TraderA",
      "skills": [],
      "level": "Third",
      "isManager": false,
      "start": "2025-09-28T08:00:00Z",
      "end": "2025-09-28T18:00:00Z",
      "m2Capacity": 2,
      "isEXPTMatchAssignable": true,
      "ignoreLeague": [],
      "assignedJobs": [
        {
          "type": "M3",
          "start": "2025-09-28T10:00:00Z",
          "end": "2025-09-28T11:30:00Z"
        }
      ]
    }
  ],
  "matches": [
    {
      "id": "100",
      "matchNo": "M2-100",
      "start": "2025-09-28T10:00:00Z",
      "end": "2025-09-28T12:00:00Z",
      "maxBet": 30000,
      "leagueCode": "SPA",
      "leagueName": "La Liga",
      "isEXPT": false,
      "isAlreadyHandled": false
    }
  ],
  "preconfiguredPairs": []
}
```

### Response Format (Multiple Concurrent Assignments)

```json
{
  "success": true,
  "status": "Optimal",
  "totalMatches": 3,
  "assignedMatches": 3,
  "assignments": [
    {
      "rowNumber": "100",
      "match": "M2-100",
      "maxBet": 35000,
      "hdp": "TraderA",
      "hdpLevel": "Third",
      "ou": "TraderA",
      "ouLevel": "Third",
      "pairType": "Single Trader (M2)",
      "score": 2450000
    },
    {
      "rowNumber": "101",
      "match": "M2-101",
      "maxBet": 28000,
      "hdp": "TraderA",
      "hdpLevel": "Third",
      "ou": "TraderA",
      "ouLevel": "Third",
      "pairType": "Single Trader (M2)",
      "score": 1568000
    },
    {
      "rowNumber": "102",
      "match": "M2-102",
      "maxBet": 22000,
      "hdp": "TraderB",
      "hdpLevel": "Second",
      "ou": "TraderB",
      "ouLevel": "Second",
      "pairType": "Single Trader (M2)",
      "score": 968000
    }
  ]
}
```

**Note:** TraderA assigned to TWO concurrent matches (M2-100 and M2-101) - this is normal for M2 when m2Capacity ≥ 2.

---

## FAQ

### Q1: Can a trader work multiple M2 matches at the same time?

**A:** **YES, but ONLY if they start at the EXACT same time.** This is M2's unique feature. Traders can work multiple M2 matches that start simultaneously, up to their `m2Capacity` limit. Matches with different start times are treated as overlap conflicts.

### Q2: What's the difference between M1 Capacity and M2 Capacity?

**A:**

- **M1 Capacity:** Only allows M3/M4 jobs alongside ONE M1 match (all same start time)
- **M2 Capacity:** Allows M2/M3/M4 jobs AND MULTIPLE M2 matches concurrently (all same start time)
- **Both require exact same start time** - different start times = overlap conflicts

### Q3: Why does M2 allow concurrent matches but M1 doesn't?

**A:** M2 matches have lower stakes (20K-40K) and simpler betting patterns. M1 matches (40K-70K) require more focus, so traders handle only one at a time.

### Q4: How does M2 Capacity count M3/M4 jobs?

**A:** M3 and M4 jobs **consume M2 Capacity**. If a trader has 1 M3 job and m2Capacity = 2, they can handle only 1 additional M2 match.

### Q5: Can M2 Capacity help with MJ or M1 jobs?

**A:** **NO.** MJ and M1 jobs always block M2 matches, regardless of M2 Capacity. M2 Capacity only applies to M2/M3/M4-level work.

### Q6: What happens if no trader has enough capacity?

**A:** Lower-value M2 matches go UNASSIGNED. The system prioritizes higher-MaxBet matches when capacity is insufficient.

### Q7: Why is my Third-level trader handling 3 matches while Elite has none?

**A:** For M2 matches, non-manager traders are strongly preferred over managers (including Elite managers). If Third has capacity and Elite is a manager, Third gets the assignments.

### Q8: Can I manually override workload balancing?

**A:** Yes. The API returns assignments, but operations managers can manually adjust before confirming with traders. However, consider the system's fair distribution logic before overriding.

### Q9: How does the system decide which matches to leave unassigned?

**A:** When capacity is insufficient, the system prioritizes higher-MaxBet matches and leaves the lowest-value matches unassigned.

### Q10: Is m2Capacity required for all traders?

**A:** No. If not specified, the system defaults to `m2Capacity: 2`. However, it's recommended to set it explicitly based on trader experience and capabilities.

---

## Glossary

**M2 (Minor 2) Match:** Lowest-priority real soccer match with MaxBet between 20,000 and 39,999, handled by a single trader for both HDP and OU roles.

**M2 Capacity:** Trader property determining how many M2/M3/M4 jobs can run concurrently. UNIQUE FEATURE: Allows multiple M2 matches simultaneously (unlike M1).

**MJ (Major) Match:** Highest-priority real match with MaxBet ≥ 70,000.

**M1 (Minor 1) Match:** Medium-priority real match with MaxBet 40,000-69,999.

**M3/M4 Match:** Virtual matches (automated) that count toward M2 Capacity.

**Concurrent Matches:** Matches that overlap in time and run simultaneously.

**Workload Balancing:** System logic that distributes concurrent matches fairly among traders with available capacity.

**Fair Share:** Expected number of concurrent matches per trader (total matches ÷ traders with capacity).

**Capacity Utilization:** Percentage of a trader's M2 Capacity being used (assigned concurrent jobs ÷ m2Capacity × 100%).

**Overlapping Group:** Set of matches that all overlap with each other in time, requiring capacity management.

**Coverage Rate:** Percentage of M2 matches successfully assigned (assigned matches ÷ total matches × 100%).

---

## Document History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-01-XX | Initial document creation |

**Maintained by:** Operations Team
**Last Updated:** January 2025
**Next Review:** June 2025
