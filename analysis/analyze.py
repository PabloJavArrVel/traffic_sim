"""
Turns the CSV files of an experiment batch into an Excel report with charts.

    python3 analysis/analyze.py                      # the newest folder in ExperimentResults/
    python3 analysis/analyze.py ExperimentResults/2026-09-27_10-00-00

Writes into that folder:
    report.xlsx   Summary (mean and 95% confidence interval per scenario), Compared to baseline,
                  Intersections, Runs (every run), Charts, and a sheet explaining every metric
    *.png         the charts
and prints the main differences from the Baseline scenario.

Every scenario runs several times with different random seeds; we report the average of those runs and how sure
we can be about it (95% confidence interval). A difference from the baseline is called "clear" when the two
intervals of the difference don't include zero (Welch's t-test, 95%).
"""
import math
import sys
from pathlib import Path

import matplotlib
matplotlib.use("Agg")   # draw to files, no window
import matplotlib.pyplot as plt
import pandas as pd
from openpyxl import load_workbook
from openpyxl.drawing.image import Image as ExcelImage
from openpyxl.styles import Font, PatternFill

BASELINE = "Baseline"

# The metrics we compare, with a readable name and whether a higher value is better.
METRICS = {
    "average_speed_kmh": ("Average speed (km/h)", True),
    "share_stopped": ("Share of cars stopped", False),
    "km_per_vehicle_per_hour": ("Km driven per vehicle per hour", True),
    "longest_stop_seconds": ("Longest stop of any car (s)", False),
    "share_of_taxis_busy": ("Share of taxis busy", None),
    "rides_completed": ("Rides completed", True),
    "share_given_up": ("Share of passengers who gave up", False),
    "average_seconds_until_pickup": ("Average wait until pickup (s)", False),
    "p90_seconds_until_pickup": ("90% of passengers picked up within (s)", False),
    "average_trip_seconds": ("Average trip time (s)", False),
    "collisions": ("Crashes", False),
    "collisions_involving_taxis": ("Crashes involving taxis", False),
    "red_lights_run": ("Red lights run", False),
    "share_speeding": ("Share of time vehicles are speeding", False),
}

METRIC_EXPLANATIONS = [
    ("average_speed_kmh", "Average speed of all vehicles (cars and taxis), stopped ones included."),
    ("share_stopped", "Share of the time vehicles are stopped (slower than 0.5 m/s), for any reason."),
    ("share_stopped_at_red_light", "...stopped because of a red or yellow light."),
    ("share_stopped_behind_car", "...stopped in a queue behind another car."),
    ("share_stopped_at_junction", "...waiting for a junction, a crossing or a lane change to be free."),
    ("km_per_vehicle_per_hour", "How much the average vehicle drives per hour: a measure of how well traffic flows."),
    ("longest_stop_seconds", "The longest time any car was stuck (red lights don't count). Large values mean jams."),
    ("share_of_taxis_busy", "Share of the time taxis have a passenger assigned or on board."),
    ("rides_completed", "Passengers taken to their destination (requested after the warm-up)."),
    ("share_given_up", "Share of passengers who gave up because no taxi was assigned in time."),
    ("average_seconds_until_pickup", "From asking for a taxi to getting in."),
    ("p90_seconds_until_pickup", "90% of the passengers got in within this time."),
    ("average_trip_seconds", "From getting in to arriving."),
    ("rebel_cars", "Ambient drivers who ignore every traffic rule (red lights, speed limits, right of way)."),
    ("collisions", "Crashes after the warm-up. A crash blocks the street for 30 s, then the cars are towed elsewhere."),
    ("collisions_involving_rebels", "Crashes where at least one car was a rebel."),
    ("collisions_between_law_abiding_drivers", "Crashes between two law-abiding cars. Always 0: law-abiding drivers only drive into cells they reserved."),
    ("collisions_involving_taxis", "Crashes where a taxi was hit."),
    ("red_lights_run", "Stop lines crossed while the light was red (only rebels do it)."),
    ("share_speeding", "Share of the time vehicles go faster than the limit of their street (only rebels do it)."),
]

# Two-sided 95% t values for small samples (degrees of freedom -> t). Above 30 we use 1.96.
T_95 = {1: 12.71, 2: 4.30, 3: 3.18, 4: 2.78, 5: 2.57, 6: 2.45, 7: 2.36, 8: 2.31, 9: 2.26, 10: 2.23,
        12: 2.18, 15: 2.13, 20: 2.09, 25: 2.06, 30: 2.04}


def t_value(degrees_of_freedom):
    if degrees_of_freedom < 1:
        return float("nan")
    for df in sorted(T_95):
        if degrees_of_freedom <= df:
            return T_95[df]
    return 1.96


def newest_results_folder():
    root = Path(__file__).resolve().parent.parent / "ExperimentResults"
    folders = sorted(p for p in root.iterdir() if p.is_dir())
    if not folders:
        sys.exit(f"No results in {root}. Run the experiments in Unity first (Traffic Simulation > Experiments).")
    return folders[-1]


def summarize(runs):
    """Mean and 95% confidence interval of every metric, per scenario (keeping the scenarios' order)."""
    rows = []
    for scenario, group in runs.groupby("scenario", sort=False):
        row = {"scenario": scenario, "runs": len(group)}
        for metric in METRICS:
            values = group[metric].dropna()
            mean = values.mean()
            half_width = t_value(len(values) - 1) * values.std(ddof=1) / math.sqrt(len(values)) if len(values) > 1 else float("nan")
            row[metric] = mean
            row[metric + "_ci95"] = half_width
        rows.append(row)
    return pd.DataFrame(rows)


def compare_to_baseline(runs):
    """For every scenario and metric: the change from the baseline, in % and whether it is clear (Welch's t-test)."""
    baseline = runs[runs["scenario"] == BASELINE]
    rows = []
    for scenario, group in runs.groupby("scenario", sort=False):
        if scenario == BASELINE:
            continue
        for metric, (name, higher_is_better) in METRICS.items():
            a, b = baseline[metric].dropna(), group[metric].dropna()
            if len(a) < 2 or len(b) < 2 or a.mean() == 0:
                continue
            difference = b.mean() - a.mean()
            variance_a, variance_b = a.var(ddof=1) / len(a), b.var(ddof=1) / len(b)
            standard_error = math.sqrt(variance_a + variance_b)
            # Welch-Satterthwaite degrees of freedom
            dof = (variance_a + variance_b) ** 2 / (
                (variance_a ** 2 / (len(a) - 1) if variance_a > 0 else 0) + (variance_b ** 2 / (len(b) - 1) if variance_b > 0 else 0)
            ) if standard_error > 0 else 1
            clear = standard_error > 0 and abs(difference) > t_value(int(dof)) * standard_error
            if higher_is_better is None or not clear:
                verdict = "clear change" if clear else "no clear change"
            else:
                verdict = "better" if (difference > 0) == higher_is_better else "worse"
            rows.append({
                "scenario": scenario, "metric": name, "baseline": a.mean(), "scenario_value": b.mean(),
                "change_percent": 100 * difference / abs(a.mean()), "clear_difference": clear, "verdict": verdict,
            })
    return pd.DataFrame(rows)


def draw_charts(runs, summary, samples, intersections, folder):
    charts = []

    # 1. The main metrics per scenario, with 95% confidence intervals
    for metric in ["average_speed_kmh", "share_stopped", "average_seconds_until_pickup", "share_given_up",
                   "share_of_taxis_busy", "longest_stop_seconds", "collisions", "red_lights_run"]:
        name = METRICS[metric][0]
        figure, axes = plt.subplots(figsize=(10, 4.5))
        colors = ["#44546A" if s == BASELINE else "#5B9BD5" for s in summary["scenario"]]
        axes.bar(summary["scenario"], summary[metric], yerr=summary[metric + "_ci95"], capsize=4, color=colors)
        axes.set_title(f"{name} (mean of runs, 95% confidence interval)")
        axes.tick_params(axis="x", rotation=35)
        for label in axes.get_xticklabels():
            label.set_horizontalalignment("right")
        figure.tight_layout()
        charts.append(save(figure, folder, f"chart_{metric}.png"))

    # 2. Why cars are stopped
    reasons = runs.groupby("scenario", sort=False)[["share_stopped_at_red_light", "share_stopped_behind_car", "share_stopped_at_junction"]].mean()
    figure, axes = plt.subplots(figsize=(10, 4.5))
    reasons.plot(kind="bar", stacked=True, ax=axes, color=["#C00000", "#ED7D31", "#FFC000"])
    axes.legend(["Red light", "Queue behind a car", "Junction / lane change"])
    axes.set_title("Why cars are stopped (share of time)")
    axes.tick_params(axis="x", rotation=35)
    figure.tight_layout()
    charts.append(save(figure, folder, "chart_stop_reasons.png"))

    # 3. Average speed over time
    figure, axes = plt.subplots(figsize=(10, 4.5))
    for scenario, group in samples.groupby("scenario", sort=False):
        over_time = group.groupby("time_seconds")["average_speed_kmh"].mean().rolling(6, min_periods=1).mean()
        axes.plot(over_time.index / 60, over_time.values, label=scenario, linewidth=2 if scenario == BASELINE else 1)
    axes.set_xlabel("minutes")
    axes.set_ylabel("km/h")
    axes.set_title("Average speed over time (mean of runs, 1-minute moving average)")
    axes.legend(fontsize=7, ncol=2)
    figure.tight_layout()
    charts.append(save(figure, folder, "chart_speed_over_time.png"))

    # 4. Queues at each intersection
    queues = intersections.groupby(["scenario", "intersection"], sort=False)["average_queue"].mean().unstack()
    figure, axes = plt.subplots(figsize=(8, 0.4 * len(queues) + 1.5))
    image = axes.imshow(queues.values, cmap="Reds", aspect="auto")
    axes.set_yticks(range(len(queues.index)), queues.index)
    axes.set_xticks(range(len(queues.columns)), [f"#{c}" for c in queues.columns])
    for (row, column), value in pd.DataFrame(queues.values).stack().items():
        axes.text(column, row, f"{value:.1f}", ha="center", va="center", fontsize=8)
    figure.colorbar(image, label="stopped cars")
    axes.set_title("Average queue at each intersection with lights")
    figure.tight_layout()
    charts.append(save(figure, folder, "chart_intersection_queues.png"))
    return charts


def save(figure, folder, name):
    path = folder / name
    figure.savefig(path, dpi=110)
    plt.close(figure)
    return path


def write_excel(folder, summary, comparison, runs, intersections, charts):
    path = folder / "report.xlsx"
    readable_summary = summary.rename(columns={m: METRICS[m][0] for m in METRICS})
    readable_summary = readable_summary.rename(columns={m + "_ci95": METRICS[m][0] + " ±95%" for m in METRICS})
    intersection_names = intersections.drop_duplicates("intersection")[["intersection", "lights"]]
    queues = intersections.groupby(["scenario", "intersection"], sort=False)["average_queue"].mean().unstack().reset_index()

    with pd.ExcelWriter(path, engine="openpyxl") as excel:
        readable_summary.to_excel(excel, sheet_name="Summary", index=False)
        comparison.to_excel(excel, sheet_name="Compared to baseline", index=False)
        queues.to_excel(excel, sheet_name="Intersections", index=False)
        intersection_names.to_excel(excel, sheet_name="Intersections", index=False, startrow=len(queues) + 3)
        runs.to_excel(excel, sheet_name="Runs", index=False)
        pd.DataFrame(METRIC_EXPLANATIONS, columns=["metric", "meaning"]).to_excel(excel, sheet_name="What the numbers mean", index=False)

    workbook = load_workbook(path)
    for sheet in workbook.worksheets:
        for cell in sheet[1]:
            cell.font = Font(bold=True)
        for column in sheet.columns:
            sheet.column_dimensions[column[0].column_letter].width = max(12, min(45, max(len(str(c.value or "")) for c in column) + 2))
    colors = {"better": "C6EFCE", "worse": "FFC7CE"}
    comparison_sheet = workbook["Compared to baseline"]
    for row in comparison_sheet.iter_rows(min_row=2):
        color = colors.get(row[-1].value)
        if color:
            for cell in row:
                cell.fill = PatternFill("solid", fgColor=color)

    chart_sheet = workbook.create_sheet("Charts", 1)
    for i, chart in enumerate(charts):
        chart_sheet.add_image(ExcelImage(str(chart)), f"A{1 + i * 26}")
    workbook.save(path)
    return path


def print_findings(comparison):
    clear = comparison[comparison["clear_difference"]]
    print("\nClear differences from the baseline (95%):")
    for scenario, group in clear.groupby("scenario", sort=False):
        changes = ", ".join(f"{r.metric} {r.change_percent:+.0f}%" for r in group.itertuples())
        print(f"  {scenario}: {changes}")


def main():
    folder = Path(sys.argv[1]) if len(sys.argv) > 1 else newest_results_folder()
    runs = pd.read_csv(folder / "summary.csv")
    samples = pd.read_csv(folder / "samples.csv")
    intersections = pd.read_csv(folder / "intersections.csv")
    collisions_file = folder / "collisions.csv"
    if collisions_file.exists() and collisions_file.stat().st_size > 0:
        collisions = pd.read_csv(collisions_file)
        if not collisions.empty:
            where = collisions.groupby("cell").size().sort_values(ascending=False).head(5)
            print("Cells with most crashes (all runs): " + ", ".join(f"{cell} ({count})" for cell, count in where.items()))

    summary = summarize(runs)
    comparison = compare_to_baseline(runs) if BASELINE in set(runs["scenario"]) else pd.DataFrame()
    charts = draw_charts(runs, summary, samples, intersections, folder)
    report = write_excel(folder, summary, comparison, runs, intersections, charts)

    print(f"Report: {report}")
    if not comparison.empty:
        print_findings(comparison)


if __name__ == "__main__":
    main()
