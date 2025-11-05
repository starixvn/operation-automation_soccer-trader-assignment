# Major (MJ) Match Assignment Rules

## Overview

The Major Match Assignment Strategy is responsible for assigning Elite, Member, and First level traders to high-value soccer matches (MaxBet >= 70,000). The system uses **Google OR-Tools CP-SAT solver** to find the optimal assignment that maximizes trader pair quality while respecting all constraints.

## Match Classification

**Match Type:** MJ (Major)
- **MaxBet Threshold:** >= 70,000
- **Validation:** All matches must meet the minimum MaxBet requirement

## Trader Requirements

### Required Information
1. **Skills:** HDP and/or OU
2. **Level:** Elite, Member, or First
3. **Manager Status:** Boolean flag indicating management role
4. **Shift Timing:** Start and end times
5. **Assigned Jobs:** Existing commitments
6. **Ignore Leagues:** List of leagues the trader should not work
7. **EXPT Capability:** Whether trader can handle EXPT matches

### Roles
Each MJ match requires **2 different traders**:
- **HDP Trader:** Handles Handicap betting
- **OU Trader:** Handles Over/Under betting

## Assignment Constraints

The system enforces the following hard constraints that **cannot be violated**:

### 1. Match Coverage Constraint
- Each match must have **at most 1 HDP trader** and **at most 1 OU trader**
- Formula: `Σ(traders assigned to role) ≤ 1` for each role

### 2. No Overlapping Matches (Critical Constraint)
- A trader **cannot work overlapping matches in any role**
- This prevents scenarios like:
  - Trader working HDP in Match1 (08:00-10:00) and OU in Match2 (09:30-11:30) ❌
  - Trader working HDP in both overlapping matches ❌
- Formula: `(HDP_m1 + OU_m1) + (HDP_m2 + OU_m2) ≤ 1` for overlapping matches
- **Overlap Definition:** Match1.Start < Match2.End AND Match2.Start < Match1.End

### 3. Role Exclusivity Constraint
- A trader **cannot be both HDP and OU** for the same match
- Formula: `HDP_assignment + OU_assignment ≤ 1` for each (trader, match) pair

### 4. All-or-Nothing Constraint
- A match must have **both HDP and OU assigned, or neither**
- Prevents partial assignments
- Formula: `Σ(HDP assignments) == Σ(OU assignments)` for each match

### 5. Trader Eligibility Constraints
A trader can only be assigned if ALL of the following are true:

#### 5.1 Skill Requirement
- Trader must have the required skill (HDP or OU)
- Check: `trader.Skills.Contains(role)`

#### 5.2 Shift Coverage
- Match must be completely within trader's shift
- Check: `match.Start >= trader.Start AND match.End <= trader.End`

#### 5.3 League Preference
- Match league must NOT be in trader's ignore list
- Check: `!trader.IgnoreLeague.Contains(match.LeagueName)`

#### 5.4 EXPT Match Restriction
- If match is EXPT, trader must be EXPT-capable
- Check: `!(match.IsEXPT AND !trader.IsEXPTMatchAssignable)`

#### 5.5 No Job Conflicts
Two types of conflicts checked:

**a) Time Overlap Conflict**
- Match cannot overlap with existing assigned jobs
- Check: `match.Start < job.End AND job.Start < match.End`

**b) Virtual Match Gap Rule**
- Must have **15-minute gap** after PES/PK virtual matches
- Virtual match types: PES5MIN, PES24, PKIF, PKCF
- Check: If `job.Type` is virtual AND `job.End < match.Start`, then `(match.Start - job.End) >= 15 minutes`

## Scoring System

The system maximizes a weighted score to find the optimal assignment. The score considers:

### 1. Trader Level Points
Base skill points by level:

| Level | Points |
|-------|--------|
| Elite | 256 |
| Member | 16 |
| First | 4 |

### 2. Trader Work Priority System

The system prioritizes who gets assigned to matches based on their skill level and role. This ensures the best traders work the most important matches.

#### Work Priority Ranking

**Who gets first choice for assignments?**

| Rank | Trader Type | Work Priority | Why? |
|------|-------------|---------------|------|
| 🥇 1st | Elite Non-Manager | **Highest** | Best skilled, fully focused on trading |
| 2nd | Elite Manager | Very High | High skill but has management duties |
| 3rd | Member Non-Manager | High | Experienced traders, fully focused |
| 4th | First Non-Manager | Medium | Developing traders, learning on the job |
| 5th | Member Manager | Low | Split focus between trading and management |
| 🥉 6th | First Manager | **Lowest** | Entry-level with management responsibilities |

#### Why Non-Managers Work Before Managers

**Operational Reasons:**

1. **Availability for Trading**
   - Non-managers can dedicate 100% focus to match trading
   - Managers may be pulled away for team issues, escalations, or meetings

2. **Managerial Duties**
   - Managers need to be available to supervise other traders
   - Managers handle urgent issues and client escalations
   - Keeping managers free ensures better team support

3. **Performance Quality**
   - Non-managers typically have sharper trading focus
   - Fewer interruptions lead to better trading decisions
   - Consistent attention throughout the match duration

4. **Team Balance**
   - Distributes workload fairly across the team
   - Prevents managers from being overworked
   - Maintains clear leadership availability

#### How Work Assignment Works

**Example Scenario:** EPL match at 10:00 AM, need HDP trader

- **3 traders available:** Elite Manager, Member Non-Manager, First Non-Manager
- **System assigns:** Elite Manager (rank 2) gets the work
- **Why not Member NM?** Elite level skill is more important than manager status
- **Result:** Best available trader works the match

**Another Scenario:** Multiple matches at same time

- **4 matches need HDP traders**
- **5 traders available:** 2 Elite NM, 1 Elite M, 2 Member NM
- **System assigns:**
  1. Elite NM → Match 1 (highest value)
  2. Elite NM → Match 2 (second highest value)
  3. Elite M → Match 3 (third highest value)
  4. Member NM → Match 4 (lowest value)
- **Result:** Better traders work higher-value matches

#### Priority in Practice

**What this means for daily operations:**

- **Elite non-managers** will be fully utilized on high-value matches
- **Elite managers** work important matches when Elite NM are occupied
- **Member level** traders handle standard matches and provide coverage
- **First level** traders get assigned to lower-value matches for experience
- **Managers at all levels** remain available for team supervision and issue resolution

### 3. Preferred Trader Pairs (Team Chemistry)

Some HDP and OU traders work better together because they understand each other's trading style and communicate effectively. The system tracks these proven partnerships and tries to assign them together.

#### Pair Work Categories

Trader pairs are organized by how well they work together:

| Category | Description | When to Use | Assignment Priority |
|----------|-------------|-------------|---------------------|
| **Primary Pairs** | Best performing teams | High-value matches (100K+, EPL, LaLiga) | **Strongly Preferred** |
| **Secondary Pairs** | Reliable partnerships | Standard major matches (70K-100K) | Preferred |
| **Backup Pairs** | Proven to work together | When primary/secondary busy | Acceptable |
| Ad-hoc (Any pairing) | No history together | Any traders available | Last resort |

**Why Pairs Matter:**

1. **Communication:** Partners who work together regularly communicate better during live matches
2. **Trust:** Familiar with each other's decision-making patterns
3. **Performance:** Historical data shows better outcomes with established pairs
4. **Efficiency:** Less coordination overhead, faster reactions to match events
5. **Consistency:** Predictable performance reduces operational risk

#### League Expertise Assignment

Some trader pairs have expertise in specific leagues. For example, a pair might be especially strong at EPL matches because they understand the league's patterns and betting behaviors.

**League Expertise Levels:**

| Expertise Level | Description | Example |
|-----------------|-------------|---------|
| **Expert** | Pair's strongest leagues | EPL, LaLiga - they trade these daily |
| **Proficient** | Leagues they know well | Serie A, Bundesliga - regular experience |
| **Competent** | Can handle effectively | Ligue 1, Eredivisie - occasional work |
| General | No specific expertise | Any other league - standard approach |

**How League Expertise Affects Work Assignment:**

**Example Configuration:**
- **Pair:** Elite Trader A (HDP) + Elite Trader B (OU)
- **Expert Leagues:** EPL, LaLiga
- **Proficient Leagues:** Serie A, Bundesliga
- **Competent Leagues:** Ligue 1, Eredivisie

**Assignment Scenarios:**

1. **EPL Match Available**
   - System **strongly prefers** this pair for EPL
   - They know EPL betting patterns intimately
   - Result: Highest work priority for this match

2. **Serie A Match Available**
   - System **prefers** this pair
   - They have good experience with Serie A
   - Result: High work priority

3. **Championship Match Available**
   - No expertise listed
   - Pair can work it, but no preference
   - Result: Same priority as other pairs

**Real-World Work Assignment:**

**Scenario:** 10:00 AM - Two matches need assignments

- **Match A:** EPL 100K
- **Match B:** Serie A 100K

**Available Pairs:**
- Pair 1: EPL experts (Primary pair)
- Pair 2: Serie A experts (Primary pair)

**System Decision:**
- Pair 1 → EPL match (expertise match!)
- Pair 2 → Serie A match (expertise match!)
- **Result:** Each pair works their strongest league

**Why This Matters:**

1. **Better Trading Decisions:** Traders familiar with league patterns make smarter bets
2. **Risk Reduction:** Expertise reduces mistakes in unfamiliar leagues
3. **Efficiency:** Less research needed during match, faster decisions
4. **Performance:** Historical data shows better P&L when traders work known leagues
5. **Confidence:** Traders perform better in familiar territory

#### Combined Pairing and Expertise Priority

**When assigning work, system considers:**

1. **Match Value** - Most important factor
2. **Trader Skill Level** - Elite vs Member vs First
3. **Pair Relationship** - Primary/Secondary/Backup teams
4. **League Expertise** - Does pair know this league well?

**Best Case Assignment:**
- **Primary pair** who are **experts** in the match league working a **high-value** match
- Example: Elite pair working EPL 200K match that's their specialty

**Acceptable Assignment:**
- **Backup pair** with **no league expertise** working a **standard-value** match
- Example: Member pair working Championship 70K match they haven't traded before

### 4. Match Value Priority

**Higher-value matches get better traders.** This is the most important factor in work assignment.

**Why Match Value Matters Most:**

- A 200K match has 4x more business impact than a 100K match
- Higher stakes require better decision-making
- Risk of error is much greater on valuable matches
- Company P&L is more sensitive to high-value match outcomes

**How It Works in Practice:**

| Match Value | Trader Assignment Priority | Typical Assignment |
|-------------|---------------------------|-------------------|
| 200K+ | **Critical** - Must get best available | Elite Primary Pairs only |
| 150K-200K | Very High - Need top traders | Elite pairs preferred |
| 100K-150K | High - Important matches | Elite or Member pairs |
| 70K-100K | Standard - Regular priority | Any qualified pair |

**Example Scenario:**

At 10:00 AM, three matches start:
- **Match A:** 200K EPL
- **Match B:** 100K Serie A
- **Match C:** 70K Championship

Available: 1 Elite pair, 1 Member pair, 1 First pair

**System Decision:**
1. Elite Pair → 200K match (most valuable)
2. Member Pair → 100K match (medium value)
3. First Pair → 70K match (standard value)

**Result:** Best traders always work the highest-value matches.

### 5. EPL Match Priority

EPL (English Premier League) matches get slight preference because:

1. **High visibility** - More client attention and scrutiny
2. **Market liquidity** - More betting volume, faster price movements
3. **Complexity** - Requires experienced traders who know the league
4. **Business importance** - Major revenue source

**When EPL Priority Matters:**

- **Same value matches:** If EPL and Serie A both are 100K, EPL gets better trader
- **Similar trader quality:** Elite Manager might go to EPL, Elite NM to other league
- **Minor adjustment:** Not as important as match value or trader skill level

## Overall Work Assignment Priority

**What determines who works which matches?**

The system considers multiple factors, ranked by importance:

### Priority Hierarchy (Most Important First)

#### 1. Match Value (Most Critical)
**"High-value matches must get the best traders"**

- 200K match will **always** get priority over 100K match
- A 200K match with Member pair is preferred over 70K match with Elite pair
- **Impact:** This is the #1 factor - protects company P&L on valuable matches

#### 2. Trader Skill Level (Very Important)
**"Better skilled traders work more important matches"**

- Elite traders prioritized for available matches
- Member traders fill in when Elite are busy
- First traders get lower-value matches for experience
- **Impact:** Ensures quality matches business requirements

#### 3. Manager Status (Important)
**"Non-managers work first, managers supervise"**

- Non-managers get assigned before managers at same skill level
- Keeps managers available for team support
- Elite Non-Manager > Elite Manager for same match value
- **Impact:** Balances operational coverage with trading needs

#### 4. Pair Relationship (Significant)
**"Teams that work well together are encouraged"**

- Primary pairs strongly preferred for assignments
- Secondary pairs used when primary unavailable
- Backup pairs acceptable if needed
- Ad-hoc pairs only as last resort
- **Impact:** Better performance through team chemistry

#### 5. League Expertise (Moderate)
**"Specialists work their expert leagues"**

- Pairs get preferred assignment to their known leagues
- Expert > Proficient > Competent > General knowledge
- Only matters when pair is already suitable for match value
- **Impact:** Fine-tunes assignments for better outcomes

#### 6. EPL League Bonus (Minor)
**"EPL matches get slight edge"**

- Very small preference for EPL over other leagues
- Only matters when all other factors are equal
- Tie-breaker between similar scenarios
- **Impact:** Minor fine-tuning, rarely the deciding factor

### Real-World Assignment Examples

#### Scenario 1: Clear Choice

**Available Work:**
- Match A: 200K EPL at 10:00 AM
- Match B: 100K Serie A at 10:00 AM

**Available Traders:**
- Pair 1: Elite NM + Elite NM (Primary pair, EPL experts)
- Pair 2: Member NM + Member NM (Secondary pair, Serie A experts)

**System Decision:**
- Pair 1 → Match A (200K) ✓
- Pair 2 → Match B (100K) ✓

**Why:** Match value dominates - Elite pair goes to higher-value match regardless of league expertise

#### Scenario 2: Equal Value, Different Skills

**Available Work:**
- Match A: 100K EPL at 10:00 AM
- Match B: 100K Bundesliga at 10:00 AM

**Available Traders:**
- Pair 1: Elite NM + Elite M (no particular expertise)
- Pair 2: Member NM + Member NM (Bundesliga experts)

**System Decision:**
- Pair 1 → Match A (EPL) ✓
- Pair 2 → Match B (Bundesliga) ✓

**Why:** Same match value, so trader skill dominates. Elite pair gets EPL (slight bonus), Member pair gets their expert league.

#### Scenario 3: Busy Elite Traders

**Available Work:**
- Match A: 150K EPL at 10:00 AM (Elite pair already working)
- Match B: 100K Serie A at 10:00 AM

**Available Traders:**
- Pair 1: Member NM + Member NM (Primary pair, EPL experts)
- Pair 2: Member NM + First NM (Backup pair, general)

**System Decision:**
- Pair 1 → Match A (150K) ✓
- Pair 2 → Match B (100K) ✓

**Why:** Match value first (150K > 100K). Then pair quality (Primary > Backup). League expertise helps Pair 1.

#### Scenario 4: Manager Availability

**Available Work:**
- Match A: 100K EPL at 10:00 AM

**Available Traders:**
- Pair 1: Elite M + Member M (Primary pair)
- Pair 2: Elite NM + Member NM (Secondary pair)

**System Decision:**
- Pair 2 → Match A ✓
- Pair 1 → Available for supervision

**Why:** Non-manager pair preferred even though Pair 1 is "Primary" tier. Keeps managers free.

### Key Takeaways

**For Operators:**
1. High-value matches automatically get best available traders
2. Elite traders will be fully utilized before Member traders get work
3. Managers remain available for team issues during heavy trading periods
4. Established pairs work together whenever possible
5. League specialists get matched to their expertise when available

**For Managers:**
1. Trust the system - it optimizes for business value
2. Manual overrides should be rare and documented
3. Monitor that Elite traders aren't overworked
4. Ensure pair configurations are up-to-date
5. Review assignment history to identify improvement opportunities

**Assignment Philosophy:**

> "Match value drives everything. Within same value tier, better traders work first.
> Non-managers cover trades, managers cover team. Pairs beat ad-hoc. Expertise helps but doesn't override fundamentals."

## Technical Score Calculation (For Reference)

For each potential (HDP trader, OU trader, match) combination:

```
HDP_Score = Level_Points × Priority_Multiplier
OU_Score = Level_Points × Priority_Multiplier

Pair_Score = HDP_Score × OU_Score × Tier_Bonus × League_Priority_Bonus

Match_Weight = (MaxBet / 1000)²

League_Multiplier = (match is EPL) ? 1.1 : 1.0

Final_Score = Pair_Score × Match_Weight × League_Multiplier
```

**Objective:** Maximize `Σ(Final_Score × Pair_Assigned)`

### Detailed Example Calculations

#### Example 1: Best Case Scenario

**Scenario:**

- HDP: Elite non-manager (256 points × 2.0 = 512)
- OU: Elite non-manager (256 points × 2.0 = 512)
- Pair: Primary tier (1.5x) with 1st priority league match (1.5x)
- Match: 100K EPL match

**Step-by-step Calculation:**

```
Step 1: Individual Trader Scores
  HDP_Score = 256 (Elite points) × 2.0 (non-manager priority) = 512
  OU_Score = 256 (Elite points) × 2.0 (non-manager priority) = 512

Step 2: Pair Score (with bonuses)
  Base_Pair_Score = 512 × 512 = 262,144
  Tier_Bonus = 1.5 (Primary)
  League_Priority_Bonus = 1.5 (1st priority)
  Pair_Score = 262,144 × 1.5 × 1.5 = 589,824

Step 3: Match Weight
  Match_Weight = (100,000 / 1000)² = 100² = 10,000

Step 4: League Multiplier
  League_Multiplier = 1.1 (EPL)

Step 5: Final Score
  Final_Score = 589,824 × 10,000 × 1.1 = 6,488,064,000
```

**Result:** This represents the **highest possible score** combination for a 100K match.

#### Example 2: Regular Ad-hoc Pair

**Scenario:**

- HDP: Member manager (16 points × 1.1 = 17.6)
- OU: First non-manager (4 points × 1.2 = 4.8)
- Pair: No preconfigured pair (1.0x tier, 1.0x league priority)
- Match: 70K Non-EPL match

**Calculation:**

```
HDP_Score = 16 × 1.1 = 17.6
OU_Score = 4 × 1.2 = 4.8
Pair_Score = 17.6 × 4.8 × 1.0 × 1.0 = 84.48
Match_Weight = (70)² = 4,900
League_Multiplier = 1.0

Final_Score = 84.48 × 4,900 × 1.0 = 413,952
```

**Result:** This is **15,682x lower** than Example 1, showing the massive preference for elite pairs.

#### Example 3: Comparison Table

| Scenario | HDP | OU | Pair Bonus | Match | Final Score | Relative |
|----------|-----|----|-----------:|------:|------------:|---------:|
| **Best Case** | Elite NM | Elite NM | Primary+1st (2.25x) | 100K EPL | 6,488,064,000 | 100.0% |
| **High Quality** | Elite M | Elite NM | Secondary+2nd (1.69x) | 100K EPL | 3,697,254,400 | 57.0% |
| **Good** | Member NM | Member NM | Backup (1.2x) | 100K | 3,686,400 | 0.06% |
| **Acceptable** | Member NM | First NM | None (1.0x) | 70K | 345,744 | 0.005% |
| **Lowest** | First M | First M | None (1.0x) | 70K | 196,000 | 0.003% |

*NM = Non-Manager, M = Manager*

#### Key Insights from Examples

**Trader Quality Impact:**

- Elite vs First (same non-manager): **64x difference** in base points (256 vs 4)
- Non-manager vs Manager (same Elite): **1.11x difference** (2.0 vs 1.8)
- Combined Elite NM vs First M: **71x difference** (512 vs 7.2)

**Pair Configuration Impact:**

- Primary + 1st Priority: **2.25x bonus**
- No pair configuration: **1.0x bonus** (baseline)
- Difference: **2.25x** improvement for configured pairs

**Match Value Impact:**

- 100K vs 70K match: **2.04x difference** in weight (10,000 vs 4,900)
- 200K vs 100K match: **4.0x difference** in weight (40,000 vs 10,000)
- Formula is **quadratic**, so value differences are squared

**Overall Priority:**

1. **Match Value** (quadratic) - Most impactful
2. **Trader Level** (exponential with priority) - Very impactful
3. **Pair Configuration** (2.25x max) - Significant
4. **League Bonus** (1.1x) - Minor fine-tuning

## Assignment Algorithm

### 1. Decision Variables
For each combination of (trader, match, role):
- Create binary variable: `x[trader, match, role] ∈ {0, 1}`
- If trader is eligible: variable can be 0 or 1
- If trader is not eligible: variable fixed to 0

### 2. Constraint Application
Apply all 5 constraint types (see Constraints section above)

### 3. Objective Function
Build objective terms for all valid (HDP, OU, match) combinations:
- Calculate score for each pair combination
- Create auxiliary variable for pair assignment
- Add weighted term to objective

### 4. Solve
Use CP-SAT solver to find optimal solution:
- **Status: Optimal** - Found best possible assignment
- **Status: Feasible** - Found a valid solution (may not be optimal)
- **Status: Infeasible** - No valid assignment exists
- **Status: Unknown** - Solver couldn't determine

### 5. Build Response
Extract assignments from solver:
- For each match, find assigned HDP and OU traders
- Calculate actual scores
- Mark unassigned matches as "N/A"
- Compute summary statistics

## Assignment Priority Order

Matches are prioritized in the following order:

1. **League Code** (alphabetical, EPL first due to 1.1x multiplier)
2. **MaxBet** (descending - higher value matches first)

This ordering is applied when displaying results, not during optimization.

## Edge Cases and Special Scenarios

### 1. Insufficient Traders
**Scenario:** Not enough traders to cover all matches
- **Result:** Some matches remain unassigned (marked as "N/A")
- **Status:** Still returns "Optimal" - best possible solution given constraints

### 2. Overlapping Matches with Limited Traders
**Scenario:** Multiple overlapping matches but few traders
- **Result:** Solver assigns to highest-value matches first
- **Unassigned:** Lower-value overlapping matches

### 3. Mixed Skill Availability
**Scenario:** Many HDP traders but few OU traders (or vice versa)
- **Result:** Matches remain unassigned due to all-or-nothing constraint
- **Example:** 10 HDP Elite traders + 1 OU Member trader = only 1 match assigned

### 4. EXPT Match with Non-EXPT Traders
**Scenario:** EXPT match but no EXPT-capable traders available
- **Result:** Match remains unassigned
- **Reason:** EXPT restriction constraint prevents assignment

### 5. Preconfigured Pair Unavailable
**Scenario:** Primary pair defined but one trader unavailable
- **Result:** System assigns next best available pair
- **Note:** May use secondary pair or create ad-hoc pair

### 6. Trader with Both Skills
**Scenario:** Trader has both HDP and OU skills
- **Allowed:** Can be assigned to ONE role for a match
- **Not Allowed:** Cannot work both roles in same match (Constraint 3)
- **Not Allowed:** Cannot work overlapping matches even in different roles (Constraint 2)

### 7. Already Handled Matches
**Scenario:** Some matches have `IsAlreadyHandled = true`
- **Result:** These matches are filtered out before optimization
- **Purpose:** Prevents reassignment of previously assigned matches

## Summary Statistics

The response includes summary metrics:

- **TotalMatches:** Count of matches to assign
- **AssignedMatches:** Count successfully assigned
- **PrimaryPairs:** Count using primary tier pairs
- **SecondaryPairs:** Count using secondary tier pairs
- **BackupPairs:** Count using backup tier pairs
- **PreconfiguredPairsUsed:** Total preconfigured pairs used

## Implementation Notes

### Technology Stack
- **Solver:** Google OR-Tools CP-SAT (Constraint Programming - Satisfiability)
- **Language:** C# / .NET 8.0
- **Pattern:** Strategy Pattern (implements IMatchAssignmentStrategy)

### Performance Characteristics
- **Solver Type:** Exact algorithm (finds optimal solution)
- **Complexity:** Polynomial for small instances, may be slow for large problems
- **Variables:** O(traders × matches × roles)
- **Constraints:** O(traders × matches²) for overlap constraints

### Key Design Decisions

1. **Two-Trader Requirement:** MJ matches always need both HDP and OU roles filled by different traders
2. **All-or-Nothing:** Prevents partial assignments that would be operationally problematic
3. **Overlap Prevention:** Traders cannot multitask across time-overlapping matches
4. **Score Maximization:** Prioritizes quality (trader level + pairs) over quantity
5. **EXPT Restriction:** Ensures only qualified traders work specialized EXPT matches

## Testing Coverage

The implementation includes comprehensive tests covering:

1. ✅ Basic assignment with valid request
2. ✅ Multiple matches assignment
3. ✅ Already handled matches filtering
4. ✅ Overlapping matches constraint
5. ✅ Trader job conflicts (time overlap + virtual gap rule)
6. ✅ Preconfigured pairs prioritization
7. ✅ Unavailable traders (shift mismatch)
8. ✅ Missing skills (no OU trader available)
9. ✅ Higher MaxBet prioritization
10. ✅ Summary statistics calculation
11. ✅ EXPT match restrictions
12. ✅ Overlap constraint with traders having both skills

Total Tests: **19 tests** (all passing)

## Configuration Example

### Sample Request
```json
{
  "traderShifts": [
    {
      "id": "1",
      "name": "EliteHDP",
      "start": "2025-09-28T08:00:00Z",
      "end": "2025-09-28T18:00:00Z",
      "skills": ["HDP"],
      "level": "Elite",
      "isManager": false,
      "isEXPTMatchAssignable": true,
      "ignoreLeague": [],
      "assignedJobs": []
    },
    {
      "id": "2",
      "name": "EliteOU",
      "start": "2025-09-28T08:00:00Z",
      "end": "2025-09-28T18:00:00Z",
      "skills": ["OU"],
      "level": "Elite",
      "isManager": false,
      "isEXPTMatchAssignable": true,
      "ignoreLeague": [],
      "assignedJobs": []
    }
  ],
  "matches": [
    {
      "id": "1",
      "matchNo": "100",
      "start": "2025-09-28T10:00:00Z",
      "end": "2025-09-28T12:00:00Z",
      "maxBet": 100000,
      "leagueCode": "EPL",
      "leagueName": "English Premier League",
      "isAlreadyHandled": false,
      "isEXPT": false
    }
  ],
  "preconfiguredPairs": [
    {
      "hdp": "EliteHDP",
      "ou": "EliteOU",
      "tier": "primary",
      "leaguePriority": {
        "1": ["EPL", "LaLiga"],
        "2": ["SerieA", "Bundesliga"],
        "3": ["Ligue1"]
      }
    }
  ]
}
```

### Sample Response
```json
{
  "success": true,
  "status": "Optimal",
  "totalMatches": 1,
  "assignedMatches": 1,
  "assignments": [
    {
      "rowNumber": "1",
      "match": "100",
      "league": "EPL",
      "maxBet": 100000,
      "time": "09-28 10:00 - 12:00",
      "hdp": "EliteHDP",
      "hdpLevel": "Elite",
      "ou": "EliteOU",
      "ouLevel": "Elite",
      "pairType": "Primary (League ✓)",
      "score": 6488064000
    }
  ],
  "summary": {
    "primaryPairs": 1,
    "secondaryPairs": 0,
    "backupPairs": 0,
    "preconfiguredPairsUsed": 1
  }
}
```

## Operational Guidelines

### When to Review Assignments

**Daily Operations:**
- Review assignments at start of each trading day
- Check for unassigned high-value matches (>150K)
- Verify Elite traders have reasonable workload (not overbooked)
- Confirm managers are available for supervision

**When to Manually Intervene:**

1. **Critical Unassigned Match**
   - High-value match (200K+) cannot be auto-assigned
   - Action: Review trader availability, consider shifting lower-value assignments

2. **Elite Trader Overload**
   - All Elite traders working simultaneously for extended period
   - Action: Consider bringing in Member traders for lower-value matches

3. **Specialized Match Requirements**
   - Client-specific trader request
   - Action: Manual override with documentation

4. **Technical Issues**
   - Solver returns "Infeasible" status unexpectedly
   - Action: Check constraint conflicts, trader availability data

### Common Assignment Issues

#### Issue 1: High-Value Match Unassigned

**Symptoms:**
- Match with MaxBet > 150K shows as "N/A"
- Elite traders appear available

**Possible Causes:**
1. Elite traders already working overlapping matches
2. Elite traders have conflicting assigned jobs
3. Elite HDP available but no Elite OU (or vice versa)
4. Match league in all Elite traders' ignore lists
5. EXPT match but no EXPT-capable traders available

**Resolution Steps:**
1. Check trader schedules for conflicts
2. Review assigned jobs list
3. Verify HDP and OU skill availability
4. Check ignore league configurations
5. Verify EXPT capability for EXPT matches

#### Issue 2: Manager Gets Assigned Over Non-Manager

**Symptoms:**
- Elite Manager assigned to match
- Elite Non-Manager appears available

**Possible Causes:**
1. Non-Manager already working overlapping match
2. Non-Manager has skill mismatch (needs both HDP and OU for pair)
3. Non-Manager has league in ignore list
4. Non-Manager shift doesn't fully cover match time

**Resolution:**
- System is working correctly - Manager assigned because NM not eligible

#### Issue 3: Ad-hoc Pair Instead of Primary Pair

**Symptoms:**
- Primary pair available but not assigned
- Different traders assigned to match

**Possible Causes:**
1. Primary pair working higher-value overlapping match
2. One member of primary pair unavailable (shift, ignore league, etc.)
3. Ad-hoc pair has better league expertise for this specific match
4. Match value drives assignment to better skilled ad-hoc pair

**Resolution:**
- Review match values - higher value takes priority
- Check both traders in primary pair for eligibility

### Performance Monitoring

**Key Metrics to Track:**

1. **Assignment Success Rate**
   - Target: >95% of matches assigned
   - Alert if: <90% for MJ matches

2. **Elite Trader Utilization**
   - Target: 70-85% of trading time
   - Alert if: >90% (overworked) or <50% (underutilized)

3. **Preconfigured Pair Usage**
   - Target: >60% of assignments use configured pairs
   - Alert if: <40% (may need pair configuration review)

4. **Manual Override Rate**
   - Target: <5% of assignments
   - Alert if: >10% (system may need tuning)

5. **Average Match Value per Trader Level**
   - Elite should average >120K
   - Member should average 80-120K
   - First should average <80K

### Troubleshooting Decision Tree

```
Match Unassigned?
├─ YES: Check trader availability
│   ├─ No traders available? → Schedule more shifts
│   ├─ Traders available but not assigned?
│   │   ├─ Check overlapping matches → Higher value match takes priority
│   │   ├─ Check skills → Need both HDP and OU available
│   │   ├─ Check ignore leagues → Trader may be excluding this league
│   │   ├─ Check EXPT capability → Match may require EXPT traders
│   │   └─ Check assigned jobs → Trader may have conflicting commitments
│   └─ All constraints satisfied? → Contact technical support
└─ NO: Assignment looks wrong?
    ├─ Lower quality trader assigned? → Check if higher quality trader available
    │   └─ Higher quality trader busy? → System working correctly
    ├─ Manager assigned over non-manager? → Check non-manager availability
    │   └─ Non-manager unavailable? → System working correctly
    └─ Wrong pair assigned? → Check match value and pair availability
```

## API Usage

### Endpoint

**POST** `/api/TraderAssignment/mj`

### Request Headers
```
Content-Type: application/json
```

### Request Body Structure

See "Configuration Example" section above for complete request format.

**Required Fields:**
- `traderShifts` - Array of available traders
- `matches` - Array of matches to assign
- `preconfiguredPairs` - Array of preferred trader pairs (can be empty)

### Response Status Codes

| Status Code | Meaning | Action |
|-------------|---------|--------|
| 200 OK | Assignment successful | Process assignments |
| 400 Bad Request | Invalid request data | Check match MaxBet values (must be >= 70K) |
| 500 Internal Server Error | System error | Check logs, contact support |

### Error Responses

**Invalid MaxBet:**
```json
{
  "error": "Invalid MJ matches",
  "message": "All matches must have maxBet >= 70,000 for MJ assignment",
  "invalidMatchIds": ["match-123", "match-456"]
}
```

**System Error:**
```json
{
  "error": "An error occurred during optimization",
  "details": "Detailed error message here"
}
```

### Integration Notes

1. **Data Freshness:** Always send current trader shift data including real-time assigned jobs
2. **Already Handled:** Mark matches as `isAlreadyHandled: true` to prevent reassignment
3. **Timeout:** API may take 5-30 seconds for complex scenarios (many traders/matches)
4. **Retry Logic:** Implement exponential backoff for 500 errors
5. **Validation:** Pre-validate MaxBet values before sending request

## Business Rules Summary

### Match Requirements
- ✅ MaxBet >= 70,000
- ✅ Both HDP and OU roles must be filled
- ✅ Each role assigned to different trader
- ✅ Match time within trader shift time
- ❌ Partial assignments not allowed

### Trader Selection Priority
1. Match value (highest priority)
2. Trader skill level (Elite > Member > First)
3. Manager status (Non-Manager > Manager)
4. Pair relationship (Primary > Secondary > Backup > Ad-hoc)
5. League expertise (Expert > Proficient > Competent > General)
6. EPL bonus (minor tie-breaker)

### Work Distribution Philosophy
- **Quality First:** Best traders work most valuable matches
- **Operational Balance:** Non-managers trade, managers supervise
- **Team Chemistry:** Established pairs encouraged
- **Expertise Utilization:** Specialists work their expert leagues
- **Fair Distribution:** System prevents single trader overload

### Constraint Hierarchy
1. **Hard Constraints:** Cannot be violated
   - Skill requirements
   - Time conflicts
   - All-or-nothing assignment
2. **Soft Preferences:** Optimized within constraints
   - Trader quality
   - Pair relationships
   - League expertise

## Frequently Asked Questions

### Q1: Why was a lower-skilled trader assigned to a match?
**A:** The system always assigns the best **available** trader. Higher-skilled traders may be:
- Working overlapping higher-value matches
- Outside their shift time
- Have league in ignore list
- Already at assigned job capacity
- Not EXPT-capable for EXPT matches

### Q2: Can I manually override assignments?
**A:** Yes, but document the reason. High manual override rates (>10%) suggest system configuration needs review.

### Q3: Why do non-managers get priority over managers?
**A:** Managers need to remain available for:
- Team supervision and support
- Client escalations
- Operational issues
- Training and mentoring

### Q4: What happens if a match can't be assigned?
**A:** Match remains unassigned (N/A in response). Review trader availability and constraints. May need to:
- Add more traders to shift
- Remove matches from ignore league lists
- Resolve schedule conflicts

### Q5: How are Primary/Secondary/Backup pairs determined?
**A:** Based on historical performance data and team manager configuration. Primary pairs show consistent excellent performance together.

### Q6: What is EXPT match restriction?
**A:** EXPT (Expert) matches require traders with proven expertise. Only traders marked as `isEXPTMatchAssignable: true` can work EXPT matches. This ensures quality on specialized matches.

### Q7: Why does EPL get special treatment?
**A:** EPL is high-visibility, high-volume, complex league. Small priority bonus (1.1x) acts as tie-breaker when other factors are equal.

### Q8: How do I add a new preconfigured pair?
**A:** Add to `preconfiguredPairs` array in request:
```json
{
  "hdp": "TraderName1",
  "ou": "TraderName2",
  "tier": "primary",  // or "secondary" or "backup"
  "leaguePriority": {
    "1": ["EPL", "LaLiga"],
    "2": ["SerieA"],
    "3": ["Ligue1"]
  }
}
```

### Q9: What if solver returns "Infeasible"?
**A:** No valid assignment exists given constraints. Common causes:
- No traders available during match times
- All traders missing required skills
- All traders have conflicting assignments
- All matches in traders' ignore leagues

Check trader availability and constraint settings.

### Q10: How long does optimization take?
**A:** Typically 1-10 seconds. Complex scenarios (50+ traders, 30+ matches) may take up to 30 seconds. This is normal for optimization algorithms.

## Related Documentation

- **M1 Assignment Rules:** For Minor 1 matches (40K-70K)
- **M2 Assignment Rules:** For Minor 2 matches (20K-40K)
- **Base Strategy Documentation:** Common helper methods and validations
- **API Endpoints:** `/api/TraderAssignment/mj` - POST endpoint for MJ assignments
- **n8n Workflow Guide:** Data transformation pipeline documentation
- **Google Sheets Setup:** Excel data format and parsing rules

## Glossary

**CP-SAT Solver:** Constraint Programming Satisfiability solver - finds optimal solution respecting all constraints

**HDP:** Handicap betting role - one of two required roles for MJ matches

**OU:** Over/Under betting role - second required role for MJ matches

**MaxBet:** Maximum betting amount for a match - determines match classification

**Preconfigured Pair:** Pre-defined HDP+OU trader combination with proven performance

**EXPT Match:** Expert-level match requiring specially qualified traders

**Assigned Job:** Existing trader commitment that creates scheduling conflict

**Virtual Match:** Short-duration automated matches (PES, PK types) requiring 15-min gap

**Ignore League:** Trader preference to skip certain leagues they don't want to trade

**All-or-Nothing Constraint:** Match must have both HDP and OU assigned, or neither

**Shift Coverage:** Trader's working hours - match must fit completely within shift
