#!/usr/bin/env python3
"""
Trader Timeline Visualization
Generates an interactive HTML timeline showing trader schedules based on assignedJobs
"""

import json
import sys
from datetime import datetime, timedelta
from pathlib import Path
from typing import List, Dict, Any

def load_data(file_path: str) -> Any:
    """Load JSON data from file"""
    with open(file_path, 'r', encoding='utf-8') as f:
        data = json.load(f)
    # Return data as-is - handle list vs object in generate_html_timeline
    return data

def parse_datetime(dt_str: str) -> datetime:
    """Parse ISO datetime string"""
    # Handle formats like "2025-10-18T18:00:00.000Z" or "2025-10-18T18:00:00Z"
    dt_str = dt_str.replace('.000Z', 'Z').replace('Z', '+00:00')
    return datetime.fromisoformat(dt_str)

def get_job_color(job_type: str) -> str:
    """Get color for job type"""
    colors = {
        'MJ': '#2563eb',  # Blue
        'M1': '#16a34a',  # Green
        'M2': '#ea580c',  # Orange
        'M3': '#9333ea',  # Purple
        'M4': '#dc2626',  # Red
        'PES': '#0891b2', # Cyan
        'PK': '#e11d48',  # Pink
    }
    return colors.get(job_type, '#6b7280')  # Gray default

def format_time(dt: datetime) -> str:
    """Format datetime for display"""
    return dt.strftime('%H:%M')

def format_date(dt: datetime) -> str:
    """Format date for display"""
    return dt.strftime('%m-%d')

def normalize_trader(trader: Dict[str, Any]) -> Dict[str, Any]:
    """Normalize trader data from different formats"""
    # Handle raw format vs API format
    if 'traderName' in trader:
        # Raw format - convert to API format
        # startTime and endTime are already full ISO datetime strings
        start_dt = trader.get('startTime', '')
        end_dt = trader.get('endTime', '')

        level = trader.get('staffLevel') or 'Unknown'
        return {
            'id': str(trader.get('id', '')),
            'name': trader.get('traderName', 'Unknown'),
            'level': level.capitalize() if isinstance(level, str) else 'Unknown',
            'start': start_dt,
            'end': end_dt,
            'assignedJobs': trader.get('assignedJobs', [])
        }
    else:
        # Already in API format
        return trader

def jobs_overlap(job1_start: datetime, job1_end: datetime, job2_start: datetime, job2_end: datetime) -> bool:
    """Check if two jobs overlap in time"""
    return job1_start < job2_end and job2_start < job1_end

def assign_job_lanes(jobs: List[Dict[str, Any]]) -> List[int]:
    """
    Assign lane numbers to jobs so overlapping jobs get different lanes.
    Returns list of lane numbers (0, 1, 2, ...) for each job.
    """
    if not jobs:
        return []

    # Sort jobs by start time
    sorted_jobs = sorted(enumerate(jobs), key=lambda x: parse_datetime(x[1]['start']))

    # Track which lanes are occupied at each time
    lanes = []  # List of (end_time, lane_number) for occupied lanes
    job_lanes = [0] * len(jobs)  # Lane assignment for each job

    for original_idx, job in sorted_jobs:
        job_start = parse_datetime(job['start'])
        job_end = parse_datetime(job['end'])

        # Remove lanes that have ended
        lanes = [(end, lane) for end, lane in lanes if end > job_start]

        # Find the first available lane
        occupied_lanes = set(lane for _, lane in lanes)
        lane = 0
        while lane in occupied_lanes:
            lane += 1

        # Assign this job to the lane
        job_lanes[original_idx] = lane
        lanes.append((job_end, lane))

    return job_lanes

def generate_html_timeline(data: Dict[str, Any], output_path: str):
    """Generate interactive HTML timeline"""

    # Handle two input formats:
    # 1. API format: {"traderShifts": [...]}
    # 2. Raw format: [{"traderName": ..., "assignedJobs": ...}, ...]
    if isinstance(data, list):
        # Raw format - filter and normalize
        raw_traders = [item for item in data if item.get('nodeType') == 'workingShift']
        traders = [normalize_trader(t) for t in raw_traders]
    else:
        # API format
        traders = data.get('traderShifts', [])

    # Filter out traders with no valid start/end times
    valid_traders = [t for t in traders if t.get('start') and t.get('end')]

    if not valid_traders:
        print(f"[!] Error: No traders with valid start/end times found!")
        print(f"[!] Total items: {len(traders)}")
        return

    traders = valid_traders

    # Find time range
    min_time = None
    max_time = None

    for trader in traders:
        shift_start = parse_datetime(trader['start'])
        shift_end = parse_datetime(trader['end'])

        if min_time is None or shift_start < min_time:
            min_time = shift_start
        if max_time is None or shift_end > max_time:
            max_time = shift_end

        if trader.get('assignedJobs'):
            for job in trader['assignedJobs']:
                job_start = parse_datetime(job['start'])
                job_end = parse_datetime(job['end'])
                if job_start < min_time:
                    min_time = job_start
                if job_end > max_time:
                    max_time = job_end

    # Add padding
    min_time = min_time - timedelta(hours=1)
    max_time = max_time + timedelta(hours=1)

    # Calculate pixel width per hour
    total_hours = (max_time - min_time).total_seconds() / 3600
    pixels_per_hour = 80
    timeline_width = int(total_hours * pixels_per_hour)

    def time_to_pixels(dt: datetime) -> int:
        """Convert datetime to pixel position"""
        hours_from_start = (dt - min_time).total_seconds() / 3600
        return int(hours_from_start * pixels_per_hour)

    # Build HTML
    html = f"""<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Trader Timeline Visualization</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}

        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: #f9fafb;
            padding: 20px;
            color: #111827;
        }}

        .container {{
            max-width: 100%;
            margin: 0 auto;
            background: white;
            border-radius: 12px;
            box-shadow: 0 1px 3px rgba(0,0,0,0.1);
            overflow: hidden;
        }}

        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 24px;
        }}

        .header h1 {{
            font-size: 28px;
            font-weight: 700;
            margin-bottom: 8px;
        }}

        .header p {{
            font-size: 14px;
            opacity: 0.9;
        }}

        .stats {{
            display: flex;
            gap: 16px;
            margin-top: 16px;
            flex-wrap: wrap;
        }}

        .stat {{
            background: rgba(255,255,255,0.2);
            padding: 12px 16px;
            border-radius: 8px;
            backdrop-filter: blur(10px);
        }}

        .stat-label {{
            font-size: 12px;
            opacity: 0.8;
            margin-bottom: 4px;
        }}

        .stat-value {{
            font-size: 24px;
            font-weight: 700;
        }}

        .controls {{
            padding: 16px 24px;
            background: #f9fafb;
            border-bottom: 1px solid #e5e7eb;
            display: flex;
            gap: 12px;
            flex-wrap: wrap;
        }}

        .filter-btn {{
            padding: 8px 16px;
            border: 1px solid #d1d5db;
            background: white;
            border-radius: 6px;
            cursor: pointer;
            font-size: 14px;
            transition: all 0.2s;
        }}

        .filter-btn:hover {{
            background: #f3f4f6;
        }}

        .filter-btn.active {{
            background: #667eea;
            color: white;
            border-color: #667eea;
        }}

        .timeline-container {{
            overflow-x: auto;
            padding: 24px;
        }}

        .timeline {{
            position: relative;
            min-width: {timeline_width}px;
        }}

        .time-axis {{
            display: flex;
            height: 40px;
            border-bottom: 2px solid #e5e7eb;
            margin-bottom: 16px;
            position: relative;
        }}

        .time-marker {{
            position: absolute;
            height: 100%;
            display: flex;
            flex-direction: column;
            align-items: center;
        }}

        .time-marker-line {{
            width: 1px;
            height: 16px;
            background: #d1d5db;
        }}

        .time-marker-label {{
            font-size: 11px;
            color: #6b7280;
            margin-top: 4px;
            white-space: nowrap;
        }}

        .trader-row {{
            display: flex;
            align-items: flex-start;
            margin-bottom: 12px;
            min-height: 60px;
        }}

        .trader-info {{
            width: 200px;
            padding-right: 16px;
            flex-shrink: 0;
        }}

        .trader-name {{
            font-weight: 600;
            font-size: 14px;
            margin-bottom: 4px;
        }}

        .trader-level {{
            font-size: 12px;
            color: #6b7280;
            display: inline-block;
            padding: 2px 8px;
            background: #f3f4f6;
            border-radius: 4px;
        }}

        .trader-timeline {{
            flex: 1;
            position: relative;
            min-height: 50px;
            background: #f9fafb;
            border-radius: 6px;
        }}

        .shift-bar {{
            position: absolute;
            height: 100%;
            background: #e5e7eb;
            border-radius: 6px;
            opacity: 0.3;
        }}

        .job-bar {{
            position: absolute;
            height: 36px;
            border-radius: 6px;
            display: flex;
            align-items: center;
            padding: 0 8px;
            color: white;
            font-size: 11px;
            font-weight: 600;
            cursor: pointer;
            transition: transform 0.2s;
            box-shadow: 0 1px 3px rgba(0,0,0,0.2);
        }}

        .job-bar:hover {{
            transform: translateY(-2px);
            box-shadow: 0 4px 6px rgba(0,0,0,0.3);
            z-index: 10;
        }}

        .job-info {{
            display: flex;
            flex-direction: column;
            gap: 2px;
        }}

        .job-type {{
            font-weight: 700;
            font-size: 12px;
        }}

        .job-time {{
            font-size: 10px;
            opacity: 0.9;
        }}

        .tooltip {{
            position: absolute;
            background: #1f2937;
            color: white;
            padding: 12px;
            border-radius: 8px;
            font-size: 12px;
            pointer-events: none;
            z-index: 1000;
            box-shadow: 0 4px 6px rgba(0,0,0,0.3);
            display: none;
            min-width: 200px;
        }}

        .tooltip-row {{
            display: flex;
            justify-content: space-between;
            margin-bottom: 4px;
        }}

        .tooltip-label {{
            opacity: 0.8;
            margin-right: 12px;
        }}

        .tooltip-value {{
            font-weight: 600;
        }}

        .legend {{
            padding: 24px;
            border-top: 1px solid #e5e7eb;
            display: flex;
            gap: 24px;
            flex-wrap: wrap;
        }}

        .legend-item {{
            display: flex;
            align-items: center;
            gap: 8px;
        }}

        .legend-color {{
            width: 24px;
            height: 24px;
            border-radius: 4px;
        }}

        .legend-label {{
            font-size: 14px;
            color: #6b7280;
        }}
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>Trader Timeline Visualization</h1>
            <p>Visual representation of trader shifts and assigned jobs</p>
            <div class="stats">
                <div class="stat">
                    <div class="stat-label">Total Traders</div>
                    <div class="stat-value">{len(traders)}</div>
                </div>
                <div class="stat">
                    <div class="stat-label">Time Range</div>
                    <div class="stat-value">{format_date(min_time)} - {format_date(max_time)}</div>
                </div>
                <div class="stat">
                    <div class="stat-label">Duration</div>
                    <div class="stat-value">{int(total_hours)}h</div>
                </div>
            </div>
        </div>

        <div class="controls">
            <button class="filter-btn active" onclick="filterByLevel('all')">All Levels</button>
            <button class="filter-btn" onclick="filterByLevel('Elite')">Elite</button>
            <button class="filter-btn" onclick="filterByLevel('Member')">Member</button>
            <button class="filter-btn" onclick="filterByLevel('First')">First</button>
            <button class="filter-btn" onclick="filterByLevel('Second')">Second</button>
            <button class="filter-btn" onclick="filterByLevel('Third')">Third</button>
        </div>

        <div class="timeline-container">
            <div class="timeline">
                <div class="time-axis">
"""

    # Generate time markers (every 2 hours)
    current = min_time
    while current <= max_time:
        pos = time_to_pixels(current)
        html += f"""
                    <div class="time-marker" style="left: {pos}px;">
                        <div class="time-marker-line"></div>
                        <div class="time-marker-label">{format_time(current)}<br>{format_date(current)}</div>
                    </div>
"""
        current += timedelta(hours=2)

    html += """
                </div>

                <div id="traders">
"""

    # Generate trader rows
    total_jobs = 0
    for trader in traders:
        trader_name = trader['name']
        trader_level = trader['level']
        shift_start = parse_datetime(trader['start'])
        shift_end = parse_datetime(trader['end'])

        shift_left = time_to_pixels(shift_start)
        shift_width = time_to_pixels(shift_end) - shift_left

        assigned_jobs = trader.get('assignedJobs', [])
        total_jobs += len(assigned_jobs)

        # Assign lanes to jobs for this trader
        job_lanes = assign_job_lanes(assigned_jobs)
        max_lane = max(job_lanes) if job_lanes else 0

        # Calculate timeline height based on number of lanes
        # Each lane needs: 36px (job height) + 7px (top margin) = 43px
        # Plus 14px for bottom padding
        timeline_height = (max_lane + 1) * 43 + 14

        html += f"""
                    <div class="trader-row" data-level="{trader_level}">
                        <div class="trader-info">
                            <div class="trader-name">{trader_name}</div>
                            <div class="trader-level">{trader_level}</div>
                        </div>
                        <div class="trader-timeline" style="height: {timeline_height}px;">
                            <div class="shift-bar" style="left: {shift_left}px; width: {shift_width}px;"></div>
"""

        # Add job bars with lane positioning
        for idx, job in enumerate(assigned_jobs):
            job_type = job['type']
            job_start = parse_datetime(job['start'])
            job_end = parse_datetime(job['end'])

            job_left = time_to_pixels(job_start)
            job_width = time_to_pixels(job_end) - job_left
            job_color = get_job_color(job_type)

            job_duration = (job_end - job_start).total_seconds() / 3600

            # Calculate top position based on lane
            lane = job_lanes[idx]
            top_position = 7 + (lane * 43)  # 7px base margin + (lane * 43px per lane)

            html += f"""
                            <div class="job-bar"
                                 style="left: {job_left}px; width: {job_width}px; top: {top_position}px; background: {job_color};"
                                 data-job-type="{job_type}"
                                 data-start="{format_time(job_start)}"
                                 data-end="{format_time(job_end)}"
                                 data-duration="{job_duration:.1f}h"
                                 onmouseenter="showTooltip(event)"
                                 onmouseleave="hideTooltip()">
                                <div class="job-info">
                                    <span class="job-type">{job_type}</span>
                                    <span class="job-time">{format_time(job_start)}-{format_time(job_end)}</span>
                                </div>
                            </div>
"""

        html += """
                        </div>
                    </div>
"""

    html += f"""
                </div>
            </div>
        </div>

        <div class="legend">
            <div class="legend-item">
                <div class="legend-color" style="background: #2563eb;"></div>
                <div class="legend-label">MJ (Major)</div>
            </div>
            <div class="legend-item">
                <div class="legend-color" style="background: #16a34a;"></div>
                <div class="legend-label">M1 (Minor 1)</div>
            </div>
            <div class="legend-item">
                <div class="legend-color" style="background: #ea580c;"></div>
                <div class="legend-label">M2 (Minor 2)</div>
            </div>
            <div class="legend-item">
                <div class="legend-color" style="background: #9333ea;"></div>
                <div class="legend-label">M3 (Minor 3)</div>
            </div>
            <div class="legend-item">
                <div class="legend-color" style="background: #dc2626;"></div>
                <div class="legend-label">M4 (Minor 4)</div>
            </div>
            <div class="legend-item">
                <div class="legend-color" style="background: #0891b2;"></div>
                <div class="legend-label">PES (Virtual)</div>
            </div>
            <div class="legend-item">
                <div class="legend-color" style="background: #e11d48;"></div>
                <div class="legend-label">PK (Virtual)</div>
            </div>
        </div>
    </div>

    <div class="tooltip" id="tooltip"></div>

    <script>
        function filterByLevel(level) {{
            const rows = document.querySelectorAll('.trader-row');
            const buttons = document.querySelectorAll('.filter-btn');

            buttons.forEach(btn => btn.classList.remove('active'));
            event.target.classList.add('active');

            rows.forEach(row => {{
                if (level === 'all' || row.dataset.level === level) {{
                    row.style.display = 'flex';
                }} else {{
                    row.style.display = 'none';
                }}
            }});
        }}

        function showTooltip(event) {{
            const tooltip = document.getElementById('tooltip');
            const target = event.currentTarget;

            const jobType = target.dataset.jobType;
            const start = target.dataset.start;
            const end = target.dataset.end;
            const duration = target.dataset.duration;

            tooltip.innerHTML = `
                <div class="tooltip-row">
                    <span class="tooltip-label">Job Type:</span>
                    <span class="tooltip-value">${{jobType}}</span>
                </div>
                <div class="tooltip-row">
                    <span class="tooltip-label">Start:</span>
                    <span class="tooltip-value">${{start}}</span>
                </div>
                <div class="tooltip-row">
                    <span class="tooltip-label">End:</span>
                    <span class="tooltip-value">${{end}}</span>
                </div>
                <div class="tooltip-row">
                    <span class="tooltip-label">Duration:</span>
                    <span class="tooltip-value">${{duration}}</span>
                </div>
            `;

            tooltip.style.display = 'block';
            tooltip.style.left = event.pageX + 15 + 'px';
            tooltip.style.top = event.pageY + 15 + 'px';
        }}

        function hideTooltip() {{
            document.getElementById('tooltip').style.display = 'none';
        }}

        // Update total jobs in header
        const totalJobs = {total_jobs};
        console.log('Total assigned jobs:', totalJobs);
    </script>
</body>
</html>
"""

    # Write HTML file
    with open(output_path, 'w', encoding='utf-8') as f:
        f.write(html)

    print(f"[+] Timeline visualization generated: {output_path}")
    print(f"[+] {len(traders)} traders, {total_jobs} total jobs")
    print(f"[+] Time range: {format_date(min_time)} {format_time(min_time)} - {format_date(max_time)} {format_time(max_time)}")

def main():
    """Main entry point"""
    if len(sys.argv) < 2:
        print("Usage: python visualize_trader_timeline.py <input_json_file> [output_html_file]")
        print("\nExample:")
        print("  python visualize_trader_timeline.py ../n8n/output/test_build_graph.json")
        print("  python visualize_trader_timeline.py ../n8n/output/build_mj_api_params.json trader_timeline.html")
        sys.exit(1)

    input_file = sys.argv[1]

    # Default output file name based on input
    if len(sys.argv) >= 3:
        output_file = sys.argv[2]
    else:
        input_path = Path(input_file)
        output_file = input_path.stem + '_timeline.html'

    # Load and process data
    print(f"[*] Loading data from: {input_file}")
    data = load_data(input_file)

    # Generate timeline
    generate_html_timeline(data, output_file)

    print(f"\n[>] Open in browser: file://{Path(output_file).absolute()}")

if __name__ == '__main__':
    main()
