using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;
using InstDotNet;

public class UWBManager
{
    private static UWB2GPSConverter.Network network;
    private static UWB2GPSConverter.Network sendNetwork;
    private static List<UWB2GPSConverter.UWB> sendUwbsList;
    private static volatile bool updateNetworkTrigger = false;
    private static bool isUpdating = false;
    private static JsonSerializerOptions jsonOptions;

    public static void Initialise()
    {
        updateNetworkTrigger = false;
        isUpdating = false;
        jsonOptions = new JsonSerializerOptions
        {
            IncludeFields = true,
            PropertyNameCaseInsensitive = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
        };
        MQTTControl.OnMessageReceived -= UpdateUwbsFromMessage;
        MQTTControl.OnMessageReceived += UpdateUwbsFromMessage;
    }

    public static void UpdateUwbsFromMessage(string message)
    {
        try
        {
            network = JsonSerializer.Deserialize<UWB2GPSConverter.Network>(message, jsonOptions);
            Console.WriteLine($"Successfully parsed mqtt message into uwb network. Found {network.uwbs.Length} uwbs.");
            updateNetworkTrigger = true;
        }
        catch (System.Exception e)
        {
            Console.Error.WriteLine($"Failed to parse mqtt message into uwb network: {e.Message}");
            return;
        }

        // don't run UpdateUwbs on the MQTT thread — defer to main thread

    }

    public static void Update()
    {
        if (updateNetworkTrigger)
        {
            updateNetworkTrigger = false;
            UpdateUwbs();
        }
    }
    private static void UpdateUwbs()
    {
        if (isUpdating)
        {
            Console.WriteLine("UpdateUwbs: already updating — skipping re-entrant call.");
            return;
        }
        isUpdating = true;

        UWB2GPSConverter.ConvertUWBToPositions(network, true);

        if (sendUwbsList == null)
        {
            sendUwbsList = new List<UWB2GPSConverter.UWB>();
        }
        else
        {
            sendUwbsList.Clear();
            sendUwbsList.TrimExcess();
        }
        foreach (UWB2GPSConverter.UWB uwb in network.uwbs)
        {
            if (uwb.latLonAlt != null && uwb.latLonAlt.Length == 3 && uwb.latLonAlt[0] != 0 && uwb.latLonAlt[1] != 0 && uwb.positionAccuracy != -1)
            {
                sendUwbsList.Add(uwb);
            }
        }
        sendNetwork = new UWB2GPSConverter.Network(sendUwbsList.ToArray());

        // Update health check
        HealthCheck.UpdateLastProcessTime();
        HealthCheck.UpdateBeaconCount(sendNetwork.uwbs.Length);
        HealthCheck.IncrementNodesProcessed(network.uwbs.Length);

        SendNetwork(sendNetwork);
        isUpdating = false;
    }


    private static void SendNetwork(UWB2GPSConverter.Network sendNetwork)
    {
        Console.WriteLine($"Sending network with {sendNetwork.uwbs.Length}/{network.uwbs.Length} uwbs.");
        string data = JsonSerializer.Serialize(sendNetwork, jsonOptions);
        _ = MQTTControl.Publish(data); // Fire and forget - don't await to avoid blocking
    }
}

