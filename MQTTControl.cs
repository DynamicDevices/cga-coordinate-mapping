using System;
using System.Buffers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Packets;   // for v5 subscribe options if needed
using MQTTnet.Protocol;  // QoS enums
using InstDotNet;

public class MQTTControl
{
    public const string DEFAULT_CLIENT_ID = "clientId-UwbManager-001";
    private static string _clientId;
    private static string _serverAddress;
    private static string _usernname;
    private static string _password;
    private static int _port;
    private static string _receiveMessageTopic;
    private static string _sendMessageTopic;
    private static int _timeoutInSeconds;
    private static int _keepAlivePeriodSeconds;

    public static System.Action<string> OnMessageReceived;

    private static IMqttClient client;
    private static CancellationTokenSource _cts;

    // Return Task so callers can await completion and observe exceptions
    public static async Task Initialise(CancellationTokenSource cts, AppConfig? config = null)
    {
        _cts = cts ?? new CancellationTokenSource();
        
        if (config != null)
        {
            _serverAddress = config.MQTT.ServerAddress;
            _port = config.MQTT.Port;
            _usernname = config.MQTT.Username;
            _password = config.MQTT.Password;
            _receiveMessageTopic = config.MQTT.ReceiveTopic;
            _sendMessageTopic = config.MQTT.SendTopic;
            _timeoutInSeconds = config.MQTT.TimeoutSeconds;
            _keepAlivePeriodSeconds = config.MQTT.KeepAlivePeriodSeconds;
            
            // Use configured client ID, or generate from hardware ID if empty
            if (string.IsNullOrWhiteSpace(config.MQTT.ClientId))
            {
                var baseClientId = HardwareId.GetMqttClientId("UwbManager");
                var processId = System.Diagnostics.Process.GetCurrentProcess().Id;
                _clientId = $"{baseClientId}-pid{processId}";
                Console.WriteLine($"Using hardware-based MQTT client ID: {_clientId} (PID: {processId})");
            }
            else
            {
                _clientId = config.MQTT.ClientId;
            }
        }
        else
        {
            // Fallback to defaults if no config provided
            _serverAddress = "mqtt.dynamicdevices.co.uk";
            _port = 1883;
            _usernname = "";
            _password = "";
            _receiveMessageTopic = "DotnetMQTT/Test/in";
            _sendMessageTopic = "DotnetMQTT/Test/out";
            _timeoutInSeconds = 10;
            _keepAlivePeriodSeconds = 60;
            var baseClientId = HardwareId.GetMqttClientId("UwbManager");
            var processId = System.Diagnostics.Process.GetCurrentProcess().Id;
            _clientId = $"{baseClientId}-pid{processId}";
        }

        var factory = new MqttClientFactory();
        client = factory.CreateMqttClient();

        // Setup handlers
        client.ApplicationMessageReceivedAsync += e =>
        {
            try
            {
                var sequence = e.ApplicationMessage.Payload;
                var bytes = sequence.IsEmpty ? Array.Empty<byte>() : sequence.ToArray();
                var payload = bytes.Length == 0 ? string.Empty : Encoding.UTF8.GetString(bytes);
                Console.WriteLine($"MSG [{e.ApplicationMessage.Topic}]: {payload}");
                // forward to any subscriber in the app
                OnMessageReceived?.Invoke(payload);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing incoming message: {ex.Message}");
            }
            return Task.CompletedTask;
        };

        client.ConnectedAsync += e =>
        {
            Console.WriteLine("MQTT: Connected.");
            return Task.CompletedTask;
        };

        client.DisconnectedAsync += e =>
        {
            Console.WriteLine($"MQTT: Disconnected. Reason: {e?.Exception?.Message ?? "none"}");
            return Task.CompletedTask;
        };

        var builder = new MqttClientOptionsBuilder()
            .WithClientId(_clientId)
            .WithTcpServer(_serverAddress, _port)
            .WithCleanSession()
            .WithTimeout(TimeSpan.FromSeconds(_timeoutInSeconds))
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(_keepAlivePeriodSeconds));

        // Add credentials only if provided
        if (!string.IsNullOrWhiteSpace(_usernname) && !string.IsNullOrWhiteSpace(_password))
        {
            builder = builder.WithCredentials(_usernname, _password);
        }

        var options = builder.Build();

        try
        {
            Console.WriteLine($"MQTT: Connecting to {_serverAddress}:{_port} ...");
            await client.ConnectAsync(options, _cts.Token).ConfigureAwait(false);

            // Subscribe (MQTT v5 supports more options; QoS shown here)
            await client.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic(_receiveMessageTopic)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build(), _cts.Token).ConfigureAwait(false);

            await Publish($"Connected to {_serverAddress}. Subscribed to {_receiveMessageTopic}. Publishing to {_sendMessageTopic}.");

            Console.WriteLine($"Connected to {_serverAddress}");
            Console.WriteLine($"MQTT: Subscribed to {_receiveMessageTopic}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MQTT: Connect/Subscribe failed: {ex.GetType().Name}: {ex.Message}");
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
                Console.WriteLine("MQTT: Disconnect complete.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MQTT: Error during disconnect: {ex.Message}");
        }
    }

    public static async Task Publish(string message)
    {
        if (client == null)
        {
            Console.WriteLine("MQTT: Publish skipped - client is null.");
            return;
        }

        if (!client.IsConnected)
        {
            Console.WriteLine("MQTT: Publish skipped - client not connected.");
            return;
        }

        var messageOut = new MqttApplicationMessageBuilder()
            .WithTopic(_sendMessageTopic)
            .WithPayload(message)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        try
        {
            await client.PublishAsync(messageOut, _cts.Token).ConfigureAwait(false);
            Console.WriteLine("MQTT: Published.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MQTT: Publish error: {ex.Message}");
        }
    }

    public static void ReceiveMessage(string message)
    {
        Console.WriteLine($"Received message");
        OnMessageReceived?.Invoke(message);
    }

    /// <summary>
    /// Check if MQTT client is connected
    /// </summary>
    public static bool IsConnected()
    {
        return client != null && client.IsConnected;
    }

}
        