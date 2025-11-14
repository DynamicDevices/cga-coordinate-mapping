using Microsoft.Extensions.Logging;

/// <summary>
/// Static logger factory and helper for application-wide logging.
/// </summary>
public static class AppLogger
{
    private static ILoggerFactory? _loggerFactory;
    private static ILogger? _defaultLogger;

    /// <summary>
    /// Initialize the logger factory with console output.
    /// </summary>
    /// <param name="logLevel">Minimum log level (default: Information)</param>
    public static void Initialize(LogLevel logLevel = LogLevel.Information)
    {
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddSimpleConsole(options =>
                {
                    options.IncludeScopes = false;
                    options.SingleLine = true;
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                })
                .SetMinimumLevel(logLevel);
        });

        _defaultLogger = _loggerFactory.CreateLogger("InstDotNet");
    }

    /// <summary>
    /// Get a logger for a specific category.
    /// </summary>
    public static ILogger GetLogger(string categoryName)
    {
        if (_loggerFactory == null)
        {
            Initialize();
        }
        if (_loggerFactory == null)
        {
            throw new InvalidOperationException("Logger factory is null after initialization");
        }
        try
        {
            return _loggerFactory.CreateLogger(categoryName);
        }
        catch (ObjectDisposedException)
        {
            // Logger was disposed (e.g., during tests), reinitialize
            Initialize();
            return _loggerFactory!.CreateLogger(categoryName);
        }
    }

    /// <summary>
    /// Get a typed logger.
    /// </summary>
    public static ILogger<T> GetLogger<T>()
    {
        if (_loggerFactory == null)
        {
            Initialize();
        }
        try
        {
            return _loggerFactory!.CreateLogger<T>();
        }
        catch (ObjectDisposedException)
        {
            // Logger was disposed (e.g., during tests), reinitialize
            Initialize();
            return _loggerFactory!.CreateLogger<T>();
        }
    }

    /// <summary>
    /// Get the default application logger.
    /// </summary>
    public static ILogger Default
    {
        get
        {
            if (_defaultLogger == null)
            {
                Initialize();
            }
            return _defaultLogger!;
        }
    }

    /// <summary>
    /// Dispose the logger factory (call on application shutdown).
    /// </summary>
    public static void Dispose()
    {
        _loggerFactory?.Dispose();
        _loggerFactory = null;
        _defaultLogger = null;
    }

    /// <summary>
    /// Parse log level from string (case-insensitive).
    /// </summary>
    public static LogLevel ParseLogLevel(string? level)
    {
        if (string.IsNullOrWhiteSpace(level))
            return LogLevel.Information;

        return level.ToUpperInvariant() switch
        {
            "TRACE" => LogLevel.Trace,
            "DEBUG" => LogLevel.Debug,
            "INFO" or "INFORMATION" => LogLevel.Information,
            "WARN" or "WARNING" => LogLevel.Warning,
            "ERROR" => LogLevel.Error,
            "CRITICAL" or "FATAL" => LogLevel.Critical,
            "NONE" => LogLevel.None,
            _ => LogLevel.Information
        };
    }
}

