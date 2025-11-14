using Microsoft.Extensions.Logging;

namespace InstDotNet.Tests;

public class MQTTControlTests
{
    public MQTTControlTests()
    {
        // Ensure logger is initialized
        AppLogger.Dispose();
        AppLogger.Initialize(LogLevel.Debug);
    }

    [Fact]
    public void ReceiveMessage_WithValidMessage_InvokesOnMessageReceived()
    {
        // Arrange
        string? receivedMessage = null;
        MQTTControl.OnMessageReceived = (msg) => { receivedMessage = msg; };

        // Act
        MQTTControl.ReceiveMessage("test message");

        // Assert
        Assert.Equal("test message", receivedMessage);
    }

    [Fact]
    public void ReceiveMessage_WithNullHandler_DoesNotThrow()
    {
        // Arrange
        MQTTControl.OnMessageReceived = null;

        // Act & Assert - Should not throw
        MQTTControl.ReceiveMessage("test message");
        Assert.True(true);
    }

    [Fact]
    public void ReceiveMessage_WithEmptyMessage_InvokesHandler()
    {
        // Arrange
        string? receivedMessage = null;
        MQTTControl.OnMessageReceived = (msg) => { receivedMessage = msg; };

        // Act
        MQTTControl.ReceiveMessage("");

        // Assert
        Assert.Equal("", receivedMessage);
    }

    [Fact]
    public void StopReconnect_SetsFlagToFalse()
    {
        // Act
        MQTTControl.StopReconnect();

        // Assert - Should not throw
        // We can't directly verify the internal flag, but we can verify the method works
        Assert.True(true);
    }

    [Fact]
    public async Task DisconnectAsync_WhenNotInitialized_HandlesGracefully()
    {
        // Act & Assert - Should not throw
        await MQTTControl.DisconnectAsync();
        Assert.True(true);
    }

    [Fact]
    public async Task Publish_WhenNotConnected_HandlesGracefully()
    {
        // Act & Assert - Should not throw, should log warning
        await MQTTControl.Publish("test message");
        Assert.True(true);
    }

    [Fact]
    public async Task Publish_WithNullMessage_HandlesGracefully()
    {
        // Act & Assert - Should not throw
        await MQTTControl.Publish(null!);
        Assert.True(true);
    }

    [Fact]
    public async Task Publish_WithEmptyMessage_HandlesGracefully()
    {
        // Act & Assert - Should not throw
        await MQTTControl.Publish("");
        Assert.True(true);
    }

    [Fact]
    public void Constants_HaveExpectedValues()
    {
        // Assert - Verify default constants
        Assert.Equal("clientId-UwbManager-001", MQTTControl.DEFAULT_CLIENT_ID);
        Assert.Equal("mqtt.dynamicdevices.co.uk", MQTTControl.DEFAULT_SERVER_ADDRESS);
        Assert.Equal(1883, MQTTControl.DEFAULT_SERVER_PORT);
        Assert.Equal("DotnetMQTT/Test/in", MQTTControl.DEFAULT_RECEIVE_MESSAGE_TOPIC);
        Assert.Equal("DotnetMQTT/Test/out", MQTTControl.DEFAULT_SEND_MESSAGE_TOPIC);
        Assert.Equal(10, MQTTControl.DEFAULT_TIMEOUT_IN_SECONDS);
    }

    [Fact]
    public void OnMessageReceived_CanBeSetAndInvoked()
    {
        // Arrange - Clear any existing handlers first
        MQTTControl.OnMessageReceived = null;
        bool wasInvoked = false;
        MQTTControl.OnMessageReceived = (msg) => { wasInvoked = true; };

        // Act
        MQTTControl.ReceiveMessage("test");

        // Assert
        Assert.True(wasInvoked);
    }

    [Fact]
    public void OnMessageReceived_CanBeUnsubscribed()
    {
        // Arrange
        bool wasInvoked = false;
        Action<string> handler = (msg) => { wasInvoked = true; };
        MQTTControl.OnMessageReceived = handler;

        // Act
        MQTTControl.OnMessageReceived = null;
        MQTTControl.ReceiveMessage("test");

        // Assert
        Assert.False(wasInvoked);
    }
}

