// Program.cs  (net8.0, MQTTnet 5.x)
using System.Text.Json;
using InstDotNet;

class Program
{
    static AppConfig? _config;

    static async Task Main()
    {
        // Display version information on startup
        Console.WriteLine($"CGA Coordinate Mapping - Version {VersionInfo.FullVersion}");
        Console.WriteLine();

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

        // Get board ID and resolve placeholders
        var boardId = HardwareId.GetMqttClientId("UwbManager");
        _config.ResolvePlaceholders(boardId);
        
        Console.WriteLine($"Board ID: {boardId}");
        Console.WriteLine($"MQTT Receive Topic: {_config.MQTT.ReceiveTopic}");
        Console.WriteLine($"MQTT Send Topic: {_config.MQTT.SendTopic}");
        Console.WriteLine();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
        await MQTTControl.Initialise(cts, _config);
        UWBManager.Initialise();

        //Try loading from test file if it exists
        //string filePath = "network_latest_output_from_uwbs_from_alex.json";
        // string filePath = "network_made_from_python_parser.json";
        // if (System.IO.File.Exists(filePath))
        // {
        //     Console.WriteLine($"Loading UWB network from {filePath}...");       //     

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

        // Interval between updates from configuration
        int updateIntervalMs = _config?.Application.UpdateIntervalMs ?? 10;

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
                Console.WriteLine($"Error in update loop: {ex.Message}");
            }
        }, cts.Token);

        Console.WriteLine("Press Ctrl+C to exit…");
        try { await Task.Delay(Timeout.Infinite, cts.Token); } catch { }

        await MQTTControl.DisconnectAsync();
    }
}


