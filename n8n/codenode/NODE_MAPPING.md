# Code Node Mapping

This document maps each JavaScript file in this directory to its corresponding node in the `Soccer_LST_Parse_Excel_File` workflow.

## Node Type Pattern

All code nodes now follow a consistent pattern:
- Each item in the output array has a `nodeType` property in its `json` object
- Instead of wrapping items in a single object with `type` and `items` properties, items are returned as an array where each item contains its own `nodeType`

## Node Mappings

### 1. **format_working_shift.js**
- **Node Name:** `format_working_shift`
- **Node ID:** `d2039d6d-eac5-4989-847e-3ea81abaaf5d`
- **Input:** Raw data from `get_working_shift` (Google Sheets - TimeTable)
- **Output:** `nodeType: 'workingShift'`
- **Description:** Transforms working shift data from Excel format, parses MYT time format, handles LST note adjustments (early in/late out), and parses BUSY column for assigned jobs

### 2. **wrap_working_shift.js**
- **Node Name:** `wrap_working_shift`
- **Node ID:** `71e5b4cb-6c72-4421-a888-1e6fe04e2e78`
- **Input:** Enriched working shift data from `enrich_working_shift`
- **Output:** `nodeType: 'workingShift'`
- **Description:** Adds nodeType to each working shift item for downstream processing

### 3. **enrich_assinged_job.js**
- **Node Name:** `enrich_assinged_job`
- **Node ID:** `583fe6ec-4ca1-4f15-b6d8-9e5be88e1056`
- **Input:** Working shifts and previous matches merged
- **Output:** `nodeType: 'workingShift'`
- **Description:** Enriches working shifts with assigned jobs from previous day matches for crossing shifts

### 4. **format_trader_list.js**
- **Node Name:** `format_trader_list`
- **Node ID:** `3be28268-fed8-4e12-8175-48622fe0f033`
- **Input:** Raw data from `get_trader_list` (Google Sheets - Traders)
- **Output:** `nodeType: 'trader'`
- **Description:** Formats trader information, normalizes staff levels (elite, member, first-fourth), and identifies management level ([PIC])

### 5. **filter_and_prepare_previous_mismatch.js**
- **Node Name:** `filter_and_prepare_previous_mismatch`
- **Node ID:** `5fd23cd2-2805-4832-8b19-c4de3da4714a`
- **Input:** Raw data from `get_previous_mismatch` (Google Sheets - Previous Mismatch)
- **Output:** `nodeType: 'previousMismatch'`
- **Description:** Filters matches between 10:00 AM and 12:00 PM (not including 12:00 PM)

### 6. **wrap_previous_mismatch.js**
- **Node Name:** `wrap_previous_mismatch`
- **Node ID:** `52a01c5e-b095-48b4-ab8b-2c972b7542fa`
- **Input:** Filtered previous mismatch data from switch node
- **Output:** `nodeType: 'previousMatch'`
- **Description:** Adds nodeType to previous match items

### 7. **format_mismatch.js**
- **Node Name:** `format_mismatch`
- **Node ID:** `5c0f387f-63ed-400c-96fc-4f4d742d6f7a`
- **Input:** Current mismatch, previous mismatch, and config data merged
- **Output:** `nodeType: 'match'`
- **Description:** Normalizes match data, detects bet types (MJ, M1-M4, virtual games, corner, booking, etc.), converts time and maxbet values

### 8. **prepare_current_mismatch.js**
- **Node Name:** `prepare_current_mismatch`
- **Node ID:** `12fcaec7-ba3b-44ba-9fd1-aa1961c85c97`
- **Input:** Raw data from `get_current_mismatch` (Google Sheets - Mismatch)
- **Output:** `nodeType: 'mismatch'`
- **Description:** Adds nodeType to current mismatch items for downstream processing

### 9. **wrap_current_mismatch.js**
- **Node Name:** `wrap_current_mismatch`
- **Node ID:** `3ceef07e-0e2b-4df0-804b-b2637b42ae19`
- **Input:** Formatted match data from switch node (current matches)
- **Output:** `nodeType: 'currentMatch'`
- **Description:** Adds nodeType to current match items

### 10. **format_config.js**
- **Node Name:** `format_config`
- **Node ID:** `60f4f65d-6bfc-4c60-b205-e6bca3bc20f9`
- **Input:** Raw data from `get_config` (Google Sheets - Config)
- **Output:** `nodeType: 'config'`
- **Description:** Extracts CurrentDate from config sheet for use in time calculations

### 11. **format_pair_trader.js**
- **Node Name:** `format_pair_trader`
- **Node ID:** `52026e72-cf4d-49fa-96ca-984fae3c15f4`
- **Input:** Raw data from `get_pair_trader` (Google Sheets - Pair Trader)
- **Output:** `nodeType: 'pairTrader'`
- **Description:** Formats trader pairs with tiers (primary, secondary, backup) and league priorities (1st, 2nd, 3rd)

### 12. **enrich_assigned_jobs_with_pointed_trader.js**
- **Node Name:** `enrich_assigned_jobs_with_pointed_trader`
- **Node ID:** `enrich-assigned-jobs-with-pointed-trader-001` (in Soccer_LST_MJ_Assignment workflow)
- **Input:** Working shifts and matches from parsed data
- **Output:** All input items (enriches only `nodeType: 'workingShift'`, passes through others)
- **Description:** Enriches working shifts with assigned jobs from Major (MJ) matches that are marked as unchanged (isUnchanged = true). Only assigns matches that start at or after the shift's start time. Filters: betType='MJ', isUnchanged=true, shiftStartTime <= matchDate. Returns all input items, only modifying workingShift items.

### 13. **AI_detect_league_code.js**
- **Node Name:** `AI_detect_league_code`
- **Node ID:** `1affb148-8dfc-45d2-a2e8-12f242d83fe1` (in Soccer_LST_MJ_Assignment workflow)
- **Input:** Working shifts, matches, and pair trader configurations
- **Output:** Single item with AI prompt data
- **Description:** Prepares data for AI-based league code detection. Extracts Major (MJ) matches with timing, pair trader configurations with league priorities, and builds system/user prompts for semantic matching of match league names to league codes.

### 14. **build_mj_api_param.js**
- **Node Name:** `build_mj_api_param`
- **Node ID:** `83ed0c60-0a69-4b5b-9355-b325a1ee8100` (in Soccer_LST_MJ_Assignment workflow)
- **Input:** Prepared data from merge node
- **Output:** API request payload
- **Description:** Builds the final MJ API request data structure. Currently a placeholder that adds a field to each item. This should be customized to format data according to the API endpoint requirements.

## Node Type Reference

| nodeType | Description | Source Nodes |
|----------|-------------|--------------|
| `workingShift` | Working shift with time range and assigned jobs | format_working_shift, wrap_working_shift, enrich_assinged_job |
| `trader` | Trader information with staff/management levels | format_trader_list |
| `previousMismatch` | Raw previous day mismatch data | filter_and_prepare_previous_mismatch |
| `previousMatch` | Formatted previous day match data | wrap_previous_mismatch |
| `mismatch` | Raw current day mismatch data | prepare_current_mismatch |
| `currentMatch` | Formatted current day match data | wrap_current_mismatch |
| `match` | Normalized match data (both current and previous) | format_mismatch |
| `config` | Configuration data (CurrentDate) | format_config |
| `pairTrader` | Trader pair configuration | format_pair_trader |

## Workflow Flow

```
Start
  ├─> get_working_shift → format_working_shift ─┐
  ├─> get_trader_list → format_trader_list ─────┴─> enrich_working_shift → wrap_working_shift ─┐
  ├─> get_previous_mismatch → filter_and_prepare_previous_mismatch ─┐                           │
  ├─> get_current_mismatch → prepare_current_mismatch ─────────────┐│                           │
  ├─> get_config → format_config ─────────────────────────────────┐││                           │
  └─> get_pair_trader → format_pair_trader ────────────────────┐  │││                           │
                                                                │  │││                           │
                prepare_data_to_format_mismatch <─────────────┴──┴┴┘                           │
                         │                                                                       │
                    format_mismatch                                                             │
                         │                                                                       │
           seperate_current_and_previous_mismatch                                               │
                    ├────────────┐                                                              │
          wrap_previous_mismatch │ wrap_current_mismatch                                        │
                    │             │                                                              │
                    └─────┬───────┘                                                              │
                          │                                                                      │
          merge_previous_mismatch_and_working_shift <─────────────────────────────────────────┘
                          │
                 enrich_assinged_job
                          │
                 output_parse_soccer_lst
```

## Usage Notes

- All code files expect and return n8n-formatted data: `[{ json: {...}, pairedItem: ... }]`
- The `nodeType` property helps identify data types when multiple data streams are merged
- Date/time handling uses UTC to avoid timezone issues
- Excel dates are calculated from December 30, 1899 epoch

