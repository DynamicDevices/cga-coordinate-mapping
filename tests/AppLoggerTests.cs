using Microsoft.Extensions.Logging;
using Xunit;

namespace InstDotNet.Tests;

public class AppLoggerTests
{
    [Fact]
    public void ParseLogLevel_ValidLevels_ReturnsCorrectLevel()
    {
        Assert.Equal(LogLevel.Trace, AppLogger.ParseLogLevel("TRACE"));
        Assert.Equal(LogLevel.Debug, AppLogger.ParseLogLevel("DEBUG"));
        Assert.Equal(LogLevel.Information, AppLogger.ParseLogLevel("INFO"));
        Assert.Equal(LogLevel.Information, AppLogger.ParseLogLevel("INFORMATION"));
        Assert.Equal(LogLevel.Warning, AppLogger.ParseLogLevel("WARN"));
        Assert.Equal(LogLevel.Warning, AppLogger.ParseLogLevel("WARNING"));
        Assert.Equal(LogLevel.Error, AppLogger.ParseLogLevel("ERROR"));
        Assert.Equal(LogLevel.Critical, AppLogger.ParseLogLevel("CRITICAL"));
        Assert.Equal(LogLevel.Critical, AppLogger.ParseLogLevel("FATAL"));
        Assert.Equal(LogLevel.None, AppLogger.ParseLogLevel("NONE"));
    }

    [Fact]
    public void ParseLogLevel_InvalidInput_ReturnsInformation()
    {
        Assert.Equal(LogLevel.Information, AppLogger.ParseLogLevel("INVALID"));
        Assert.Equal(LogLevel.Information, AppLogger.ParseLogLevel("unknown"));
        Assert.Equal(LogLevel.Information, AppLogger.ParseLogLevel(""));
        Assert.Equal(LogLevel.Information, AppLogger.ParseLogLevel(null));
    }

    [Fact]
    public void ParseLogLevel_CaseInsensitive_Works()
    {
        Assert.Equal(LogLevel.Debug, AppLogger.ParseLogLevel("debug"));
        Assert.Equal(LogLevel.Debug, AppLogger.ParseLogLevel("Debug"));
        Assert.Equal(LogLevel.Debug, AppLogger.ParseLogLevel("DEBUG"));
        Assert.Equal(LogLevel.Error, AppLogger.ParseLogLevel("error"));
        Assert.Equal(LogLevel.Error, AppLogger.ParseLogLevel("Error"));
    }

    [Fact]
    public void Initialize_CreatesLoggerFactory()
    {
        // Arrange
        AppLogger.Dispose(); // Clean up any existing instance

        // Act
        AppLogger.Initialize(LogLevel.Debug);

        // Assert
        var logger = AppLogger.GetLogger<AppLoggerTests>();
        Assert.NotNull(logger);
        Assert.True(logger.IsEnabled(LogLevel.Debug));
        Assert.False(logger.IsEnabled(LogLevel.Trace));

        // Cleanup
        AppLogger.Dispose();
    }

    [Fact]
    public void Initialize_WithDifferentLevels_SetsCorrectMinimum()
    {
        // Arrange
        AppLogger.Dispose();

        try
        {
            // Act & Assert - Debug level
            AppLogger.Initialize(LogLevel.Debug);
            var logger1 = AppLogger.GetLogger<AppLoggerTests>();
            Assert.True(logger1.IsEnabled(LogLevel.Debug));
            Assert.True(logger1.IsEnabled(LogLevel.Information));
            Assert.False(logger1.IsEnabled(LogLevel.Trace));

            AppLogger.Dispose();

            // Act & Assert - Warning level
            AppLogger.Initialize(LogLevel.Warning);
            var logger2 = AppLogger.GetLogger<AppLoggerTests>();
            Assert.True(logger2.IsEnabled(LogLevel.Warning));
            Assert.True(logger2.IsEnabled(LogLevel.Error));
            Assert.False(logger2.IsEnabled(LogLevel.Information));
        }
        finally
        {
            // Cleanup
            AppLogger.Dispose();
        }
    }

    [Fact]
    public void GetLogger_ReturnsTypedLogger()
    {
        // Arrange
        AppLogger.Dispose();
        AppLogger.Initialize(LogLevel.Information);

        // Act
        var logger = AppLogger.GetLogger<AppLoggerTests>();

        // Assert
        Assert.NotNull(logger);
        Assert.IsAssignableFrom<ILogger<AppLoggerTests>>(logger);

        // Cleanup
        AppLogger.Dispose();
    }

    [Fact]
    public void GetLogger_WithStringCategory_ReturnsLogger()
    {
        // Arrange
        AppLogger.Dispose();
        AppLogger.Initialize(LogLevel.Information);

        // Act
        var logger = AppLogger.GetLogger("TestCategory");

        // Assert
        Assert.NotNull(logger);

        // Cleanup
        AppLogger.Dispose();
    }

    [Fact]
    public void Default_ReturnsDefaultLogger()
    {
        // Arrange
        AppLogger.Dispose();
        AppLogger.Initialize(LogLevel.Information);

        // Act
        var logger = AppLogger.Default;

        // Assert
        Assert.NotNull(logger);

        // Cleanup
        AppLogger.Dispose();
    }

    [Fact]
    public void Dispose_CleansUpResources()
    {
        // Arrange
        AppLogger.Initialize(LogLevel.Information);
        var logger = AppLogger.GetLogger<AppLoggerTests>();
        Assert.NotNull(logger);

        // Act
        AppLogger.Dispose();

        // Assert - Should be able to reinitialize
        AppLogger.Initialize(LogLevel.Debug);
        var logger2 = AppLogger.GetLogger<AppLoggerTests>();
        Assert.NotNull(logger2);

        // Cleanup
        AppLogger.Dispose();
    }
}

