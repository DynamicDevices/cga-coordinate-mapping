using System.Numerics;

using Xunit;

namespace InstDotNet.Tests;

public class WGS84ConverterTests
{
    [Fact]
    public void LatLonAltEstimate_SmallOffset_ReturnsApproximateCoordinates()
    {
        // Arrange - Reference point in Manchester, UK
        double refLat = 53.485;
        double refLon = -2.192;
        double refAlt = 0.0; // meters
        
        var refPoint = new Vector3(0, 0, 0);
        // 100 meters north (positive X in ENU - East, North, Up)
        var currentPoint = new Vector3(0, 100, 0);

        // Act
        var result = WGS84Converter.LatLonAltEstimate(refLat, refLon, refAlt, refPoint, currentPoint);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        // Should be slightly north of reference (higher latitude)
        Assert.True(result[0] > refLat, "Latitude should increase when moving north");
        // Longitude should be approximately the same for small northward movement
        Assert.InRange(result[1], refLon - 0.001, refLon + 0.001);
    }

    [Fact]
    public void LatLonAltEstimate_ZeroOffset_ReturnsReferenceCoordinates()
    {
        // Arrange
        double refLat = 53.485;
        double refLon = -2.192;
        double refAlt = 0.0;
        var refPoint = new Vector3(0, 0, 0);
        var currentPoint = new Vector3(0, 0, 0);

        // Act
        var result = WGS84Converter.LatLonAltEstimate(refLat, refLon, refAlt, refPoint, currentPoint);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        // Should be very close to reference (within rounding errors)
        Assert.InRange(result[0], refLat - 0.0001, refLat + 0.0001);
        Assert.InRange(result[1], refLon - 0.0001, refLon + 0.0001);
    }

    [Fact]
    public void LengthOneDegOfLatInMetresAtRefPoint_Manchester_ReturnsApproximateValue()
    {
        // Arrange - Manchester latitude
        double lat = 53.485;

        // Act
        var result = WGS84Converter.LengthOneDegOfLatInMetresAtRefPoint(lat);

        // Assert
        // At Manchester latitude, one degree of latitude should be approximately 111,000 meters
        Assert.InRange(result, 110000, 112000);
    }

    [Fact]
    public void LengthOneDegOfLonInMetresAtRefPoint_Manchester_ReturnsApproximateValue()
    {
        // Arrange - Manchester latitude
        double lat = 53.485;

        // Act
        var result = WGS84Converter.LengthOneDegOfLonInMetresAtRefPoint(lat);

        // Assert
        // At Manchester latitude, one degree of longitude should be less than latitude (cosine effect)
        // Approximately 66,000-67,000 meters
        Assert.InRange(result, 65000, 70000);
        // Should be less than latitude length
        var latLength = WGS84Converter.LengthOneDegOfLatInMetresAtRefPoint(lat);
        Assert.True(result < latLength, "Longitude length should be less than latitude length at this latitude");
    }

    [Fact]
    public void LengthOneDegOfLonInMetresAtRefPoint_Equator_ReturnsMaximumValue()
    {
        // Arrange - Equator
        double lat = 0.0;

        // Act
        var result = WGS84Converter.LengthOneDegOfLonInMetresAtRefPoint(lat);

        // Assert
        // At equator, longitude length should be maximum (approximately 111,000 meters)
        Assert.InRange(result, 110000, 112000);
    }
}

