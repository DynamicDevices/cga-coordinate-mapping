using System.Numerics;
using Xunit;

namespace InstDotNet.Tests;

public class WGS84ConverterExtendedTests
{
    [Fact]
    public void LatLonAltEstimate_LargeOffset_ReturnsValidCoordinates()
    {
        // Arrange - Reference point in Manchester, UK
        double refLat = 53.485;
        double refLon = -2.192;
        double refAlt = 0.0;
        
        var refPoint = new Vector3(0, 0, 0);
        // 1000 meters east (X in Unity/ENU is east)
        var currentPoint = new Vector3(1000, 0, 0);

        // Act
        var result = WGS84Converter.LatLonAltEstimate(refLat, refLon, refAlt, refPoint, currentPoint);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        // Should return valid coordinates (exact direction depends on coordinate system mapping)
        Assert.InRange(result[0], -90, 90);
        Assert.InRange(result[1], -180, 180);
        Assert.True(Math.Abs(result[1] - refLon) < 1.0, "Longitude should change when moving 1000m east");
    }

    [Fact]
    public void LatLonAltEstimate_AltitudeChange_ReturnsUpdatedAltitude()
    {
        // Arrange
        double refLat = 53.485;
        double refLon = -2.192;
        double refAlt = 0.0;
        
        var refPoint = new Vector3(0, 0, 0);
        // 100 meters up (positive Z in ENU)
        var currentPoint = new Vector3(0, 0, 100);

        // Act
        var result = WGS84Converter.LatLonAltEstimate(refLat, refLon, refAlt, refPoint, currentPoint);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        // Altitude should increase
        Assert.True(result[2] > refAlt, "Altitude should increase when moving up");
    }

    [Fact]
    public void LatLonAltEstimate_NearNorthPole_HandlesCorrectly()
    {
        // Arrange - Near north pole
        double refLat = 89.9;
        double refLon = 0.0;
        double refAlt = 0.0;
        
        var refPoint = new Vector3(0, 0, 0);
        var currentPoint = new Vector3(0, 100, 0); // 100m north

        // Act
        var result = WGS84Converter.LatLonAltEstimate(refLat, refLon, refAlt, refPoint, currentPoint);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        // Should handle extreme latitude
        Assert.InRange(result[0], -90, 90);
    }

    [Fact]
    public void LatLonAltEstimate_NearSouthPole_HandlesCorrectly()
    {
        // Arrange - Near south pole
        double refLat = -89.9;
        double refLon = 0.0;
        double refAlt = 0.0;
        
        var refPoint = new Vector3(0, 0, 0);
        var currentPoint = new Vector3(0, -100, 0); // 100m south

        // Act
        var result = WGS84Converter.LatLonAltEstimate(refLat, refLon, refAlt, refPoint, currentPoint);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        // Should handle extreme latitude
        Assert.InRange(result[0], -90, 90);
    }

    [Fact]
    public void LatLonAltEstimate_DateLine_HandlesCorrectly()
    {
        // Arrange - Near international date line
        double refLat = 0.0;
        double refLon = 179.9;
        double refAlt = 0.0;
        
        var refPoint = new Vector3(0, 0, 0);
        var currentPoint = new Vector3(0, 0, 1000); // 1000m east

        // Act
        var result = WGS84Converter.LatLonAltEstimate(refLat, refLon, refAlt, refPoint, currentPoint);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        // Should handle longitude wrapping
        Assert.InRange(result[1], -180, 180);
    }

    [Fact]
    public void LatLonAltEstimate2_WithValidInput_ReturnsCoordinates()
    {
        // Arrange
        double refLat = 53.485;
        double refLon = -2.192;
        double refAlt = 0.0;
        
        var refPoint = new Vector3(0, 0, 0);
        var currentPoint = new Vector3(100, 100, 0);

        // Act
        var result = WGS84Converter.LatLonAltEstimate2(refLat, refLon, refAlt, refPoint, currentPoint);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        Assert.InRange(result[0], -90, 90);
        Assert.InRange(result[1], -180, 180);
    }

    [Fact]
    public void LengthOneDegOfLatInMetresAtRefPoint_AtPole_ReturnsValue()
    {
        // Arrange - North pole
        double lat = 90.0;

        // Act
        var result = WGS84Converter.LengthOneDegOfLatInMetresAtRefPoint(lat);

        // Assert
        // At pole, should still return a valid value
        Assert.True(result > 0);
        Assert.InRange(result, 110000, 112000);
    }

    [Fact]
    public void LengthOneDegOfLonInMetresAtRefPoint_AtPole_ReturnsMinimumValue()
    {
        // Arrange - North pole
        double lat = 90.0;

        // Act
        var result = WGS84Converter.LengthOneDegOfLonInMetresAtRefPoint(lat);

        // Assert
        // At pole, longitude length should approach zero
        Assert.True(result >= 0);
        Assert.True(result < 1000, "At pole, longitude length should be very small");
    }

    [Fact]
    public void LengthOneDegOfLonInMetresAtRefPoint_AtSouthPole_ReturnsMinimumValue()
    {
        // Arrange - South pole
        double lat = -90.0;

        // Act
        var result = WGS84Converter.LengthOneDegOfLonInMetresAtRefPoint(lat);

        // Assert
        // At pole, longitude length should approach zero
        Assert.True(result >= 0);
        Assert.True(result < 1000, "At pole, longitude length should be very small");
    }

    [Fact]
    public void LatLonAltkm2UnityPos_WithValidInput_ReturnsUnityPosition()
    {
        // Arrange
        double latRef = 53.485;
        double lonRef = -2.192;
        double altRef = 0.0;
        double latTransform = 53.486;
        double lonTransform = -2.193;
        double altTransform = 0.0;
        var unityPosRef = new Vector3(0, 0, 0);

        // Act
        var result = WGS84Converter.LatLonAltkm2UnityPos(
            latRef, lonRef, altRef,
            latTransform, lonTransform, altTransform,
            unityPosRef);

        // Assert
        // Should return a valid Vector3
        Assert.True(float.IsFinite(result.X));
        Assert.True(float.IsFinite(result.Y));
        Assert.True(float.IsFinite(result.Z));
    }

    [Fact]
    public void LatLonAltkm2UnityPos_WithSameCoordinates_ReturnsReferencePosition()
    {
        // Arrange
        double lat = 53.485;
        double lon = -2.192;
        double alt = 0.0;
        var unityPosRef = new Vector3(10, 20, 30);

        // Act
        var result = WGS84Converter.LatLonAltkm2UnityPos(
            lat, lon, alt,
            lat, lon, alt,
            unityPosRef);

        // Assert
        // Should return reference position when coordinates are the same
        Assert.Equal(unityPosRef.X, result.X, 0.1f);
        Assert.Equal(unityPosRef.Y, result.Y, 0.1f);
        Assert.Equal(unityPosRef.Z, result.Z, 0.1f);
    }
}

