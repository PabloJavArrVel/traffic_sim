using System;
using System.Collections.Generic;

public class Lane
{
    public TrafficEdge Edge;
    public List<VehicleAgent> Vehicles = new();

    public VehicleAgent GetVehicleAhead(VehicleAgent v)
    {
        int index = v.LaneIndex;

        if (index < Vehicles.Count - 1)
            return Vehicles[index + 1];

        return null;
    }

    public bool IsFree(float position, float length)
    {
        foreach (var v in Vehicles)
        {
            // Only care about vehicles near the entry point
            if (v.Position > length * 2f) continue;
            
            float dist = Math.Abs(v.Position - position);
            if (dist < (v.Length + length)) return false;
        }

        return true;
    }

    public void RemoveVehicle(VehicleAgent v)
    {
        int index = v.LaneIndex;

        Vehicles.RemoveAt(index);

        for (int i = index; i < Vehicles.Count; i++)
            Vehicles[i].LaneIndex = i;
    }
}