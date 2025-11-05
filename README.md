# Soccer LST Trader Assignment System

Automated constraint-based optimization system that assigns soccer traders to live matches using Google OR-Tools CP-SAT solver.

## Overview

**Problem:** Manual trader assignment takes 2-3 hours daily with frequent conflicts and suboptimal pairings.

**Solution:** Automated optimization in <2 seconds with zero conflicts, optimal pairs, and respect for capacity limits.

**Tech Stack:** .NET 8, Google OR-Tools, n8n, Google Sheets

## Architecture

```
Google Sheets (Soccer LST spreadsheet)
    ↓
n8n Workflows
├─ Soccer_LST_Parse_Excel_File (parse 6 sheets, detect bet types)
├─ Soccer_LST_MJ_Assignment (Major: ≥70K maxBet)
├─ Soccer_LST_M1_Assignment (Minor 1: 40K-70K maxBet)
└─ Soccer_LST_M2_Assignment (Minor 2: 20K-40K maxBet)
    ↓
.NET API (http://soccer-lst.poc.consultechailab.com)
├─ TraderAssignmentController (/mj, /m1, /m2 endpoints)
├─ TraderAssignmentService (Strategy orchestrator)
└─ Assignment Strategies (MJ, M1, M2 - OR-Tools CP-SAT)
    ↓
Google Sheets (HDP/OU columns updated)
```

### Strategy Pattern Implementation

The system uses a **Strategy Pattern** for different match types:

- **MajorMatchAssignmentStrategy** (MJ): MaxBet ≥ 70K
  - Dual-role assignment (separate HDP & OU traders)
  - Uses preconfigured pairs with tier bonuses
  - League priority multipliers (EPL priority)
  - CP-SAT constraint programming optimization

- **MinorOneMatchAssignmentStrategy** (M1): MaxBet 40K-70K
  - Single trader handles both HDP & OU roles
  - M1Capacity for concurrent match handling
  - Priority-based distribution

- **MinorTwoMatchAssignmentStrategy** (M2): MaxBet 20K-40K
  - Single trader, concurrent capability
  - M2Capacity for workload balancing
  - Lowest priority assignments

## Quick Start

### 1. Run API Locally

```bash
cd D:\Work\LST\soccer-lst
dotnet build src/SoccerLST.TraderAssignment.API/SoccerLST.TraderAssignment.API.csproj
dotnet run --project src/SoccerLST.TraderAssignment.API/SoccerLST.TraderAssignment.API.csproj

# Access:
# - Swagger: http://localhost:5000/swagger
# - Health: http://localhost:5000/api/TraderAssignment/health
```

### 2. Run with Docker

```bash
docker build -t trader-assignment-api .
docker run -d -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development \
  --name trader-assignment-api trader-assignment-api

# Test
curl http://localhost:8080/api/TraderAssignment/health
```

### 3. Setup n8n Workflows

1. Import 4 workflows from `n8n/workflow/` directory:
   - `Soccer_LST_Parse_Excel_File.json`
   - `Soccer_LST_MJ_Assignment.json`
   - `Soccer_LST_M1_Assignment.json`
   - `Soccer_LST_M2_Assignment.json`
2. Configure Google Sheets OAuth2 credentials
3. Update API endpoints:
   - MJ: `http://your-api-host:8080/api/TraderAssignment/mj`
   - M1: `http://your-api-host:8080/api/TraderAssignment/m1`
   - M2: `http://your-api-host:8080/api/TraderAssignment/m2`
4. Set spreadsheet ID: `1XptidH4Dt8b7DhTgpVDGjTs4fGxGHRFNJd0fqzaaR-M`

## Google Sheets Structure

**TimeTable Sheet** (Trader Shifts)
```
Row | Date  | Trader | MYT   | LST Note | Busy      | bettype
19  | 45678 | Alice  | 12>20 | E1       | 14>15     | HDP/M1*2/M2*1
```
- `Date`: Excel date (45678 = 2025-01-15)
- `MYT`: Malaysia time "12>20" = 12:00 to 20:00
- `Busy`: Comma-separated "14>15,18>19"
- `bettype`: Skills and capacity "HDP/M1*2" = HDP skill, M1 capacity 2

**Mismatch Sheet** (Matches)
```
Row | No | Time    | League          | MaxBet | HDP | OU | Unchanged
3   | 1  | 2:00 PM | ENGLISH PREMIER | 70K    |     |    |
```
- Mark "x" in `Unchanged` to lock assignment

**Other Sheets:** Traders, Pair Trader, Config, Trigger

## API Reference

### Endpoints

#### POST /api/TraderAssignment/mj

Assign Major matches (maxBet ≥ 70,000)

#### POST /api/TraderAssignment/m1

Assign Minor 1 matches (40,000 ≤ maxBet < 70,000)

#### POST /api/TraderAssignment/m2

Assign Minor 2 matches (20,000 ≤ maxBet < 40,000)

#### GET /api/TraderAssignment/health

Health check endpoint

### Request Format (All Endpoints)

**Request Body:**
```json
{
  "matches": [{
    "id": "1",
    "matchNo": "1",
    "start": "2025-01-15T14:00:00Z",
    "end": "2025-01-15T16:00:00Z",
    "maxBet": 70000,
    "leagueCode": "EPL",
    "leagueName": "ENGLISH PREMIER",
    "isAlreadyHandled": false,
    "isEXPT": false,
    "isEAL": false
  }],
  "traderShifts": [{
    "id": "1",
    "name": "Alice",
    "start": "2025-01-15T12:00:00Z",
    "end": "2025-01-15T20:00:00Z",
    "skills": ["HDP", "OU"],
    "level": "Elite",
    "isManager": false,
    "m1Capacity": 2,
    "m2Capacity": 1,
    "ignoreLeague": ["SPA"],
    "assignedJobs": [
      {
        "type": "MJ",
        "start": "2025-01-15T10:00:00Z",
        "end": "2025-01-15T12:00:00Z"
      }
    ],
    "isEXPTMatchAssignable": true
  }],
  "preconfiguredPairs": [{
    "hdp": "Alice",
    "ou": "Bob",
    "tier": "primary",
    "leaguePriority": {
      "1": ["EPL"],
      "2": ["SPA"],
      "3": ["ITL"]
    }
  }]
}
```

### Response Format

**Success Response:**
```json
{
  "success": true,
  "status": "OPTIMAL",
  "totalMatches": 10,
  "assignedMatches": 8,
  "assignments": [
    {
      "rowNumber": "3",
      "match": "1",
      "league": "EPL",
      "maxBet": 70000,
      "time": "2:00 PM",
      "hdp": "Alice",
      "hdpLevel": "Elite",
      "ou": "Bob",
      "ouLevel": "Elite",
      "pairType": "primary",
      "score": 98304
    }
  ],
  "summary": {
    "preconfiguredPairsUsed": 5,
    "primaryPairs": 3,
    "secondaryPairs": 1,
    "backupPairs": 1
  }
}
```

**Validation Error Response:**
```json
{
  "success": false,
  "status": "Invalid match range: All matches must have maxBet between 40000 and 69999 for M1 assignment",
  "totalMatches": 0,
  "assignedMatches": 0,
  "assignments": []
}
```

## Optimization Rules

### Core Constraints (MJ Matches)

1. **Match Coverage**: Each match assigned at most 1 HDP trader and 1 OU trader
2. **No Time Overlaps**: Traders cannot work on overlapping matches
3. **No Dual Roles**: Same trader cannot be both HDP and OU for a match
4. **All-or-Nothing**: Matches must have both roles assigned or neither
5. **Skills Match**: Trader must have required skill (HDP or OU)
6. **Shift Availability**: Match must fall within trader's shift time
7. **Assigned Jobs**: No conflicts with existing assignedJobs from previous levels
8. **League Restrictions**: Respect trader's ignoreLeague preferences
9. **EXPT Capability**: Only traders with isEXPTMatchAssignable can handle EXPT matches
10. **Virtual Match Gap**: 15-minute gap required after PES/PK virtual matches

### M1/M2 Specific Rules

**M1 (Single Trader Model):**

- One trader handles both HDP & OU roles
- M1Capacity allows concurrent match handling (e.g., 2 matches simultaneously)
- Priority-based distribution to balance workload

**M2 (High Concurrency):**

- One trader handles both roles
- M2Capacity typically higher (e.g., 3-5 concurrent matches)
- Workload balancing across available traders

### Scoring System (MJ Only)

**Level Base Points:**
```
Elite: 256 points
Member: 16 points
First: 4 points
```

**Priority Multipliers:**
```
Elite non-manager: 2.0x (highest priority)
Elite manager: 1.8x
Member non-manager: 1.3x
First non-manager: 1.2x
Member manager: 1.1x
First manager: 1.0x (lowest priority)
```

**Preconfigured Pair Tier Bonuses:**
```
Primary: 1.5x
Secondary: 1.3x
Backup: 1.2x
```

**League Priority Bonuses:**
```
1st priority league: 1.5x
2nd priority league: 1.3x
3rd priority league: 1.1x
```

**Objective Function:**
```
Pair_Score = (HDP_points × HDP_priority) × (OU_points × OU_priority)
             × Tier_Bonus × League_Bonus

Total_Score = Σ (Pair_Score × (MaxBet/1000)²)

Example: Elite+Elite EPL primary pair for 70K match = 6,502,809,600 points
```

### Virtual Match Types

These match types require a 15-minute gap before next assignment:

- **PES5MIN**: 5-minute PES virtual matches
- **PES24**: 24-minute PES virtual matches
- **PKIF**: Penalty kick international friendly
- **PKCF**: Penalty kick club friendly

## Manual Operation Workflow

### Sequential Process: MJ → M1 → M2 → M3 → M4

**For Each Level:**

1. **Activate Workflow** (n8n)
   - Deactivate previous workflow
   - Activate current workflow (toggle ON)
   - Change Trigger cell A1 value
   - Wait 30-60 seconds

2. **Review Assignments** (Google Sheets)
   - Check HDP/OU columns filled
   - Verify no conflicts with higher priority levels
   - Check capacity limits (m1Capacity, m2Capacity)
   - Manually adjust if needed

3. **Lock Assignments**
   - Mark "x" in Unchanged column for approved matches
   - Leave blank for matches to re-optimize

4. **Move to Next Level**

### Best Practices

✅ **DO:** Start with MJ, review each level, mark "x" for approved, deactivate previous workflow

⚠️ **DON'T:** Skip levels, run multiple workflows simultaneously, exceed capacity limits

### Common Issues

**Blank HDP/OU:** No available trader → Check TimeTable sheet availability

**Capacity Exceeded:** Update bettype in Traders sheet (e.g., "HDP/M1*3")

**Workflow Fails:** Verify 6 sheets exist with correct names

## Project Structure

```
soccer-trader-assignment/
├── src/
│   ├── SoccerLST.TraderAssignment.API/           # ASP.NET Core Web API
│   │   ├── Controllers/
│   │   │   └── TraderAssignmentController.cs     # /mj, /m1, /m2 endpoints
│   │   ├── Program.cs                            # DI, middleware, Swagger
│   │   ├── appsettings.json                      # Configuration
│   │   └── sample-request.json                   # Example API request
│   └── SoccerLST.TraderAssignment.Core/          # Business Logic
│       ├── Models/                               # Domain models
│       │   ├── Match.cs
│       │   ├── TraderShift.cs
│       │   ├── PreconfiguredPair.cs
│       │   ├── AssignmentRequest.cs
│       │   ├── AssignmentResponse.cs
│       │   └── AssignedJob.cs
│       ├── Services/
│       │   ├── TraderAssignmentService.cs        # Strategy orchestrator
│       │   ├── IMatchAssignmentStrategy.cs       # Strategy interface
│       │   └── Strategies/
│       │       ├── RealMatchAssignmentStrategyBase.cs
│       │       ├── MajorMatchAssignmentStrategy.cs      # MJ (OR-Tools)
│       │       ├── MinorOneMatchAssignmentStrategy.cs   # M1
│       │       └── MinorTwoMatchAssignmentStrategy.cs   # M2
│       └── Extensions/
│           └── ServiceCollectionExtensions.cs    # DI registration
├── tests/
│   └── SoccerLST.TraderAssignment.Core.Tests/    # NUnit tests
│       ├── Services/
│       │   ├── TraderAssignmentServiceTests.cs
│       │   └── Strategies/
│       │       ├── MajorMatchAssignmentStrategyTests.cs
│       │       ├── MajorMatchAssignmentStrategy.EPLPriorityTests.cs
│       │       ├── MajorMatchAssignmentStrategy.GapRulesTests.cs
│       │       ├── MinorOneMatchAssignmentStrategyTests.cs
│       │       ├── MinorOneMatchAssignmentStrategy.*.Tests.cs (5 files)
│       │       ├── MinorTwoMatchAssignmentStrategyTests.cs
│       │       └── MinorTwoMatchAssignmentStrategy.*.Tests.cs (3 files)
│       └── Utils/
│           └── TestHelpers.cs
├── n8n/
│   ├── workflow/                                 # n8n workflow JSON files
│   │   ├── Soccer_LST_Parse_Excel_File.json      # Parser (6 sheets)
│   │   ├── Soccer_LST_MJ_Assignment.json         # MJ workflow
│   │   ├── Soccer_LST_M1_Assignment.json         # M1 workflow
│   │   └── Soccer_LST_M2_Assignment.json         # M2 workflow
│   ├── codenode/                                 # JavaScript transformations
│   │   ├── ExcelParser/                          # 9 parsing nodes
│   │   │   ├── format_working_shift.js
│   │   │   ├── format_mismatch.js
│   │   │   └── format_pair_trader.js
│   │   ├── MJ/                                   # MJ transformation nodes
│   │   │   ├── build_mj_api_param.js
│   │   │   └── detect_league_code.js
│   │   ├── M1/                                   # M1 transformation nodes
│   │   │   └── build_m1_api_param.js
│   │   └── M2/                                   # M2 transformation nodes
│   │       └── build_m2_api_param.js
│   └── output/                                   # Test/debug outputs
├── docs/
│   ├── MJ_Assignment_Rules.md                    # MJ detailed rules
│   ├── M1_Assignment_Rules.md                    # M1 detailed rules
│   ├── M2_Assignment_Rules.md                    # M2 detailed rules
│   └── League_Code_Detection_Migration.md
├── tempcode/                                     # Temporary test scripts
├── CLAUDE.md                                     # Claude Code instructions
├── README.md                                     # This file
├── Dockerfile                                    # Multi-stage .NET 8 build
├── .dockerignore
├── .gitignore
└── soccer-lst.sln                                # Visual Studio solution
```

## Development

### Technology Stack

**Backend:**

- .NET 8.0 (ASP.NET Core Web API)
- Google OR-Tools v9.14.6206 (CP-SAT constraint solver)
- Microsoft.Extensions.DependencyInjection

**Testing:**

- NUnit 3 (test framework)
- NSubstitute (mocking)
- FluentAssertions (assertion library)

**Data & Integration:**

- n8n (workflow automation)
- Google Sheets (data source)
- JavaScript (n8n code nodes)

**DevOps:**

- Docker (multi-stage build)
- Git version control

### Run Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity detailed

# Run specific test file
dotnet test --filter "FullyQualifiedName~MajorMatchAssignmentStrategyTests"

# Test coverage: 17 test files, comprehensive strategy coverage
```

### Build & Deploy

```bash
# Local development build
dotnet build

# Production Docker build
docker build -t trader-assignment-api .

# Run Docker container
docker run -d -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  --name trader-assignment-api \
  trader-assignment-api

# View logs
docker logs -f trader-assignment-api
```

### Adding New Features

**Add New Match Property:**

1. Update `src/SoccerLST.TraderAssignment.Core/Models/Match.cs`
2. Update n8n transformation: `n8n/codenode/MJ/build_mj_api_param.js`
3. Update n8n workflow: `n8n/workflow/Soccer_LST_MJ_Assignment.json`
4. Rebuild and test: `dotnet build && dotnet test`
5. Rebuild Docker image: `docker build -t trader-assignment-api .`

**Add New Constraint:**

1. Edit strategy file (e.g., `Strategies/MajorMatchAssignmentStrategy.cs`)
2. Add constraint via `model.Add(...)`
3. Write unit test in `tests/` directory
4. Run tests: `dotnet test`

**Modify Scoring:**

1. Edit objective function in strategy file
2. Update constants (level points, multipliers, bonuses)
3. Add tests to verify scoring behavior

### n8n Code Node Workflow

**Pattern Used:**

```javascript
const nodeTypeHandlers = {
  mismatch: (data) => { /* transform match data */ },
  workingShift: (data) => { /* transform trader shift */ },
  pairTrader: (data) => { /* transform pair config */ }
};

for (const item of allItems) {
  const handler = nodeTypeHandlers[item.json.nodeType];
  if (handler) handler(item.json);
}
```

**Sync Code Nodes to Workflow:**

After editing code nodes in n8n:

1. Export workflow from n8n UI
2. Save to `n8n/workflow/` directory
3. Commit changes to Git

**Test Transformations Locally:**

```bash
# Create test scripts in tempcode/ directory
node tempcode/test-transform.js
node tempcode/verify-skills.js
```

## Troubleshooting

**API 500 Error:** Check logs `docker logs trader-assignment-api`

**Slow API (>10s):** Reduce matches or increase Docker RAM to 4GB+

**Google Sheets Permission Error:** Re-authorize OAuth2 credential

**Unassigned Matches:** No trader available during match time or missing skills

## Performance

- **Solve time:** 0.5-2 seconds (MJ optimization with OR-Tools)
- **Capacity:** 50+ matches, 20+ traders per run
- **API response:** <3 seconds end-to-end
- **Tests:** 17 test files, 100+ test cases, 100% pass rate
- **Docker image:** ~200MB (multi-stage build optimization)

## Key Features

- **Zero Conflicts:** Constraint-based optimization ensures no time overlaps
- **Optimal Pairs:** Maximizes trader pair quality with configurable scoring
- **Capacity Aware:** Respects M1/M2 concurrent match limits per trader
- **League Preferences:** Honors trader league restrictions and priorities
- **Cascading Assignments:** MJ → M1 → M2 sequential workflow with job carryover
- **EXPT/EAL Support:** Special match type handling and restrictions
- **Virtual Match Gaps:** Automatic 15-minute gap enforcement after PES/PK matches
- **Real-time Updates:** Direct Google Sheets integration via n8n
- **Configurable Tiers:** Primary/Secondary/Backup pair priorities
- **Workload Balancing:** Fair distribution across traders (M1/M2)

## Support & Documentation

- **API Docs:** [http://localhost:5000/swagger](http://localhost:5000/swagger)
- **Developer Guide:** See [CLAUDE.md](CLAUDE.md)
- **Business Rules:** See [docs/](docs/) directory
  - [MJ_Assignment_Rules.md](docs/MJ_Assignment_Rules.md)
  - [M1_Assignment_Rules.md](docs/M1_Assignment_Rules.md)
  - [M2_Assignment_Rules.md](docs/M2_Assignment_Rules.md)
- **Production API:** <http://soccer-lst.poc.consultechailab.com>

## License

Internal project for Soccer LST trading operations.
