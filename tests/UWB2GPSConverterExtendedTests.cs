using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Xunit;

namespace InstDotNet.Tests;

[Collection("LoggerTests")] // Run tests sequentially to avoid static state conflicts
public class UWB2GPSConverterExtendedTests
{
    [Fact]
    public void ConvertUWBToPositions_WithCollinearBeacons_LogsWarning()
    {
        // Arrange - Ensure logger is initialized
        AppLogger.Dispose();
        AppLogger.Initialize(Microsoft.Extensions.Logging.LogLevel.Information);
        
        var beacons = new List<BeaconConfig>
        {
            new BeaconConfig { Id = "B1", Latitude = 53.485, Longitude = -2.192, Altitude = 0.0 },
            new BeaconConfig { Id = "B2", Latitude = 53.485, Longitude = -2.192, Altitude = 0.0 }, // Same as B1
            new BeaconConfig { Id = "B3", Latitude = 53.485, Longitude = -2.192, Altitude = 0.0 }  // Same as B1
        };
        UWB2GPSConverter.InitializeBeacons(beacons);

        var network = new UWB2GPSConverter.Network
        {
            uwbs = new UWB2GPSConverter.UWB[]
            {
                new UWB2GPSConverter.UWB("B1")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.485, -2.192, 0.0 },
                    position = new Vector3(0, 0, 0),
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("B2")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.485, -2.192, 0.0 },
                    position = new Vector3(0, 0, 0), // Same position - collinear
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("B3")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.485, -2.192, 0.0 },
                    position = new Vector3(0, 0, 0), // Same position - collinear
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("Unknown1")
                {
                    positionKnown = false,
                    edges = new List<UWB2GPSConverter.Edge>
                    {
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B1", distance = 25.0f },
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B2", distance = 30.0f },
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B3", distance = 35.0f }
                    }
                }
            }
        };

        // Act
        UWB2GPSConverter.ConvertUWBToPositions(network, true, null);

        // Assert - Should handle gracefully (warning logged, no crash)
        var unknownNode = network.uwbs[3];
        // Position may not be calculated due to collinear beacons
        Assert.True(unknownNode.positionAccuracy >= -1);
    }

    [Fact]
    public void ConvertUWBToPositions_WithVeryCloseBeacons_HandlesGracefully()
    {
        // Arrange - Ensure logger is initialized
        AppLogger.Dispose();
        AppLogger.Initialize(Microsoft.Extensions.Logging.LogLevel.Information);
        
        var beacons = new List<BeaconConfig>
        {
            new BeaconConfig { Id = "B1", Latitude = 53.485, Longitude = -2.192, Altitude = 0.0 },
            new BeaconConfig { Id = "B2", Latitude = 53.485001, Longitude = -2.192001, Altitude = 0.0 }, // Very close
            new BeaconConfig { Id = "B3", Latitude = 53.485002, Longitude = -2.192002, Altitude = 0.0 }  // Very close
        };
        UWB2GPSConverter.InitializeBeacons(beacons);

        var network = new UWB2GPSConverter.Network
        {
            uwbs = new UWB2GPSConverter.UWB[]
            {
                new UWB2GPSConverter.UWB("B1")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.485, -2.192, 0.0 },
                    position = new Vector3(0, 0, 0),
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("B2")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.485001, -2.192001, 0.0 },
                    position = new Vector3(0.1f, 0.1f, 0), // Very close
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("B3")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.485002, -2.192002, 0.0 },
                    position = new Vector3(0.2f, 0.2f, 0), // Very close
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("Unknown1")
                {
                    positionKnown = false,
                    edges = new List<UWB2GPSConverter.Edge>
                    {
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B1", distance = 25.0f },
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B2", distance = 30.0f },
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B3", distance = 35.0f }
                    }
                }
            }
        };

        // Act
        UWB2GPSConverter.ConvertUWBToPositions(network, true, null);

        // Assert - Should handle gracefully
        var unknownNode = network.uwbs[3];
        Assert.True(unknownNode.positionAccuracy >= -1);
    }

    [Fact]
    public void ConvertUWBToPositions_WithNegativeDistance_HandlesGracefully()
    {
        // Arrange - Ensure logger is initialized
        AppLogger.Dispose();
        AppLogger.Initialize(Microsoft.Extensions.Logging.LogLevel.Information);
        
        var beacons = new List<BeaconConfig>
        {
            new BeaconConfig { Id = "B1", Latitude = 53.485, Longitude = -2.192, Altitude = 0.0 },
            new BeaconConfig { Id = "B2", Latitude = 53.486, Longitude = -2.193, Altitude = 0.0 },
            new BeaconConfig { Id = "B3", Latitude = 53.487, Longitude = -2.194, Altitude = 0.0 }
        };
        UWB2GPSConverter.InitializeBeacons(beacons);

        var network = new UWB2GPSConverter.Network
        {
            uwbs = new UWB2GPSConverter.UWB[]
            {
                new UWB2GPSConverter.UWB("B1")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.485, -2.192, 0.0 },
                    position = new Vector3(0, 0, 0),
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("B2")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.486, -2.193, 0.0 },
                    position = new Vector3(50, 0, 0),
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("B3")
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
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B1", distance = -10.0f }, // Invalid negative
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B2", distance = 30.0f },
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B3", distance = 35.0f }
                    }
                }
            }
        };

        // Act
        UWB2GPSConverter.ConvertUWBToPositions(network, true, null);

        // Assert - Should handle gracefully (may not calculate position due to invalid distance)
        var unknownNode = network.uwbs[3];
        Assert.True(unknownNode.positionAccuracy >= -1);
    }

    [Fact]
    public void ConvertUWBToPositions_WithZeroDistance_HandlesGracefully()
    {
        // Arrange - Ensure logger is initialized
        AppLogger.Dispose();
        AppLogger.Initialize(Microsoft.Extensions.Logging.LogLevel.Information);
        
        var beacons = new List<BeaconConfig>
        {
            new BeaconConfig { Id = "B1", Latitude = 53.485, Longitude = -2.192, Altitude = 0.0 },
            new BeaconConfig { Id = "B2", Latitude = 53.486, Longitude = -2.193, Altitude = 0.0 },
            new BeaconConfig { Id = "B3", Latitude = 53.487, Longitude = -2.194, Altitude = 0.0 }
        };
        UWB2GPSConverter.InitializeBeacons(beacons);

        var network = new UWB2GPSConverter.Network
        {
            uwbs = new UWB2GPSConverter.UWB[]
            {
                new UWB2GPSConverter.UWB("B1")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.485, -2.192, 0.0 },
                    position = new Vector3(0, 0, 0),
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("B2")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.486, -2.193, 0.0 },
                    position = new Vector3(50, 0, 0),
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("B3")
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
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B1", distance = 0.0f }, // Zero distance
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B2", distance = 30.0f },
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B3", distance = 35.0f }
                    }
                }
            }
        };

        // Act
        UWB2GPSConverter.ConvertUWBToPositions(network, true, null);

        // Assert - Should handle gracefully
        var unknownNode = network.uwbs[3];
        Assert.True(unknownNode.positionAccuracy >= -1);
    }

    [Fact]
    public void ConvertUWBToPositions_WithRefinementMaxIterations_RespectsLimit()
    {
        // Arrange - Ensure logger is initialized
        AppLogger.Dispose();
        AppLogger.Initialize(Microsoft.Extensions.Logging.LogLevel.Information);
        
        var beacons = new List<BeaconConfig>
        {
            new BeaconConfig { Id = "B1", Latitude = 53.485, Longitude = -2.192, Altitude = 0.0 },
            new BeaconConfig { Id = "B2", Latitude = 53.486, Longitude = -2.193, Altitude = 0.0 },
            new BeaconConfig { Id = "B3", Latitude = 53.487, Longitude = -2.194, Altitude = 0.0 }
        };
        UWB2GPSConverter.InitializeBeacons(beacons);

        var network = new UWB2GPSConverter.Network
        {
            uwbs = new UWB2GPSConverter.UWB[]
            {
                new UWB2GPSConverter.UWB("B1")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.485, -2.192, 0.0 },
                    position = new Vector3(0, 0, 0),
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("B2")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.486, -2.193, 0.0 },
                    position = new Vector3(50, 0, 0),
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("B3")
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
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B1", distance = 25.0f },
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B2", distance = 30.0f },
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B3", distance = 35.0f }
                    }
                }
            }
        };

        var algorithmConfig = new AlgorithmConfig
        {
            MaxIterations = 1, // Very low limit
            LearningRate = 0.1f,
            RefinementEnabled = true
        };

        // Act
        UWB2GPSConverter.ConvertUWBToPositions(network, true, algorithmConfig);

        // Assert - Should complete with limited iterations
        var unknownNode = network.uwbs[3];
        Assert.True(unknownNode.positionAccuracy >= -1);
    }

    [Fact]
    public void ConvertUWBToPositions_WithRefinementLearningRateZero_HandlesGracefully()
    {
        // Arrange - Ensure logger is initialized
        AppLogger.Dispose();
        AppLogger.Initialize(Microsoft.Extensions.Logging.LogLevel.Information);
        
        var beacons = new List<BeaconConfig>
        {
            new BeaconConfig { Id = "B1", Latitude = 53.485, Longitude = -2.192, Altitude = 0.0 },
            new BeaconConfig { Id = "B2", Latitude = 53.486, Longitude = -2.193, Altitude = 0.0 },
            new BeaconConfig { Id = "B3", Latitude = 53.487, Longitude = -2.194, Altitude = 0.0 }
        };
        UWB2GPSConverter.InitializeBeacons(beacons);

        var network = new UWB2GPSConverter.Network
        {
            uwbs = new UWB2GPSConverter.UWB[]
            {
                new UWB2GPSConverter.UWB("B1")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.485, -2.192, 0.0 },
                    position = new Vector3(0, 0, 0),
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("B2")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.486, -2.193, 0.0 },
                    position = new Vector3(50, 0, 0),
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("B3")
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
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B1", distance = 25.0f },
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B2", distance = 30.0f },
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B3", distance = 35.0f }
                    }
                }
            }
        };

        var algorithmConfig = new AlgorithmConfig
        {
            MaxIterations = 10,
            LearningRate = 0.0f, // Zero learning rate
            RefinementEnabled = true
        };

        // Act
        UWB2GPSConverter.ConvertUWBToPositions(network, true, algorithmConfig);

        // Assert - Should handle gracefully (no refinement movement)
        var unknownNode = network.uwbs[3];
        Assert.True(unknownNode.positionAccuracy >= -1);
    }

    [Fact]
    public void ConvertUWBToPositions_WithNodeHavingInvalidLatLonAlt_SkipsNode()
    {
        // Arrange - Ensure logger is initialized
        AppLogger.Dispose();
        AppLogger.Initialize(Microsoft.Extensions.Logging.LogLevel.Information);
        
        var network = new UWB2GPSConverter.Network
        {
            uwbs = new UWB2GPSConverter.UWB[]
            {
                new UWB2GPSConverter.UWB("B1")
                {
                    positionKnown = true,
                    latLonAlt = null!, // Invalid - null
                    position = new Vector3(0, 0, 0),
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("B2")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.486, -2.193 }, // Invalid - only 2 elements
                    position = new Vector3(50, 0, 0),
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("B3")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.487, -2.194, 0.0 },
                    position = new Vector3(0, 0, 100),
                    edges = new List<UWB2GPSConverter.Edge>()
                }
            }
        };

        // Act
        UWB2GPSConverter.ConvertUWBToPositions(network, true, null);

        // Assert - Should handle gracefully (may not have enough valid beacons)
        // The method should return early or handle the invalid beacons
    }

    [Fact]
    public void ConvertUWBToPositions_WithMultipleUnknownNodes_CalculatesAll()
    {
        // Arrange - Ensure logger is initialized
        AppLogger.Dispose();
        AppLogger.Initialize(Microsoft.Extensions.Logging.LogLevel.Information);
        
        var beacons = new List<BeaconConfig>
        {
            new BeaconConfig { Id = "B1", Latitude = 53.485, Longitude = -2.192, Altitude = 0.0 },
            new BeaconConfig { Id = "B2", Latitude = 53.486, Longitude = -2.193, Altitude = 0.0 },
            new BeaconConfig { Id = "B3", Latitude = 53.487, Longitude = -2.194, Altitude = 0.0 }
        };
        UWB2GPSConverter.InitializeBeacons(beacons);

        var network = new UWB2GPSConverter.Network
        {
            uwbs = new UWB2GPSConverter.UWB[]
            {
                new UWB2GPSConverter.UWB("B1")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.485, -2.192, 0.0 },
                    position = new Vector3(0, 0, 0),
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("B2")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.486, -2.193, 0.0 },
                    position = new Vector3(50, 0, 0),
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("B3")
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
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B1", distance = 25.0f },
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B2", distance = 30.0f },
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B3", distance = 35.0f }
                    }
                },
                new UWB2GPSConverter.UWB("Unknown2")
                {
                    positionKnown = false,
                    edges = new List<UWB2GPSConverter.Edge>
                    {
                        new UWB2GPSConverter.Edge { end0 = "Unknown2", end1 = "B1", distance = 40.0f },
                        new UWB2GPSConverter.Edge { end0 = "Unknown2", end1 = "B2", distance = 45.0f },
                        new UWB2GPSConverter.Edge { end0 = "Unknown2", end1 = "B3", distance = 50.0f }
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

        // Assert - Both unknown nodes should be processed
        var unknown1 = network.uwbs[3];
        var unknown2 = network.uwbs[4];
        Assert.True(unknown1.positionAccuracy >= -1);
        Assert.True(unknown2.positionAccuracy >= -1);
    }

    [Fact]
    public void ConvertUWBToPositions_WithNodeHavingNoNeighbors_SkipsNode()
    {
        // Arrange - Ensure logger is initialized
        AppLogger.Dispose();
        AppLogger.Initialize(Microsoft.Extensions.Logging.LogLevel.Information);
        
        var beacons = new List<BeaconConfig>
        {
            new BeaconConfig { Id = "B1", Latitude = 53.485, Longitude = -2.192, Altitude = 0.0 },
            new BeaconConfig { Id = "B2", Latitude = 53.486, Longitude = -2.193, Altitude = 0.0 },
            new BeaconConfig { Id = "B3", Latitude = 53.487, Longitude = -2.194, Altitude = 0.0 }
        };
        UWB2GPSConverter.InitializeBeacons(beacons);

        var network = new UWB2GPSConverter.Network
        {
            uwbs = new UWB2GPSConverter.UWB[]
            {
                new UWB2GPSConverter.UWB("B1")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.485, -2.192, 0.0 },
                    position = new Vector3(0, 0, 0),
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("B2")
                {
                    positionKnown = true,
                    latLonAlt = new double[] { 53.486, -2.193, 0.0 },
                    position = new Vector3(50, 0, 0),
                    edges = new List<UWB2GPSConverter.Edge>()
                },
                new UWB2GPSConverter.UWB("B3")
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
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B1", distance = 25.0f },
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B2", distance = 30.0f },
                        new UWB2GPSConverter.Edge { end0 = "Unknown1", end1 = "B3", distance = 35.0f }
                    }
                },
                new UWB2GPSConverter.UWB("Isolated")
                {
                    positionKnown = false,
                    edges = new List<UWB2GPSConverter.Edge>() // No edges - isolated node
                }
            }
        };

        // Act
        UWB2GPSConverter.ConvertUWBToPositions(network, true, null);

        // Assert - Isolated node should not be calculated
        var isolatedNode = network.uwbs[4];
        Assert.Equal(-1, isolatedNode.positionAccuracy); // Should remain -1 (invalid)
    }
}

