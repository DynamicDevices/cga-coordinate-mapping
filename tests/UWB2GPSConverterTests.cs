using System.Collections.Generic;
using System.Numerics;

namespace InstDotNet.Tests;

public class UWB2GPSConverterTests
{
    [Fact]
    public void TryGetEndFromEdge_CurrentNodeIsEnd0_ReturnsEnd1()
    {
        // Arrange
        var edge = new UWB2GPSConverter.Edge
        {
            end0 = "NodeA",
            end1 = "NodeB",
            distance = 10.0f
        };
        var nodeMap = new Dictionary<string, UWB2GPSConverter.UWB>
        {
            ["NodeA"] = new UWB2GPSConverter.UWB("NodeA"),
            ["NodeB"] = new UWB2GPSConverter.UWB("NodeB")
        };

        // Act
        var result = UWB2GPSConverter.TryGetEndFromEdge(edge, "NodeA", nodeMap, out var end);

        // Assert
        Assert.True(result);
        Assert.NotNull(end);
        Assert.Equal("NodeB", end.id);
    }

    [Fact]
    public void TryGetEndFromEdge_CurrentNodeIsEnd1_ReturnsEnd0()
    {
        // Arrange
        var edge = new UWB2GPSConverter.Edge
        {
            end0 = "NodeA",
            end1 = "NodeB",
            distance = 10.0f
        };
        var nodeMap = new Dictionary<string, UWB2GPSConverter.UWB>
        {
            ["NodeA"] = new UWB2GPSConverter.UWB("NodeA"),
            ["NodeB"] = new UWB2GPSConverter.UWB("NodeB")
        };

        // Act
        var result = UWB2GPSConverter.TryGetEndFromEdge(edge, "NodeB", nodeMap, out var end);

        // Assert
        Assert.True(result);
        Assert.NotNull(end);
        Assert.Equal("NodeA", end.id);
    }

    [Fact]
    public void TryGetEndFromEdge_CurrentNodeNotInEdge_ReturnsFalse()
    {
        // Arrange
        var edge = new UWB2GPSConverter.Edge
        {
            end0 = "NodeA",
            end1 = "NodeB",
            distance = 10.0f
        };
        var nodeMap = new Dictionary<string, UWB2GPSConverter.UWB>
        {
            ["NodeA"] = new UWB2GPSConverter.UWB("NodeA"),
            ["NodeB"] = new UWB2GPSConverter.UWB("NodeB"),
            ["NodeC"] = new UWB2GPSConverter.UWB("NodeC")
        };

        // Act
        var result = UWB2GPSConverter.TryGetEndFromEdge(edge, "NodeC", nodeMap, out var end);

        // Assert
        Assert.False(result);
        Assert.Null(end);
    }

    [Fact]
    public void TryGetEndFromEdge_OtherEndNotInMap_ReturnsFalse()
    {
        // Arrange
        var edge = new UWB2GPSConverter.Edge
        {
            end0 = "NodeA",
            end1 = "NodeB",
            distance = 10.0f
        };
        var nodeMap = new Dictionary<string, UWB2GPSConverter.UWB>
        {
            ["NodeA"] = new UWB2GPSConverter.UWB("NodeA")
            // NodeB is missing
        };

        // Act
        var result = UWB2GPSConverter.TryGetEndFromEdge(edge, "NodeA", nodeMap, out var end);

        // Assert
        Assert.False(result);
        Assert.Null(end);
    }

    [Fact]
    public void TryGetEndFromEdge_NullEdge_ReturnsFalse()
    {
        // Arrange
        UWB2GPSConverter.Edge? edge = null;
        var nodeMap = new Dictionary<string, UWB2GPSConverter.UWB>();

        // Act
        var result = UWB2GPSConverter.TryGetEndFromEdge(edge!, "NodeA", nodeMap, out var end);

        // Assert
        Assert.False(result);
        Assert.Null(end);
    }

    [Fact]
    public void TryGetEndFromEdge_NullNodeMap_ReturnsFalse()
    {
        // Arrange
        var edge = new UWB2GPSConverter.Edge
        {
            end0 = "NodeA",
            end1 = "NodeB",
            distance = 10.0f
        };
        Dictionary<string, UWB2GPSConverter.UWB>? nodeMap = null;

        // Act
        var result = UWB2GPSConverter.TryGetEndFromEdge(edge, "NodeA", nodeMap!, out var end);

        // Assert
        Assert.False(result);
        Assert.Null(end);
    }

    [Fact]
    public void TryGetEndFromEdge_EmptyCurrentNodeId_ReturnsFalse()
    {
        // Arrange
        var edge = new UWB2GPSConverter.Edge
        {
            end0 = "NodeA",
            end1 = "NodeB",
            distance = 10.0f
        };
        var nodeMap = new Dictionary<string, UWB2GPSConverter.UWB>();

        // Act
        var result = UWB2GPSConverter.TryGetEndFromEdge(edge, "", nodeMap, out var end);

        // Assert
        Assert.False(result);
        Assert.Null(end);
    }

    [Fact]
    public void EdgeErrorSquared_PerfectMatch_ReturnsZero()
    {
        // Arrange
        var end0 = new UWB2GPSConverter.UWB("NodeA")
        {
            position = new Vector3(0, 0, 0)
        };
        var end1 = new UWB2GPSConverter.UWB("NodeB")
        {
            position = new Vector3(5, 0, 0)
        };
        float edgeDistance = 5.0f;

        // Act
        var result = UWB2GPSConverter.EdgeErrorSquared(end0, end1, edgeDistance);

        // Assert
        Assert.Equal(0f, result, 5f);
    }

    [Fact]
    public void EdgeErrorSquared_DistanceMismatch_ReturnsSquaredError()
    {
        // Arrange
        var end0 = new UWB2GPSConverter.UWB("NodeA")
        {
            position = new Vector3(0, 0, 0)
        };
        var end1 = new UWB2GPSConverter.UWB("NodeB")
        {
            position = new Vector3(5, 0, 0)  // Actual distance = 5
        };
        float edgeDistance = 3.0f;  // Expected distance = 3, error = 2

        // Act
        var result = UWB2GPSConverter.EdgeErrorSquared(end0, end1, edgeDistance);

        // Assert
        Assert.Equal(4f, result, 5f);  // 2^2 = 4
    }
}

