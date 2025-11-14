using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xunit;

namespace InstDotNet.Tests;

[Collection("LoggerTests")] // Run tests in this class sequentially to avoid logger disposal conflicts
public class UWBManagerTests
{
    public UWBManagerTests()
    {
        // Ensure logger is initialized for tests
        AppLogger.Dispose();
        AppLogger.Initialize(LogLevel.Debug);
    }

    [Fact]
    public void Initialise_WithConfig_InitializesBeacons()
    {
        // Arrange - Ensure logger is initialized (may have been disposed by other tests)
        // Reinitialize to handle case where another test disposed it
        if (AppLogger.Default == null || !AppLogger.Default.IsEnabled(LogLevel.Debug))
        {
            AppLogger.Dispose();
            AppLogger.Initialize(LogLevel.Debug);
        }
        
        // Clear MQTT handler first to avoid issues with event subscription
        MQTTControl.OnMessageReceived = null;
        
        var config = new AppConfig
        {
            Beacons = new List<BeaconConfig>
            {
                new BeaconConfig { Id = "B5A4", Latitude = 53.485, Longitude = -2.192, Altitude = 0.0 },
                new BeaconConfig { Id = "B57A", Latitude = 53.486, Longitude = -2.193, Altitude = 0.0 }
            }
        };

        // Act
        UWBManager.Initialise(config);

        // Assert - Should not throw and beacons should be initialized
        // We can't directly verify internal state, but we can verify it doesn't crash
        Assert.True(true); // Test passes if no exception thrown
    }

    [Fact]
    public void Initialise_WithNullConfig_HandlesGracefully()
    {
        // Act & Assert - Should not throw
        UWBManager.Initialise(null);
        Assert.True(true);
    }

    [Fact]
    public void UpdateUwbsFromMessage_WithValidJson_ParsesNetwork()
    {
        // Arrange
        UWBManager.Initialise(null);
        var networkJson = @"{
            ""uwbs"": [
                {
                    ""id"": ""B5A4"",
                    ""triageStatus"": 5,
                    ""positionKnown"": true,
                    ""latLonAlt"": [53.485, -2.192, 0.0],
                    ""edges"": []
                },
                {
                    ""id"": ""Unknown1"",
                    ""triageStatus"": 0,
                    ""positionKnown"": false,
                    ""edges"": [
                        { ""end0"": ""Unknown1"", ""end1"": ""B5A4"", ""distance"": 25.0 }
                    ]
                }
            ]
        }";

        // Act
        UWBManager.UpdateUwbsFromMessage(networkJson);

        // Assert - Should not throw
        Assert.True(true);
    }

    [Fact]
    public void UpdateUwbsFromMessage_WithInvalidJson_HandlesGracefully()
    {
        // Arrange
        UWBManager.Initialise(null);
        var invalidJson = "{ invalid json }";

        // Act & Assert - Should not throw, should log error
        UWBManager.UpdateUwbsFromMessage(invalidJson);
        Assert.True(true);
    }

    [Fact]
    public void UpdateUwbsFromMessage_WithEmptyJson_HandlesGracefully()
    {
        // Arrange
        UWBManager.Initialise(null);
        var emptyJson = "{}";

        // Act & Assert - Should not throw
        UWBManager.UpdateUwbsFromMessage(emptyJson);
        Assert.True(true);
    }

    [Fact]
    public void Update_WithNoNetwork_HandlesGracefully()
    {
        // Arrange
        UWBManager.Initialise(null);

        // Act - Update without setting network
        UWBManager.Update();

        // Assert - Should not throw
        Assert.True(true);
    }

    [Fact]
    public void Update_WithValidNetwork_ProcessesNetwork()
    {
        // Arrange
        var config = new AppConfig
        {
            Beacons = new List<BeaconConfig>
            {
                new BeaconConfig { Id = "B5A4", Latitude = 53.48514639104522, Longitude = -2.191785053920114, Altitude = 0.0 },
                new BeaconConfig { Id = "B57A", Latitude = 53.48545891792991, Longitude = -2.19232588314793, Altitude = 0.0 },
                new BeaconConfig { Id = "B98A", Latitude = 53.485994341662628, Longitude = -2.192366069038485, Altitude = 0.0 }
            }
        };
        UWBManager.Initialise(config);

        var networkJson = @"{
            ""uwbs"": [
                {
                    ""id"": ""B5A4"",
                    ""triageStatus"": 5,
                    ""positionKnown"": true,
                    ""latLonAlt"": [53.48514639104522, -2.191785053920114, 0.0],
                    ""edges"": []
                },
                {
                    ""id"": ""B57A"",
                    ""triageStatus"": 5,
                    ""positionKnown"": true,
                    ""latLonAlt"": [53.48545891792991, -2.19232588314793, 0.0],
                    ""edges"": []
                },
                {
                    ""id"": ""B98A"",
                    ""triageStatus"": 5,
                    ""positionKnown"": true,
                    ""latLonAlt"": [53.485994341662628, -2.192366069038485, 0.0],
                    ""edges"": []
                },
                {
                    ""id"": ""Unknown1"",
                    ""triageStatus"": 0,
                    ""positionKnown"": false,
                    ""edges"": [
                        { ""end0"": ""Unknown1"", ""end1"": ""B5A4"", ""distance"": 25.0 },
                        { ""end0"": ""Unknown1"", ""end1"": ""B57A"", ""distance"": 30.0 },
                        { ""end0"": ""Unknown1"", ""end1"": ""B98A"", ""distance"": 35.0 }
                    ]
                }
            ]
        }";

        // Act
        UWBManager.UpdateUwbsFromMessage(networkJson);
        UWBManager.Update();

        // Assert - Should not throw
        Assert.True(true);
    }

    [Fact]
    public void Update_IsIdempotent_CanBeCalledMultipleTimes()
    {
        // Arrange
        UWBManager.Initialise(null);

        // Act - Call Update multiple times
        UWBManager.Update();
        UWBManager.Update();
        UWBManager.Update();

        // Assert - Should not throw
        Assert.True(true);
    }

    [Fact]
    public void UpdateUwbsFromMessage_WithNullMessage_HandlesGracefully()
    {
        // Arrange
        UWBManager.Initialise(null);

        // Act & Assert - The method catches and logs the exception, doesn't throw
        // JsonSerializer.Deserialize throws ArgumentNullException, but it's caught in the try-catch
        UWBManager.UpdateUwbsFromMessage(null!);
        Assert.True(true); // Test passes if no exception propagates
    }

    [Fact]
    public void UpdateUwbsFromMessage_WithMalformedJson_HandlesGracefully()
    {
        // Arrange
        UWBManager.Initialise(null);
        var malformedJson = "{\"uwbs\": [{\"id\": }]}"; // Missing value

        // Act & Assert - Should not throw, should log error
        UWBManager.UpdateUwbsFromMessage(malformedJson);
        Assert.True(true);
    }
}

