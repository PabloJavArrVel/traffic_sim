using UnityEngine;

/// <summary>The texts the HUDs show for taxis and pedestrians (in Spanish, like the rest of the UI).</summary>
public static class TaxiDescriptions
{
    public static string Status(Taxi taxi)
    {
        switch (taxi.State)
        {
            case TaxiState.Cruising:
                return "Disponible";
            case TaxiState.GoingToPickup:
                return $"En camino al pasajero ({taxi.Passenger.PickupCell.Position}) · {Distance(taxi.DistanceToGo)}";
            case TaxiState.Boarding:
                return "Subiendo pasajero";
            case TaxiState.Carrying:
                return $"En viaje a {taxi.Passenger.DestinationCell.Position} · {Distance(taxi.DistanceToGo)}";
            default:
                return "Bajando pasajero";
        }
    }

    public static string Status(Pedestrian pedestrian)
    {
        switch (pedestrian.State)
        {
            case PedestrianState.WaitingForTaxi: return "Esperando taxi";
            case PedestrianState.TaxiOnTheWay: return "Taxi asignado";
            case PedestrianState.Riding: return "En taxi";
            case PedestrianState.Arrived: return "Llegó";
            default: return "Se cansó de esperar";
        }
    }

    public static string Distance(float meters)
    {
        if (meters >= 1000f) return $"{meters / 1000f:F1} km";
        if (meters >= 10f) return $"{Mathf.RoundToInt(meters)} m";
        return "< 10 m";
    }
}
