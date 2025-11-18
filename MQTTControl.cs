using System;
using System.Buffers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Packets;   // for v5 subscribe options if needed
using MQTTnet.Protocol;  // QoS enums

public class MQTTControl
{
    public const string DEFAULT_CLIENT_ID = "clientId-UwbManager-001";
    public const string DEFAULT_SERVER_ADDRESS = "mqtt.dynamicdevices.co.uk";
    public const string DEFAULT_USERNAME = "";
    public const string DEFAULT_PASSWORD = "";
    public const int DEFAULT_SERVER_PORT = 1883;
    public const string DEFAULT_RECEIVE_MESSAGE_TOPIC = "DotnetMQTT/Test/in";
    public const string DEFAULT_SEND_MESSAGE_TOPIC = "DotnetMQTT/Test/out";
    public const int DEFAULT_TIMEOUT_IN_SECONDS = 10; //not currently being used - need to for safety?
    private static string _clientId;
    private static string _serverAddress;
    private static string _usernname;
    private static string _password;
    private static int _port;
    private static string _receiveMessageTopic;
    private static string _sendMessageTopic;
    private static int _timeoutInSeconds;


    public static System.Action<string> OnMessageReceived;

    private static IMqttClient client;
    private static CancellationTokenSource _cts;

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
        _usernname = username;
        _password = password;
        _receiveMessageTopic = receiveMessageTopic;
        _sendMessageTopic = sendMessageTopic;
        _timeoutInSeconds = timeoutSeconds;

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
            .WithCleanSession();

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

}
        