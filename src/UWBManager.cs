#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using InstDotNet;

public class UWBManager
{
    private static UWB2GPSConverter.Network? network;
    private static UWB2GPSConverter.Network? sendNetwork;
    private static List<UWB2GPSConverter.UWB>? sendUwbsList;
    private static volatile bool updateNetworkTrigger = false;
    private static bool isUpdating = false;
    private static JsonSerializerOptions? jsonOptions;
    private static ILogger? _logger;
    private static AppConfig? _config;

    public static void Initialise(AppConfig? config = null)
    {
        _config = config;
        _logger = AppLogger.GetLogger<UWBManager>();
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
            
            // Initialize beacons from configuration (optional - beacons can also come from MQTT data)
            if (_config?.Beacons != null && _config.Beacons.Count > 0)
            {
                UWB2GPSConverter.InitializeBeacons(_config.Beacons);
                _logger?.LogInformation("Initialized {Count} beacons from configuration", _config.Beacons.Count);
            }
            else
            {
                _logger?.LogInformation("No beacons configured - expecting beacons to be provided via MQTT data with positionKnown = true and latLonAlt coordinates");
            }
        
        // network will be set when message received
    }

    public static void UpdateUwbsFromMessage(string message)
    {
        try
        {
            if (message == null)
            {
                _logger?.LogError("Message is null, cannot deserialize");
                return;
            }
            if (jsonOptions == null)
            {
                _logger?.LogError("jsonOptions is null, cannot deserialize message");
                return;
            }
            network = JsonSerializer.Deserialize<UWB2GPSConverter.Network>(message, jsonOptions);
            if (network != null && network.uwbs != null)
            {
                _logger?.LogInformation("Successfully parsed MQTT message into UWB network. Found {Count} UWBs.", network.uwbs.Length);
                
                // Log detailed node information at DEBUG level
                foreach (var node in network.uwbs)
                {
                    if (node != null)
                    {
                        string latLonAltStr = node.latLonAlt != null && node.latLonAlt.Length >= 3
                            ? $"{node.latLonAlt[0]:F6}, {node.latLonAlt[1]:F6}, {node.latLonAlt[2]:F2}"
                            : "null";
                        
                        string edgeInfo = node.edges != null && node.edges.Count > 0
                            ? string.Join(", ", node.edges.Select(e => {
                                // Determine which end is the other node (not the current node)
                                string otherEnd = (e.end0 == node.id) ? e.end1 : e.end0;
                                return $"{otherEnd}:{e.distance:F2}m";
                            }))
                            : "no edges";
                        
                        _logger?.LogDebug("  Node {Id}: positionKnown={Known}, latLonAlt=[{LatLonAlt}], edges=[{Edges}]",
                            node.id, node.positionKnown, latLonAltStr, edgeInfo);
                    }
                }
            }
            updateNetworkTrigger = true;
        }
        catch (System.Exception e)
        {
            _logger?.LogError(e, "Failed to parse MQTT message into UWB network");
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
            _logger?.LogDebug("UpdateUwbs: already updating — skipping re-entrant call.");
            return;
        }

        isUpdating = true;

        if (network == null || network.uwbs == null || network.uwbs.Length == 0)
        {
            _logger?.LogWarning("UpdateUwbs: network is null or empty, skipping update.");
            isUpdating = false;
            return;
        }

        bool refine = _config?.Algorithm.RefinementEnabled ?? true;
        UWB2GPSConverter.ConvertUWBToPositions(network, refine, _config?.Algorithm);

        // Update health check metrics
        HealthCheck.UpdateLastProcessTime();
        var beaconCount = network.uwbs.Count(u => u.positionKnown);
        HealthCheck.UpdateBeaconCount(beaconCount);
        HealthCheck.IncrementNodesProcessed(network.uwbs.Length);

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
            if (uwb.latLonAlt != null && uwb.latLonAlt.Length == 3 && uwb.positionAccuracy != -1)
            {
                sendUwbsList.Add(uwb);
            }
        }
        sendNetwork = new UWB2GPSConverter.Network(sendUwbsList.ToArray());

        SendNetwork(sendNetwork);
        isUpdating = false;
    }


    private static void SendNetwork(UWB2GPSConverter.Network sendNetwork)
    {
        _logger?.LogInformation("Sending network with {Count} UWBs.", sendNetwork.uwbs.Length);
        if (jsonOptions == null)
        {
            _logger?.LogError("jsonOptions is null, cannot serialize network");
            return;
        }
        string data = JsonSerializer.Serialize(sendNetwork, jsonOptions);
        
        // Log the JSON payload at DEBUG level
        _logger?.LogDebug("Publishing JSON payload to MQTT: {JsonPayload}", data);
        
        _ = Task.Run(async () => await MQTTControl.Publish(data));
    }
}

