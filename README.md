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
└─ Soccer_LST_M1_Assignment (Minor 1: 40K-70K maxBet)
    ↓
.NET API (http://soccer-lst.poc.consultechailab.com)
├─ TraderAssignmentService (OR-Tools CP-SAT)
└─ Constraints: overlaps, skills, pairs, capacity
    ↓
Google Sheets (HDP/OU columns updated)
```

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

1. Import 3 workflows from `n8n/workflow/` directory
2. Configure Google Sheets OAuth2 credentials
3. Update API endpoint: `http://your-api-host:8080/api/TraderAssignment/optimize`
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

### POST /api/TraderAssignment/optimize

**Request:**
```json
{
  "matches": [{
    "id": "1", "matchNo": "1",
    "start": "2025-01-15T14:00:00Z", "end": "2025-01-15T16:00:00Z",
    "maxBet": 70000, "leagueCode": "EPL", "isAlreadyHandled": false
  }],
  "traderShifts": [{
    "id": "1", "name": "Alice",
    "start": "2025-01-15T12:00:00Z", "end": "2025-01-15T20:00:00Z",
    "skills": ["HDP"], "level": "Elite", "isManager": false,
    "m1Capacity": 2, "m2Capacity": 1, "assignedJobs": []
  }],
  "preconfiguredPairs": [{
    "hdp": "Alice", "ou": "Bob", "tier": "primary",
    "leaguePriority": { "1": ["EPL"], "2": ["SPA"] }
  }]
}
```

**Response:**
```json
{
  "assignments": [{"matchId": "1", "hdp": "Alice", "ou": "Bob", "score": 98304.0}],
  "totalScore": 98304.0, "matchesAssigned": 1, "matchesUnassigned": 0
}
```

## Optimization

### Constraints
1. No time overlaps
2. Both HDP and OU assigned or neither
3. Skills match (trader has HDP/OU)
4. Respect existing assignedJobs
5. No dual roles (same trader for HDP and OU)

### Scoring
```
Base Points: Elite: 256, Member: 16, First: 4
Priority Multiplier: Elite non-manager: 2.0x, Elite manager: 1.8x
Tier Bonus: Primary: 1.5x, Secondary: 1.3x, Backup: 1.2x
League Priority: 1st: 1.5x, 2nd: 1.3x, 3rd: 1.1x

Formula: Pair_Score × (MaxBet/1000)²
Example: Elite+Elite EPL primary = 6,502,809,600 points
```

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
soccer-lst/
├── src/
│   ├── SoccerLST.TraderAssignment.API/     # Web API
│   └── SoccerLST.TraderAssignment.Core/    # Business logic, OR-Tools
├── tests/
│   └── SoccerLST.TraderAssignment.Core.Tests/  # 23 unit tests
├── n8n/
│   ├── workflow/                           # 3 workflow JSON files
│   └── codenode/
│       ├── ExcelParser/                    # 9 parsing files
│       ├── MJ/                             # 3 MJ assignment files
│       └── M1/                             # 2 M1 assignment files
├── Dockerfile
└── README.md
```

## Development

### Run Tests
```bash
dotnet test  # 23 tests, <1 second
```

### Add New Match Property
1. Update `Match.cs`
2. Update `build_mj_api_param.js`
3. Rebuild: `dotnet build && docker build -t trader-assignment-api .`

### Sync Code Nodes
After editing in n8n, export workflow and save to `n8n/workflow/`

## Troubleshooting

**API 500 Error:** Check logs `docker logs trader-assignment-api`

**Slow API (>10s):** Reduce matches or increase Docker RAM to 4GB+

**Google Sheets Permission Error:** Re-authorize OAuth2 credential

**Unassigned Matches:** No trader available during match time or missing skills

## Performance

- Solve time: 0.5-2 seconds
- Handles: 50+ matches, 20+ traders
- API response: <3 seconds end-to-end
- Tests: 23 unit tests, 100% pass rate

## Support

- **API Docs:** http://localhost:5000/swagger
- **Developer Guide:** See `CLAUDE.md`
