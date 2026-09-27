# Experiment analysis

`analyze.py` turns the results of an experiment batch into `report.xlsx` (plus PNG charts) in the same folder.

1. Install once: `pip install -r analysis/requirements.txt`
2. Run the experiments in Unity (**Traffic Simulation > Experiments > Run**) and press **Make Excel report**,
   or from a terminal: `python3 analysis/analyze.py` (uses the newest folder in `ExperimentResults/`).

Every scenario runs several times with different random seeds. The report shows the average of those runs with
a 95% confidence interval, and the "Compared to baseline" sheet marks a change as better or worse only when it
is clear at 95% (Welch's t-test); otherwise it says "no clear change". The CSV files can also be opened in
Excel or pandas directly:

| File | One row per |
|---|---|
| `summary.csv` | run (scenario + seed): the key numbers |
| `samples.csv` | run and 10-second interval: speed, stopped cars, taxis busy... |
| `rides.csv` | finished ride request: waiting and trip times, gave up or not |
| `intersections.csv` | run and intersection with lights: average queue |
