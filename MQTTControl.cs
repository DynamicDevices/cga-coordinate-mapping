#nullable enable
using System;
using System.Buffers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Packets;   // for v5 subscribe options if needed
using MQTTnet.Protocol;  // QoS enums
using Microsoft.Extensions.Logging;

public class MQTTControl
{
    private static ILogger? _logger;
    public const string DEFAULT_CLIENT_ID = "clientId-UwbManager-001";
    public const string DEFAULT_SERVER_ADDRESS = "mqtt.dynamicdevices.co.uk";
    public const string DEFAULT_USERNAME = "";
    public const string DEFAULT_PASSWORD = "";
    public const int DEFAULT_SERVER_PORT = 1883;
    public const string DEFAULT_RECEIVE_MESSAGE_TOPIC = "DotnetMQTT/Test/in";
    public const string DEFAULT_SEND_MESSAGE_TOPIC = "DotnetMQTT/Test/out";
    public const int DEFAULT_TIMEOUT_IN_SECONDS = 10; //not currently being used - need to for safety?
    private static string _clientId = string.Empty;
    private static string _serverAddress = string.Empty;
    private static string _username = string.Empty;
    private static string _password = string.Empty;
    private static int _port;
    private static string _receiveMessageTopic = string.Empty;
    private static string _sendMessageTopic = string.Empty;
    private static int _timeoutInSeconds;


    public static System.Action<string>? OnMessageReceived;

    private static IMqttClient? client;
    private static CancellationTokenSource? _cts;

    // Return Task so callers can await completion and observe exceptions
    public static async Task Initialise(CancellationTokenSource cts,
        string clientId = DEFAULT_CLIENT_ID,
        string serverAddress = DEFAULT_SERVER_ADDRESS,
        int port = DEFAULT_SERVER_PORT,
        string username = DEFAULT_USERNAME,
        string password = DEFAULT_PASSWORD,
        string receiveMessageTopic = DEFAULT_RECEIVE_MESSAGE_TOPIC,
        string sendMessageTopic = DEFAULT_SEND_MESSAGE_TOPIC,
        int timeoutSeconds = DEFAULT_TIMEOUT_IN_SECONDS)
    {
        _cts = cts ?? new CancellationTokenSource();
        _clientId = clientId;
        _serverAddress = serverAddress;
        _port = port;
        _username = username;
        _password = password;
        _receiveMessageTopic = receiveMessageTopic;
        _sendMessageTopic = sendMessageTopic;
        _timeoutInSeconds = timeoutSeconds;

        var factory = new MqttClientFactory();
        client = factory.CreateMqttClient();

        _logger = AppLogger.GetLogger<MQTTControl>();

        // Setup handlers
        client.ApplicationMessageReceivedAsync += e =>
        {
            try
            {
                var sequence = e.ApplicationMessage.Payload;
                var bytes = sequence.IsEmpty ? Array.Empty<byte>() : sequence.ToArray();
                var payload = bytes.Length == 0 ? string.Empty : Encoding.UTF8.GetString(bytes);
                _logger?.LogDebug("MSG [{Topic}]: {Payload}", e.ApplicationMessage.Topic, payload);
                // forward to any subscriber in the app
                OnMessageReceived?.Invoke(payload);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing incoming message");
            }
            return Task.CompletedTask;
        };

        client.ConnectedAsync += e =>
        {
            _logger?.LogInformation("MQTT: Connected.");
            return Task.CompletedTask;
        };

        client.DisconnectedAsync += e =>
        {
            _logger?.LogWarning("MQTT: Disconnected. Reason: {Reason}", e?.Exception?.Message ?? "none");
            return Task.CompletedTask;
        };

        var builder = new MqttClientOptionsBuilder()
            .WithClientId(_clientId)
            .WithTcpServer(_serverAddress, _port)
            .WithCleanSession();

        // Add credentials only if provided
        if (!string.IsNullOrWhiteSpace(_username) && !string.IsNullOrWhiteSpace(_password))
        {
            builder = builder.WithCredentials(_username, _password);
        }

        var options = builder.Build();

        try
        {
            _logger?.LogInformation("MQTT: Connecting to {Server}:{Port}...", _serverAddress, _port);
            await client.ConnectAsync(options, _cts.Token).ConfigureAwait(false);

            // Subscribe (MQTT v5 supports more options; QoS shown here)
            await client.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic(_receiveMessageTopic)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build(), _cts.Token).ConfigureAwait(false);

            await Publish($"Connected to {_serverAddress}. Subscribed to {_receiveMessageTopic}. Publishing to {_sendMessageTopic}.");

            _logger?.LogInformation("Connected to {Server}", _serverAddress);
            _logger?.LogInformation("MQTT: Subscribed to {Topic}", _receiveMessageTopic);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "MQTT: Connect/Subscribe failed: {ExceptionType}", ex.GetType().Name);
            // rethrow so caller can observe if they awaited Initialise
            throw;
        }
    }

    public static async Task DisconnectAsync()
    {
        if (client == null)
        {
            return;
        }

        try
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync().ConfigureAwait(false);
                _logger?.LogInformation("MQTT: Disconnect complete.");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "MQTT: Error during disconnect");
        }
    }

    public static async Task Publish(string message)
    {
        if (client == null)
        {
            _logger?.LogWarning("MQTT: Publish skipped - client is null.");
            return;
        }

        if (!client.IsConnected)
        {
            _logger?.LogWarning("MQTT: Publish skipped - client not connected.");
            return;
        }

        var messageOut = new MqttApplicationMessageBuilder()
            .WithTopic(_sendMessageTopic)
            .WithPayload(message)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        try
        {
            if (client == null || _cts == null)
            {
                _logger?.LogWarning("MQTT: Cannot publish - client or cancellation token is null");
                return;
            }
            await client.PublishAsync(messageOut, _cts.Token).ConfigureAwait(false);
            _logger?.LogDebug("MQTT: Published to {Topic}", _sendMessageTopic);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "MQTT: Publish error");
        }
    }

    public static void ReceiveMessage(string message)
    {
        _logger?.LogDebug("Received message");
        OnMessageReceived?.Invoke(message);
    }

}
        