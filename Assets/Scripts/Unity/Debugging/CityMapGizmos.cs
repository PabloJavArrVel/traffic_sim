using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Draws the road network of CityMap.xlsx in the Scene view, without pressing Play, so you can check your edits:
///   white lines      where cars can drive from each cell (a small dot marks the end they drive to)
///   orange squares   junction cells
///   colored spheres  traffic lights (same color = same intersection)
///   cyan squares     (Play mode only) the cells each car holds right now
/// Put it on the same GameObject as the SimulationManager. Save the Excel file and the drawing updates.
/// </summary>
[RequireComponent(typeof(SimulationManager))]
public class CityMapGizmos : MonoBehaviour
{
    public bool showLinks = true;
    public bool showLaneChanges = false;
    public bool showJunctions = true;
    public bool showTrafficLights = true;
    public bool showHeldCellsInPlayMode = true;

    static readonly Color[] IntersectionColors =
    {
        Color.red, Color.green, Color.blue, Color.magenta, Color.yellow, Color.cyan, new Color(1f, 0.5f, 0f), Color.white
    };

    RoadNetwork network;
    MapPlacement placement;
    string loadedFile;
    DateTime loadedFileTime;
    string loadProblem;

    void OnDrawGizmos()
    {
        if (!LoadNetworkIfNeeded()) return;

        float cellSize = placement.ToWorldLength(network.MetersPerCell);
        foreach (RoadCell cell in network.Cells)
        {
            if (showJunctions && cell.IsJunction)
            {
                Gizmos.color = new Color(1f, 0.55f, 0f, 0.8f);
                Gizmos.DrawWireCube(placement.ToWorld(cell.Center, 0.05f), new Vector3(cellSize * 0.9f, 0.02f, cellSize * 0.9f));
            }

            if (!showLinks) continue;
            foreach (CellLink link in cell.Exits)
            {
                if (link.IsLaneChange && !showLaneChanges) continue;
                Gizmos.color = link.IsLaneChange ? new Color(0.5f, 0.5f, 1f, 0.5f) : new Color(1f, 1f, 1f, 0.8f);
                Vector3 from = placement.ToWorld(link.From.Center, 0.1f);
                Vector3 to = placement.ToWorld(link.To.Center, 0.1f);
                Gizmos.DrawLine(from, to);
                Gizmos.DrawSphere(Vector3.Lerp(from, to, 0.8f), cellSize * 0.05f);
            }
        }

        if (showTrafficLights) DrawTrafficLights(cellSize);
        if (showHeldCellsInPlayMode && Application.isPlaying) DrawHeldCells(cellSize);
    }

    void DrawTrafficLights(float cellSize)
    {
        for (int i = 0; i < network.TrafficLightControllers.Count; i++)
        {
            Gizmos.color = IntersectionColors[i % IntersectionColors.Length];
            foreach (TrafficLight light in network.TrafficLightControllers[i].Lights)
            {
                MapPoint stopLine = light.Cell.Center + light.TrafficDirection.ToMapVector() * (network.MetersPerCell / 2f);
                Gizmos.DrawSphere(placement.ToWorld(stopLine, 0.3f), cellSize * 0.12f);
            }
        }
    }

    void DrawHeldCells(float cellSize)
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
        foreach (RoadCell cell in network.Cells)
        {
            if (cell.IsFree) continue;
            Gizmos.DrawCube(placement.ToWorld(cell.Center, 0.02f), new Vector3(cellSize * 0.8f, 0.01f, cellSize * 0.8f));
        }
    }

    /// <summary>In Play mode we draw the running simulation; otherwise we read the Excel file again when it changes.</summary>
    bool LoadNetworkIfNeeded()
    {
        var simulation = GetComponent<SimulationManager>();
        if (Application.isPlaying)
        {
            if (simulation.World == null) return false;
            network = simulation.World.Network;
            placement = simulation.Placement;
            return true;
        }

        string path = CityMapLoader.PathTo(simulation.cityMapFile);
        if (!File.Exists(path)) return false;

        DateTime fileTime = File.GetLastWriteTimeUtc(path);
        bool upToDate = network != null && loadedFile == path && loadedFileTime == fileTime;
        if (upToDate) return true;

        loadedFile = path;
        loadedFileTime = fileTime;
        var settings = new SimulationSettings();
        try
        {
            network = CityMapLoader.LoadRoadNetwork(simulation.cityMapFile, settings);
            placement = new MapPlacement(simulation.cellA1Position, simulation.cellSize, settings.MetersPerCell);
            loadProblem = null;
            return true;
        }
        catch (CityMapException problem)
        {
            network = null;
            if (loadProblem != problem.Message) Debug.LogWarning(problem.Message);
            loadProblem = problem.Message;
            return false;
        }
    }
}
