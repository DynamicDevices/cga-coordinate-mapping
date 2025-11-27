using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    private static string debugMessageOut = "";

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
        debugMessageOut = "";
    }

    public static void UpdateUwbsFromMessage(string message)
    {
        debugMessageOut = "";
        try
        {
            network = JsonSerializer.Deserialize<UWB2GPSConverter.Network>(message, jsonOptions);
            AddToDebugMessage($"Successfully parsed mqtt message into uwb network. Found {network.uwbs.Length} uwbs.");

            // don't run UpdateUwbs on the MQTT thread — defer to main thread
            updateNetworkTrigger = true;
        }
        catch (System.Exception e)
        {
            AddToDebugMessage($"Failed to parse mqtt message into uwb network. Error: {e}", true);
            return;
        }        
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
            //if (uwb.latLonAlt != null && uwb.latLonAlt.Length == 3 && uwb.latLonAlt[0] != 0 && uwb.latLonAlt[1] != 0 && uwb.positionAccuracy != -1)
            //{
                sendUwbsList.Add(uwb);
            //}
        }
        sendNetwork = new UWB2GPSConverter.Network(sendUwbsList.ToArray());

        // Update health check
        HealthCheck.UpdateLastProcessTime();
        HealthCheck.UpdateBeaconCount(sendNetwork.uwbs.Length);
        HealthCheck.IncrementNodesProcessed(network.uwbs.Length);

        SendNetwork(sendNetwork);
        isUpdating = false;
    }

    public static void AddToDebugMessage(string message, bool publishToMQTT = false, bool sendToConsole = true)
    {
        debugMessageOut += "\n" + message;
        if (sendToConsole)
        {
            Console.WriteLine(message);
        }
        if (publishToMQTT)
        {
            _ = MQTTControl.PublishDebugMessage(debugMessageOut);
        }
    }

    private static void SendNetwork(UWB2GPSConverter.Network sendNetwork)
    {
        AddToDebugMessage($"Sending network with {sendNetwork.uwbs.Length}/{network.uwbs.Length} uwbs.", true);

        string data = JsonSerializer.Serialize(sendNetwork, jsonOptions);
        _ = MQTTControl.Publish(data); // Fire and forget - don't await to avoid blocking
    }
}

