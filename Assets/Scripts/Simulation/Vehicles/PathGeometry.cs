/// <summary>
/// Where to draw a point of a vehicle's path, and which way the vehicle faces there.
///
/// In the simulation cars go straight from cell center to cell center and turn sharply at the centers.
/// To look natural we round each 90° corner with a curve (a quadratic Bezier curve) that starts
/// 'cornerRadius' meters before the corner and ends 'cornerRadius' meters after it,
/// and we draw lane changes as a gentle S-curve instead of a diagonal line.
/// </summary>
public static class PathGeometry
{
    public static MapPose PoseAt(VehiclePath path, PathPoint point, float cornerRadius)
    {
        CellLink link = path.LinkAt(point.Index);
        if (link.IsLaneChange)
            return LaneChangePose(link, point.Distance);

        bool hasNextLink = point.Index + 1 < path.LastIndex;
        if (hasNextLink && point.Distance > link.Length - cornerRadius)
        {
            CellLink next = path.LinkAt(point.Index + 1);
            if (Turns(link, next))
            {
                // First half of the corner at the end of this link.
                float progress = (point.Distance - (link.Length - cornerRadius)) / (2f * cornerRadius);
                return CornerPose(link, next, cornerRadius, progress);
            }
        }

        bool hasPreviousLink = point.Index > 0;
        if (hasPreviousLink && point.Distance < cornerRadius)
        {
            CellLink previous = path.LinkAt(point.Index - 1);
            if (Turns(previous, link))
            {
                // Second half of the corner at the start of this link.
                float progress = 0.5f + point.Distance / (2f * cornerRadius);
                return CornerPose(previous, link, cornerRadius, progress);
            }
        }

        MapPoint forward = link.Heading.ToMapVector();
        return new MapPose(link.From.Center + forward * point.Distance, forward);
    }

    static bool Turns(CellLink first, CellLink second) => first.Heading != second.Heading;

    /// <summary>progress goes from 0 ('radius' before the corner) to 1 ('radius' after it).</summary>
    static MapPose CornerPose(CellLink linkIn, CellLink linkOut, float radius, float progress)
    {
        MapPoint corner = linkIn.To.Center;
        MapPoint start = corner - linkIn.Heading.ToMapVector() * radius;
        MapPoint end = corner + linkOut.Heading.ToMapVector() * radius;

        float t = progress;
        MapPoint position = (1 - t) * (1 - t) * start + 2 * (1 - t) * t * corner + t * t * end;
        MapPoint forward = 2 * (1 - t) * (corner - start) + 2 * t * (end - corner);
        return new MapPose(position, forward.Normalized);
    }

    static MapPose LaneChangePose(CellLink link, float distance)
    {
        MapPoint forwardDirection = link.Heading.ToMapVector();
        MapPoint fromTo = link.To.Center - link.From.Center;
        MapPoint forwardPart = forwardDirection * MapPoint.Dot(fromTo, forwardDirection);
        MapPoint sidewaysPart = fromTo - forwardPart;

        // Move forward at a steady pace and sideways following a smooth S-curve.
        float t = distance / link.Length;
        float sideways = t * t * (3f - 2f * t);
        float sidewaysSlope = 6f * t * (1f - t);

        MapPoint position = link.From.Center + forwardPart * t + sidewaysPart * sideways;
        MapPoint forward = forwardPart + sidewaysPart * sidewaysSlope;
        return new MapPose(position, forward.Normalized);
    }
}
