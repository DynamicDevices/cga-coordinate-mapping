#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using InstDotNet;

class Program
{
    static AppConfig? _config;

    static async Task Main()
    {
        // Load configuration
        try
        {
            _config = AppConfig.Load();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load configuration: {ex.Message}");
            Console.Error.WriteLine("Using default configuration values");
            _config = new AppConfig();
        }

        // Initialize logging from configuration or environment variable
        var logLevelString = _config.Application.LogLevel ?? Environment.GetEnvironmentVariable("LOG_LEVEL");
        var logLevel = AppLogger.ParseLogLevel(logLevelString);
        AppLogger.Initialize(logLevel);

        var logger = AppLogger.GetLogger<Program>();

        // Display version information
        logger.LogInformation("CGA Coordinate Mapping - {Version}", VersionInfo.FullVersion);
        logger.LogInformation("Log level: {LogLevel} (from config: '{ConfigValue}')", logLevel, logLevelString ?? "null");
        logger.LogInformation("Configuration loaded: {BeaconCount} beacons configured", _config.Beacons.Count);
        logger.LogInformation("");

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            logger.LogInformation("Shutdown requested by user");
        };

        try
        {
            // Initialize health check
            HealthCheck.Initialize(logger);
            
            // Start health check server
            int healthCheckPort = _config.Application.HealthCheckPort;
            try
            {
                HealthCheckServer.Start(healthCheckPort, logger);
                logger.LogInformation("Health check endpoint available at http://localhost:{Port}/health", healthCheckPort);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to start health check server on port {Port}, continuing without health endpoint", healthCheckPort);
            }

            await MQTTControl.Initialise(cts, _config);
            UWBManager.Initialise(_config);

            // Run one immediate update, then start a background loop to update repeatedly
            UWBManager.Update();

            // Interval between updates from configuration
            int updateIntervalMs = _config.Application.UpdateIntervalMs;

            // Start background loop that will run until Ctrl+C cancels the token
            _ = Task.Run(async () =>
            {
                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        UWBManager.Update();
                        await Task.Delay(updateIntervalMs, cts.Token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    // expected on shutdown
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in update loop");
                }
            }, cts.Token);

            logger.LogInformation("Press Ctrl+C to exit…");
            try { await Task.Delay(Timeout.Infinite, cts.Token); } catch { }

            logger.LogInformation("Shutting down...");
            HealthCheckServer.Stop();
            MQTTControl.StopReconnect();
            await MQTTControl.DisconnectAsync();
            AppLogger.Dispose();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Fatal error in application");
            AppLogger.Dispose();
            throw;
        }
    }
}
