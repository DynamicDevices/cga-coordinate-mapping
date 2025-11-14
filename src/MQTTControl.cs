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
using InstDotNet;
using System.Security.Cryptography.X509Certificates;

public class MQTTControl
{
    private static ILogger? _logger;
    public const string DEFAULT_CLIENT_ID = "clientId-UwbManager-001"; // Fallback if hardware ID unavailable
    public const string DEFAULT_SERVER_ADDRESS = "mqtt.dynamicdevices.co.uk";
    public const string DEFAULT_USERNAME = "";
    public const string DEFAULT_PASSWORD = "";
    public const int DEFAULT_SERVER_PORT = 1883;
    public const string DEFAULT_RECEIVE_MESSAGE_TOPIC = "DotnetMQTT/Test/in";
    public const string DEFAULT_SEND_MESSAGE_TOPIC = "DotnetMQTT/Test/out";
    public const int DEFAULT_TIMEOUT_IN_SECONDS = 10;
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
    private static AppConfig? _config;
    private static volatile bool _shouldReconnect = false;

    // Return Task so callers can await completion and observe exceptions
    public static async Task Initialise(CancellationTokenSource cts, AppConfig? config = null,
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
        _config = config;
        
            // Use config if provided, otherwise use parameters
            if (config?.MQTT != null)
            {
                // Use hardware-based client ID if config doesn't specify one, or if it's the default
                if (string.IsNullOrWhiteSpace(config.MQTT.ClientId) || 
                    config.MQTT.ClientId == DEFAULT_CLIENT_ID)
                {
                    _clientId = HardwareId.GetMqttClientId("UwbManager");
                    _logger?.LogInformation("Using hardware-based MQTT client ID: {ClientId}", _clientId);
                }
                else
                {
                    _clientId = config.MQTT.ClientId;
                }
                _serverAddress = config.MQTT.ServerAddress;
            _port = config.MQTT.Port;
            _username = config.MQTT.Username;
            _password = config.MQTT.Password;
            _receiveMessageTopic = config.MQTT.ReceiveTopic;
            _sendMessageTopic = config.MQTT.SendTopic;
            _timeoutInSeconds = config.MQTT.TimeoutSeconds;
        }
        else
        {
            // Use hardware-based client ID if default is provided, otherwise use parameter
            if (clientId == DEFAULT_CLIENT_ID || string.IsNullOrWhiteSpace(clientId))
            {
                _clientId = HardwareId.GetMqttClientId("UwbManager");
            }
            else
            {
                _clientId = clientId;
            }
            _serverAddress = serverAddress;
            _port = port;
            _username = username;
            _password = password;
            _receiveMessageTopic = receiveMessageTopic;
            _sendMessageTopic = sendMessageTopic;
            _timeoutInSeconds = timeoutSeconds;
        }

        var factory = new MqttClientFactory();
        client = factory.CreateMqttClient();

        _logger = AppLogger.GetLogger<MQTTControl>();
        
        // Log the client ID being used
        _logger.LogInformation("MQTT Client ID: {ClientId}", _clientId);

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
            
            // Auto-reconnect if enabled
            if (_config?.MQTT.AutoReconnect == true && _shouldReconnect && !_cts?.Token.IsCancellationRequested == true)
            {
                _logger?.LogInformation("MQTT: Auto-reconnect enabled, will attempt to reconnect...");
                _ = Task.Run(async () => await AttemptReconnectAsync(_cts?.Token ?? CancellationToken.None));
            }
            
            return Task.CompletedTask;
        };

        // Get keepalive period from config (default: 60 seconds)
        // KeepAlive prevents broker from disconnecting idle clients
        int keepAlivePeriod = _config?.MQTT.KeepAlivePeriodSeconds ?? 60;
        
        var builder = new MqttClientOptionsBuilder()
            .WithClientId(_clientId)
            .WithTcpServer(_serverAddress, _port)
            .WithCleanSession()
            .WithTimeout(TimeSpan.FromSeconds(_timeoutInSeconds))
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(keepAlivePeriod));

        // Add TLS/SSL if configured
        if (_config?.MQTT.UseTls == true)
        {
            // Configure TLS options for MQTTnet 5.0
            var tlsOptions = new MqttClientTlsOptions
            {
                UseTls = true,
                IgnoreCertificateChainErrors = _config.MQTT.AllowUntrustedCertificates,
                IgnoreCertificateRevocationErrors = _config.MQTT.AllowUntrustedCertificates,
                AllowUntrustedCertificates = _config.MQTT.AllowUntrustedCertificates
            };

            // Load client certificate if provided
            if (!string.IsNullOrWhiteSpace(_config.MQTT.CertificatePath))
            {
                try
                {
                    var certBytes = System.IO.File.ReadAllBytes(_config.MQTT.CertificatePath);
                    var certificate = new X509Certificate2(
                        certBytes,
                        _config.MQTT.CertificatePassword);
                    // Note: Client certificate support may require additional configuration
                    // depending on MQTTnet version. This is a placeholder for future enhancement.
                    _logger?.LogInformation("MQTT: Loaded client certificate from {Path} (certificate authentication not yet fully implemented)", _config.MQTT.CertificatePath);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "MQTT: Failed to load client certificate from {Path}", _config.MQTT.CertificatePath);
                    throw;
                }
            }

            builder = builder.WithTlsOptions(tlsOptions);
            _logger?.LogInformation("MQTT: TLS/SSL enabled (AllowUntrustedCertificates: {AllowUntrusted})", 
                _config.MQTT.AllowUntrustedCertificates);
        }

        // Add credentials only if provided
        if (!string.IsNullOrWhiteSpace(_username) && !string.IsNullOrWhiteSpace(_password))
        {
            builder = builder.WithCredentials(_username, _password);
        }

        var options = builder.Build();

        // Attempt connection with retry logic
        _shouldReconnect = true;
        await ConnectWithRetryAsync(options, _cts.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Check if MQTT client is connected
    /// </summary>
    public static bool IsConnected()
    {
        return client != null && client.IsConnected;
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

    /// <summary>
    /// Manually triggers the message received handler.
    /// Typically used for testing or manual message injection.
    /// </summary>
    /// <param name="message">The message to process</param>
    public static void ReceiveMessage(string message)
    {
        _logger?.LogDebug("Received message");
        OnMessageReceived?.Invoke(message);
    }

    private static async Task ConnectWithRetryAsync(MqttClientOptions options, CancellationToken cancellationToken)
    {
        int maxRetries = _config?.MQTT.RetryAttempts ?? 5;
        int baseDelay = _config?.MQTT.RetryDelaySeconds ?? 2;
        double backoffMultiplier = _config?.MQTT.RetryBackoffMultiplier ?? 2.0;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger?.LogInformation("MQTT: Connection cancelled");
                    return;
                }

                if (attempt > 0)
                {
                    int delay = (int)(baseDelay * Math.Pow(backoffMultiplier, attempt - 1));
                    _logger?.LogInformation("MQTT: Retry attempt {Attempt}/{MaxRetries} after {Delay}s...", 
                        attempt + 1, maxRetries, delay);
                    await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _logger?.LogInformation("MQTT: Connecting to {Server}:{Port}...", _serverAddress, _port);
                }

                await client!.ConnectAsync(options, cancellationToken).ConfigureAwait(false);

                // Subscribe (MQTT v5 supports more options; QoS shown here)
                await client.SubscribeAsync(new MqttTopicFilterBuilder()
                    .WithTopic(_receiveMessageTopic)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build(), cancellationToken).ConfigureAwait(false);

                await Publish($"Connected to {_serverAddress}. Subscribed to {_receiveMessageTopic}. Publishing to {_sendMessageTopic}.");

                _logger?.LogInformation("Connected to {Server}", _serverAddress);
                _logger?.LogInformation("MQTT: Subscribed to {Topic}", _receiveMessageTopic);
                return; // Success
            }
            catch (OperationCanceledException)
            {
                _logger?.LogInformation("MQTT: Connection cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "MQTT: Connection attempt {Attempt}/{MaxRetries} failed: {Error}", 
                    attempt + 1, maxRetries, ex.Message);
                
                if (attempt == maxRetries - 1)
                {
                    _logger?.LogError("MQTT: All connection attempts failed. Giving up.");
                    throw;
                }
            }
        }
    }

    private static async Task AttemptReconnectAsync(CancellationToken cancellationToken)
    {
        if (client == null || _config?.MQTT == null)
        {
            return;
        }

        int reconnectDelay = _config.MQTT.ReconnectDelaySeconds;
        _logger?.LogInformation("MQTT: Waiting {Delay}s before reconnection attempt...", reconnectDelay);
        await Task.Delay(TimeSpan.FromSeconds(reconnectDelay), cancellationToken).ConfigureAwait(false);

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        // Get keepalive period from config (default: 60 seconds)
        int keepAlivePeriod = _config?.MQTT.KeepAlivePeriodSeconds ?? 60;
        
        var builder = new MqttClientOptionsBuilder()
            .WithClientId(_clientId)
            .WithTcpServer(_serverAddress, _port)
            .WithCleanSession()
            .WithTimeout(TimeSpan.FromSeconds(_timeoutInSeconds))
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(keepAlivePeriod));

        // Add TLS/SSL if configured (same as initial connection)
        if (_config?.MQTT.UseTls == true)
        {
            // Configure TLS options for MQTTnet 5.0
            var tlsOptions = new MqttClientTlsOptions
            {
                UseTls = true,
                IgnoreCertificateChainErrors = _config.MQTT.AllowUntrustedCertificates,
                IgnoreCertificateRevocationErrors = _config.MQTT.AllowUntrustedCertificates,
                AllowUntrustedCertificates = _config.MQTT.AllowUntrustedCertificates
            };

            // Load client certificate if provided (for future enhancement)
            if (!string.IsNullOrWhiteSpace(_config.MQTT.CertificatePath))
            {
                try
                {
                    var certBytes = System.IO.File.ReadAllBytes(_config.MQTT.CertificatePath);
                    var certificate = new X509Certificate2(
                        certBytes,
                        _config.MQTT.CertificatePassword);
                    // Note: Client certificate support may require additional configuration
                    _logger?.LogDebug("MQTT: Client certificate loaded during reconnect (certificate authentication not yet fully implemented)");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "MQTT: Failed to load client certificate during reconnect");
                    throw;
                }
            }

            builder = builder.WithTlsOptions(tlsOptions);
        }

        if (!string.IsNullOrWhiteSpace(_username) && !string.IsNullOrWhiteSpace(_password))
        {
            builder = builder.WithCredentials(_username, _password);
        }

        var options = builder.Build();

        try
        {
            await ConnectWithRetryAsync(options, cancellationToken).ConfigureAwait(false);
            _logger?.LogInformation("MQTT: Reconnection successful");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "MQTT: Reconnection failed");
            // Will retry again on next disconnect event if auto-reconnect is still enabled
        }
    }

    /// <summary>
    /// Stops automatic reconnection attempts.
    /// Call this before graceful shutdown to prevent reconnection during shutdown.
    /// </summary>
    public static void StopReconnect()
    {
        _shouldReconnect = false;
    }

}
        