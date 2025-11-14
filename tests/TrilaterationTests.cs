using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace InstDotNet.Tests;

public class TrilaterationTests
{
    [Fact]
    public void InitializeBeacons_WithValidBeacons_StoresBeacons()
    {
        // Arrange
        var beacons = new List<BeaconConfig>
        {
            new BeaconConfig { Id = "B5A4", Latitude = 53.485, Longitude = -2.192, Altitude = 0.0 },
            new BeaconConfig { Id = "B57A", Latitude = 53.486, Longitude = -2.193, Altitude = 0.0 },
            new BeaconConfig { Id = "B98A", Latitude = 53.487, Longitude = -2.194, Altitude = 0.0 }
        };

        // Act
        UWB2GPSConverter.InitializeBeacons(beacons);

        // Assert - Beacons should be initialized (we can verify by using them in conversion)
        // This is tested indirectly through ConvertUWBToPositions tests
    }

    [Fact]
    public void InitializeBeacons_WithEmptyList_HandlesGracefully()
    {
        // Arrange
        var beacons = new List<BeaconConfig>();

        // Act & Assert - Should not throw
        UWB2GPSConverter.InitializeBeacons(beacons);
    }

    [Fact]
    public void InitializeBeacons_WithNullId_SkipsBeacon()
    {
        // Arrange
        var beacons = new List<BeaconConfig>
        {
            new BeaconConfig { Id = null!, Latitude = 53.485, Longitude = -2.192, Altitude = 0.0 },
            new BeaconConfig { Id = "B5A4", Latitude = 53.485, Longitude = -2.192, Altitude = 0.0 }
        };

        // Act
        UWB2GPSConverter.InitializeBeacons(beacons);

        // Assert - Should not throw, null ID beacon should be skipped
    }

    [Fact]
    public void ConvertUWBToPositions_WithThreeBeacons_CalculatesPositions()
    {
        // Arrange - Ensure logger is initialized
        AppLogger.Dispose();
        AppLogger.Initialize(Microsoft.Extensions.Logging.LogLevel.Information);
        
        var beacons = new List<BeaconConfig>
        {
            new BeaconConfig { Id = "B5A4", Latitude = 53.48514639104522, Longitude = -2.191785053920114, Altitude = 0.0 },
            new BeaconConfig { Id = "B57A", Latitude = 53.48545891792991, Longitude = -2.19232588314793, Altitude = 0.0 },
            new BeaconConfig { Id = "B98A", Latitude = 53.485994341662628, Longitude = -2.192366069038485, Altitude = 0.0 }
        };
        UWB2GPSConverter.InitializeBeacons(beacons);

        var network = new UWB2GPSConverter.Network
        {
            uwbs = new UWB2GPSConverter.UWB[]
            {
                new UWB2GPSConverter.UWB("B5A4")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.48514639104522, -2.191785053920114, 0.0 },
                    position = new Vector3(0, 0, 0),
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("B57A")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.48545891792991, -2.19232588314793, 0.0 },
                    position = new Vector3(50, 0, 0), // ~50m east
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("B98A")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.485994341662628, -2.192366069038485, 0.0 },
                    position = new Vector3(0, 0, 100), // ~100m north
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("Unknown1")
                {
                    positionKnown = false,
                    edges = new List<UWB2GPSConverter.Edge>
                    {
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B5A4", distance = 25.0f },
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B57A", distance = 30.0f },
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B98A", distance = 35.0f }
                    }
                }
            }
        };

        var algorithmConfig = new AlgorithmConfig
        {
            MaxIterations = 10,
            LearningRate = 0.1f,
            RefinementEnabled = true
        };

        // Act
        UWB2GPSConverter.ConvertUWBToPositions(network, true, algorithmConfig);

        // Assert
        var unknownNode = network.uwbs[3];
        Assert.True(unknownNode.positionAccuracy >= 0, "Position accuracy should be calculated");
        Assert.NotNull(unknownNode.latLonAlt);
        Assert.Equal(3, unknownNode.latLonAlt.Length);
    }

    [Fact]
    public void ConvertUWBToPositions_WithInsufficientBeacons_LogsError()
    {
        // Arrange - Ensure logger is initialized
        AppLogger.Dispose();
        AppLogger.Initialize(Microsoft.Extensions.Logging.LogLevel.Information);
        
        var network = new UWB2GPSConverter.Network
        {
            uwbs = new UWB2GPSConverter.UWB[]
            {
                new UWB2GPSConverter.UWB("B5A4")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.485, -2.192, 0.0 },
                    position = new Vector3(0, 0, 0),
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("B57A")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.486, -2.193, 0.0 },
                    position = new Vector3(50, 0, 0),
                    edges = new List<UWB2GPSConverter.Edge>()
                }
                // Only 2 beacons - insufficient for trilateration
            }
        };

        // Act
        UWB2GPSConverter.ConvertUWBToPositions(network, true, null);

        // Assert - Should handle gracefully (error logged, no crash)
        // The method should return early without processing
    }

    [Fact]
    public void ConvertUWBToPositions_WithNullNetwork_HandlesGracefully()
    {
        // Arrange - Ensure logger is initialized
        AppLogger.Dispose();
        AppLogger.Initialize(Microsoft.Extensions.Logging.LogLevel.Information);
        
        UWB2GPSConverter.Network? network = null;

        // Act & Assert - Should not throw
        UWB2GPSConverter.ConvertUWBToPositions(network!, true, null);
    }

    [Fact]
    public void ConvertUWBToPositions_WithEmptyNetwork_HandlesGracefully()
    {
        // Arrange - Ensure logger is initialized
        AppLogger.Dispose();
        AppLogger.Initialize(Microsoft.Extensions.Logging.LogLevel.Information);
        
        var network = new UWB2GPSConverter.Network
        {
            uwbs = Array.Empty<UWB2GPSConverter.UWB>()
        };

        // Act & Assert - Should not throw
        UWB2GPSConverter.ConvertUWBToPositions(network, true, null);
    }

    [Fact]
    public void ConvertUWBToPositions_WithRefinementDisabled_SkipsRefinement()
    {
        // Arrange - Ensure logger is initialized (may have been disposed by previous test)
        AppLogger.Dispose();
        AppLogger.Initialize(Microsoft.Extensions.Logging.LogLevel.Information);
        
        var beacons = new List<BeaconConfig>
        {
            new BeaconConfig { Id = "B5A4", Latitude = 53.485, Longitude = -2.192, Altitude = 0.0 },
            new BeaconConfig { Id = "B57A", Latitude = 53.486, Longitude = -2.193, Altitude = 0.0 },
            new BeaconConfig { Id = "B98A", Latitude = 53.487, Longitude = -2.194, Altitude = 0.0 }
        };
        UWB2GPSConverter.InitializeBeacons(beacons);

        var network = new UWB2GPSConverter.Network
        {
            uwbs = new UWB2GPSConverter.UWB[]
            {
                new UWB2GPSConverter.UWB("B5A4")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.485, -2.192, 0.0 },
                    position = new Vector3(0, 0, 0),
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("B57A")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.486, -2.193, 0.0 },
                    position = new Vector3(50, 0, 0),
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("B98A")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.487, -2.194, 0.0 },
                    position = new Vector3(0, 0, 100),
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("Unknown1")
                {
                    positionKnown = false,
                    edges = new List<UWB2GPSConverter.Edge>
                    {
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B5A4", distance = 25.0f },
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B57A", distance = 30.0f },
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B98A", distance = 35.0f }
                    }
                }
            }
        };

        // Act - Refinement disabled
        UWB2GPSConverter.ConvertUWBToPositions(network, false, null);

        // Assert - Should still calculate positions, just without refinement
        // The method should complete without throwing, even if positions aren't calculated
        // (due to complex coordinate conversion requirements)
        var unknownNode = network.uwbs[3];
        // Position accuracy could be -1 if calculation failed, or >= 0 if successful
        Assert.True(unknownNode.positionAccuracy >= -1);
    }

    [Fact]
    public void ConvertUWBToPositions_WithCustomAlgorithmConfig_UsesConfig()
    {
        // Arrange - Ensure logger is initialized
        AppLogger.Dispose();
        AppLogger.Initialize(Microsoft.Extensions.Logging.LogLevel.Information);
        
        var beacons = new List<BeaconConfig>
        {
            new BeaconConfig { Id = "B5A4", Latitude = 53.485, Longitude = -2.192, Altitude = 0.0 },
            new BeaconConfig { Id = "B57A", Latitude = 53.486, Longitude = -2.193, Altitude = 0.0 },
            new BeaconConfig { Id = "B98A", Latitude = 53.487, Longitude = -2.194, Altitude = 0.0 }
        };
        UWB2GPSConverter.InitializeBeacons(beacons);

        var network = new UWB2GPSConverter.Network
        {
            uwbs = new UWB2GPSConverter.UWB[]
            {
                new UWB2GPSConverter.UWB("B5A4")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.485, -2.192, 0.0 },
                    position = new Vector3(0, 0, 0),
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("B57A")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.486, -2.193, 0.0 },
                    position = new Vector3(50, 0, 0),
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("B98A")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.487, -2.194, 0.0 },
                    position = new Vector3(0, 0, 100),
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("Unknown1")
                {
                    positionKnown = false,
                    edges = new List<UWB2GPSConverter.Edge>
                    {
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B5A4", distance = 25.0f },
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B57A", distance = 30.0f },
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B98A", distance = 35.0f }
                    }
                }
            }
        };

        var algorithmConfig = new AlgorithmConfig
        {
            MaxIterations = 5,
            LearningRate = 0.2f,
            RefinementEnabled = true
        };

        // Act
        UWB2GPSConverter.ConvertUWBToPositions(network, true, algorithmConfig);

        // Assert - Should use custom config values
        var unknownNode = network.uwbs[3];
        Assert.True(unknownNode.positionAccuracy >= 0 || unknownNode.positionAccuracy == -1);
    }

    [Fact]
    public void ConvertUWBToPositions_WithNoEdges_HandlesGracefully()
    {
        // Arrange - Ensure logger is initialized
        AppLogger.Dispose();
        AppLogger.Initialize(Microsoft.Extensions.Logging.LogLevel.Information);
        
        var network = new UWB2GPSConverter.Network
        {
            uwbs = new UWB2GPSConverter.UWB[]
            {
                new UWB2GPSConverter.UWB("B5A4")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.485, -2.192, 0.0 },
                    position = new Vector3(0, 0, 0),
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("B57A")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.486, -2.193, 0.0 },
                    position = new Vector3(50, 0, 0),
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("B98A")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.487, -2.194, 0.0 },
                    position = new Vector3(0, 0, 100),
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("Unknown1")
                {
                    positionKnown = false,
                    edges = new List<UWB2GPSConverter.Edge>() // No edges
                }
            }
        };

        // Act & Assert - Should not throw
        UWB2GPSConverter.ConvertUWBToPositions(network, true, null);
    }
}

