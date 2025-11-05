# M3/M4 (Virtual Matches) Assignment Rules

## Document Overview

This document explains how the system assigns traders to **M3 and M4 virtual matches** using constraint-based optimization. M3/M4 matches are automated virtual soccer matches with lower complexity than real matches.

**Target Audience:** Operations managers, trading supervisors, and system administrators responsible for M3/M4 match coverage.

---

## Table of Contents

1. [Match Classification](#match-classification)
2. [Assignment Model](#assignment-model)
3. [Core Assignment Rules](#core-assignment-rules)
4. [Special League Rules](#special-league-rules)
5. [Match Pairing Strategy](#match-pairing-strategy)
6. [Trader Eligibility](#trader-eligibility)
7. [Real-World Scenarios](#real-world-scenarios)
8. [Operational Guidelines](#operational-guidelines)
9. [Troubleshooting Decision Tree](#troubleshooting-decision-tree)
10. [API Usage](#api-usage)
11. [FAQ](#faq)
12. [Glossary](#glossary)

---

## Match Classification

### M3/M4 Match Criteria

**M3 Matches:** Virtual soccer matches with MaxBet < 20,000
**M4 Matches:** Virtual soccer matches with MaxBet < 12,000 (subset of M3)

**Key Characteristics:**
- **Automated virtual matches** (not real soccer games)
- **All traders eligible** (no skill requirements)
- **Can run alongside M1/M2 matches** (using M1/M2 Capacity)
- **Lower priority** than real matches (MJ, M1, M2)
- **Special pairing rules** for higher-value matches (MaxBet > 12K)

**Match Hierarchy:**
```
Priority Order:
1. MJ Matches (70K+) - Highest Priority (Real)
2. M1 Matches (40K-70K) - High Priority (Real)
3. M2 Matches (20K-40K) - Medium Priority (Real)
4. M3 Matches (<20K) - Low Priority (Virtual)
5. M4 Matches (<12K) - Lowest Priority (Virtual)
```

---

## Assignment Model

### Single Trader Model

Like M1 and M2, **M3/M4 matches are handled by a single trader**:

```
M3 Match (15K MaxBet)
├─ HDP Role → TraderA
└─ OU Role → TraderA (same trader)
```

### Pairing Model (MaxBet > 12K)

**For M3 matches with MaxBet > 12K**, traders handle **TWO matches simultaneously**:

```
High-Value M3 Match (15K MaxBet)
├─ Primary Match: 15K MaxBet
└─ Paired Match: Must be EAL or EXPT (≤12K)

Result: 1 Trader handles BOTH matches
```

---

## Core Assignment Rules

### Rule 1: Universal Eligibility

**All traders can work M3/M4 matches** - no skill requirements (HDP/OU) or level restrictions.

**Rationale:**
- Virtual matches are automated with lower complexity
- Suitable for training junior traders
- No specialized market knowledge required

### Rule 2: MaxBet-Based Pairing

**For M3 matches with MaxBet > 12K:**

Traders must handle **2 matches simultaneously**:
- **Primary Match:** The high-value match (MaxBet > 12K)
- **Paired Match:** Must meet one of these criteria:
  1. **EAL match** (any MaxBet), OR
  2. **EXPT match** with MaxBet ≤ 12K

**For M3/M4 matches with MaxBet ≤ 12K:**

Traders can handle **1 match only** (no pairing required).

### Rule 3: Match Timing Requirement

**Paired matches MUST start at the EXACT same time.**

```
Valid Pairing:
├─ Primary Match (15K): 10:00 - 11:30
└─ EAL Match (10K): 10:00 - 11:30 ✓ (same start time)

Invalid Pairing:
├─ Primary Match (15K): 10:00 - 11:30
└─ EAL Match (10K): 10:30 - 12:00 ✗ (different start time)
```

### Rule 4: EXPT Pairing Restrictions

When pairing with EXPT matches:

**Required Conditions:**
1. EXPT match MaxBet **must be ≤ 12K**
2. EXPT match **must start at same time** as primary match
3. Trader **must have isEXPTMatchAssignable = true**

**Example - Valid EXPT Pairing:**
```
Primary Match: 15K MaxBet, 10:00-11:30
Paired EXPT Match: 10K MaxBet, 10:00-11:30, isEXPT: true
Trader: isEXPTMatchAssignable: true
Result: ✓ Valid pairing
```

**Example - Invalid EXPT Pairing:**
```
Primary Match: 15K MaxBet, 10:00-11:30
Paired EXPT Match: 13K MaxBet, 10:00-11:30, isEXPT: true
Result: ✗ Invalid (EXPT MaxBet > 12K)
```

---

## Special League Rules

### Special League 1: KOREA K LEAGUE 1

**Rule:** **NO MIXING** - One trader per match only.

**League Name Pattern:** `KOREA K LEAGUE 1`

**Restrictions:**
- Cannot pair with EAL matches
- Cannot pair with EXPT matches
- Each match requires dedicated trader
- Applies regardless of MaxBet value

**Why Special?**
- K League 1 is a premium Korean league
- Requires full trader attention
- Higher quality standards for virtual match monitoring

**Example:**
```
Match: KOREA K LEAGUE 1, 15K MaxBet, 10:00-11:30
Available EAL Match: 10K MaxBet, 10:00-11:30

Normal Rule: Would pair 15K + EAL (MaxBet > 12K)
K League 1 Rule: Assign 1 trader to 15K match ONLY (no pairing)
```

### Special League 2: NETHERLANDS EERSTE DIVISIE (PLAY OFF)

**Rule:** **NO MIXING** - One trader per match only.

**League Name Pattern:** `NETHERLANDS EERSTE DIVISIE (PLAY OFF)` or `NETHERLANDS EERSTE DIVISIE PLAY OFF`

**Restrictions:**
- Cannot pair with EAL matches
- Cannot pair with EXPT matches
- Each match requires dedicated trader
- Applies regardless of MaxBet value

**Why Special?**
- Play-off matches determine promotion to Eredivisie (top tier)
- Higher stakes and competitive intensity
- Requires dedicated monitoring

**Example:**
```
Match: NETHERLANDS EERSTE DIVISIE (PLAY OFF), 18K MaxBet, 14:00-15:30
Available EAL Match: 8K MaxBet, 14:00-15:30

Normal Rule: Would pair 18K + EAL (MaxBet > 12K)
Special Rule: Assign 1 trader to 18K match ONLY (no pairing)
```

---

## Match Pairing Strategy

### Pairing Priority

When pairing high-value M3 matches (MaxBet > 12K), the system uses this priority:

1. **EAL matches** (highest priority)
2. **EXPT matches with MaxBet ≤ 12K** (second priority)

### EAL Match Pairing

**EAL (Early Assignment League)** matches can be paired with any high-value M3 match:

**Criteria:**
- Match has `isEAL: true`
- Match starts at same time as primary match
- No MaxBet restriction for EAL matches

**Example - Valid EAL Pairing:**
```
Primary Match: 16K MaxBet, 10:00-11:30
EAL Match: 5K MaxBet, 10:00-11:30, isEAL: true
Result: ✓ Paired (1 trader handles both)
```

### EXPT Match Pairing

**EXPT matches** can be paired if they meet MaxBet restriction:

**Criteria:**
- Match has `isEXPT: true`
- Match MaxBet **≤ 12K**
- Match starts at same time as primary match
- Trader has `isEXPTMatchAssignable: true`

**Example - Valid EXPT Pairing:**
```
Primary Match: 14K MaxBet, 10:00-11:30
EXPT Match: 12K MaxBet, 10:00-11:30, isEXPT: true
Trader: isEXPTMatchAssignable: true
Result: ✓ Paired (1 trader handles both)
```

**Example - Invalid EXPT Pairing (MaxBet too high):**
```
Primary Match: 14K MaxBet, 10:00-11:30
EXPT Match: 13K MaxBet, 10:00-11:30, isEXPT: true
Result: ✗ Cannot pair (EXPT MaxBet > 12K)
```

### No Pairing Available

If no suitable pairing match exists, the high-value match goes **UNASSIGNED**:

```
Primary Match: 15K MaxBet, 10:00-11:30
Available Matches:
- EXPT Match: 14K MaxBet, 10:00-11:30 ✗ (MaxBet too high)
- EAL Match: 8K MaxBet, 10:30-12:00 ✗ (different start time)

Result: 15K match UNASSIGNED (no valid pairing)
```

---

## Trader Eligibility

### Basic Eligibility

**All traders can work M3/M4 matches** with these requirements:

1. **Shift Time Coverage:** Match must be within trader's shift
2. **No Skill Requirements:** Unlike MJ matches, no HDP/OU skills needed
3. **No Level Requirements:** All levels (Elite, Member, First, Second, Third) can work M3/M4

### EXPT Match Eligibility

For traders to work EXPT matches (paired or standalone):

**Required:** `isEXPTMatchAssignable: true`

**Example - EXPT Restriction:**
```
EXPT Match (10K MaxBet) paired with Primary (15K)
├─ TraderA: isEXPTMatchAssignable: true → ✓ Eligible
└─ TraderB: isEXPTMatchAssignable: false → ✗ Not eligible
```

### Concurrent Work Capacity

M3/M4 matches can run alongside M1/M2 matches using capacity rules:

**M1 Capacity:** M3/M4 jobs can run with ONE M1 match (same start time)
**M2 Capacity:** M3/M4 jobs can run with multiple M2 matches (same start time)

**Example - M1 Capacity:**
```
TraderA (m1Capacity: 2)
├─ M1 Match: 50K, 10:00-11:30
└─ M3 Match: 15K + EAL 8K, 10:00-11:30 ✓ (within capacity)
```

---

## Real-World Scenarios

### Scenario 1: Standard High-Value M3 Pairing

**Situation:**

- M3 Match: 16K MaxBet, 10:00-11:30 (needs pairing)
- EAL Match: 7K MaxBet, 10:00-11:30 (same start time)
- EXPT Match: 11K MaxBet, 10:00-11:30 (same start time)
- Trader available with isEXPTMatchAssignable: true

**System Behavior:**

1. Primary match (16K) requires pairing (MaxBet > 12K)
2. Two pairing options available:
   - EAL (7K) - Priority 1
   - EXPT (11K) - Priority 2
3. System selects **EAL match** (higher priority)
4. **TraderA** → Assigned to 16K + EAL 7K

**Why?**
- EAL matches have higher pairing priority
- Both matches start at same time
- Single trader handles both virtual matches efficiently

### Scenario 2: EXPT Pairing When No EAL Available

**Situation:**

- M3 Match: 14K MaxBet, 14:00-15:30 (needs pairing)
- No EAL matches at 14:00
- EXPT Match: 10K MaxBet, 14:00-15:30
- Trader available with isEXPTMatchAssignable: true

**System Behavior:**

1. Primary match (14K) requires pairing
2. No EAL matches available
3. EXPT match (10K) meets criteria:
   - MaxBet ≤ 12K ✓
   - Same start time ✓
   - Trader is EXPT-capable ✓
4. **TraderA** → Assigned to 14K + EXPT 10K

**Key Learning:**
- EXPT matches can pair when no EAL available
- EXPT MaxBet limit (≤12K) is critical
- Trader must be EXPT-capable

### Scenario 3: K League 1 - No Pairing Allowed

**Situation:**

- M3 Match: KOREA K LEAGUE 1, 15K MaxBet, 10:00-11:30
- EAL Match: 8K MaxBet, 10:00-11:30 (same start time)
- Trader available

**System Behavior:**

1. Primary match is K League 1 (special league)
2. Special rule: NO MIXING (no pairing allowed)
3. EAL match cannot be paired (rule override)
4. **TraderA** → Assigned to K League 1 match ONLY
5. **TraderB** → Assigned to EAL match separately

**Why?**
- K League 1 requires dedicated attention
- Special league rules override standard pairing rules
- Each match gets dedicated trader

### Scenario 4: EXPT MaxBet Too High

**Situation:**

- M3 Match: 17K MaxBet, 10:00-11:30 (needs pairing)
- No EAL matches available
- EXPT Match: 13K MaxBet, 10:00-11:30
- Trader available with isEXPTMatchAssignable: true

**System Behavior:**

1. Primary match (17K) requires pairing
2. No EAL matches available
3. EXPT match (13K) checked:
   - MaxBet ≤ 12K? ✗ (13K > 12K limit)
4. EXPT match cannot be paired
5. **17K match goes UNASSIGNED** (no valid pairing)

**Operational Response:**
- Alert: "M3 match (17K) unassigned - no valid pairing available"
- Options:
  - Wait for EAL match at same time
  - Find EXPT match with MaxBet ≤ 12K
  - Manually assign with override

### Scenario 5: Netherlands Eerste Divisie Play-Off

**Situation:**

- M3 Match: NETHERLANDS EERSTE DIVISIE (PLAY OFF), 18K MaxBet, 16:00-17:30
- EAL Match: 6K MaxBet, 16:00-17:30
- Trader available

**System Behavior:**

1. Primary match is Eerste Divisie Play-Off (special league)
2. Special rule: NO MIXING (promotion play-off)
3. Cannot pair with EAL match
4. **TraderA** → Assigned to Play-Off match ONLY
5. **TraderB** → Assigned to EAL match separately

**Why?**
- Play-off matches determine Eredivisie promotion
- High-stakes virtual match requiring focus
- Special league rules ensure quality

---

## Operational Guidelines

### Before Each Assignment Run

**1. Verify Match Categorization**

- Confirm MaxBet values are accurate
- Identify matches requiring pairing (MaxBet > 12K)
- Check for special leagues (K League 1, Eerste Divisie Play-Off)

**2. Review Match Schedule**

- Identify groups of matches with same start time
- Check EAL match availability for pairing
- Check EXPT match availability (MaxBet ≤ 12K)

**3. Verify Trader Availability**

- Confirm shift times cover match periods
- Check EXPT-capable traders for EXPT match pairing
- Ensure traders not overloaded with M1/M2 matches

### During Assignment Process

**4. Monitor Pairing Results**

- Check high-value matches (>12K) are paired
- Verify special league matches are NOT paired
- Confirm EXPT pairing meets MaxBet limit

**5. Handle Unassigned Matches**

- **High-value unpaired:** No suitable EAL/EXPT match at same time
- **EXPT match:** No EXPT-capable traders available
- **Capacity exceeded:** Trader at M1/M2 capacity limit

### After Assignment

**6. Review Coverage**

- Calculate pairing success rate: Paired / Total High-Value Matches
- Identify unpaired high-value matches
- Check special league compliance (K League 1, Eerste Divisie)

**7. Communicate to Traders**

- Highlight paired match assignments (2 matches)
- Flag special league matches (no pairing, full attention)
- Confirm concurrent M1/M2 work if applicable

### Best Practices

**Pairing Strategy**

- Schedule EAL matches at common start times (10:00, 14:00, 16:00) to maximize pairing opportunities
- Maintain pool of low-MaxBet EXPT matches (≤12K) for pairing
- Monitor special league schedules to avoid pairing attempts

**Trader Development**

- Use M3/M4 matches to train junior traders (Second/Third level)
- Gradually introduce paired matches (2 concurrent)
- Train traders on EXPT match requirements

**Quality Control**

- Enforce special league rules (no manual override for K League 1 / Eerste Divisie)
- Verify EXPT MaxBet limit during pairing (≤12K)
- Monitor trader performance on paired matches

---

## Troubleshooting Decision Tree

### Problem: High-Value M3 Match (>12K) Unassigned

**Check 1: Are there matches at the same start time?**

→ **NO:** No pairing opportunities
   - **Solution:** Reschedule primary match or add EAL/EXPT matches at same time

→ **YES:** Continue to Check 2

**Check 2: Is this a special league (K League 1 or Eerste Divisie Play-Off)?**

→ **YES:** Special league requires dedicated trader
   - **Solution:** Assign trader to this match only (no pairing)

→ **NO:** Continue to Check 3

**Check 3: Are there EAL matches at same start time?**

→ **YES:** EAL match should be paired
   - **Check 3a:** Is trader available?
     - **NO:** Add more traders to shift
     - **YES:** Bug - report to system administrator

→ **NO:** Continue to Check 4

**Check 4: Are there EXPT matches at same start time?**

→ **NO:** No pairing options available
   - **Solution:** Add EAL or low-MaxBet EXPT matches at same time

→ **YES:** Continue to Check 5

**Check 5: Do EXPT matches meet MaxBet ≤ 12K?**

→ **NO:** EXPT MaxBet too high
   - **Solution:** Find EXPT matches with MaxBet ≤ 12K

→ **YES:** Continue to Check 6

**Check 6: Are there EXPT-capable traders available?**

→ **NO:** No traders with isEXPTMatchAssignable: true
   - **Solution:** Bring in EXPT-capable traders or remove EXPT flag from match

→ **YES:** Bug - report to system administrator

### Problem: Special League Match (K League 1) Paired with Another Match

**This should NEVER happen**

→ **Bug:** Special league rule violation
   - **Action:** Report to system administrator immediately
   - **Workaround:** Manually separate assignments

### Problem: EXPT Match with MaxBet > 12K Paired

**This should NEVER happen**

→ **Bug:** EXPT MaxBet limit violation
   - **Action:** Report to system administrator immediately
   - **Workaround:** Break pairing and reassign separately

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
      "m1Capacity": 2,
      "m2Capacity": 2,
      "isEXPTMatchAssignable": true,
      "ignoreLeague": [],
      "assignedJobs": []
    }
  ],
  "matches": [
    {
      "id": "100",
      "matchNo": "M3-100",
      "start": "2025-09-28T10:00:00Z",
      "end": "2025-09-28T11:30:00Z",
      "maxBet": 16000,
      "leagueCode": "ENG2",
      "leagueName": "England Championship",
      "isEXPT": false,
      "isEAL": false,
      "isAlreadyHandled": false
    },
    {
      "id": "101",
      "matchNo": "M3-101",
      "start": "2025-09-28T10:00:00Z",
      "end": "2025-09-28T11:30:00Z",
      "maxBet": 8000,
      "leagueCode": "ENG2",
      "leagueName": "England Championship",
      "isEXPT": false,
      "isEAL": true,
      "isAlreadyHandled": false
    }
  ],
  "preconfiguredPairs": []
}
```

### Response Format (Paired Assignment)

```json
{
  "success": true,
  "status": "Optimal",
  "totalMatches": 2,
  "assignedMatches": 2,
  "assignments": [
    {
      "rowNumber": "100",
      "match": "M3-100",
      "league": "ENG2",
      "maxBet": 16000,
      "time": "09-28 10:00 - 11:30",
      "hdp": "TraderA",
      "hdpLevel": "Third",
      "ou": "TraderA",
      "ouLevel": "Third",
      "pairType": "M3 Paired with EAL",
      "score": 512000,
      "pairedWith": "M3-101"
    },
    {
      "rowNumber": "101",
      "match": "M3-101",
      "league": "ENG2",
      "maxBet": 8000,
      "time": "09-28 10:00 - 11:30",
      "hdp": "TraderA",
      "hdpLevel": "Third",
      "ou": "TraderA",
      "ouLevel": "Third",
      "pairType": "EAL (paired)",
      "score": 128000,
      "pairedWith": "M3-100"
    }
  ]
}
```

**Note:** Both matches assigned to **same trader (TraderA)** - this is correct for paired M3 assignments.

---

## FAQ

### Q1: Can all traders work M3/M4 matches?

**A:** Yes. Unlike MJ matches which require specific skills, M3/M4 virtual matches can be worked by all traders regardless of level or skills.

### Q2: Why do high-value M3 matches (>12K) require pairing?

**A:** Pairing optimizes trader utilization. Since M3 matches are automated virtual matches with lower complexity, experienced traders can monitor two matches simultaneously - one high-value (>12K) and one paired match (EAL or EXPT ≤12K).

### Q3: What's the difference between EAL and EXPT match pairing?

**A:**

- **EAL pairing:** Higher priority, no MaxBet restriction, any trader can work
- **EXPT pairing:** Second priority, MaxBet must be ≤ 12K, requires EXPT-capable trader

### Q4: Can K League 1 matches be paired?

**A:** **NO.** KOREA K LEAGUE 1 is a special league that requires dedicated trader attention. Each K League 1 match must be assigned to a separate trader, regardless of MaxBet value.

### Q5: What happens if no pairing match is available?

**A:** The high-value M3 match (>12K) goes UNASSIGNED. Operations must either add suitable EAL/EXPT matches at the same start time or manually override the pairing requirement.

### Q6: Can EXPT matches with MaxBet > 12K be paired?

**A:** **NO.** EXPT matches used for pairing must have MaxBet ≤ 12K. This limit ensures the paired workload remains manageable for a single trader.

### Q7: Why does NETHERLANDS EERSTE DIVISIE (PLAY OFF) have special rules?

**A:** Play-off matches determine promotion to Eredivisie (top Dutch league). These high-stakes virtual matches require dedicated attention, so pairing is not allowed.

### Q8: Can M3/M4 matches run alongside M1/M2 matches?

**A:** Yes. M3/M4 matches can run concurrently with M1/M2 matches using M1 Capacity or M2 Capacity rules, as long as they start at the exact same time.

### Q9: What if a trader is not EXPT-capable but an EXPT pairing is needed?

**A:** The high-value M3 match cannot be paired with that EXPT match for that trader. The system will either find an EXPT-capable trader or leave the match unassigned.

### Q10: Are special league rules enforced automatically?

**A:** Yes. The system automatically detects K League 1 and Netherlands Eerste Divisie (Play-Off) matches and prevents pairing. Manual override should not be used for these leagues.

---

## Glossary

**M3 Match:** Virtual soccer match with MaxBet < 20,000, automated with lower complexity than real matches.

**M4 Match:** Virtual soccer match with MaxBet < 12,000, subset of M3 matches.

**Paired Assignment:** Assigning a single trader to handle TWO M3/M4 matches simultaneously (required for M3 with MaxBet > 12K).

**Primary Match:** The high-value M3 match (MaxBet > 12K) that requires pairing.

**Paired Match:** The secondary match (EAL or EXPT ≤12K) that runs alongside the primary match.

**EAL Match:** Early Assignment League match, highest priority for pairing, no MaxBet restriction.

**EXPT Match:** Special experience match requiring trained traders, can be paired if MaxBet ≤ 12K.

**K League 1:** Korean premier league (KOREA K LEAGUE 1), special rule: no pairing allowed.

**Eerste Divisie Play-Off:** Netherlands second-tier promotion play-off (NETHERLANDS EERSTE DIVISIE PLAY OFF), special rule: no pairing allowed.

**Same Start Time:** Critical requirement for pairing - both matches must start at exact same time (e.g., both at 10:00:00).

**M1/M2 Capacity:** Trader property allowing M3/M4 matches to run alongside M1/M2 real matches at same start time.

**Universal Eligibility:** All traders can work M3/M4 matches regardless of level or skills (except EXPT restrictions).

---

## Document History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-01-XX | Initial document creation |

**Maintained by:** Operations Team
**Last Updated:** January 2025
**Next Review:** June 2025
