using FluentAssertions;
using Spot.Domain.Common;
using Xunit;

namespace Spot.Tests;

public class SpatialCalculationsTests
{
    // Anchor: NYC Civic Center
    private const double CenterLat = 40.7128;
    private const double CenterLon = -74.0060;

    [Fact]
    public void DistanceMeters_ToSelf_ShouldBeZero()
    {
        double distance = GeoUtils.DistanceMeters(CenterLat, CenterLon, CenterLat, CenterLon);
        distance.Should().BeApproximately(0.0, 0.001);
    }

    [Fact]
    public void DistanceMeters_StoreWithin1500m_ShouldBeAccurateAndUnderRadius()
    {
        // Store 1: ~250m
        double store1Lat = 40.7145;
        double store1Lon = -74.0080;
        double dist1 = GeoUtils.DistanceMeters(CenterLat, CenterLon, store1Lat, store1Lon);

        dist1.Should().BeInRange(200, 350);
        dist1.Should().BeLessThan(1500);

        // Store 2: ~650m
        double store2Lat = 40.7180;
        double store2Lon = -74.0020;
        double dist2 = GeoUtils.DistanceMeters(CenterLat, CenterLon, store2Lat, store2Lon);

        dist2.Should().BeInRange(550, 750);
        dist2.Should().BeLessThan(1500);

        // Store 3: ~1250m
        double store3Lat = 40.7220;
        double store3Lon = -73.9980;
        double dist3 = GeoUtils.DistanceMeters(CenterLat, CenterLon, store3Lat, store3Lon);

        dist3.Should().BeInRange(1100, 1400);
        dist3.Should().BeLessThan(1500);
    }

    [Fact]
    public void DistanceMeters_StoreOutside1500m_ShouldExceedRadius()
    {
        // Store 4: ~2600m away
        double store4Lat = 40.7350;
        double store4Lon = -74.0120;
        double dist4 = GeoUtils.DistanceMeters(CenterLat, CenterLon, store4Lat, store4Lon);

        dist4.Should().BeGreaterThan(1500);
        dist4.Should().BeInRange(2400, 2800);
    }
}
