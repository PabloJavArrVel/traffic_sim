using UnityEngine;
using System.Collections.Generic;

public class CityRenderer : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Standard straight road segment")]
    public GameObject roadPrefab;

    [Tooltip("Traffic light pole — spawned beside a road tile")]
    public GameObject trafficLightPrefab;

    [Tooltip("Central roundabout / glorieta")]
    public GameObject roundaboutPrefab;

    [Tooltip("Building block")]
    public GameObject buildingPrefab;

    [Tooltip("Parking lot")]
    public GameObject parkingPrefab;

    [Header("Params")]
    [Tooltip("Must match real-world tile size (4.5 m)")]
    public float tileSize = 1f;

    // ---------------------------------------------------------------
    // Grid legend
    //   R    – road (intersection / no dominant direction)
    //   RE   – road running East-West
    //   RN   – road running North-South
    //   RT_E – traffic-light tile, E-W road
    //   RT_N – traffic-light tile, N-S road
    //   Rb   – roundabout
    //   B    – building
    //   P    – parking
    // ---------------------------------------------------------------

    public void BuildCity(string[,] grid)
    {
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                // Place origin at (0,0,0); Z goes negative into the scene
                Vector3 pos = new Vector3(col * tileSize, 0f, -row * tileSize);
                SpawnTile(grid[row, col], pos);
            }
        }
    }

    void SpawnTile(string cell, Vector3 pos)
    {
        // --- Parse base type and optional orientation suffix ---
        string baseType;
        Quaternion rot = Quaternion.identity;

        if (cell.EndsWith("_E"))
        {
            baseType = cell[..^2];          // strip "_E"
            rot = Quaternion.Euler(0, 90f, 0);
        }
        else if (cell.EndsWith("_N"))
        {
            baseType = cell[..^2];          // strip "_N"
            rot = Quaternion.identity;       // prefab default faces N-S
        }
        else
        {
            baseType = cell;
        }

        switch (baseType)
        {
            case "R":
            case "RE":
            case "RN":
                Spawn(roadPrefab, pos, rot);
                break;

            case "RT":
                // Road tile + traffic light pole offset to the side
                Spawn(roadPrefab, pos, rot);
                if (trafficLightPrefab != null)
                {
                    // Offset pole to the right-hand kerb of the road direction
                    Vector3 poleOffset = rot * new Vector3(tileSize * 0.4f, 0f, 0f);
                    Spawn(trafficLightPrefab, pos + poleOffset,
                          Quaternion.Euler(0, rot.eulerAngles.y + 90f, 0));
                }
                break;

            case "Rb":
                Spawn(roundaboutPrefab, pos, Quaternion.identity);
                break;

            case "B":
                Spawn(buildingPrefab, pos, Quaternion.identity);
                break;

            case "P":
                Spawn(parkingPrefab != null ? parkingPrefab : buildingPrefab,
                      pos, Quaternion.identity);
                break;
        }
    }

    void Spawn(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (prefab != null)
            Instantiate(prefab, pos, rot, transform);
    }
}