#nullable enable
using Microsoft.Extensions.Configuration;

namespace InstDotNet;

/// <summary>
/// Application configuration model loaded from appsettings.json
/// </summary>
public class AppConfig
{
    public MqttConfig MQTT { get; set; } = new();
    public ApplicationConfig Application { get; set; } = new();
    public AlgorithmConfig Algorithm { get; set; } = new();
    public List<BeaconConfig> Beacons { get; set; } = new();

    /// <summary>
    /// Load configuration from appsettings.json and environment variables
    /// </summary>
    public static AppConfig Load()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        var configuration = builder.Build();
        var config = new AppConfig();
        configuration.Bind(config);
        return config;
    }
}

public class MqttConfig
{
    public string ServerAddress { get; set; } = string.Empty;
    public int Port { get; set; } = 1883;
    public string ClientId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ReceiveTopic { get; set; } = string.Empty;
    public string SendTopic { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 10;
    public int RetryAttempts { get; set; } = 5;
    public int RetryDelaySeconds { get; set; } = 2;
    public double RetryBackoffMultiplier { get; set; } = 2.0;
    public bool AutoReconnect { get; set; } = true;
    public int ReconnectDelaySeconds { get; set; } = 5;
    public bool UseTls { get; set; } = false;
    public bool AllowUntrustedCertificates { get; set; } = false;
    public string? CertificatePath { get; set; }
    public string? CertificatePassword { get; set; }
}

public class ApplicationConfig
{
    public int UpdateIntervalMs { get; set; } = 10;
    public string LogLevel { get; set; } = "Information";
}

public class AlgorithmConfig
{
    public int MaxIterations { get; set; } = 10;
    public float LearningRate { get; set; } = 0.1f;
    public bool RefinementEnabled { get; set; } = true;
}

public class BeaconConfig
{
    public string Id { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Altitude { get; set; }
}

