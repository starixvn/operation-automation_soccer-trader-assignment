# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Soccer Live System Trading (LST) trader assignment system consisting of:
- **.NET 8 Trader Assignment API**: Constraint-based optimization engine using Google OR-Tools
- **n8n Workflows**: Data transformation pipelines for Excel parsing and API integration
- **Docker deployment**: Containerized API for production use

## Build and Run Commands

### .NET API

```bash
# Build the API
dotnet build src/SoccerLST.TraderAssignment.API/SoccerLST.TraderAssignment.API.csproj

# Run the API locally
dotnet run --project src/SoccerLST.TraderAssignment.API/SoccerLST.TraderAssignment.API.csproj

# API will be available at:
# - HTTP: http://localhost:5000
# - HTTPS: https://localhost:5001
# - Swagger: http://localhost:5000/swagger
```

### Docker

```bash
# Build Docker image
docker build -t trader-assignment-api .

# Run in Development mode
docker run -d -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development --name trader-assignment-api trader-assignment-api

# Run in Production mode
docker run -d -p 8080:8080 --name trader-assignment-api trader-assignment-api

# Stop and remove container
docker stop trader-assignment-api && docker rm trader-assignment-api

# View logs
docker logs trader-assignment-api
docker logs -f trader-assignment-api  # Follow logs
```

### Test with Sample Data

```bash
# Test with transformed n8n data
curl -X POST http://localhost:8080/api/TraderAssignment/optimize \
  -H "Content-Type: application/json" \
  -d @n8n/output/transformed_api_params.json

# Health check
curl http://localhost:8080/api/TraderAssignment/health
```

## Architecture

### High-Level System Flow

```
Excel Data (Google Sheets)
  ↓
n8n Workflow: Soccer_LST_Parse_Excel_File
  ├─ Extracts: Working Shifts, Matches, Trader Pairs, Config
  ├─ Transforms: Date/time parsing, betType detection, level normalization
  └─ Outputs: Standardized data with nodeType tags
  ↓
n8n Workflow: Soccer_LST_MJ_Assignment
  ├─ Filters: Only MJ (Major) matches
  ├─ Enriches: Adds assigned jobs from unchanged matches
  ├─ Transforms: Maps fields to API format (build_mj_api_param.js)
  └─ Calls: POST /api/TraderAssignment/mj
  ↓
Trader Assignment API (.NET)
  ├─ Constraint Engine: Google OR-Tools CP-SAT solver
  ├─ Optimization: Maximizes weighted pair scores
  └─ Returns: Optimal trader-to-match assignments
```

### .NET API Architecture

**Two-Project Solution:**
- `SoccerLST.TraderAssignment.Core`: Business logic, models, optimization service
- `SoccerLST.TraderAssignment.API`: Web API layer, controllers, middleware

**Key Components:**

1. **TraderAssignmentService** (Core/Services/)
   - Uses Google OR-Tools CP-SAT solver for constraint programming
   - Builds decision variables for all trader-match-role combinations
   - Implements 5 core constraints (no overlaps, both roles or none, etc.)
   - Calculates weighted scores based on trader levels, pairs, and leagues

2. **Models** (Core/Models/)
   - `Match`: Represents a soccer match with timing, league, maxBet, isAlreadyHandled
   - `TraderShift`: Trader availability with skills (HDP/OU), level (Elite/Member/First), assignedJobs
   - `PreconfiguredPair`: Trader pairs with tier (primary/secondary/backup) and league priorities
   - `AssignmentRequest`: API input combining all three
   - `AssignmentResponse`: Optimized assignments with scores and summary stats

3. **JSON Serialization**
   - Uses `JsonNamingPolicy.CamelCase` for API consistency
   - Request/response bodies use camelCase (e.g., `traderShifts`, `maxBet`)

### n8n Code Node Architecture

**Pattern: Strategy Pattern with nodeType Handlers**

All n8n code nodes follow this pattern for processing different data types:

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

**Key Transformation: build_mj_api_param.js**

Located in `n8n/codenode/build_mj_api_param.js`, this node transforms n8n data to API format:

- **Field Mappings:**
  - `matchDate` → `start`
  - `endDate` → `end`
  - `traderName` → `name`
  - `rowNumber` → `id` (source of truth for IDs)
  - `bettype` → `skills` (parsed: only HDP and OU)
  - `staffLevel` → `level` (capitalized: "elite" → "Elite")
  - `isUnchanged` → `isAlreadyHandled`

- **Skills Parsing Logic:**
  - `"HDP/M1*2"` → `["HDP"]`
  - `"OU/M1*2"` → `["OU"]`
  - `"2/M1*2"` → `["HDP", "OU"]` (versatile trader)
  - `"2[OP]"` → `[]` (ignored, not MJ-relevant)
  - `"OU/R-FS/M1*2"` → `["OU"]` (R-FS filtered out)

**To sync code node changes to workflow:**

```bash
node n8n/codenode/sync-to-workflow.js
```

### Optimization Scoring System

**Level Points:**
- Elite: 256 points
- Member: 16 points
- First: 4 points

**Priority Multipliers:**
- Elite non-manager: 2.0x (highest)
- Elite manager: 1.8x
- Member non-manager: 1.3x
- First non-manager: 1.2x
- Member manager: 1.1x
- First manager: 1.0x (lowest)

**Tier Bonuses (Preconfigured Pairs):**
- Primary: 1.5x
- Secondary: 1.3x
- Backup: 1.2x

**League Priority Bonuses:**
- 1st priority: 1.5x
- 2nd priority: 1.3x
- 3rd priority: 1.1x

**Formula:**
```
Pair Score = (HDP_points × HDP_priority) × (OU_points × OU_priority) × Tier_Bonus × League_Bonus
Objective = Maximize Σ (Pair_Score × (MaxBet/1000)²)
```

## Important Conventions

### CRITICAL: Temporary Code Location

**⚠️ ALWAYS create ALL test/temporary/generated code in `tempcode/` directory - NO EXCEPTIONS:**

```javascript
// ❌ NEVER do this:
Write('D:\\Work\\LST\\soccer-lst\\test-transform.js', ...)
Write('D:\\Work\\LST\\soccer-lst\\verify-data.js', ...)
Write('D:\\Work\\LST\\soccer-lst\\sample-test.js', ...)

// ✅ ALWAYS do this instead:
Write('D:\\Work\\LST\\soccer-lst\\tempcode\\test-transform.js', ...)
Write('D:\\Work\\LST\\soccer-lst\\tempcode\\verify-data.js', ...)
Write('D:\\Work\\LST\\soccer-lst\\tempcode\\sample-test.js', ...)
```

**What belongs in `tempcode/`:**
- ANY test scripts
- ANY verification scripts
- ANY transformation test files
- ANY sample/demo code
- ANY debugging scripts
- ANY one-off utilities
- ANY experimental code

**Why this is critical:**
- Keeps the repository clean and organized
- Clearly separates production code from temporary code
- Makes it easy to identify and clean up temporary files
- Prevents accidentally committing test files to production areas

**Before creating ANY file, ask yourself:**
- Is this production code that will be used long-term? → Put in appropriate src/ or n8n/ folder
- Is this temporary, test, or experimental? → Put in `tempcode/`

When in doubt, use `tempcode/` - it's better to move a file later than to clutter production directories.

### n8n Data Format

All n8n code nodes expect and return this structure:

```javascript
return [{
  json: {
    traderShifts: [...],
    matches: [...],
    preconfiguredPairs: [...]
  }
}];
```

Each item in input arrays has a `nodeType` property for identification when streams are merged.

### Date/Time Handling

- **All times in UTC** to avoid timezone issues
- **Excel dates** calculated from December 30, 1899 epoch
- **Match timing** includes `start` and `end` (DateTimeOffset)
- **Shift timing** includes `start`, `end`, and crossing shift detection

## Common Development Tasks

### Adding a New Match Property

1. Add property to `src/SoccerLST.TraderAssignment.Core/Models/Match.cs`
2. Update `n8n/codenode/build_mj_api_param.js` transformation
3. Sync to workflow: `node n8n/codenode/sync-to-workflow.js`
4. Rebuild Docker image
5. Test with sample data

### Modifying Optimization Constraints

Edit `src/SoccerLST.TraderAssignment.Core/Services/TraderAssignmentService.cs`:
- Decision variables are in `var x = new Dictionary<...>`
- Constraints added via `model.Add(...)`
- Objective function is `model.Maximize(...)`

### Testing n8n Transformations Locally

```bash
# Create test data transformation
node n8n/codenode/test-transform.js

# Verify skills parsing
node n8n/codenode/verify-skills.js

# Output goes to: n8n/output/transformed_api_params.json
```

## Project Structure

```
soccer-lst/
├── src/
│   ├── SoccerLST.TraderAssignment.API/       # Web API layer
│   │   ├── Controllers/
│   │   │   └── TraderAssignmentController.cs
│   │   ├── Models/                           # API-specific models
│   │   └── Program.cs                        # Startup, DI, middleware
│   └── SoccerLST.TraderAssignment.Core/      # Business logic
│       ├── Models/                           # Domain models
│       │   ├── Match.cs
│       │   ├── TraderShift.cs
│       │   ├── PreconfiguredPair.cs
│       │   └── ...
│       └── Services/
│           └── TraderAssignmentService.cs    # OR-Tools optimization
├── n8n/
│   ├── workflow/
│   │   ├── Soccer_LST_Parse_Excel_File.json
│   │   └── Soccer_LST_MJ_Assignment.json
│   ├── codenode/                             # JavaScript transformations
│   │   ├── build_mj_api_param.js            # Main API mapper
│   │   ├── NODE_MAPPING.md                   # Code node documentation
│   │   └── sync-to-workflow.js              # Workflow sync utility
│   └── output/                               # Transformation outputs
├── tempcode/                                 # Temporary/test scripts
├── Dockerfile                                # Multi-stage .NET 8 build
├── .dockerignore
├── .gitignore
└── .claude/
    └── instructions.md                       # Claude Code conventions
```

## nodeType Reference

When working with n8n workflows, data is tagged with `nodeType`:

| nodeType | Description | Used By |
|----------|-------------|---------|
| `workingShift` | Trader availability + assigned jobs | API as `traderShifts` |
| `mismatch` | Raw match data (MJ only filtered) | API as `matches` |
| `pairTrader` | Preconfigured pairs + tiers | API as `preconfiguredPairs` |
| `trader` | Trader metadata (not used in MJ API) | Parse workflow only |
| `config` | CurrentDate for calculations | Parse workflow only |

## Key Constraints in Optimization

1. Each match assigned at most 1 HDP + 1 OU trader
2. Traders cannot work overlapping matches
3. Traders cannot work both HDP and OU in same match
4. Matches require both roles assigned or no roles (all-or-nothing)
5. Traders cannot be assigned during their existing `assignedJobs`

## Docker Notes

- **Multi-stage build** optimizes image size
- **libgomp1** required for Google OR-Tools parallel processing
- **Port 8080** for HTTP (8081 also exposed)
- **.dockerignore** excludes bin/, obj/, and development files
- **Production mode** disables Swagger by default (enable with Development environment)
