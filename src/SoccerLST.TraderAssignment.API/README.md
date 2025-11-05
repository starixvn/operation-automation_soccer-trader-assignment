# Trader Assignment API

A .NET 8 Web API that optimizes trader assignments to soccer matches using Google OR-Tools constraint programming solver.

## Overview

This API is a .NET implementation of the Python-based trader assignment system (`MJTraderAssignment.py`). It uses constraint programming to optimally assign traders to soccer matches based on:

- Trader availability and skills (HDP, OU)
- Trader levels (Elite, Member, First) and manager status
- Preconfigured trader pairs with tier bonuses
- League priorities for specific pairs
- Match importance (maxBet values)

## Features

- **Constraint-Based Optimization**: Uses Google OR-Tools CP-SAT solver
- **Priority System**: Weighted scoring based on trader level and manager status
- **Preconfigured Pairs**: Support for primary, secondary, and backup pairs with bonuses
- **League Matching**: League-specific priority bonuses for preconfigured pairs
- **Conflict Detection**: Handles assigned jobs and shift overlaps
- **RESTful API**: Clean HTTP endpoints with Swagger documentation

## Getting Started

### Prerequisites

- .NET 8 SDK
- Google.OrTools NuGet package (already included)

### Running the API

```bash
cd D:\Work\LST\soccer-lst\src\TraderAssignmentApi
dotnet run
```

The API will start at:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`
- Swagger UI: `https://localhost:5001/swagger`

## API Endpoints

### POST `/api/TraderAssignment/optimize`

Optimizes trader assignments for the given matches and trader shifts.

**Request Body:**
```json
{
  "TraderShifts": [...],
  "Matches": [...],
  "PreconfiguredPairs": [...]
}
```

**Response:**
```json
{
  "Success": true,
  "Status": "Optimal",
  "TotalMatches": 20,
  "AssignedMatches": 18,
  "Assignments": [...],
  "Summary": {
    "PreconfiguredPairsUsed": 5,
    "PrimaryPairs": 3,
    "SecondaryPairs": 1,
    "BackupPairs": 1
  }
}
```

### GET `/api/TraderAssignment/health`

Health check endpoint.

## Data Models

### TraderShift
```json
{
  "Id": "30",
  "Name": "CLGKelven",
  "Start": "2025-09-28T02:00:00.000Z",
  "End": "2025-09-29T13:00:00.000Z",
  "Skills": ["OU"],
  "Level": "First",
  "IsManager": true,
  "AssignedJobs": [
    {
      "Type": "BS",
      "Start": "2025-09-28T05:00:00.000Z",
      "End": "2025-09-28T14:00:00.000Z"
    }
  ]
}
```

### Match
```json
{
  "Id": "152",
  "MatchNo": "152",
  "Start": "2025-09-28T18:30:00.000Z",
  "End": "2025-09-28T20:30:00.000Z",
  "MaxBet": 120000,
  "LeagueCode": "ITA"
}
```

### PreconfiguredPair
```json
{
  "Hdp": "CLGKilos",
  "Ou": "CLGKit",
  "Tier": "primary",
  "LeaguePriority": {
    "1": ["SPA"],
    "2": ["ITA"],
    "3": ["EPL"]
  }
}
```

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

1. Each match has at most 1 HDP trader + 1 OU trader
2. Traders cannot work overlapping matches
3. Traders cannot work both roles in the same match
4. Matches must have both roles assigned or no roles at all
5. Traders cannot be assigned during their assigned jobs (BS, etc.)

## Testing

Use the included `sample-request.json` file to test the API:

```bash
curl -X POST https://localhost:5001/api/TraderAssignment/optimize \
  -H "Content-Type: application/json" \
  -d @sample-request.json
```

Or use the Swagger UI at `https://localhost:5001/swagger` for interactive testing.

## Project Structure

```
TraderAssignmentApi/
├── Controllers/
│   └── TraderAssignmentController.cs
├── Models/
│   ├── AssignedJob.cs
│   ├── AssignmentRequest.cs
│   ├── AssignmentResponse.cs
│   ├── Match.cs
│   ├── MatchAssignment.cs
│   ├── PreconfiguredPair.cs
│   └── TraderShift.cs
├── Services/
│   └── TraderAssignmentService.cs
├── Program.cs
├── sample-request.json
└── README.md
```

## Differences from Python Version

- Uses Google.OrTools for .NET instead of Python OR-Tools
- RESTful API instead of command-line script
- Structured response format with detailed assignment information
- Swagger/OpenAPI documentation
- Dependency injection and modern .NET patterns

## License

Internal use only.
