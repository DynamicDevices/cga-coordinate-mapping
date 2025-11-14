# Contributing to CGA Coordinate Mapping

Thank you for your interest in contributing to CGA Coordinate Mapping! This document provides guidelines and instructions for contributing to the project.

## Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone git@github.com:YOUR_USERNAME/cga-coordinate-mapping.git
   cd cga-coordinate-mapping
   ```
3. **Create a branch** for your changes:
   ```bash
   git checkout -b feature/your-feature-name
   # or
   git checkout -b fix/your-bug-fix
   ```

## Contribution Process

### 1. Make Your Changes

- Follow the existing code style and conventions
- Write clear, self-documenting code
- Add comments for complex logic
- Update documentation as needed (README.md, PROJECT_CONTEXT.md)
- Add or update unit tests for new functionality
- Ensure all tests pass: `dotnet test`

### 2. Commit Your Changes

- Write clear, descriptive commit messages
- Use present tense ("Add feature" not "Added feature")
- Reference issue numbers if applicable: `Fix #123: Description`
- Keep commits focused and atomic

Example:
```bash
git commit -m "Add retry logic for MQTT connection failures

- Implement exponential backoff retry mechanism
- Add configuration for max retry attempts
- Update tests to cover retry scenarios
- Fixes #42"
```

### 3. Submit a Pull Request

**All contributions must be submitted via Pull Request (PR).**

1. **Push your branch** to your fork:
   ```bash
   git push origin feature/your-feature-name
   ```

2. **Create a Pull Request** on GitHub:
   - Go to the original repository
   - Click "New Pull Request"
   - Select your fork and branch
   - Fill out the PR template (if available)

3. **PR Requirements**:
   - Provide a clear title and description
   - Explain what changes you made and why
   - Reference any related issues
   - Ensure CI checks pass (builds and tests)
   - Update documentation if needed

### 4. PR Review Process

- Maintainers will review your PR
- Address any feedback or requested changes
- Once approved, a maintainer will merge your PR

## Copyright Assignment

**Important**: By contributing to this project, you agree to assign copyright of your contributions to the project maintainers.

### What This Means

When you submit a Pull Request, you are granting the project maintainers:
- The right to use, modify, and distribute your contributions
- The right to license your contributions under the GPLv3 license (or any future license chosen by the project)
- The right to include your contributions in the project

### Why Copyright Assignment?

- Ensures the project can be maintained and evolved consistently
- Allows for potential future license changes if needed
- Protects the project from copyright disputes
- Maintains clear ownership for legal purposes

### How to Indicate Agreement

By submitting a Pull Request, you are implicitly agreeing to assign copyright. If you have concerns or questions about copyright assignment, please discuss them in your PR or contact the maintainers before submitting.

## Code Style Guidelines

### C# Style

- Follow standard C# naming conventions:
  - Classes: `PascalCase`
  - Methods: `PascalCase`
  - Private fields: `_camelCase` with underscore prefix
  - Local variables: `camelCase`
  - Constants: `UPPER_SNAKE_CASE` or `PascalCase`

- Use meaningful variable and method names
- Keep methods focused and small
- Use `async/await` for I/O operations
- Include XML documentation comments for public APIs

### Example

```csharp
/// <summary>
/// Calculates the distance between two UWB nodes.
/// </summary>
/// <param name="node1">First UWB node</param>
/// <param name="node2">Second UWB node</param>
/// <returns>Distance in meters</returns>
public static float CalculateDistance(UWB node1, UWB node2)
{
    // Implementation
}
```

### Logging

- Use structured logging with `ILogger`:
  ```csharp
  _logger.LogInformation("Processing {NodeCount} nodes", nodeCount);
  ```
- Use appropriate log levels:
  - `Trace`: Very detailed diagnostic information
  - `Debug`: Diagnostic information for debugging
  - `Information`: General informational messages
  - `Warning`: Warning messages for potential issues
  - `Error`: Error messages for failures
  - `Critical`: Critical failures requiring immediate attention

### Error Handling

- Use try-catch blocks for operations that can fail
- Log errors with context using structured logging
- Continue processing when possible (don't crash on single node failures)
- Return meaningful error messages

## Testing Requirements

### Unit Tests

- Add unit tests for new functionality
- Maintain or improve test coverage
- All tests must pass before PR submission
- Run tests locally: `dotnet test`

### Test Structure

- One test class per component being tested
- Use descriptive test method names: `MethodName_Scenario_ExpectedResult`
- Follow Arrange-Act-Assert pattern
- Use appropriate assertions with clear messages

Example:
```csharp
[Fact]
public void TryGetEndFromEdge_CurrentNodeIsEnd0_ReturnsEnd1()
{
    // Arrange
    var edge = new UWB2GPSConverter.Edge { /* ... */ };
    
    // Act
    var result = UWB2GPSConverter.TryGetEndFromEdge(edge, "NodeA", nodeMap, out var end);
    
    // Assert
    Assert.True(result);
    Assert.Equal("NodeB", end.id);
}
```

## Documentation

### Code Documentation

- Update `PROJECT_CONTEXT.md` if you:
  - Add new components or features
  - Change the architecture
  - Modify data flow
  - Update the Mermaid diagram (if architecture changes)

### User Documentation

- Update `README.md` if you:
  - Add new features or configuration options
  - Change build or deployment procedures
  - Add new dependencies or requirements

### Inline Comments

- Comment complex algorithms or business logic
- Explain "why" not "what" (code should be self-documenting)
- Keep comments up-to-date with code changes

## Project-Specific Guidelines

### MQTT Integration

- Follow existing MQTT patterns in `MQTTControl.cs`
- Use structured logging for connection events
- Handle disconnections gracefully

### Coordinate Conversion

- Maintain accuracy in coordinate transformations
- Document any assumptions about coordinate systems
- Add tests for edge cases (poles, date line, etc.)

### Performance

- Consider performance implications for real-time processing
- Use efficient data structures (e.g., Dictionary for O(1) lookups)
- Profile if making significant algorithm changes

## Reporting Issues

### Bug Reports

When reporting bugs, please include:
- Clear description of the issue
- Steps to reproduce
- Expected vs. actual behavior
- Environment details (.NET version, OS, etc.)
- Relevant log output
- Any error messages or stack traces

### Feature Requests

For feature requests, please include:
- Use case and motivation
- Proposed solution or approach
- Potential impact on existing functionality
- Any alternatives considered

## Questions?

If you have questions about contributing:
- Open an issue for discussion
- Check existing issues and PRs
- Review `PROJECT_CONTEXT.md` for architecture details

## Code of Conduct

- Be respectful and professional
- Provide constructive feedback
- Help others learn and improve
- Follow the project's technical decisions

## License

By contributing, you agree that your contributions will be licensed under the same GPLv3 license that covers the project.

---

Thank you for contributing to CGA Coordinate Mapping! 🎉

