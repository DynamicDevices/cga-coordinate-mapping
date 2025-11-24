// Program.cs  (net8.0, MQTTnet 5.x)
using System.Text.Json;

class Program
{


    static async Task Main()
    {
        // Display version information on startup
        Console.WriteLine($"CGA Coordinate Mapping - Version {VersionInfo.FullVersion}");
        Console.WriteLine();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
        await MQTTControl.Initialise(cts);
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
                Console.WriteLine($"Error in update loop: {ex.Message}");
            }
        }, cts.Token);

        Console.WriteLine("Press Ctrl+C to exit…");
        try { await Task.Delay(Timeout.Infinite, cts.Token); } catch { }

        await MQTTControl.DisconnectAsync();
    }
}


