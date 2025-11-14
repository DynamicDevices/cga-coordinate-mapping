using System.Numerics;

namespace InstDotNet.Tests;

public class VectorExtensionsTests
{
    [Fact]
    public void Normalized_UnitVector_ReturnsSame()
    {
        // Arrange
        var vector = new Vector3(1, 0, 0);

        // Act
        var result = vector.Normalized();

        // Assert
        var length = (float)result.Length();
        Assert.Equal(1.0f, length, 5f);
        Assert.Equal(vector, result);
    }

    [Fact]
    public void Normalized_NonUnitVector_ReturnsNormalized()
    {
        // Arrange
        var vector = new Vector3(3, 4, 0);
        var expected = new Vector3(0.6f, 0.8f, 0);

        // Act
        var result = vector.Normalized();

        // Assert
        var length = (float)result.Length();
        Assert.Equal(1.0f, length, 5f);
        Assert.Equal(expected.X, result.X, 5f);
        Assert.Equal(expected.Y, result.Y, 5f);
        Assert.Equal(0f, result.Z, 5f);
    }

    [Fact]
    public void Normalized_ZeroVector_ReturnsZero()
    {
        // Arrange
        var vector = Vector3.Zero;

        // Act
        var result = vector.Normalized();

        // Assert
        Assert.Equal(Vector3.Zero, result);
    }

    [Fact]
    public void Cross_OrthogonalVectors_ReturnsPerpendicular()
    {
        // Arrange
        var a = new Vector3(1, 0, 0);
        var b = new Vector3(0, 1, 0);
        var expected = new Vector3(0, 0, 1);

        // Act
        var result = Vector3Extensions.Cross(a, b);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Cross_ParallelVectors_ReturnsZero()
    {
        // Arrange
        var a = new Vector3(1, 0, 0);
        var b = new Vector3(2, 0, 0);

        // Act
        var result = Vector3Extensions.Cross(a, b);

        // Assert
        Assert.Equal(Vector3.Zero, result);
    }

    [Fact]
    public void Dot_OrthogonalVectors_ReturnsZero()
    {
        // Arrange
        var a = new Vector3(1, 0, 0);
        var b = new Vector3(0, 1, 0);

        // Act
        var result = Vector3Extensions.Dot(a, b);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Dot_ParallelVectors_ReturnsProductOfLengths()
    {
        // Arrange
        var a = new Vector3(2, 0, 0);
        var b = new Vector3(3, 0, 0);

        // Act
        var result = Vector3Extensions.Dot(a, b);

        // Assert
        Assert.Equal(6f, result);
    }

    [Fact]
    public void Distance_SamePoint_ReturnsZero()
    {
        // Arrange
        var a = new Vector3(1, 2, 3);
        var b = new Vector3(1, 2, 3);

        // Act
        var result = Vector3Extensions.Distance(a, b);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Distance_DifferentPoints_ReturnsCorrectDistance()
    {
        // Arrange
        var a = new Vector3(0, 0, 0);
        var b = new Vector3(3, 4, 0);

        // Act
        var result = Vector3Extensions.Distance(a, b);

        // Assert
        Assert.Equal(5f, result, 5f);
    }

    [Fact]
    public void Distance_3DPoints_ReturnsCorrectDistance()
    {
        // Arrange
        var a = new Vector3(0, 0, 0);
        var b = new Vector3(1, 1, 1);

        // Act
        var result = Vector3Extensions.Distance(a, b);

        // Assert
        var expectedDist = (float)Math.Sqrt(3);
        Assert.Equal(expectedDist, result, 5f);
    }

    [Fact]
    public void Zero_ReturnsZeroVector()
    {
        // Act
        var result = Vector3Extensions.Zero;

        // Assert
        Assert.Equal(Vector3.Zero, result);
    }
}

