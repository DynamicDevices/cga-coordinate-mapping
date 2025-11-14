using System.Reflection;

using Xunit;

namespace InstDotNet.Tests;

public class VersionInfoTests
{
    [Fact]
    public void Version_IsNotNull()
    {
        // Act
        var version = VersionInfo.Version;

        // Assert
        Assert.NotNull(version);
        // Version could be "0.0.0" if assembly version is not set, which is valid
        Assert.NotEmpty(version);
    }

    [Fact]
    public void Version_MatchesSemanticVersioning()
    {
        // Act
        var version = VersionInfo.Version;

        // Assert - Should match pattern like "1.0.0"
        var parts = version.Split('.');
        Assert.True(parts.Length >= 2, "Version should have at least MAJOR.MINOR");
        Assert.True(int.TryParse(parts[0], out _), "Major version should be numeric");
        Assert.True(int.TryParse(parts[1], out _), "Minor version should be numeric");
    }

    [Fact]
    public void BuildDate_IsNotNull()
    {
        // Act
        var buildDate = VersionInfo.BuildDate;

        // Assert
        Assert.NotNull(buildDate);
        // Could be "Unknown" if metadata is not set, which is valid
        Assert.NotEmpty(buildDate);
    }

    [Fact]
    public void GitCommitHash_IsNotNull()
    {
        // Act
        var commitHash = VersionInfo.GitCommitHash;

        // Assert
        Assert.NotNull(commitHash);
        // Could be "Unknown" if not built with git, but should not be null
    }

    [Fact]
    public void FullVersion_ContainsAllComponents()
    {
        // Act
        var fullVersion = VersionInfo.FullVersion;

        // Assert
        Assert.NotNull(fullVersion);
        Assert.NotEmpty(fullVersion);
        Assert.Contains(VersionInfo.Version, fullVersion);
        Assert.Contains(VersionInfo.BuildDate, fullVersion);
        Assert.Contains(VersionInfo.GitCommitHash, fullVersion);
    }

    [Fact]
    public void FullVersion_Format_IsCorrect()
    {
        // Act
        var fullVersion = VersionInfo.FullVersion;

        // Assert - Should contain version, build date, and commit hash
        // Format: "{Version} (Build: {BuildDate}, Commit: {GitCommitHash})"
        Assert.Contains("Build:", fullVersion);
        Assert.Contains("Commit:", fullVersion);
    }
}

