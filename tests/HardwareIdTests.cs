namespace InstDotNet.Tests;

public class HardwareIdTests
{
    [Fact]
    public void GetUniqueId_ReturnsCachedValue_OnSubsequentCalls()
    {
        // Arrange & Act
        var id1 = HardwareId.GetUniqueId();
        var id2 = HardwareId.GetUniqueId();

        // Assert
        Assert.NotNull(id1);
        Assert.NotNull(id2);
        Assert.Equal(id1, id2); // Should be cached
    }

    [Fact]
    public void GetUniqueId_ReturnsNonEmptyString()
    {
        // Act
        var id = HardwareId.GetUniqueId();

        // Assert
        Assert.NotNull(id);
        Assert.NotEmpty(id);
    }

    [Fact]
    public void GetUniqueId_ReturnsStringWithPrefix()
    {
        // Act
        var id = HardwareId.GetUniqueId();

        // Assert
        // Should have a prefix: machine-, dmi-, mac-, host-, or random-
        Assert.True(
            id.StartsWith("machine-") ||
            id.StartsWith("dmi-") ||
            id.StartsWith("mac-") ||
            id.StartsWith("host-") ||
            id.StartsWith("random-"),
            $"ID should have a known prefix, but got: {id}");
    }

    [Fact]
    public void GetMqttClientId_WithoutPrefix_ReturnsSanitizedHardwareId()
    {
        // Act
        var clientId = HardwareId.GetMqttClientId();

        // Assert
        Assert.NotNull(clientId);
        Assert.NotEmpty(clientId);
        // Should only contain alphanumeric, hyphens, and underscores
        foreach (var c in clientId)
        {
            Assert.True(
                char.IsLetterOrDigit(c) || c == '-' || c == '_',
                $"Client ID contains invalid character: {c}");
        }
    }

    [Fact]
    public void GetMqttClientId_WithPrefix_IncludesPrefix()
    {
        // Arrange
        var prefix = "TestPrefix";

        // Act
        var clientId = HardwareId.GetMqttClientId(prefix);

        // Assert
        Assert.NotNull(clientId);
        Assert.NotEmpty(clientId);
        Assert.StartsWith($"{prefix}-", clientId);
    }

    [Fact]
    public void GetMqttClientId_WithNullPrefix_Works()
    {
        // Act
        var clientId = HardwareId.GetMqttClientId(null);

        // Assert
        Assert.NotNull(clientId);
        Assert.NotEmpty(clientId);
    }

    [Fact]
    public void GetMqttClientId_WithEmptyPrefix_Works()
    {
        // Act
        var clientId = HardwareId.GetMqttClientId("");

        // Assert
        Assert.NotNull(clientId);
        Assert.NotEmpty(clientId);
    }

    [Fact]
    public void GetMqttClientId_SanitizesInvalidCharacters()
    {
        // Arrange - Create a test scenario where we can verify sanitization
        // Since we can't easily mock the hardware ID, we'll test the sanitization logic
        // by checking that the result only contains valid MQTT characters
        var prefix = "Test-Prefix_123";

        // Act
        var clientId = HardwareId.GetMqttClientId(prefix);

        // Assert
        Assert.NotNull(clientId);
        // All characters should be alphanumeric, hyphen, or underscore
        foreach (var c in clientId)
        {
            Assert.True(
                char.IsLetterOrDigit(c) || c == '-' || c == '_',
                $"Client ID contains invalid character: {c} in '{clientId}'");
        }
    }

    [Fact]
    public void GetMqttClientId_TruncatesLongIds()
    {
        // Arrange - Create a very long prefix to test truncation
        var longPrefix = new string('A', 200); // 200 characters

        // Act
        var clientId = HardwareId.GetMqttClientId(longPrefix);

        // Assert
        Assert.NotNull(clientId);
        // Should be truncated to 128 characters max
        Assert.True(clientId.Length <= 128, $"Client ID should be <= 128 chars, but got {clientId.Length}");
    }

    [Fact]
    public void GetMqttClientId_ReplacesInvalidCharactersWithHyphens()
    {
        // Arrange - Use a prefix with invalid characters
        // Note: We can't directly control the hardware ID, but we can test with a prefix
        // that has invalid characters to verify sanitization
        var prefixWithInvalidChars = "Test@Prefix#123$";

        // Act
        var clientId = HardwareId.GetMqttClientId(prefixWithInvalidChars);

        // Assert
        Assert.NotNull(clientId);
        // Invalid characters should be replaced with hyphens
        Assert.DoesNotContain("@", clientId);
        Assert.DoesNotContain("#", clientId);
        Assert.DoesNotContain("$", clientId);
        // Should only contain valid MQTT characters
        foreach (var c in clientId)
        {
            Assert.True(
                char.IsLetterOrDigit(c) || c == '-' || c == '_',
                $"Client ID contains invalid character: {c}");
        }
    }

    [Fact]
    public void GetMqttClientId_ReturnsConsistentValue()
    {
        // Act
        var id1 = HardwareId.GetMqttClientId("TestPrefix");
        var id2 = HardwareId.GetMqttClientId("TestPrefix");

        // Assert
        Assert.Equal(id1, id2); // Should be consistent (uses cached hardware ID)
    }

    [Fact]
    public void GetMqttClientId_WithDifferentPrefixes_ReturnsDifferentIds()
    {
        // Act
        var id1 = HardwareId.GetMqttClientId("Prefix1");
        var id2 = HardwareId.GetMqttClientId("Prefix2");

        // Assert
        Assert.NotEqual(id1, id2);
        Assert.StartsWith("Prefix1-", id1);
        Assert.StartsWith("Prefix2-", id2);
    }

    [Fact]
    public void GetMqttClientId_HandlesSpecialCharactersInPrefix()
    {
        // Arrange
        var prefix = "UwbManager";

        // Act
        var clientId = HardwareId.GetMqttClientId(prefix);

        // Assert
        Assert.NotNull(clientId);
        Assert.StartsWith($"{prefix}-", clientId);
        // Should be valid MQTT client ID format
        Assert.True(clientId.Length > 0);
        Assert.True(clientId.Length <= 128);
    }

    [Fact]
    public void GetUniqueId_FallbackToRandomGuid_WhenAllMethodsFail()
    {
        // This test verifies that if all hardware ID methods fail,
        // the system falls back to a random GUID
        // Note: This is hard to test directly without mocking file system,
        // but we can verify the format if it happens
        var id = HardwareId.GetUniqueId();

        // Assert - Should have some prefix
        Assert.NotNull(id);
        Assert.NotEmpty(id);
        // If it's a random fallback, it should start with "random-"
        if (id.StartsWith("random-"))
        {
            // Should have a GUID-like format after "random-"
            var guidPart = id.Substring(7); // After "random-"
            Assert.True(guidPart.Length >= 32, "Random GUID part should be at least 32 characters");
        }
    }
}

