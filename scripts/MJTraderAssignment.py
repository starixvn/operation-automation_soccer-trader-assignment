from ortools.sat.python import cp_model
from datetime import datetime

def to_minutes(dt):
    # Handle both formats: with and without 'Z' suffix
    dt_str = dt.replace('Z', '').replace('.000', '')
    return int(datetime.strptime(dt_str, "%Y-%m-%dT%H:%M:%S").timestamp() / 60)

# --- Input Data ---
# Extracted from MJ.json - Real trader shifts for 2025-09-28
# Note: (1st) level is mapped to "First", regular names without parentheses remain as is
traderShifts = [
    # Early shifts (2:00-5:00 start)
    {"id": "30", "name": "CLGKelven", "start": "2025-09-28T02:00:00.000Z", "end": "2025-09-29T13:00:00.000Z", "skills": ["OU"], "level": "First", "isManager": True},
    {"id": "31", "name": "CLGHoo", "start": "2025-09-28T03:00:00.000Z", "end": "2025-09-28T14:00:00.000Z", "skills": ["OU"], "level": "Elite", "isManager": True,
     "assignedJobs": [{"type": "BS", "start": "2025-09-28T05:00:00.000Z", "end": "2025-09-28T14:00:00.000Z"}]},
    {"id": "32", "name": "CLGKokchen", "start": "2025-09-28T03:00:00.000Z", "end": "2025-09-28T14:00:00.000Z", "skills": ["OU"], "level": "First", "isManager": False},
    {"id": "33", "name": "CLGTommy", "start": "2025-09-28T03:00:00.000Z", "end": "2025-09-28T14:00:00.000Z", "skills": ["HDP"], "level": "Member", "isManager": False},
    {"id": "34", "name": "CLGParker", "start": "2025-09-28T03:00:00.000Z", "end": "2025-09-28T14:00:00.000Z", "skills": ["HDP"], "level": "First", "isManager": False},
    {"id": "35", "name": "SLGBoy", "start": "2025-09-28T03:00:00.000Z", "end": "2025-09-28T14:00:00.000Z", "skills": ["HDP", "OU"], "level": "Member", "isManager": False},
    {"id": "39", "name": "CLGHendra", "start": "2025-09-28T04:30:00.000Z", "end": "2025-09-28T15:30:00.000Z", "skills": ["HDP"], "level": "First", "isManager": True},
    {"id": "40", "name": "CLGAIK", "start": "2025-09-28T05:00:00.000Z", "end": "2025-09-28T16:00:00.000Z", "skills": ["OU"], "level": "First", "isManager": True},
    {"id": "43", "name": "CLGCandy", "start": "2025-09-28T05:00:00.000Z", "end": "2025-09-28T16:00:00.000Z", "skills": ["HDP"], "level": "First", "isManager": True,
     "assignedJobs": [{"type": "BS", "start": "2025-09-28T05:00:00.000Z", "end": "2025-09-28T16:00:00.000Z"}]},
    
    # Afternoon shifts (13:00-14:30 start)
    {"id": "52", "name": "CLGShootbird", "start": "2025-09-28T13:00:00.000Z", "end": "2025-09-29T00:00:00.000Z", "skills": ["OU"], "level": "Elite", "isManager": False},
    {"id": "54", "name": "CLGKern", "start": "2025-09-28T13:30:00.000Z", "end": "2025-09-29T00:30:00.000Z", "skills": ["OU"], "level": "First", "isManager": True},
    {"id": "55", "name": "CLGFai", "start": "2025-09-28T13:00:00.000Z", "end": "2025-09-29T00:00:00.000Z", "skills": ["HDP"], "level": "First", "isManager": True},
    {"id": "56", "name": "CLGKuik", "start": "2025-09-28T13:30:00.000Z", "end": "2025-09-29T00:30:00.000Z", "skills": ["HDP", "OU"], "level": "First", "isManager": True},
    {"id": "60", "name": "SLGWubai", "start": "2025-09-28T13:00:00.000Z", "end": "2025-09-29T00:00:00.000Z", "skills": ["OU"], "level": "First", "isManager": False},
    {"id": "61", "name": "SLGLina", "start": "2025-09-28T13:00:00.000Z", "end": "2025-09-28T23:00:00.000Z", "skills": ["OU"], "level": "First", "isManager": False},
    {"id": "66", "name": "CLGIndo", "start": "2025-09-28T14:00:00.000Z", "end": "2025-09-29T01:00:00.000Z", "skills": ["HDP", "OU"], "level": "Member", "isManager": False},
    {"id": "71", "name": "CLGBoonlay", "start": "2025-09-28T14:30:00.000Z", "end": "2025-09-29T01:30:00.000Z", "skills": ["HDP", "OU"], "level": "Member", "isManager": False},
    {"id": "72", "name": "CLGSiang", "start": "2025-09-28T14:30:00.000Z", "end": "2025-09-29T01:30:00.000Z", "skills": ["HDP", "OU"], "level": "Member", "isManager": False},
    
    # Evening shifts (16:00-21:00 start)
    {"id": "79", "name": "CLGHan", "start": "2025-09-28T16:00:00.000Z", "end": "2025-09-29T03:00:00.000Z", "skills": ["OU"], "level": "Elite", "isManager": True,
     "assignedJobs": [{"type": "BS", "start": "2025-09-28T16:00:00.000Z", "end": "2025-09-29T03:00:00.000Z"}]},
    {"id": "81", "name": "SLGPing", "start": "2025-09-28T18:00:00.000Z", "end": "2025-09-29T05:00:00.000Z", "skills": ["HDP", "OU"], "level": "First", "isManager": True},
    {"id": "82", "name": "SLGJiaming", "start": "2025-09-28T18:00:00.000Z", "end": "2025-09-29T05:00:00.000Z", "skills": ["OU"], "level": "Elite", "isManager": True},
    {"id": "84", "name": "CLGWen", "start": "2025-09-28T18:30:00.000Z", "end": "2025-09-29T05:30:00.000Z", "skills": ["HDP"], "level": "Elite", "isManager": True},
    {"id": "86", "name": "CLGShuan", "start": "2025-09-28T18:00:00.000Z", "end": "2025-09-29T05:00:00.000Z", "skills": ["OU"], "level": "Elite", "isManager": False},
    {"id": "87", "name": "CLGKilos", "start": "2025-09-28T18:00:00.000Z", "end": "2025-09-29T05:00:00.000Z", "skills": ["HDP"], "level": "Elite", "isManager": False},
    {"id": "88", "name": "CLGKit", "start": "2025-09-28T18:00:00.000Z", "end": "2025-09-29T05:00:00.000Z", "skills": ["HDP", "OU"], "level": "Elite", "isManager": False},
    {"id": "97", "name": "CLGSeang", "start": "2025-09-28T19:00:00.000Z", "end": "2025-09-29T06:00:00.000Z", "skills": ["OU"], "level": "Elite", "isManager": True},
    {"id": "98", "name": "CLGBay", "start": "2025-09-28T19:00:00.000Z", "end": "2025-09-29T06:00:00.000Z", "skills": ["HDP", "OU"], "level": "First", "isManager": True},
    {"id": "99", "name": "CLGAaron", "start": "2025-09-28T19:00:00.000Z", "end": "2025-09-29T06:00:00.000Z", "skills": ["OU"], "level": "Member", "isManager": False},
    {"id": "104", "name": "SLGAlbert", "start": "2025-09-28T19:00:00.000Z", "end": "2025-09-29T05:00:00.000Z", "skills": ["HDP", "OU"], "level": "First", "isManager": True},
    {"id": "114", "name": "SLGHanwei", "start": "2025-09-28T19:00:00.000Z", "end": "2025-09-29T05:00:00.000Z", "skills": ["HDP", "OU"], "level": "Elite", "isManager": False},
    {"id": "115", "name": "SLGBou", "start": "2025-09-28T19:00:00.000Z", "end": "2025-09-29T06:00:00.000Z", "skills": ["HDP", "OU"], "level": "Member", "isManager": False},
    {"id": "116", "name": "SLGMing", "start": "2025-09-28T19:00:00.000Z", "end": "2025-09-29T06:00:00.000Z", "skills": ["HDP", "OU"], "level": "Member", "isManager": False},
    {"id": "123", "name": "SLGLuke", "start": "2025-09-28T20:00:00.000Z", "end": "2025-09-29T06:00:00.000Z", "skills": ["HDP", "OU"], "level": "First", "isManager": True},
    {"id": "129", "name": "CLGChang", "start": "2025-09-28T21:00:00.000Z", "end": "2025-09-29T08:00:00.000Z", "skills": ["HDP"], "level": "Member", "isManager": False},
    {"id": "130", "name": "CLGJoseph", "start": "2025-09-28T21:00:00.000Z", "end": "2025-09-29T08:00:00.000Z", "skills": ["HDP", "OU"], "level": "Member", "isManager": False},
    {"id": "131", "name": "CLGJacky", "start": "2025-09-28T21:00:00.000Z", "end": "2025-09-29T08:00:00.000Z", "skills": ["OU"], "level": "Member", "isManager": False},
    
    # Next day shifts (2025-09-29) - Short morning shifts
    {"id": "139", "name": "CLGHoo", "start": "2025-09-29T04:00:00.000Z", "end": "2025-09-29T14:00:00.000Z", "skills": ["OU"], "level": "Elite", "isManager": True,
     "assignedJobs": [
         {"type": "BS", "start": "2025-09-29T04:00:00.000Z", "end": "2025-09-29T07:00:00.000Z"},
         {"type": "BS", "start": "2025-09-29T09:00:00.000Z", "end": "2025-09-29T14:00:00.000Z"}
     ]},
    {"id": "141", "name": "CLGCandy", "start": "2025-09-29T05:00:00.000Z", "end": "2025-09-29T14:00:00.000Z", "skills": ["HDP"], "level": "First", "isManager": True,
     "assignedJobs": [{"type": "BS", "start": "2025-09-29T05:00:00.000Z", "end": "2025-09-29T14:00:00.000Z"}]},
    {"id": "142", "name": "CLGKokchen", "start": "2025-09-29T05:00:00.000Z", "end": "2025-09-29T14:00:00.000Z", "skills": ["OU"], "level": "First", "isManager": False},
    {"id": "145", "name": "SLGBoy", "start": "2025-09-29T05:00:00.000Z", "end": "2025-09-29T14:00:00.000Z", "skills": ["HDP", "OU"], "level": "Member", "isManager": False},
]

matches = [
    {"id": "152", "matchNo": "152", "start": "2025-09-28T18:30:00.000Z", "end": "2025-09-28T20:30:00.000Z", "maxBet": 120000, "leagueCode": "ITA"},
    {"id": "211", "matchNo": "211", "start": "2025-09-28T20:00:00.000Z", "end": "2025-09-28T22:00:00.000Z", "maxBet": 120000, "leagueCode": "SPA"},
    {"id": "249", "matchNo": "249", "start": "2025-09-28T21:00:00.000Z", "end": "2025-09-28T23:00:00.000Z", "maxBet": 120000, "leagueCode": "EPL"},
    {"id": "250", "matchNo": "250", "start": "2025-09-28T21:00:00.000Z", "end": "2025-09-28T23:00:00.000Z", "maxBet": 120000, "leagueCode": "ITA"},
    {"id": "251", "matchNo": "251", "start": "2025-09-28T21:00:00.000Z", "end": "2025-09-28T23:00:00.000Z", "maxBet": 120000, "leagueCode": "ITA"},
    {"id": "259", "matchNo": "259", "start": "2025-09-28T21:00:00.000Z", "end": "2025-09-28T23:00:00.000Z", "maxBet": 70000, "leagueCode": None},  # FRANCE LIGUE 1
    {"id": "305", "matchNo": "305", "start": "2025-09-28T21:30:00.000Z", "end": "2025-09-28T23:30:00.000Z", "maxBet": 120000, "leagueCode": "GER"},
    {"id": "339", "matchNo": "339", "start": "2025-09-28T22:15:00.000Z", "end": "2025-09-29T00:15:00.000Z", "maxBet": 120000, "leagueCode": "SPA"},
    {"id": "386", "matchNo": "386", "start": "2025-09-28T23:15:00.000Z", "end": "2025-09-29T01:15:00.000Z", "maxBet": 70000, "leagueCode": None},  # FRANCE LIGUE 1
    {"id": "387", "matchNo": "387", "start": "2025-09-28T23:15:00.000Z", "end": "2025-09-29T01:15:00.000Z", "maxBet": 70000, "leagueCode": None},  # FRANCE LIGUE 1
    {"id": "388", "matchNo": "388", "start": "2025-09-28T23:15:00.000Z", "end": "2025-09-29T01:15:00.000Z", "maxBet": 70000, "leagueCode": None},  # FRANCE LIGUE 1
    {"id": "390", "matchNo": "390", "start": "2025-09-28T23:30:00.000Z", "end": "2025-09-29T01:30:00.000Z", "maxBet": 120000, "leagueCode": "EPL"},
    {"id": "391", "matchNo": "391", "start": "2025-09-28T23:30:00.000Z", "end": "2025-09-29T01:30:00.000Z", "maxBet": 120000, "leagueCode": "GER"},
    {"id": "408", "matchNo": "408", "start": "2025-09-29T00:00:00.000Z", "end": "2025-09-29T02:00:00.000Z", "maxBet": 120000, "leagueCode": "ITA"},
    {"id": "448", "matchNo": "448", "start": "2025-09-29T00:30:00.000Z", "end": "2025-09-29T02:30:00.000Z", "maxBet": 120000, "leagueCode": "SPA"},
    {"id": "486", "matchNo": "486", "start": "2025-09-29T01:30:00.000Z", "end": "2025-09-29T03:30:00.000Z", "maxBet": 120000, "leagueCode": "GER"},
    {"id": "539", "matchNo": "539", "start": "2025-09-29T02:45:00.000Z", "end": "2025-09-29T04:45:00.000Z", "maxBet": 120000, "leagueCode": "ITA"},
    {"id": "540", "matchNo": "540", "start": "2025-09-29T02:45:00.000Z", "end": "2025-09-29T04:45:00.000Z", "maxBet": 70000, "leagueCode": None},  # FRANCE LIGUE 1
    {"id": "542", "matchNo": "542", "start": "2025-09-29T03:00:00.000Z", "end": "2025-09-29T05:00:00.000Z", "maxBet": 120000, "leagueCode": "SPA"},
]

# --- Preconfigured Trader Pairs (Priority Assignment) ---
# Extracted from MJ.json pair configuration data (rows 3-14)
# Three tiers: Primary (HDP+OU1), Secondary (HDP+OU2), Backup (HDPBK+OUBK)
# Tier bonuses: Primary 2.0x > Secondary 1.6x > Backup 1.3x
# IMPORTANT: Preconfig pairs ALWAYS preferred over regular pairs of same level
# CONSTRAINT: 2 Member preconfig pairs must NEVER score > 1 Elite with another trader
preconfigured_pairs = [
    # Row 3 - Primary and Backup
    {
        "hdp": "CLGBoon",
        "ou": "CLGShootbird",
        "tier": "primary"
    },
    {
        "hdp": "CLGWen",
        "ou": "CLGHoo",
        "tier": "backup"
    },
    # Row 4 - Primary, Secondary, and Backup
    {
        "hdp": "CLGChai",
        "ou": "CLGSeang",
        "tier": "primary"
    },
    {
        "hdp": "CLGChai",
        "ou": "CLGShuan",
        "tier": "secondary"
    },
    {
        "hdp": "SLGHanwei",
        "ou": "SLGJiaming",
        "tier": "backup"
    },
    # Row 5 - Primary, Secondary, and Backup
    {
        "hdp": "CLGChian",
        "ou": "CLGHan",
        "tier": "primary"
    },
    {
        "hdp": "CLGChian",
        "ou": "CLGShuan",
        "tier": "secondary"
    },
    {
        "hdp": "CLGGee",
        "ou": "CLGShuan",
        "tier": "backup"
    },
    # Row 6 - Primary with league priority
    {
        "hdp": "CLGKilos", 
        "ou": "CLGKit",
        "tier": "primary",
        "league_priority": {
            1: ["SPA"],
            2: ["ITA"],
            3: ["EPL"]
        }
    },
    # Row 7 - Primary with league priority
    {
        "hdp": "SLGHanwei", 
        "ou": "SLGBou",
        "tier": "primary",
        "league_priority": {
            1: ["GER"],
            2: ["SPA"],
            3: ["ITA"]
        }
    },
    # Row 8 - Primary, Secondary, Backup with league priority
    {
        "hdp": "SLGMing", 
        "ou": "SLGBou",
        "tier": "primary",
        "league_priority": {
            1: ["GER"],
            2: ["SPA"],
            3: ["ITA"]
        }
    },
    {
        "hdp": "SLGMing", 
        "ou": "SLGBoy",
        "tier": "secondary",
        "league_priority": {
            1: ["GER"],
            2: ["SPA"],
            3: ["ITA"]
        }
    },
    {
        "hdp": "SLGBoy",
        "ou": "SLGBou",
        "tier": "backup",
        "league_priority": {
            1: ["GER"],
            2: ["SPA"],
            3: ["ITA"]
        }
    },
    # Row 9 - Primary with league priority
    {
        "hdp": "CLGIndo", 
        "ou": "CLGJon",
        "tier": "primary",
        "league_priority": {
            1: ["SPA"],
            2: ["ITA"],
            3: ["EPL"]
        }
    },
    # Row 10 - Only HDP defined with league priority (no OU partner)
    # Skipped as incomplete pair
    
    # Row 11 - Primary with league priority
    {
        "hdp": "CLGJoseph", 
        "ou": "CLGJacky",
        "tier": "primary",
        "league_priority": {
            1: ["EPL"],
            2: ["SPA"],
            3: ["ITA"]
        }
    },
    # Row 12 - Primary with league priority
    {
        "hdp": "CLGAlikin", 
        "ou": "CLGBoonlay",
        "tier": "primary",
        "league_priority": {
            1: ["GER"],
            2: ["EPL"],
            3: ["ITA"]
        }
    },
    # Row 13 - Primary with league priority
    {
        "hdp": "CLGChang", 
        "ou": "CLGAaron",
        "tier": "primary",
        "league_priority": {
            1: ["ITA"],
            2: ["GER"],
            3: ["SPA"]
        }
    },
    # Row 14 - Only HDP defined (no OU partner, no priority)
    # Skipped as incomplete pair
]

# --- Convert Time ---
for t in traderShifts:
    t["start_min"] = to_minutes(t["start"])
    t["end_min"] = to_minutes(t["end"])
    
    # Convert time for assignedJobs if exists
    if "assignedJobs" in t:
        for job in t["assignedJobs"]:
            job["start_min"] = to_minutes(job["start"])
            job["end_min"] = to_minutes(job["end"])

for m in matches:
    m["start_min"] = to_minutes(m["start"])
    m["end_min"] = to_minutes(m["end"])

# --- Build Model ---
model = cp_model.CpModel()
roles = ["HDP", "OU"]

# --- Level Points System ---
level_points = {"First": 4, "Member": 16, "Elite": 256}

# --- Priority System (based on level + manager status) ---
# Higher priority = more preferred for assignment
# Priority order: Elite non-manager > Elite manager > Member non-manager > First non-manager > Member manager > First manager
def get_trader_priority(trader):
    """
    Returns priority multiplier based on level and manager status
    Elite non-manager: 2.0x (highest priority) - unchanged
    Elite manager: 1.8x - unchanged
    Member non-manager: 1.3x (reduced from 1.5x)
    First non-manager: 1.2x (reduced from 1.3x)
    Member manager: 1.1x - unchanged
    First manager: 1.0x (lowest priority) - unchanged
    """
    level = trader["level"]
    is_manager = trader.get("isManager", False)
    
    if level == "Elite":
        return 1.8 if is_manager else 2.0  # Unchanged for Elite
    elif level == "Member":
        return 1.1 if is_manager else 1.3  # Reduced non-mgr from 1.5x to 1.3x
    else:  # First
        return 1.0 if is_manager else 1.2  # Reduced non-mgr from 1.3x to 1.2x

# --- Variables ---
x = {}
for t in traderShifts:
    for m in matches:
        for role in roles:
            # Check basic conditions: role in skills, match within shift time
            if (role in t["skills"]) and (m["start_min"] >= t["start_min"]) and (m["end_min"] <= t["end_min"]):
                # Check if match overlaps with any assignedJobs
                has_conflict = False
                if "assignedJobs" in t:
                    for job in t["assignedJobs"]:
                        # Check overlap: match overlaps with job
                        overlap = (m["start_min"] < job["end_min"]) and (job["start_min"] < m["end_min"])
                        if overlap:
                            has_conflict = True
                            break
                
                if has_conflict:
                    # Cannot assign due to assignedJob conflict
                    x[(t["id"], m["id"], role)] = model.NewConstant(0)
                else:
                    # Can assign
                    x[(t["id"], m["id"], role)] = model.NewBoolVar(f"x_{t['id']}_{m['id']}_{role}")
            else:
                x[(t["id"], m["id"], role)] = model.NewConstant(0)

# --- 1️⃣ Mỗi match có tối đa 1 HDP + 1 OU (có thể không assign nếu không đủ trader) ---
for m in matches:
    for role in roles:
        model.Add(sum(x[(t["id"], m["id"], role)] for t in traderShifts) <= 1)

# --- 2️⃣ Trader không trùng giờ (same shift ID) ---
# A trader cannot work in ANY overlapping matches, regardless of role
for t in traderShifts:
    for i in range(len(matches)):
        for j in range(i + 1, len(matches)):
            m1, m2 = matches[i], matches[j]
            overlap = (m1["start_min"] < m2["end_min"]) and (m2["start_min"] < m1["end_min"])
            if overlap:
                # Sum ALL roles across both matches must be <= 1
                all_assignments = sum(x[(t["id"], m1["id"], role)] for role in roles) + \
                                 sum(x[(t["id"], m2["id"], role)] for role in roles)
                model.Add(all_assignments <= 1)

# --- 3️⃣ Trader không làm 2 role cùng trận ---
for t in traderShifts:
    for m in matches:
        model.Add(x[(t["id"], m["id"], "HDP")] + x[(t["id"], m["id"], "OU")] <= 1)

# --- 3.5️⃣ Match phải có cả 2 roles hoặc không có role nào ---
for m in matches:
    # Nếu có HDP thì phải có OU, và ngược lại
    hdp_assigned = sum(x[(t["id"], m["id"], "HDP")] for t in traderShifts)
    ou_assigned = sum(x[(t["id"], m["id"], "OU")] for t in traderShifts)
    model.Add(hdp_assigned == ou_assigned)

# --- 4️⃣ Xác định điểm cặp của mỗi match ---
# Pair Score = (HDP_points × HDP_priority) × (OU_points × OU_priority) × (Tier Bonus) × (League Priority Bonus)
# 
# Level Points: First=4, Member=16, Elite=256
# Priority Multipliers (based on level + manager status):
#   Elite non-manager: 2.0x (highest priority)
#   Elite manager: 1.8x
#   Member non-manager: 1.5x
#   First non-manager: 1.3x
#   Member manager: 1.1x
#   First manager: 1.0x (lowest priority)
#
# Base scores (without priority):
#   Elite+Elite = 256×256 = 65,536
#   Elite+Member = 256×16 = 4,096
#   Elite+First = 256×4 = 1,024
#   Member+Member = 16×16 = 256
#
# With priority (examples):
#   Elite non-mgr + Elite non-mgr = (256×2.0) × (256×2.0) = 262,144
#   Elite mgr + Elite mgr = (256×1.8) × (256×1.8) = 212,992
#   Elite non-mgr + Elite mgr = (256×2.0) × (256×1.8) = 236,544
#   Member non-mgr + Member non-mgr = (16×1.3) × (16×1.3) = 432.64
#   First non-mgr + First non-mgr = (4×1.2) × (4×1.2) = 23.04
#
# Tier Bonuses (for preconfigured pairs) - REDUCED to satisfy constraints:
#   Primary (HDP + OU1): 1.5x (reduced from 2.0x)
#   Secondary (HDP + OU2): 1.3x (reduced from 1.6x)
#   Backup (HDPBK + OUBK): 1.2x (reduced from 1.3x)
#
# League Priority Bonuses (additional, if defined) - RESTORED:
#   1st priority league: 1.5x (restored)
#   2nd priority league: 1.3x (restored)
#   3rd priority league: 1.1x (unchanged)
#   No league match: 1x
#
# Examples with adjusted bonuses:
#   Elite mgr + Elite mgr + Backup: (256×1.8)² × 1.2 = 212,336 × 1.2 = 254,803
#   Elite non-mgr + Elite mgr (Regular): (256×2.0) × (256×1.8) = 236,544
#   Elite non-mgr + Elite non-mgr + Primary + 1st: 262,144 × 1.5 × 1.5 = 589,824
#   Member non-mgr + Member non-mgr + Primary + 1st: (16×1.3)² × 1.5 × 1.5 = 432.64 × 2.25 = 973 (still << Elite)
#   First non-mgr + First non-mgr + Primary + 1st: (4×1.2)² × 1.5 × 1.5 = 23.04 × 2.25 = 51.84 < Member+First 70.40 ✓
#
# CONSTRAINTS SATISFIED:
#   ✓ Preconfig pairs beat regular pairs of same level
#   ✓ 2 Member preconfig NEVER score > 1 Elite with another trader
#   ✓ 2 First preconfig NEVER score > 1 Member + 1 First regular

# Helper: check if pair is preconfigured and get tier + league bonus
def get_pair_config(hdp_id, ou_id, match_league):
    """
    Returns (is_preconfigured, tier_bonus, league_bonus_multiplier, tier_name)
    Tier bonuses: Primary 1.5x, Secondary 1.3x, Backup 1.2x (REDUCED)
    League bonuses: 1st 1.5x, 2nd 1.3x, 3rd 1.1x (RESTORED)
    Preconfig pairs beat regular pairs of same level
    All constraints satisfied
    """
    for pair in preconfigured_pairs:
        if pair["hdp"] == hdp_id and pair["ou"] == ou_id:
            # Get tier bonus (reduced to satisfy First constraint)
            tier = pair.get("tier", "primary")
            if tier == "primary":
                tier_bonus = 1.5  # Reduced from 2.0x
                tier_name = "Primary"
            elif tier == "secondary":
                tier_bonus = 1.3  # Reduced from 1.6x
                tier_name = "Secondary"
            elif tier == "backup":
                tier_bonus = 1.2  # Reduced from 1.3x
                tier_name = "Backup"
            else:
                tier_bonus = 1.0
                tier_name = "Unknown"
            
            # Check if league_priority is defined
            league_priorities = pair.get("league_priority", None)
            
            # If no league_priority defined at all, just return tier bonus without league bonus
            if league_priorities is None:
                return (True, tier_bonus, 1, tier_name)  # No league priority defined: 1x
            
            # Check 1st priority (RESTORED to 1.5x)
            if match_league in league_priorities.get(1, []):
                return (True, tier_bonus, 1.5, tier_name)  # 1st priority: 1.5x
            # Check 2nd priority (RESTORED to 1.3x)
            elif match_league in league_priorities.get(2, []):
                return (True, tier_bonus, 1.3, tier_name)   # 2nd priority: 1.3x
            # Check 3rd priority (unchanged)
            elif match_league in league_priorities.get(3, []):
                return (True, tier_bonus, 1.1, tier_name)    # 3rd priority: 1.1x
            else:
                return (True, tier_bonus, 1, tier_name)    # Preconfigured but no league match: 1x
    
    return (False, 1, 1, None)  # Not preconfigured: 1x, 1x

# For backward compatibility
def is_preconfigured_pair(hdp_id, ou_id):
    is_preconfig, _, _, _ = get_pair_config(hdp_id, ou_id, None)
    return is_preconfig

pair_score = {}
for m in matches:
    # Max score = 256 × 2.0 (priority) × 256 × 2.0 (priority) × 1.5 (tier) × 1.5 (league) = 589,824
    pair_score[m["id"]] = model.NewIntVar(0, 600000, f"pair_score_{m['id']}")
    # Xác định trader HDP và OU
    possible_pairs = []
    for t1 in traderShifts:
        for t2 in traderShifts:
            if t1["id"] != t2["id"]:
                if "HDP" in t1["skills"] and "OU" in t2["skills"]:
                    # Get priority multipliers
                    hdp_priority = get_trader_priority(t1)
                    ou_priority = get_trader_priority(t2)
                    
                    # Điểm cặp = (điểm HDP × priority) × (điểm OU × priority)
                    hdp_contribution = level_points[t1["level"]] * hdp_priority
                    ou_contribution = level_points[t2["level"]] * ou_priority
                    base_score = hdp_contribution * ou_contribution
                    
                    # Check if preconfigured and get tier + league bonus (use names, not IDs)
                    is_preconfig, tier_bonus, league_bonus, tier_name = get_pair_config(t1["name"], t2["name"], m["leagueCode"])
                    
                    # Apply bonuses (use integer multiplication by scaling)
                    if is_preconfig:
                        # Apply both tier and league bonuses
                        # score = base × tier_bonus × league_bonus
                        score = int(base_score * tier_bonus * league_bonus)
                    else:
                        score = int(base_score)  # Apply priority only
                    
                    pair_var = model.NewBoolVar(f"pair_{m['id']}_{t1['id']}_{t2['id']}")
                    model.Add(x[(t1["id"], m["id"], "HDP")] == 1).OnlyEnforceIf(pair_var)
                    model.Add(x[(t2["id"], m["id"], "OU")] == 1).OnlyEnforceIf(pair_var)
                    possible_pairs.append((pair_var, score))
    
    # Ràng buộc: tối đa một pair được chọn (hoặc không có pair nào nếu match không được assign)
    model.Add(sum(var for var, _ in possible_pairs) <= 1)
    # Gán điểm tương ứng (nếu không có pair nào thì điểm = 0)
    model.Add(pair_score[m["id"]] == sum(score * var for var, score in possible_pairs))

# --- 5️⃣ Objective: maximize tổng điểm cặp * maxBet ---
# maxBet được bình phương để tăng cường ưu tiên trận lớn
objective_terms = []
for m in matches:
    # Tăng trọng số maxBet để ưu tiên cặp Elite+Elite cho trận có maxBet cao nhất
    maxBet_weight = (m["maxBet"] // 1000) ** 2
    objective_terms.append(pair_score[m["id"]] * maxBet_weight)

model.Maximize(sum(objective_terms))

# --- Solve ---
solver = cp_model.CpSolver()
status = solver.Solve(model)

# --- Output ---
print("=== Optimal Assignment (Pair Priority + League Matching) ===")
print(f"Total Matches: {len(matches)}")
print(f"Total Trader Shifts: {len(traderShifts)}")
print(f"Solver Status: {solver.StatusName(status)}\n")

if status == cp_model.OPTIMAL:
    # Collect all assignment data
    assignments = []
    for m in sorted(matches, key=lambda mm: (m['leagueCode'] if m['leagueCode'] else 'ZZZ', -mm["maxBet"])):
        league_display = m['leagueCode'] if m['leagueCode'] else "N/A"
        
        hdp_trader = None
        ou_trader = None
        for t in traderShifts:
            if solver.Value(x[(t["id"], m["id"], "HDP")]) == 1:
                hdp_trader = t
            if solver.Value(x[(t["id"], m["id"], "OU")]) == 1:
                ou_trader = t

        if not hdp_trader or not ou_trader:
            assignments.append({
                'match': m['matchNo'],
                'league': league_display,
                'maxBet': m['maxBet'],
                'time': f"{m['start'][5:10]} {m['start'][11:16]} - {m['end'][11:16]}",
                'hdp_id': 'N/A',
                'hdp': 'N/A',
                'hdp_level': 'N/A',
                'ou_id': 'N/A',
                'ou': 'N/A',
                'ou_level': 'N/A',
                'pair_type': '❌ UNASSIGNED',
                'score': 0
            })
        else:
            is_preconfig, tier_bonus, league_bonus, tier_name = get_pair_config(hdp_trader['name'], ou_trader['name'], m['leagueCode'])
            
            if is_preconfig:
                # Format: [Tier Icon] Tier + League Priority
                if tier_name == "Primary":
                    tier_icon = "🥇"
                elif tier_name == "Secondary":
                    tier_icon = "🥈"
                elif tier_name == "Backup":
                    tier_icon = "🥉"
                else:
                    tier_icon = "⭐"
                
                # Add league priority info if applicable
                if league_bonus == 1.5:
                    pair_type = f"{tier_icon} {tier_name} 1st"
                elif league_bonus == 1.3:
                    pair_type = f"{tier_icon} {tier_name} 2nd"
                elif league_bonus == 1.1:
                    pair_type = f"{tier_icon} {tier_name} 3rd"
                else:
                    pair_type = f"{tier_icon} {tier_name}"
            else:
                pair_type = "Regular"
            
            assignments.append({
                'match': m['matchNo'],
                'league': league_display,
                'maxBet': m['maxBet'],
                'time': f"{m['start'][5:10]} {m['start'][11:16]} - {m['end'][11:16]}",
                'hdp_id': hdp_trader['id'],
                'hdp': hdp_trader['name'],
                'hdp_level': hdp_trader['level'],
                'ou_id': ou_trader['id'],
                'ou': ou_trader['name'],
                'ou_level': ou_trader['level'],
                'pair_type': pair_type,
                'score': solver.Value(pair_score[m['id']])
            })
    
    # Print table header
    print("\n" + "="*160)
    print(f"{'Match':<8} {'League':<6} {'MaxBet':>10} {'Time':<20} {'Score':>12} {'HDP Trader':<16} {'Level':<8} {'OU Trader':<16} {'Level':<8} {'Pair Type':<20}")
    print("="*160)
    
    # Print table rows
    assigned_count = 0
    for a in assignments:
        if a['hdp'] != 'N/A':
            assigned_count += 1
        
        print(f"{a['match']:<8} {a['league']:<6} {a['maxBet']:>10,} {a['time']:<20} {a['score']:>12,} {a['hdp']:<16} {a['hdp_level']:<8} {a['ou']:<16} {a['ou_level']:<8} {a['pair_type']:<20}")
    
    print("="*160)
    print(f"\nSummary: {assigned_count}/{len(matches)} matches assigned")
    
    # Print preconfigured pair summary
    preconfig_count = sum(1 for a in assignments if 'Primary' in a['pair_type'] or 'Secondary' in a['pair_type'] or 'Backup' in a['pair_type'])
    primary_count = sum(1 for a in assignments if 'Primary' in a['pair_type'])
    secondary_count = sum(1 for a in assignments if 'Secondary' in a['pair_type'])
    backup_count = sum(1 for a in assignments if 'Backup' in a['pair_type'])
    
    print(f"Preconfigured Pairs Used: {preconfig_count} matches")
    if primary_count > 0:
        print(f"  - Primary pairs: {primary_count}")
    if secondary_count > 0:
        print(f"  - Secondary pairs: {secondary_count}")
    if backup_count > 0:
        print(f"  - Backup pairs: {backup_count}")
    
else:
    print("❌ No solution found! Not enough traders to cover all matches.")
    print("Consider adding more trader shifts or reducing the number of matches.")
