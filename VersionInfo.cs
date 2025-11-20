using System.Reflection;

/// <summary>
/// Provides version information for the application
/// </summary>
public static class VersionInfo
{
    private static readonly Lazy<string> _buildDate = new(() => GetBuildDate());
    private static readonly Lazy<string> _gitCommitHash = new(() => GetGitCommitHash());

    /// <summary>
    /// Gets the full version string (e.g., "1.0.0" or "1.0.0-beta")
    /// </summary>
    public static string Version => GetVersion();

    /// <summary>
    /// Gets the assembly version
    /// </summary>
    public static string AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";

    /// <summary>
    /// Gets the file version
    /// </summary>
    public static string FileVersion => GetFileVersion();

    /// <summary>
    /// Gets the informational version (includes version suffix if present)
    /// </summary>
    public static string InformationalVersion => GetInformationalVersion();

    /// <summary>
    /// Gets the build date and time (UTC)
    /// </summary>
    public static string BuildDate => _buildDate.Value;

    /// <summary>
    /// Gets the git commit hash
    /// </summary>
    public static string GitCommitHash => _gitCommitHash.Value;

    /// <summary>
    /// Gets the full version string with all available information
    /// </summary>
    public static string FullVersion => $"{Version} (Build: {BuildDate}, Commit: {GitCommitHash})";

    private static string GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "Unknown";
    }

    private static string GetFileVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var fileVersionAttr = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
        return fileVersionAttr?.Version ?? "Unknown";
    }

    private static string GetInformationalVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var infoVersionAttr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        return infoVersionAttr?.InformationalVersion ?? Version;
    }

    private static string GetBuildDate()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>();
        var buildDateAttr = metadata.FirstOrDefault(m => m.Key == "BuildDate");
        return buildDateAttr?.Value ?? "Unknown";
    }

    private static string GetGitCommitHash()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>();
        var commitAttr = metadata.FirstOrDefault(m => m.Key == "GitCommitHash");
        return commitAttr?.Value ?? "Unknown";
    }
}

