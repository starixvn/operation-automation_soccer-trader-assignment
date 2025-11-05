# M1 (Minor 1) Match Assignment Rules

## Document Overview

This document explains how the system assigns traders to **Minor 1 (M1) matches** using constraint-based optimization. It covers match classification, trader qualifications, work priority rules, concurrent job handling, and operational guidelines.

**Target Audience:** Operations managers, trading supervisors, and system administrators responsible for M1 match coverage.

---

## Table of Contents

1. [Match Classification](#match-classification)
2. [Assignment Model](#assignment-model)
3. [Core Constraints](#core-constraints)
4. [Trader Work Priority System](#trader-work-priority-system)
5. [M1 Capacity and Concurrent Jobs](#m1-capacity-and-concurrent-jobs)
6. [Virtual Match Gap Rules](#virtual-match-gap-rules)
7. [League Filtering](#league-filtering)
8. [EXPT Match Restrictions](#expt-match-restrictions)
9. [Work Assignment Priority Hierarchy](#work-assignment-priority-hierarchy)
10. [Real-World Scenarios](#real-world-scenarios)
11. [Operational Guidelines](#operational-guidelines)
12. [Troubleshooting Decision Tree](#troubleshooting-decision-tree)
13. [API Usage](#api-usage)
14. [FAQ](#faq)
15. [Glossary](#glossary)

---

## Match Classification

### M1 Match Criteria

**MaxBet Range:** 40,000 - 69,999

**Key Characteristics:**
- **Single trader** handles both HDP and OU roles
- Lower priority than MJ matches (Major matches take precedence)
- Higher priority than M2/M3/M4 matches
- No skill-specific requirements (any trader can work M1)

**Valid MaxBet Examples:**
- 40,000 ✓ (minimum)
- 55,000 ✓
- 69,999 ✓ (maximum)
- 39,999 ✗ (too low - below M1 range)
- 70,000 ✗ (too high - enters M2 range)

---

## Assignment Model

### Single Trader Model

Unlike MJ matches (which require HDP + OU pairs), **M1 matches are handled by a single versatile trader**:

```
M1 Match (50K MaxBet)
├─ HDP Role → TraderA
└─ OU Role → TraderA (same trader)
```

**Why Single Trader?**
- M1 matches have moderate complexity and lower stake limits
- More efficient resource utilization
- Allows experienced traders to work independently
- Frees up elite traders for higher-priority MJ matches

### Decision Variables

The optimization engine creates binary decision variables:

```
x[traderId, matchId] ∈ {0, 1}

Where:
  1 = trader is assigned to this M1 match (handles both roles)
  0 = trader is not assigned to this M1 match
```

---

## Core Constraints

The system enforces 2 fundamental constraints to ensure valid assignments:

### Constraint 1: One Trader Per Match (Maximum)

**Rule:** Each M1 match can have at most 1 trader assigned.

**Rationale:**
- M1 matches require only one trader (handles both HDP and OU)
- Prevents duplicate assignments
- Allows partial assignments when resources are limited

**Formula:**
```
For each match m:
  Σ x[trader, m] ≤ 1
  (Sum across all traders ≤ 1)
```

**Example:**
```
Match M1-100 (50K MaxBet, 10:00-12:00)
├─ TraderA: x[A, M1-100] = 1 ✓ (assigned)
├─ TraderB: x[B, M1-100] = 0
└─ TraderC: x[C, M1-100] = 0

Result: Match assigned to TraderA (both HDP and OU)
```

### Constraint 2: No Overlapping Matches Per Trader

**Rule:** A trader cannot work on two M1 matches that overlap in time.

**Rationale:**
- Physical limitation: one trader can only monitor one match at a time
- Ensures trader focus and match quality
- M1Capacity only applies to M3/M4 jobs, NOT to multiple M1 matches

**Formula:**
```
For each trader t and overlapping match pair (m1, m2):
  x[t, m1] + x[t, m2] ≤ 1
  (Trader can work at most ONE of the two overlapping matches)
```

**Example:**
```
TraderA Schedule:
├─ Match M1-100: 10:00-12:00 (assigned) ✓
└─ Match M1-101: 11:00-13:00 (overlaps) ✗ Cannot assign

Reason: M1-101 overlaps with M1-100 by 1 hour
```

**Important Note:** Even if a trader has M1Capacity=2 or higher, they can still only work **ONE M1 match** at any given time. M1Capacity only determines how many M3/M4 virtual jobs can run alongside an M1 match (see [M1 Capacity](#m1-capacity-and-concurrent-jobs)).

---

## Trader Work Priority System

The system prioritizes traders based on **experience level** and **manager status** to optimize match coverage.

### M1-Specific Priority Philosophy

**Key Principle:** For M1 matches (moderate complexity, lower stakes), the system **prefers non-manager traders over managers**, even if managers have higher experience levels.

**Why?**
- Reserves managers for supervisory duties and crisis management
- Develops non-manager traders' skills on moderate-complexity matches
- Reduces manager workload on routine trading operations
- Elite/Member/First non-managers gain valuable experience

### Trader Level Points

Experience levels determine base work capacity:

| Level | Points | Typical Role |
|-------|--------|--------------|
| **Elite** | 64 | Senior trader with extensive experience |
| **Member** | 16 | Intermediate trader with solid fundamentals |
| **First** | 4 | Junior trader with basic competency |
| **Second** | 2 | Entry-level trader in training |
| **Third** | 1 | Trainee or temporary staff |

**Work capacity increases exponentially with level** to reflect skill differences.

### M1 Work Priority Multipliers

The system applies different multipliers for managers vs. non-managers:

#### Non-Manager Priority (Regular Staff)

**Elite Non-Manager:** 2.0x
- **Highest priority** for M1 matches
- Handles matches efficiently and independently
- Frees up managers for supervisory duties

**Member Non-Manager:** 1.3x
- Strong priority for M1 assignments
- Builds experience on moderate-complexity matches

**First Non-Manager:** 1.2x
- Good opportunity for skill development
- Handles M1 matches with proven competency

**Second Non-Manager:** 1.0x
- Baseline priority for entry-level traders
- Can cover M1 matches when needed

**Third Non-Manager:** 0.9x
- Lower priority but still eligible
- May be assigned when higher-level staff unavailable

#### Manager Priority (Supervisory Staff)

**Critical Rule:** ALL manager traders have **very low priority** for M1 matches, regardless of experience level. This ensures managers remain available for supervisory duties.

**Elite Manager:** 0.010x (10x **lower** than Elite non-manager)
- **Work Score Example:** 64 × 0.010 = 0.64
- Lower than Third non-manager (1 × 0.9 = 0.9)

**Member Manager:** 0.009x
- **Work Score Example:** 16 × 0.009 = 0.144

**First Manager:** 0.008x
- **Work Score Example:** 4 × 0.008 = 0.032

**Second Manager:** 0.007x
- **Work Score Example:** 2 × 0.007 = 0.014

**Third Manager:** 0.006x
- **Work Score Example:** 1 × 0.006 = 0.006 (lowest priority)

### Work Priority Rankings (High to Low)

**Complete ranking for M1 match assignments:**

1. **Elite Non-Manager** (128.0) - Highest Priority
2. **Member Non-Manager** (20.8)
3. **First Non-Manager** (4.8)
4. **Second Non-Manager** (2.0)
5. **Third Non-Manager** (0.9)
6. **Elite Manager** (0.64) ← Managers start here
7. **Member Manager** (0.144)
8. **First Manager** (0.032)
9. **Second Manager** (0.014)
10. **Third Manager** (0.006) - Lowest Priority

**Key Insight:** The system ensures that even a **Third non-manager** (score: 0.9) will be preferred over an **Elite manager** (score: 0.64) for M1 matches, demonstrating the strong preference for keeping managers available for supervisory work.

### Operational Reasoning

**Why Non-Managers First?**

1. **Manager Availability:** Keeps experienced managers available for:
   - Handling escalations and disputes
   - Supervising multiple traders simultaneously
   - Managing high-stakes MJ matches (where managers have higher priority)
   - Training and mentoring junior staff

2. **Skill Development:** Provides non-manager traders with:
   - Hands-on experience with moderate-complexity matches
   - Independent decision-making opportunities
   - Path to promotion and skill advancement

3. **Resource Efficiency:** Optimizes workforce by:
   - Using appropriate skill levels for match complexity
   - Preventing over-qualification (Elite managers on routine M1 matches)
   - Balancing workload across all experience levels

4. **Coverage Flexibility:** If no non-managers are available:
   - System automatically falls back to manager traders
   - Elite managers take priority over lower-level managers
   - Ensures all matches can be covered when needed

---

## M1 Capacity and Concurrent Jobs

### What is M1 Capacity?

**M1 Capacity** is a trader-specific property that determines **how many M3/M4 virtual jobs can run concurrently with an M1 match** at the same start time.

**Common Values:**
- `m1Capacity: 1` - Cannot run M1 match alongside M3/M4 jobs
- `m1Capacity: 2` - Can run 1 M3/M4 job alongside 1 M1 match
- `m1Capacity: 3` - Can run 2 M3/M4 jobs alongside 1 M1 match

### Critical Rule: M1 Capacity ONLY Applies to M3/M4 Jobs

**M1 Capacity does NOT allow:**
- Multiple M1 matches at the same time ✗
- Multiple M2 matches at the same time ✗
- M1 match alongside MJ match ✗
- M1 match alongside M2 match ✗

**M1 Capacity DOES allow:**
- M1 match + M3 job (same start time) ✓
- M1 match + M4 job (same start time) ✓

### Capacity Calculation Rules

#### Rule 1: Same Start Time Required

**M3/M4 jobs can ONLY run alongside M1 matches if they start at the EXACT same time.**

**Example - Valid Concurrent Work:**
```
TraderA (m1Capacity: 2)
├─ M3 Job:  10:00 - 11:30
└─ M1 Match: 10:00 - 11:30 ✓ (same start time)
```

**Example - Invalid Concurrent Work:**
```
TraderA (m1Capacity: 2)
├─ M3 Job:  10:00 - 12:00
└─ M1 Match: 10:30 - 12:00 ✗ (different start time - treated as overlap)
```

#### Rule 2: Capacity Check

**Formula:**
```
Required Capacity = (Number of concurrent M3/M4 jobs) + 1

Example:
- 1 M3 job + 1 M1 match = requires m1Capacity ≥ 2
- 2 M3 jobs + 1 M1 match = requires m1Capacity ≥ 3
```

### Concurrent Assignment Examples

#### Example 1: Capacity Allows Assignment

```
TraderA Properties:
- m1Capacity: 2
- Shift: 08:00 - 18:00

Assigned Jobs:
- M3 Job: 10:00 - 11:30

New M1 Match:
- Match M1-100: 10:00 - 11:30

Capacity Check:
- Concurrent M3/M4 jobs with same start time: 1
- Required capacity: 1 + 1 = 2
- Trader's M1Capacity: 2
- Result: ✓ Assignment allowed
```

#### Example 2: Capacity Blocks Assignment

```
TraderB Properties:
- m1Capacity: 1
- Shift: 08:00 - 18:00

Assigned Jobs:
- M3 Job: 14:00 - 15:30

New M1 Match:
- Match M1-200: 14:00 - 15:30

Capacity Check:
- Concurrent M3/M4 jobs with same start time: 1
- Required capacity: 1 + 1 = 2
- Trader's M1Capacity: 1
- Result: ✗ Insufficient capacity (needs m1Capacity ≥ 2)
```

#### Example 3: Multiple M3 Jobs Exceed Capacity

```
TraderC Properties:
- m1Capacity: 2
- Shift: 08:00 - 18:00

Assigned Jobs:
- M3 Job 1: 10:00 - 11:30
- M3 Job 2: 10:00 - 12:00

New M1 Match:
- Match M1-300: 10:00 - 11:30

Capacity Check:
- Concurrent M3/M4 jobs with same start time: 2
- Required capacity: 2 + 1 = 3
- Trader's M1Capacity: 2
- Result: ✗ Insufficient capacity (needs m1Capacity ≥ 3)
```

#### Example 4: Different Start Time = Overlap

```
TraderD Properties:
- m1Capacity: 3
- Shift: 08:00 - 18:00

Assigned Jobs:
- M3 Job: 10:00 - 12:00

New M1 Match:
- Match M1-400: 10:30 - 12:00

Analysis:
- Start times differ (10:00 vs. 10:30)
- Match overlaps with M3 job
- Capacity logic does NOT apply (requires same start time)
- Result: ✗ Blocked due to time overlap
```

### Job Types and M1 Capacity

| Job Type | Can Run Alongside M1? | Requires M1 Capacity? |
|----------|----------------------|----------------------|
| **M3** | Yes (if same start time) | Yes |
| **M4** | Yes (if same start time) | Yes |
| **MJ** | No (always blocks) | No |
| **M1** | No (always blocks) | No |
| **M2** | No (always blocks) | No |
| **PES5MIN** | No (15-min gap required) | No |
| **PES24** | No (15-min gap required) | No |
| **PKIF** | No (15-min gap required) | No |
| **PKCF** | No (15-min gap required) | No |

### Why M1 Capacity Exists

**Operational Rationale:**

1. **Virtual Match Nature:** M3/M4 are automated virtual matches that require minimal trader intervention
2. **Multitasking Capability:** Experienced traders can monitor virtual matches while working M1 real matches
3. **Resource Optimization:** Allows efficient use of trader time during overlapping schedules
4. **Risk Management:** Real matches (M1/M2/MJ) require full attention, but virtual matches (M3/M4) can run in background

**Example Real-World Scenario:**
```
TraderA (Elite, m1Capacity: 2) at 10:00:
├─ M3 Virtual Match (automated, minimal monitoring)
└─ M1 Real Match (active trading, primary focus)

This is acceptable because:
- M3 runs automatically with occasional checks
- TraderA's primary focus is on M1 real match
- Both jobs start at same time (structured scheduling)
```

---

## Virtual Match Gap Rules

### 15-Minute Gap Requirement

**Rule:** After completing certain virtual match types, traders must have a **15-minute gap** before starting the next M1 match.

**Affected Job Types:**
- **PES5MIN** - 5-minute PES (Pro Evolution Soccer) virtual matches
- **PES24** - 24-hour PES league virtual matches
- **PKIF** - Penalty Kick (International Friendly) virtual matches
- **PKCF** - Penalty Kick (Club Friendly) virtual matches

### Gap Rule Logic

**Valid Assignment:**
```
TraderA Schedule:
├─ PES5MIN Job: 09:00 - 10:00 (ends at 10:00)
├─ Gap: 10:00 - 10:15 (15 minutes)
└─ M1 Match: 10:15 - 12:15 ✓ (starts at 10:15, gap ≥ 15 minutes)
```

**Invalid Assignment:**
```
TraderB Schedule:
├─ PKIF Job: 09:00 - 10:00 (ends at 10:00)
├─ Gap: 10:00 - 10:10 (only 10 minutes)
└─ M1 Match: 10:10 - 12:10 ✗ (gap < 15 minutes)
```

### Edge Cases

#### Exactly 15 Minutes

**Allowed:** A gap of exactly 15 minutes is valid.

```
PES24 ends: 10:00
M1 Match starts: 10:15
Gap: 15 minutes ✓
```

#### Non-Virtual Jobs

**No Gap Required:** Regular jobs (MJ, M1, M2, M3, M4, PESCF, etc.) do not require a 15-minute gap.

```
TraderC Schedule:
├─ MJ Match: 09:00 - 11:00 (ends at 11:00)
└─ M1 Match: 11:00 - 13:00 ✓ (starts immediately, no gap required)
```

#### Multiple Virtual Jobs

**Each Job Checked Independently:**

```
TraderD Schedule:
├─ PES5MIN: 08:00 - 08:05 (ends 08:05)
├─ PKIF: 09:00 - 09:30 (ends 09:30)
└─ M1 Match: 09:45 - 11:45 ✓ (15 min gap after PKIF)
```

### Why 15-Minute Gap?

**Operational Reasons:**

1. **Recovery Time:** Traders need a break after fast-paced virtual matches
2. **Focus Transition:** Switch from automated virtual monitoring to active real match trading
3. **System Preparation:** Time to review upcoming M1 match details and prepare strategy
4. **Error Prevention:** Reduces fatigue-related mistakes in real match trading

---

## League Filtering

### IgnoreLeague Property

Traders can specify leagues they prefer not to work using the `ignoreLeague` property.

**Example:**
```json
{
  "name": "TraderA",
  "level": "Elite",
  "ignoreLeague": ["EPL", "Premier"]
}
```

### Filtering Rules

**Rule 1: Substring Matching (Case-Insensitive)**

The system checks if any `ignoreLeague` pattern appears **anywhere** in the match's `leagueName`.

**Example:**
```
Trader ignores: ["EPL"]

Match League: "EPL Championship" → Blocked (contains "EPL")
Match League: "English Premier League" → Allowed (doesn't contain exact "EPL")
Match League: "epl asia" → Blocked (case-insensitive match)
```

**Rule 2: Multiple Ignore Patterns**

Traders can ignore multiple league patterns:

```
Trader ignores: ["EPL", "La Liga", "Championship"]

Match 1: "EPL U21" → Blocked
Match 2: "Spanish La Liga" → Blocked
Match 3: "Serie A" → Allowed
Match 4: "Championship Playoff" → Blocked
```

**Rule 3: Empty or Null Ignore List**

If `ignoreLeague` is `null` or `[]`, the trader can work **all leagues**.

**Rule 4: Match with No League Name**

If a match has no `leagueName` (null or empty), it **cannot be filtered** and is always available to all traders.

### Operational Use Cases

#### Use Case 1: Regional Specialization

```
TraderA (Asia-Pacific specialist):
- ignoreLeague: ["EPL", "La Liga", "Serie A", "Bundesliga"]
- Works: Asian leagues, minor European leagues

TraderB (European specialist):
- ignoreLeague: ["J-League", "K-League", "A-League"]
- Works: Major European leagues
```

#### Use Case 2: Conflict Avoidance

```
TraderC:
- ignoreLeague: ["La Liga"]
- Reason: Supports Barcelona, prefers not to trade their matches

TraderD:
- ignoreLeague: ["Premier League"]
- Reason: Previously worked for EPL team, avoiding conflict of interest
```

#### Use Case 3: Multiple Traders for Same Match

```
Match: "English Premier League - Arsenal vs Chelsea"
├─ TraderA (ignoreLeague: ["EPL", "Premier"]) → Blocked
├─ TraderB (ignoreLeague: ["La Liga"]) → Available
└─ TraderC (ignoreLeague: null) → Available

Result: System assigns TraderB or TraderC based on priority
```

---

## EXPT Match Restrictions

### What are EXPT Matches?

**EXPT Matches** are special matches that require traders with **extra experience or training** to handle properly.

**Key Properties:**
- Match has `isEXPT: true`
- Trader has `isEXPTMatchAssignable: true/false`

### Assignment Rules

#### Rule 1: EXPT Match + Non-EXPT Trader = Blocked

```
Match M1-100:
- isEXPT: true
- MaxBet: 50,000

TraderA:
- isEXPTMatchAssignable: false

Result: ✗ TraderA cannot be assigned to M1-100
```

#### Rule 2: EXPT Match + EXPT Trader = Allowed

```
Match M1-200:
- isEXPT: true
- MaxBet: 55,000

TraderB:
- isEXPTMatchAssignable: true

Result: ✓ TraderB can be assigned to M1-200
```

#### Rule 3: Non-EXPT Match + Any Trader = Allowed

```
Match M1-300:
- isEXPT: false
- MaxBet: 45,000

TraderC:
- isEXPTMatchAssignable: false

Result: ✓ TraderC can be assigned to M1-300 (EXPT restriction doesn't apply)
```

#### Rule 4: EXPT Traders Can Work Non-EXPT Matches

```
Match M1-400:
- isEXPT: false

TraderD:
- isEXPTMatchAssignable: true

Result: ✓ TraderD can work non-EXPT matches (no restriction)
```

### Operational Examples

#### Example 1: Mixed Trader Pool

```
EXPT Match M1-500 (isEXPT: true, 60K MaxBet)

Available Traders:
├─ TraderA (Elite, isEXPTMatchAssignable: true) → Eligible ✓
├─ TraderB (Member, isEXPTMatchAssignable: false) → Blocked ✗
└─ TraderC (First, isEXPTMatchAssignable: true) → Eligible ✓

System assigns: TraderA (higher level priority)
```

#### Example 2: No EXPT Traders Available

```
EXPT Match M1-600 (isEXPT: true, 50K MaxBet)

Available Traders:
├─ TraderX (Elite, isEXPTMatchAssignable: false) → Blocked
├─ TraderY (Member, isEXPTMatchAssignable: false) → Blocked
└─ TraderZ (First, isEXPTMatchAssignable: false) → Blocked

System result: Match goes UNASSIGNED
Warning: "No EXPT-capable traders available for M1-600"
```

#### Example 3: EXPT Trader Saves the Day

```
Peak Hour Scenario:

Non-EXPT Matches:
├─ M1-701: Assigned to TraderA (non-EXPT capable)
├─ M1-702: Assigned to TraderB (non-EXPT capable)
└─ M1-703: Assigned to TraderC (non-EXPT capable)

EXPT Match arrives:
- M1-704 (isEXPT: true, 65K MaxBet)

Available EXPT Trader:
- TraderD (Elite, isEXPTMatchAssignable: true) ✓

Result: TraderD handles M1-704, all matches covered
```

### Why EXPT Restrictions Exist

**Operational Rationale:**

1. **Risk Management:** EXPT matches may involve:
   - Higher-risk betting scenarios
   - More complex rule exceptions
   - Sensitive markets requiring regulatory compliance

2. **Skill Requirements:** EXPT-capable traders have:
   - Advanced training on exception handling
   - Experience with complex match scenarios
   - Knowledge of special regulatory requirements

3. **Quality Assurance:** Restricting EXPT matches to trained traders ensures:
   - Consistent handling of special cases
   - Reduced error rates on complex matches
   - Better customer service for high-value bets

---

## Work Assignment Priority Hierarchy

The system considers **6 factors** when deciding which trader works which match:

### 1. Shift Time Coverage (Eligibility Filter)

**Priority: CRITICAL - Must Pass**

Match must be completely within trader's shift time.

```
Trader Shift: 08:00 - 16:00
Match 1: 10:00 - 12:00 → ✓ Eligible
Match 2: 15:00 - 17:00 → ✗ Blocked (extends past shift end)
```

### 2. League Filtering (Eligibility Filter)

**Priority: CRITICAL - Must Pass**

Match league must not be in trader's ignore list.

```
Trader ignores: ["EPL"]
Match: "EPL Championship" → ✗ Blocked
```

### 3. EXPT Match Restriction (Eligibility Filter)

**Priority: CRITICAL - Must Pass**

EXPT matches require EXPT-capable traders.

```
Match: isEXPT = true
Trader: isEXPTMatchAssignable = false → ✗ Blocked
```

### 4. Assigned Jobs Conflict Check (Eligibility Filter)

**Priority: CRITICAL - Must Pass**

Match must not overlap with existing assigned jobs (unless allowed by M1 Capacity for M3/M4).

```
Assigned Job: MJ 10:00 - 12:00
M1 Match: 11:00 - 13:00 → ✗ Blocked (overlap)
```

### 5. Trader Level and Manager Status (Optimization)

**Priority: HIGH - Affects Assignment**

Non-manager traders strongly preferred over managers.

```
Elite Non-Manager: 128.0 work score
Elite Manager: 0.64 work score
→ Non-manager assigned even though both are Elite level
```

### 6. Match MaxBet Value (Optimization)

**Priority: MEDIUM - Fine-Tuning**

Higher MaxBet matches amplify trader priority differences.

```
Trader Score: 20.8
Match 1 (40K): 20.8 × (40)² = 33,280
Match 2 (65K): 20.8 × (65)² = 87,880
→ System assigns higher-priority traders to higher MaxBet matches
```

### Priority Application Flow

```
┌─────────────────────────────┐
│ 1. Shift Time Coverage?    │
│    NO → Reject Trader       │
│    YES ↓                    │
└─────────────────────────────┘
         │
         ↓
┌─────────────────────────────┐
│ 2. League Filtering?        │
│    Ignored → Reject Trader  │
│    OK ↓                     │
└─────────────────────────────┘
         │
         ↓
┌─────────────────────────────┐
│ 3. EXPT Restriction?        │
│    Blocked → Reject Trader  │
│    OK ↓                     │
└─────────────────────────────┘
         │
         ↓
┌─────────────────────────────┐
│ 4. Assigned Jobs Conflict?  │
│    Conflict → Reject Trader │
│    OK ↓                     │
└─────────────────────────────┘
         │
         ↓
┌─────────────────────────────┐
│ 5. Calculate Work Priority  │
│    Level × Priority × MaxBet│
│    Store Score ↓            │
└─────────────────────────────┘
         │
         ↓
┌─────────────────────────────┐
│ 6. Optimization Engine      │
│    Maximize Total Scores    │
│    Assign Traders to Matches│
└─────────────────────────────┘
```

---

## Real-World Scenarios

### Scenario 1: Peak Hour with Limited Traders

**Situation:**
- 4 overlapping M1 matches (10:00-12:00)
- MaxBets: 42K, 52K, 61K, 68K
- 3 available traders: Elite non-manager, Member manager, Third non-manager

**System Behavior:**

1. **Elite non-manager** → Assigned to 68K match (highest MaxBet)
2. **Third non-manager** → Assigned to 61K match (second highest)
3. **Member manager** → Assigned to 52K match (third highest)
4. **42K match** → UNASSIGNED (insufficient traders)

**Why?**
- Elite handles highest-value match (optimal skill utilization)
- Third non-manager preferred over Member manager (manager priority rules)
- System prioritizes coverage of higher-value matches

**Operational Response:**
- Alert: "Match M1-100 (42K) unassigned - need additional trader coverage"
- Manager can manually reassign or bring in additional staff

### Scenario 2: EXPT Match Arrives During Peak

**Situation:**
- 3 non-EXPT matches already assigned
- EXPT match arrives (65K MaxBet, isEXPT: true)
- Only 1 EXPT-capable trader available: Elite manager

**System Behavior:**

1. Non-EXPT matches: Continue with assigned non-manager traders
2. EXPT match: **Elite manager assigned** (only EXPT-capable trader available)

**Why?**
- EXPT requirement overrides manager priority rules
- Elite manager's EXPT capability is critical for this match
- Non-manager traders blocked by EXPT restriction

**Key Learning:**
- EXPT restrictions are eligibility filters, not optimization factors
- System always respects eligibility constraints, even if it means using managers

### Scenario 3: M3 Job Concurrent Assignment

**Situation:**
- TraderA (m1Capacity: 2) has M3 job at 14:00-15:30
- M1 match arrives: 14:00-15:30 (same start time, 50K MaxBet)
- No other traders available

**System Behavior:**

1. Check M1 Capacity: m1Capacity = 2 ✓
2. Check Start Time: M3 and M1 both start at 14:00 ✓
3. Calculate Required Capacity: 1 M3 + 1 M1 = 2 ✓
4. **Assign TraderA to M1 match** (M3 job continues in background)

**Why?**
- TraderA's capacity allows concurrent work
- Same start time enables structured multitasking
- M3 job is automated, requires minimal monitoring

**Operational Note:**
- TraderA now monitoring both M3 virtual and M1 real match
- Supervisor should verify TraderA is not overloaded

### Scenario 4: Virtual Match Gap Violation

**Situation:**
- TraderB finishes PES5MIN job at 10:00
- M1 match available: 10:10-12:10 (only 10-minute gap)
- No other traders available

**System Behavior:**

1. Check Gap: 10:10 - 10:00 = 10 minutes
2. Required Gap: 15 minutes
3. Gap Violation: 10 < 15 ✗
4. **Match goes UNASSIGNED**

**Why?**
- 15-minute gap is mandatory for trader recovery
- System enforces rest period for focus and accuracy
- Better to leave match unassigned than risk trader fatigue errors

**Operational Response:**
- Alert: "Match M1-200 unassigned due to gap rule violation"
- Options: Wait 5 more minutes, or find another trader without recent virtual jobs

---

## Operational Guidelines

### Before Each Assignment Run

**1. Verify Trader Availability**
- Confirm shift times are accurate
- Check that EXPT-capable traders are scheduled for EXPT match coverage
- Update ignoreLeague preferences if traders have new restrictions

**2. Review M1 Match Schedule**
- Identify EXPT matches that require special handling
- Check for peak hours with multiple overlapping matches
- Flag high-MaxBet matches (>60K) for priority coverage

**3. Check Assigned Jobs**
- Ensure MJ assignments are complete (MJ takes priority)
- Verify virtual match schedules (for M1 Capacity and gap rule planning)
- Update any changed match times or cancellations

### During Assignment Process

**4. Monitor Optimization Results**
- Check for unassigned matches
- Verify high-MaxBet matches are assigned to experienced traders
- Confirm manager workload is minimal (unless EXPT matches require them)

**5. Handle Warnings**
- **"Insufficient M1 Capacity"** → Trader needs higher capacity or fewer concurrent jobs
- **"Gap rule violation"** → Reschedule match or find trader without recent virtual jobs
- **"No EXPT traders available"** → Escalate to bring in EXPT-capable staff

### After Assignment

**6. Review Coverage**
- Calculate coverage rate: Assigned / Total Matches
- Identify unassigned matches and assign manually if needed
- Check manager utilization (should be low for M1 matches)

**7. Communicate to Traders**
- Send assignments with match details (time, league, MaxBet)
- Flag EXPT matches clearly
- Confirm concurrent M3/M4 jobs if applicable

**8. Prepare for Real-Time Adjustments**
- Keep backup traders on standby
- Monitor for late match changes or trader unavailability
- Have manager approval process for emergency reassignments

### Best Practices

**Trader Development**
- Assign Third/Second level traders to lower-MaxBet M1 matches for training
- Gradually increase complexity as traders gain experience
- Track trader performance to inform future priority adjustments

**Manager Utilization**
- Reserve managers for supervisory duties and EXPT matches
- Use manager assignments as last resort for non-EXPT M1 matches
- Balance manager workload across all match types (MJ, M1, M2)

**M1 Capacity Planning**
- Assess which traders can handle concurrent M3/M4 jobs
- Increase m1Capacity for experienced traders to improve efficiency
- Monitor trader stress levels when running multiple concurrent jobs

**EXPT Match Coverage**
- Maintain sufficient EXPT-capable traders on every shift
- Schedule EXPT traders strategically to cover peak EXPT match hours
- Train additional traders on EXPT requirements to expand pool

---

## Troubleshooting Decision Tree

### Problem: Match Unassigned

**Check 1: Are there any eligible traders?**

→ **NO:** No traders available during match time
   - **Solution:** Add traders to shift, or extend existing trader shifts

→ **YES:** Continue to Check 2

**Check 2: Is the match EXPT?**

→ **YES:** Match requires isEXPTMatchAssignable = true
   - **Check 2a:** Do any eligible traders have EXPT capability?
     - **NO:** Bring in EXPT-capable trader or remove EXPT requirement
     - **YES:** Continue to Check 3

→ **NO:** Continue to Check 3

**Check 3: Are eligible traders blocked by assigned jobs?**

→ **YES:** Existing MJ/M1/M2 jobs overlap with this match
   - **Check 3a:** Can M1 Capacity help? (Only if blocking jobs are M3/M4 with same start time)
     - **NO:** Reassign other jobs or add more traders
     - **YES:** Increase trader's m1Capacity

→ **NO:** Continue to Check 4

**Check 4: Are eligible traders blocked by gap rule?**

→ **YES:** Traders have recent PES/PK jobs within 15 minutes
   - **Solution:** Wait for gap period to pass, or reassign virtual jobs

→ **NO:** Continue to Check 5

**Check 5: Are eligible traders blocked by league filter?**

→ **YES:** All traders have match league in ignoreLeague list
   - **Solution:** Update trader preferences or add traders who can work this league

→ **NO:** Continue to Check 6

**Check 6: System may have prioritized other overlapping matches**

→ **Solution:** Check if higher-MaxBet matches at same time took priority
   - Add more traders to cover all overlapping matches

### Problem: Manager Assigned to M1 Match

**Check 1: Are there non-manager traders available?**

→ **NO:** All traders are managers
   - **Expected Behavior:** Manager assignment is correct (no alternative)

→ **YES:** Continue to Check 2

**Check 2: Is the match EXPT?**

→ **YES:** Manager may be the only EXPT-capable trader
   - **Check 2a:** Do non-managers have EXPT capability?
     - **NO:** Manager assignment is correct (EXPT requirement)
     - **YES:** Bug - report to system administrator

→ **NO:** Continue to Check 3

**Check 3: Are non-managers blocked by assigned jobs?**

→ **YES:** Non-managers have overlapping MJ/M1/M2 jobs
   - **Expected Behavior:** Manager assignment is correct (no alternative)

→ **NO:** Bug - report to system administrator

### Problem: Low-Level Trader Assigned to High-MaxBet Match

**Check 1: What is the match MaxBet?**

→ **60K-69K:** High-value M1 match
   - **Check 1a:** Are Elite/Member traders available?
     - **NO:** Expected behavior (use available traders)
     - **YES:** Continue to Check 2

**Check 2: Do Elite/Member traders have overlapping matches?**

→ **YES:** Higher-priority traders are busy with other matches
   - **Solution:** Add more Elite/Member traders to shift for better coverage

→ **NO:** Continue to Check 3

**Check 3: Are Elite/Member traders blocked by other constraints?**

→ **YES:** Check EXPT, league filter, assigned jobs, gap rules
   - **Solution:** Resolve blocking constraints or adjust match schedule

→ **NO:** Bug - report to system administrator

### Problem: Trader Assigned to Two Overlapping Matches

**This should NEVER happen**

→ **Bug:** Constraint 2 violation - report to system administrator immediately
   - Provide: Trader ID, Match IDs, Match times
   - Manually reassign one match to different trader to prevent operational failure

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
      "level": "Elite",
      "isManager": false,
      "start": "2025-09-28T08:00:00Z",
      "end": "2025-09-28T18:00:00Z",
      "m1Capacity": 2,
      "isEXPTMatchAssignable": true,
      "ignoreLeague": ["EPL"],
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
      "matchNo": "M1-100",
      "start": "2025-09-28T10:00:00Z",
      "end": "2025-09-28T12:00:00Z",
      "maxBet": 50000,
      "leagueCode": "EPL",
      "leagueName": "English Premier League",
      "isEXPT": false,
      "isAlreadyHandled": false
    }
  ],
  "preconfiguredPairs": []
}
```

### Response Format (Success)

```json
{
  "success": true,
  "status": "Optimal",
  "totalMatches": 1,
  "assignedMatches": 1,
  "assignments": [
    {
      "rowNumber": "100",
      "match": "M1-100",
      "league": "EPL",
      "maxBet": 50000,
      "time": "09-28 10:00 - 12:00",
      "hdp": "TraderA",
      "hdpLevel": "Elite",
      "ou": "TraderA",
      "ouLevel": "Elite",
      "pairType": "Single Trader (M1)",
      "score": 8320000
    }
  ],
  "summary": {
    "primaryPairs": 0,
    "secondaryPairs": 0,
    "backupPairs": 0,
    "preconfiguredPairsUsed": 0
  }
}
```

### Response Format (Partial Assignment)

```json
{
  "success": true,
  "status": "Optimal",
  "totalMatches": 2,
  "assignedMatches": 1,
  "assignments": [
    {
      "rowNumber": "100",
      "match": "M1-100",
      "maxBet": 50000,
      "hdp": "TraderA",
      "ou": "TraderA",
      "pairType": "Single Trader (M1)",
      "score": 8320000
    },
    {
      "rowNumber": "101",
      "match": "M1-101",
      "maxBet": 60000,
      "hdp": "N/A",
      "ou": "N/A",
      "pairType": "❌ UNASSIGNED",
      "score": 0
    }
  ]
}
```

### Validation Errors

**Invalid MaxBet Range:**
```json
{
  "success": false,
  "error": "Found 2 matches outside M1 range (40K-70K). All M1 matches must have MaxBet between 40,000 and 70,000."
}
```

---

## FAQ

### Q1: Why was a Third-level trader assigned instead of an Elite manager?

**A:** For M1 matches, the system strongly prefers non-manager traders over managers, regardless of experience level. This keeps managers available for supervisory duties, MJ matches, and EXPT match coverage.

### Q2: Can a trader work two M1 matches at the same time?

**A:** No. Traders can only work **one M1 match** at any given time, regardless of their M1 Capacity. M1 Capacity only determines how many M3/M4 virtual jobs can run alongside a single M1 match.

### Q3: Why is my trader with m1Capacity: 2 not assigned to concurrent M1 matches?

**A:** M1 Capacity does NOT allow multiple M1 matches. It only allows M3/M4 virtual jobs to run concurrently with ONE M1 match, and only if they start at the exact same time.

### Q4: What happens if no EXPT-capable traders are available for an EXPT match?

**A:** The match goes UNASSIGNED. EXPT restrictions are hard constraints - the system will never assign a non-EXPT trader to an EXPT match, even if it's the only trader available.

### Q5: Why does the system prefer higher-MaxBet matches?

**A:** Higher MaxBet matches represent higher stakes and financial risk. The system assigns more experienced traders to these matches and ensures they're prioritized for coverage.

### Q6: Can traders ignore multiple leagues?

**A:** Yes. The `ignoreLeague` property accepts a list of patterns. If a match league contains ANY pattern in the list (case-insensitive), the trader won't be assigned to that match.

### Q7: What if two matches have the exact same MaxBet?

**A:** The system uses trader priority (level × manager status) to break ties. For same-level traders, the optimizer makes the best overall assignment across all matches.

### Q8: Why was a match left unassigned even though a trader was available?

**A:** Check for:
- Shift time mismatch
- League filter blocking
- EXPT requirement not met
- Assigned job overlap
- Virtual match gap rule violation
- Higher-priority overlapping matches taking precedence

### Q9: How does the system handle matches that start at the same time?

**A:** The optimizer considers all overlapping matches simultaneously and makes the best overall assignment. Higher-MaxBet matches and higher-priority traders are favored.

### Q10: Can I manually override the system's assignments?

**A:** Yes. The API returns assignments, but operations managers can manually adjust them before confirming with traders. Always document manual overrides for audit purposes.

---

## Glossary

**M1 (Minor 1) Match:** Real soccer match with MaxBet between 40,000 and 69,999, handled by a single trader for both HDP and OU roles.

**MJ (Major) Match:** High-priority real match with MaxBet ≥ 70,000, requires separate HDP and OU traders.

**M2 (Minor 2) Match:** Lower-priority real match with MaxBet < 40,000.

**M3/M4 Match:** Virtual matches (automated) that can run concurrently with M1 matches if M1 Capacity allows.

**HDP (Handicap):** Betting role where trader manages handicap market for a match.

**OU (Over/Under):** Betting role where trader manages over/under total goals market.

**M1 Capacity:** Trader property determining how many M3/M4 virtual jobs can run concurrently with an M1 match at the same start time.

**Manager:** Trader with supervisory responsibilities, deprioritized for M1 match assignments.

**EXPT Match:** Special match requiring traders with extra experience or training (`isEXPT: true`).

**isEXPTMatchAssignable:** Trader property indicating whether they can work EXPT matches.

**ignoreLeague:** Trader property listing league patterns they prefer not to work.

**assignedJobs:** Pre-existing work commitments (MJ, M1, M2, M3, M4, PES, PK) that block new assignments.

**Gap Rule:** 15-minute mandatory rest period after virtual matches (PES5MIN, PES24, PKIF, PKCF) before starting M1 matches.

**Coverage Bonus:** Large base score (1,000,000 points) added to ensure matches are assigned even with low-priority traders.

**CP-SAT Solver:** Google OR-Tools Constraint Programming solver that finds optimal assignments.

**Work Score:** Calculated value (level points × priority × MaxBet²) used to optimize trader-match assignments.

---

## Document History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-01-XX | Initial document creation |

**Maintained by:** Operations Team
**Last Updated:** January 2025
**Next Review:** June 2025
