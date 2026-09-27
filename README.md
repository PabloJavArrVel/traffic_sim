# Traffic Sim — Simulación de Robotaxis Autónomos

Proyecto para la materia **Modelación de sistemas multiagentes con gráficas computacionales (Gpo 570)**.

A small city with autonomous taxis, ambient traffic and pedestrians, built in Unity. Every car, taxi, pedestrian and
the taxi dispatcher is an **agent** that perceives, deliberates and acts at every simulation step.

## Integrantes
* David Isaí Díaz Avilés (A01666448)
* Edgar Martínez Retes (A01706825)
* Elisheba Hannai Trejo Leyva (A01736967)
* Ian García González (A01706892)
* Óscar Orlando De la Paz Ibarra (A01644484)
* Pablo Javier Arreola Velasco (A01747824)

## Herramientas de Trabajo
* **Comunicación:** WhatsApp y Zoom.
* **Desarrollo:** Unity 3D y C#.
* **Control de Versiones:** GitHub.

---

## How to run

1. Install **Unity 6000.3.25f1** (Unity Hub → Installs). Other versions may upgrade the project files.
2. Open this folder as a Unity project.
3. Open `Assets/Scenes/City.unity` and press **Play**.

The `SimController` object in the scene has the settings you will want to change: number of cars and taxis, the
driver mix, share of rebel drivers, speed limits, light timing, pedestrians, simulation speed, and a random seed (the same seed gives the same
traffic every run). Or type the name of a scenario from `Experiments.xlsx` in its `scenario` field.

### Controls

| Mode | Keys |
|---|---|
| Follow camera | `←` `→` or `A` `D`: previous / next target · `Tab`: follow all vehicles, taxis, cars or pedestrians · `F`: free camera |
| Free camera | `W` `A` `S` `D` `Q` `E`: move · right mouse button + drag: look around · `Shift`: faster · `F`: back to follow mode |

The buttons at the bottom of the screen do the same. The panel at the top right shows every taxi and the passengers.

---

## The city map: `Assets/StreamingAssets/CityMap.xlsx`

The streets are **not** placed by hand in Unity: the simulation reads them from the `Map` sheet of this Excel file
every time you press Play. Each cell of the sheet is one road tile of the scene (1.2 Unity units, 5.4 m).

| Code | Meaning |
|---|---|
| `>` `<` `^` `v` | Street. Traffic drives in the arrow's direction (east, west, north, south). One cell = one lane. |
| `S>` `S<` `S^` `Sv` | Street with a traffic light. Cars wait at the end of this cell while it is red. |
| `X` | Building. |
| `R` | Roundabout island. |
| `0`-`9` | Parking spot (not used yet). |

How cars move between cells:

* **Straight:** into the next cell in the arrow's direction, unless that cell points straight back. If it points
  sideways, the car turns there.
* **Turn:** into a cell on the left or right whose arrow points away from the car.
* **Lanes:** cells side by side with the same arrow are a street with several lanes; cars change lanes diagonally.
* **Junction:** a cell cars can enter from more than one direction. Cars take turns there.
* **Crossing:** a junction cars can also leave in more than one direction. Cars only enter it when they can drive all
  the way through, so they never block the traffic crossing their path.
* **Traffic lights** that touch each other, or guard the same junction, form one intersection: its east-west and its
  north-south lights take turns (green, yellow, all red).

To change the city:

1. `Traffic Simulation > Open City Map in Excel`, edit the `Map` sheet, save.
2. The Scene view redraws the network straight away (white lines: where cars can drive; orange squares: junctions;
   colored spheres: traffic lights, same color = same intersection).
3. `Traffic Simulation > Check City Map` lists any street that goes nowhere or can't be reached.

The other sheets of the file explain the codes (`Legend`), every decision taken when the map was made the source of
truth (`Changes`), and keep the old versions (`Old Unity wiring`, `Original design`).

---

## How the simulation works

**The world ticks in fixed steps of 0.05 s.** Each step, the traffic lights update, then *every* agent perceives, then
every agent deliberates, then every agent acts (`World.Tick`). Unity only draws the result, blending between steps so
movement looks smooth.

**One car per cell.** A street cell belongs to at most one car at a time, and a car only drives into cells it holds.
Each step a car reserves the free cells it will need soon (as far as it needs to be able to stop), then drives, never
past its cells, and gives back the cells it left behind. This single rule keeps cars from ever overlapping, also at
junctions and while changing lanes. The tests check it independently with geometry.

**Speed** comes from the Intelligent Driver Model (`CarFollowing`): with free road a car speeds up to the speed limit;
with something ahead (a car, a red light, a busy junction) it slows down smoothly and stops just before it. Cars also
slow down before corners. Each ambient driver has a slightly different personality (`DriverProfile`).

**Traffic rules** (`TrafficRules.cs`). Law-abiding drivers, which means every taxi and, by default, every ambient
car, follow all of them perfectly:
* **Lights:** stop at red; at yellow, stop if you can do it comfortably.
* **Speed limits:** 50 km/h on avenues (two lanes side by side), 30 km/h on one-lane streets and in the roundabout.
  Cars slow down *before* entering a slower street.
* **Right of way** where no light decides: cars in the roundabout go first, then cars going straight along the street,
  then cars turning in; between equals, whoever waited longest.
* **Crossings:** never stop inside one.

**Rebels** (`rebelShare` in the inspector, `RebelShare` in experiments) ignore all of these rules. They run red lights,
drive at 60 km/h, don't give way, and drive into cells other cars have reserved. They brake for cars they can see, but
when they can't stop in time, **they crash**: two cars on one cell. A crash blocks the street for 30 s, then a tow
truck moves both cars elsewhere. Rebels are drawn in red, and crashed cars are darkened. Law-abiding cars never crash
into each other, and the tests check it.

**Routes** are the shortest way through the cells (Dijkstra, `RouteFinder`). Lane changes count as a bit longer, and
cells where a car is stopped count as much longer, so cars avoid jams. Ambient cars drive to a random place, then to
another one. A car stuck for a long time (not at a red light) looks for another way.

**Taxis and passengers.** Pedestrians appear on sidewalks and ask the `FleetManager` for a ride. It sends the closest
free taxi (by driving distance), first come, first served. The taxi drives to the pedestrian, stops while they get in,
drives them to their destination and lets them out. A pedestrian who waits too long without a taxi gives up.

### Experiments

Scenarios live in `Assets/StreamingAssets/Experiments.xlsx`, one row each (its `Help` sheet explains the columns):
number of cars and taxis, passenger demand, driver mix (calm vs. in a hurry), share of rebel drivers, speed limits, green-light time,
duration and how many random seeds to repeat it with. Empty cells keep the baseline value.

* **Run them:** `Traffic Simulation > Experiments` in Unity (pick scenarios, press Run; the window shows the average
  of each scenario), or from a terminal:
  `Unity -batchmode -quit -projectPath . -executeMethod ExperimentsCommandLine.RunAll -excelReport`.
  Each run is 20 simulated minutes without graphics. Runs go in parallel, one per processor core: the 80 runs of
  `Experiments.xlsx` take about 5 minutes on a 10-core Mac.
* **Results:** CSV files in `ExperimentResults/<date>/` (not committed). **Make Excel report** (or
  `python3 analysis/analyze.py`) adds `report.xlsx` with averages, 95% confidence intervals, a comparison with the
  baseline and charts. See `analysis/README.md`.
* **Watch one:** type the scenario's name in the SimulationManager's `scenario` field and press Play. The panel at the
  top right shows the same metrics live.

### Project structure

| Folder | Assembly | What's inside |
|---|---|---|
| `Assets/Scripts/Simulation` | `TrafficSim.Simulation` | The simulation, in plain C# with no Unity code: map reading, road network, traffic lights, vehicles, pedestrians, dispatcher, world. |
| `Assets/Scripts/Unity` | `TrafficSim.Unity` | Everything Unity: `SimulationManager` (starts and runs the world), views that move the GameObjects, HUDs, camera, day/night, Scene view gizmos. |
| `Assets/Editor` | `TrafficSim.Editor` | The `Traffic Simulation` menu. |
| `Assets/Tests` | `TrafficSim.Tests.*` | EditMode and PlayMode tests. |
| `Assets/StreamingAssets` | – | `CityMap.xlsx` and `Experiments.xlsx`. |
| `analysis` | – | Python script that turns experiment results into an Excel report. |
| `Assets/Art`, `Assets/Prefabs` | – | Models, textures and prefabs (city, vehicles, people, easter eggs). |

Because the simulation has no Unity code, it can run and be tested without opening a scene.

### Tests

`Window > General > Test Runner` in Unity, or from a terminal (with the project closed in the editor):

```
Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults results.xml
Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults results.xml
```

* **EditMode:** reading the Excel file, the map rules, the traffic light cycle, cars following each other, stopping at
  red lights and taking turns at merges, and 10 minutes of traffic in the real city with an overlap check every few
  steps, checks that traffic keeps flowing and that taxis complete rides.
* **PlayMode:** opens the City scene and lets it run; any error logged fails the test.

### Notes

* Large models are committed directly, without Git LFS (the biggest file is 28 MB, under GitHub's 100 MB limit, and
  GitHub's free LFS quota is small for a team that clones often). To start using LFS later: install `git lfs`, run
  `git lfs track "*.glb" "*.fbx" "*.png" "*.jpg" "*.exr"` and commit `.gitattributes`.
* All the numbers that tune the traffic (speed limit, light timings, gaps...) are in `SimulationSettings.cs`.
