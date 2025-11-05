# Trader Assignment API

A .NET 8 Web API that optimizes trader assignments to soccer matches using Google OR-Tools constraint programming solver.

## Overview

This API uses constraint programming to optimally assign traders to soccer matches across three match tiers (MJ, M1, M2) based on:

- **Trader availability and skills** (HDP, OU)
- **Trader levels** (Elite, Member, First) and manager status
- **Match tier** based on MaxBet value (MJ >= 70K, M1: 40K-70K, M2: 20K-40K)
- **Preconfigured trader pairs** with tier bonuses (primary, secondary, backup)
- **League priorities** for specific pairs
- **Match importance** (MaxBet values)
- **Special match types** (EXPT, EAL) with specific trader requirements
- **Trader capacities** for handling multiple M1/M2 matches simultaneously

## Architecture

**Strategy Pattern Implementation:**

- `TraderAssignmentService`: Orchestrates assignment operations by delegating to match-type-specific strategies
- `MajorMatchAssignmentStrategy`: Handles MJ matches (MaxBet >= 70,000)
- `MinorOneMatchAssignmentStrategy`: Handles M1 matches (MaxBet 40,000-70,000)
- `MinorTwoMatchAssignmentStrategy`: Handles M2 matches (MaxBet 20,000-40,000)

Each strategy implements `IMatchAssignmentStrategy` and uses Google OR-Tools CP-SAT solver for constraint-based optimization.

## Features

- **Three-Tier Match Assignment**: Separate endpoints for MJ, M1, and M2 matches
- **Constraint-Based Optimization**: Uses Google OR-Tools CP-SAT solver
- **Priority System**: Weighted scoring based on trader level and manager status
- **Preconfigured Pairs**: Support for primary, secondary, and backup pairs with bonuses
- **League Matching**: League-specific priority bonuses for preconfigured pairs
- **Conflict Detection**: Handles assigned jobs and shift overlaps
- **Capacity Management**: M1/M2 traders can handle multiple matches simultaneously
- **Special Match Handling**: EXPT and EAL matches with trader restrictions
- **RESTful API**: Clean HTTP endpoints with Swagger documentation

## Getting Started

### Prerequisites

- .NET 8 SDK
- Google.OrTools NuGet package (already included)

### Running the API

```bash
cd src/SoccerLST.TraderAssignment.API
dotnet run
```

The API will start at:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`
- Swagger UI: `https://localhost:5001/swagger`

## API Endpoints

### POST `/api/TraderAssignment/mj`

Assigns traders to Major (MJ) matches with MaxBet >= 70,000.

**Request Body:**
```json
{
  "traderShifts": [...],
  "matches": [...],
  "preconfiguredPairs": [...]
}
```

**Response:**
```json
{
  "success": true,
  "status": "Optimal",
  "totalMatches": 20,
  "assignedMatches": 18,
  "assignments": [...],
  "summary": {
    "preconfiguredPairsUsed": 5,
    "primaryPairs": 3,
    "secondaryPairs": 1,
    "backupPairs": 1
  }
}
```

### POST `/api/TraderAssignment/m1`

Assigns traders to Minor 1 (M1) matches with MaxBet between 40,000 and 70,000.

**Validation:** Returns 400 Bad Request if any match has MaxBet outside the 40K-70K range.

**Request/Response:** Same format as `/mj` endpoint.

### POST `/api/TraderAssignment/m2`

Assigns traders to Minor 2 (M2) matches with MaxBet between 20,000 and 40,000.

**Validation:** Returns 400 Bad Request if any match has MaxBet outside the 20K-40K range.

**Request/Response:** Same format as `/mj` endpoint.

### GET `/health`

Health check endpoint.

**Response:**
```json
{
  "status": "healthy",
  "service": "SoccerLST.TraderAssignment.API",
  "timestamp": "2025-01-15T10:30:00Z"
}
```

## Data Models

### TraderShift

```json
{
  "id": "30",
  "name": "CLGKelven",
  "start": "2025-09-28T02:00:00.000Z",
  "end": "2025-09-29T13:00:00.000Z",
  "skills": ["OU"],
  "level": "First",
  "isManager": true,
  "m1Capacity": 1,
  "m2Capacity": 1,
  "isEXPTMatchAssignable": true,
  "ignoreLeague": ["ENG2"],
  "assignedJobs": [
    {
      "type": "BS",
      "start": "2025-09-28T05:00:00.000Z",
      "end": "2025-09-28T14:00:00.000Z"
    }
  ]
}
```

**Fields:**

- `id`: Unique identifier (usually row number from source data)
- `name`: Trader name
- `start`/`end`: Shift availability window (UTC DateTimeOffset)
- `skills`: Array of skills - `["HDP"]`, `["OU"]`, or `["HDP", "OU"]`
- `level`: Trader level - `"Elite"`, `"Member"`, or `"First"`
- `isManager`: Whether trader has manager duties (affects priority)
- `m1Capacity`: Number of M1 matches trader can handle simultaneously (default: 1)
- `m2Capacity`: Number of M2 matches trader can handle simultaneously (default: 1)
- `isEXPTMatchAssignable`: Whether trader can be assigned to EXPT matches (default: true)
- `ignoreLeague`: (Optional) Array of league codes to exclude this trader from
- `assignedJobs`: (Optional) Pre-existing job assignments that block trader availability

### Match

```json
{
  "id": "152",
  "matchNo": "152",
  "start": "2025-09-28T18:30:00.000Z",
  "end": "2025-09-28T20:30:00.000Z",
  "maxBet": 120000,
  "leagueCode": "ITA",
  "leagueName": "Italy Serie A",
  "isAlreadyHandled": false,
  "isEXPT": false,
  "isEAL": false
}
```

**Fields:**

- `id`: Unique match identifier
- `matchNo`: Display match number
- `start`/`end`: Match timing window (UTC DateTimeOffset)
- `maxBet`: Maximum bet amount (determines match tier: MJ >= 70K, M1: 40K-70K, M2: 20K-40K)
- `leagueCode`: Short league code (e.g., "EPL", "SPA", "ITA")
- `leagueName`: Full league name
- `isAlreadyHandled`: If true, match is skipped from optimization (already assigned)
- `isEXPT`: Expert match flag (requires `isEXPTMatchAssignable: true` traders)
- `isEAL`: English league flag (special handling rules)

### PreconfiguredPair

```json
{
  "hdp": "CLGKilos",
  "ou": "CLGKit",
  "tier": "primary",
  "leaguePriority": {
    "1": ["SPA", "EPL"],
    "2": ["ITA"],
    "3": ["GER"]
  }
}
```

**Fields:**

- `hdp`: Trader name for HDP (handicap) role
- `ou`: Trader name for OU (over/under) role
- `tier`: Pair importance - `"primary"` (1.5x bonus), `"secondary"` (1.3x), or `"backup"` (1.2x)
- `leaguePriority`: (Optional) League-specific bonuses - priority 1 (1.5x), 2 (1.3x), 3 (1.1x)

## Scoring System

### Level Points
- **Elite**: 256 points
- **Member**: 16 points
- **First**: 4 points

### Priority Multipliers
- **Elite non-manager**: 2.0x (highest priority)
- **Elite manager**: 1.8x
- **Member non-manager**: 1.3x
- **First non-manager**: 1.2x
- **Member manager**: 1.1x
- **First manager**: 1.0x (lowest priority)

### Tier Bonuses (Preconfigured Pairs)
- **Primary**: 1.5x
- **Secondary**: 1.3x
- **Backup**: 1.2x

### League Priority Bonuses
- **1st priority**: 1.5x
- **2nd priority**: 1.3x
- **3rd priority**: 1.1x

### Pair Score Formula
```
Pair Score = (HDP_points × HDP_priority) × (OU_points × OU_priority) × Tier_Bonus × League_Bonus
```

### Objective Function
```
Maximize: Σ (Pair_Score × (MaxBet/1000)²)
```

## Constraints

The optimization engine enforces these constraints:

1. **Match Assignment**: Each match has at most 1 HDP trader + 1 OU trader
2. **No Overlaps**: Traders cannot work overlapping matches (MJ only; M1/M2 allow overlaps based on capacity)
3. **Role Exclusivity**: Traders cannot work both HDP and OU roles in the same match
4. **All-or-Nothing**: Matches must have both roles assigned or no roles at all
5. **Job Conflicts**: Traders cannot be assigned during their `assignedJobs` (BS, etc.)
6. **Skill Matching**: Traders must have the required skill (HDP or OU) for their assigned role
7. **Shift Boundaries**: Match timing must fall within trader's shift start/end
8. **EXPT Eligibility**: EXPT matches can only be assigned to traders with `isEXPTMatchAssignable: true`
9. **League Exclusion**: Traders cannot be assigned to leagues in their `ignoreLeague` list
10. **Capacity Limits**: M1/M2 traders are limited by `m1Capacity`/`m2Capacity` for simultaneous matches

## Testing

### Using cURL

```bash
# Test MJ endpoint
curl -X POST http://localhost:5000/api/TraderAssignment/mj \
  -H "Content-Type: application/json" \
  -d @sample-mj-request.json

# Test M1 endpoint
curl -X POST http://localhost:5000/api/TraderAssignment/m1 \
  -H "Content-Type: application/json" \
  -d @sample-m1-request.json

# Test M2 endpoint
curl -X POST http://localhost:5000/api/TraderAssignment/m2 \
  -H "Content-Type: application/json" \
  -d @sample-m2-request.json

# Health check
curl http://localhost:5000/health
```

### Using Swagger UI

Navigate to `http://localhost:5000/swagger` for interactive API testing with auto-generated documentation.

### Using Docker

```bash
# Build Docker image
docker build -t trader-assignment-api .

# Run container
docker run -d -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development --name trader-assignment-api trader-assignment-api

# Test the API
curl -X POST http://localhost:8080/api/TraderAssignment/mj \
  -H "Content-Type: application/json" \
  -d @sample-mj-request.json
```

## Project Structure

```
SoccerLST.TraderAssignment/
├── src/
│   ├── SoccerLST.TraderAssignment.API/          # Web API layer
│   │   ├── Controllers/
│   │   │   └── TraderAssignmentController.cs    # MJ, M1, M2 endpoints
│   │   ├── Program.cs                           # Startup, DI, middleware
│   │   └── README.md                            # This file
│   │
│   └── SoccerLST.TraderAssignment.Core/         # Business logic
│       ├── Models/                              # Domain models
│       │   ├── AssignedJob.cs
│       │   ├── AssignmentRequest.cs
│       │   ├── AssignmentResponse.cs
│       │   ├── Match.cs
│       │   ├── MatchAssignment.cs
│       │   ├── PreconfiguredPair.cs
│       │   └── TraderShift.cs
│       │
│       ├── Services/
│       │   ├── TraderAssignmentService.cs       # Orchestration service
│       │   ├── IMatchAssignmentStrategy.cs      # Strategy interface
│       │   └── Strategies/
│       │       ├── MajorMatchAssignmentStrategy.cs
│       │       ├── MinorOneMatchAssignmentStrategy.cs
│       │       └── MinorTwoMatchAssignmentStrategy.cs
│       │
│       └── Extensions/
│           └── ServiceCollectionExtensions.cs   # DI registration
│
├── Dockerfile
└── .dockerignore
```

## Integration with n8n Workflows

This API is designed to work with n8n workflows that:

1. **Parse Excel data** from Google Sheets (trader shifts, matches, pairs)
2. **Transform data** to API format (see `n8n/codenode/build_mj_api_param.js`)
3. **Call appropriate endpoint** (`/mj`, `/m1`, or `/m2`) based on match tier
4. **Process assignments** and update tracking sheets

The API expects **camelCase** JSON (enforced by `JsonNamingPolicy.CamelCase` in [Program.cs:10](Program.cs#L10)).

## Key Design Decisions

- **Strategy Pattern**: Each match type (MJ, M1, M2) has its own strategy implementation, making it easy to customize rules per tier
- **Validation at Controller**: Match tier validation happens at the controller level before delegating to strategies
- **Constraint Programming**: Google OR-Tools CP-SAT solver finds optimal assignments under complex constraints
- **camelCase JSON**: Consistent with JavaScript/n8n ecosystem
- **Dependency Injection**: Clean separation of concerns with DI container

## Performance

- Typical solve time: 1-5 seconds for 20-50 matches with 10-30 traders
- CP-SAT solver status: `Optimal`, `Feasible`, or `Infeasible`
- Docker image: ~220MB (multi-stage build with runtime-only dependencies)

## License

Internal use only.
