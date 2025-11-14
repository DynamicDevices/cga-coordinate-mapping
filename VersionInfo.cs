#nullable enable
using System.Reflection;

/// <summary>
/// Version information for the application.
/// This class is populated at build time with version, build date, and git commit hash.
/// </summary>
public static class VersionInfo
{
    public static string Version { get; } = GetVersion();
    public static string BuildDate { get; } = GetBuildDate();
    public static string GitCommitHash { get; } = GetGitCommitHash();
    public static string FullVersion { get; } = $"{Version} (Build: {BuildDate}, Commit: {GitCommitHash})";

    private static string GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        if (version != null)
        {
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }
        return "0.0.0";
    }

    private static string GetBuildDate()
    {
        // Try to get from assembly metadata (set at build time)
        var assembly = Assembly.GetExecutingAssembly();
        var buildDateAttr = assembly.GetCustomAttribute<AssemblyMetadataAttribute>();
        
        // Check for BuildDate metadata
        var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>();
        foreach (var attr in metadata)
        {
            if (attr.Key == "BuildDate")
            {
                return attr.Value ?? "Unknown";
            }
        }

        // Fallback: use file write time
        try
        {
            var fileInfo = new FileInfo(assembly.Location);
            return fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss UTC");
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string GetGitCommitHash()
    {
        // Try to get from assembly metadata (set at build time)
        var assembly = Assembly.GetExecutingAssembly();
        var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>();
        foreach (var attr in metadata)
        {
            if (attr.Key == "GitCommitHash")
            {
                return attr.Value ?? "Unknown";
            }
        }

        // Fallback: try to read from .git directory (development builds)
        try
        {
            var gitDir = FindGitDirectory();
            if (gitDir != null)
            {
                var headFile = Path.Combine(gitDir, "HEAD");
                if (File.Exists(headFile))
                {
                    var headContent = File.ReadAllText(headFile).Trim();
                    if (headContent.StartsWith("ref: "))
                    {
                        var refPath = headContent.Substring(5);
                        var refFile = Path.Combine(gitDir, refPath);
                        if (File.Exists(refFile))
                        {
                            var commitHash = File.ReadAllText(refFile).Trim();
                            return commitHash.Substring(0, Math.Min(7, commitHash.Length));
                        }
                    }
                    else if (headContent.Length >= 7)
                    {
                        return headContent.Substring(0, 7);
                    }
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        return "Unknown";
    }

    private static string? FindGitDirectory()
    {
        var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (currentDir != null)
        {
            var gitDir = Path.Combine(currentDir.FullName, ".git");
            if (Directory.Exists(gitDir))
            {
                return gitDir;
            }
            currentDir = currentDir.Parent;
        }
        return null;
    }
}

