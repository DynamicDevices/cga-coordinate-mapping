#nullable enable
using System;
using System.Text.Json;

namespace InstDotNet;

/// <summary>
/// Health check service that tracks application health status
/// </summary>
public static class HealthCheck
{
    private static DateTime _lastUpdateTime = DateTime.MinValue;
    private static int _beaconCount = 0;
    private static int _totalNodesProcessed = 0;
    private static DateTime _startTime = DateTime.UtcNow;

    /// <summary>
    /// Initialize the health check service
    /// </summary>
    public static void Initialize()
    {
        _startTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Update the last processing time
    /// </summary>
    public static void UpdateLastProcessTime()
    {
        _lastUpdateTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Update beacon count
    /// </summary>
    public static void UpdateBeaconCount(int count)
    {
        _beaconCount = count;
    }

    /// <summary>
    /// Increment total nodes processed
    /// </summary>
    public static void IncrementNodesProcessed(int count = 1)
    {
        _totalNodesProcessed += count;
    }

    /// <summary>
    /// Get current health status
    /// </summary>
    public static HealthStatus GetStatus()
    {
        var mqttConnected = MQTTControl.IsConnected();
        var uptime = DateTime.UtcNow - _startTime;
        var timeSinceLastUpdate = _lastUpdateTime == DateTime.MinValue 
            ? TimeSpan.Zero 
            : DateTime.UtcNow - _lastUpdateTime;

        return new HealthStatus
        {
            Status = mqttConnected && timeSinceLastUpdate.TotalSeconds < 60 ? "healthy" : "degraded",
            MqttConnected = mqttConnected,
            LastUpdateTime = _lastUpdateTime == DateTime.MinValue ? null : _lastUpdateTime,
            TimeSinceLastUpdate = timeSinceLastUpdate.TotalSeconds,
            BeaconCount = _beaconCount,
            TotalNodesProcessed = _totalNodesProcessed,
            UptimeSeconds = uptime.TotalSeconds,
            Version = VersionInfo.FullVersion
        };
    }

    /// <summary>
    /// Get health status as JSON string
    /// </summary>
    public static string GetStatusJson()
    {
        var status = GetStatus();
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        return JsonSerializer.Serialize(status, options);
    }
}

/// <summary>
/// Health status model
/// </summary>
public class HealthStatus
{
    public string Status { get; set; } = "unknown";
    public bool MqttConnected { get; set; }
    public DateTime? LastUpdateTime { get; set; }
    public double TimeSinceLastUpdate { get; set; }
    public int BeaconCount { get; set; }
    public int TotalNodesProcessed { get; set; }
    public double UptimeSeconds { get; set; }
    public string Version { get; set; } = string.Empty;
}

