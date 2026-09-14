using System;
using NetTopologySuite.Geometries;

namespace Spot.Domain.Common;

public static class GeoUtils
{
    private const double EarthRadiusMeters = 6371000.0;
    public const int SridWgs84 = 4326;

    private static readonly GeometryFactory Factory = new(new PrecisionModel(), SridWgs84);

    /// <summary>
    /// Creates a WGS 84 Point from latitude and longitude.
    /// Point coordinates in NTS are (X = Longitude, Y = Latitude).
    /// </summary>
    public static Point CreatePoint(double latitude, double longitude)
    {
        return Factory.CreatePoint(new Coordinate(longitude, latitude));
    }

    /// <summary>
    /// Computes the great-circle distance between two points on the WGS 84 sphere in meters using the Haversine formula.
    /// </summary>
    public static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    /// <summary>
    /// Computes the distance in meters between two NetTopologySuite Points.
    /// Point.X = Longitude, Point.Y = Latitude.
    /// </summary>
    public static double DistanceMeters(Point p1, Point p2)
    {
        return DistanceMeters(p1.Y, p1.X, p2.Y, p2.X);
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
