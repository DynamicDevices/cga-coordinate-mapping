// Program.cs  (net8.0, MQTTnet 5.x)
using System.Text.Json;
using Microsoft.Extensions.Logging;

class Program
{
    static async Task Main()
    {
        // Initialize logging from environment variable or default to Information
        var logLevel = AppLogger.ParseLogLevel(Environment.GetEnvironmentVariable("LOG_LEVEL"));
        AppLogger.Initialize(logLevel);

        var logger = AppLogger.GetLogger<Program>();

        // Display version information
        logger.LogInformation("CGA Coordinate Mapping - {Version}", VersionInfo.FullVersion);
        logger.LogInformation("Log level: {LogLevel}", logLevel);
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
            await MQTTControl.Initialise(cts);
            UWBManager.Initialise();

        //Try loading from Python-generated file if it exists
        // string filePath = "TestNodes.json";
        // if (System.IO.File.Exists(filePath))
        // {
        //     //Console.WriteLine("Loading UWB network from Python-generated file...");       //     
        //     Console.WriteLine("Loading UWB network from my TestNodes...");       //     

        //     try
        //     {
        //         string json = System.IO.File.ReadAllText(filePath);
        //         UWBManager.UpdateUwbsFromMessage(json);
        //         Console.WriteLine($"Loaded UWB network from {filePath}");
        //     }
        //     catch (System.Exception e)
        //     {
        //         Console.Error.WriteLine($"Failed to load UWB network from file: {e.Message}");
        //     }
        // }
        

        // Run one immediate update, then start a background loop to update repeatedly
        UWBManager.Update();

        // Interval between updates in milliseconds (few ms as requested)
        const int updateIntervalMs = 10;

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

    
