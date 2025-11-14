# Code Coverage Improvement Plan

**Current Status**: 53.1% line coverage, 40.6% branch coverage (615/1157 lines, 189/465 branches) ✅ **IMPROVED from 26.1%/7.1%**

## Coverage Analysis

### ✅ Well-Tested Areas
- **VectorExtensions** (11 tests) - Vector math utilities
- **UWB2GPSConverter** (8 tests) - Edge handling and error calculations
- **WGS84Converter** (6 tests) - Basic coordinate conversions

### ✅ Improved Coverage (Previously 0%)
1. **HardwareId.cs** - ✅ Now tested (14 tests) - Hardware ID generation, MQTT client ID sanitization
2. **MQTTControl.cs** - ✅ Now tested (11 tests) - Message handling, event subscription, constants
3. **UWBManager.cs** - ✅ Now tested (10 tests) - Initialization, message parsing, update logic

### ❌ Remaining Gaps (Low Coverage)
1. **Program.cs** - Main entry point, configuration loading, application lifecycle
2. **AppConfig.cs** - Configuration loading and binding (partial coverage)
3. **VersionInfo.cs** - Version information display (partial coverage)
4. **UWB2GPSConverter** - Main conversion algorithm, trilateration, refinement (partial coverage)
5. **WGS84Converter** - Many internal geodetic conversion methods (partial coverage)

---

## Improvement Options

### Option 1: Unit Tests for Core Components (Quick Wins)
**Priority**: High | **Effort**: Medium | **Impact**: +15-20% coverage

**Targets**:
- `AppConfig.Load()` - Configuration loading and validation
- `AppLogger` - Logging initialization and log level parsing
- `VersionInfo` - Version information retrieval
- `UWBManager.Initialise()` - Initialization logic
- `UWBManager.UpdateUwbsFromMessage()` - JSON parsing

**Estimated Coverage Gain**: 15-20%

**Example Tests**:
```csharp
[Fact]
public void AppConfig_Load_LoadsFromJsonFile()
{
    // Arrange - ensure appsettings.json exists
    // Act
    var config = AppConfig.Load();
    // Assert
    Assert.NotNull(config);
    Assert.NotNull(config.MQTT);
    Assert.True(config.Beacons.Count > 0);
}

[Fact]
public void AppLogger_ParseLogLevel_ValidLevels()
{
    Assert.Equal(LogLevel.Debug, AppLogger.ParseLogLevel("DEBUG"));
    Assert.Equal(LogLevel.Information, AppLogger.ParseLogLevel("INFO"));
    // ... etc
}
```

---

### Option 2: Integration Tests for MQTT (High Value)
**Priority**: High | **Effort**: High | **Impact**: +10-15% coverage

**Targets**:
- MQTT connection and disconnection
- Message publishing and receiving
- Retry logic with exponential backoff
- Auto-reconnection on disconnect
- Error handling scenarios

**Approach**: Use embedded MQTT broker (MQTTnet test server) or mock IMqttClient

**Estimated Coverage Gain**: 10-15%

**Example Tests**:
```csharp
[Fact]
public async Task MQTTControl_Initialise_ConnectsSuccessfully()
{
    // Arrange - setup test MQTT broker
    // Act
    await MQTTControl.Initialise(cts, config);
    // Assert - verify connection
}

[Fact]
public async Task MQTTControl_Retry_ExponentialBackoff()
{
    // Test retry logic with failing connection
}
```

---

### Option 3: Algorithm Tests for Trilateration (Critical)
**Priority**: High | **Effort**: Medium | **Impact**: +20-25% coverage

**Targets**:
- `ConvertUWBToPositions()` - Main conversion algorithm
- Trilateration with various beacon configurations
- Iterative refinement algorithm
- Edge cases (collinear beacons, insufficient beacons, etc.)
- Beacon initialization and application

**Estimated Coverage Gain**: 20-25%

**Example Tests**:
```csharp
[Fact]
public void ConvertUWBToPositions_ThreeBeacons_CalculatesPositions()
{
    // Arrange - create network with 3 beacons and unknown nodes
    // Act
    UWB2GPSConverter.ConvertUWBToPositions(network, true, algorithmConfig);
    // Assert - verify positions calculated
}

[Fact]
public void ConvertUWBToPositions_InsufficientBeacons_LogsError()
{
    // Test with < 3 beacons
}

[Fact]
public void InitializeBeacons_AppliesToNetwork()
{
    // Test beacon configuration application
}
```

---

### Option 4: WGS84Converter Comprehensive Tests (Medium Value)
**Priority**: Medium | **Effort**: Medium | **Impact**: +15-20% coverage

**Targets**:
- All internal geodetic conversion methods
- Edge cases (poles, date line, extreme coordinates)
- ECEF transformations
- ENU calculations
- Coordinate system conversions

**Estimated Coverage Gain**: 15-20%

**Example Tests**:
```csharp
[Fact]
public void LatLonAltEstimate_ExtremeCoordinates_HandlesCorrectly()
{
    // Test near poles, date line, etc.
}

[Fact]
public void LatLonAltkm2UnityPos_ConvertsCorrectly()
{
    // Test coordinate system conversion
}
```

---

### Option 5: UWBManager Integration Tests (Medium Value)
**Priority**: Medium | **Effort**: Medium | **Impact**: +10-15% coverage

**Targets**:
- Message parsing and deserialization
- Update loop logic
- Network filtering
- Error handling in message processing

**Estimated Coverage Gain**: 10-15%

**Example Tests**:
```csharp
[Fact]
public void UpdateUwbsFromMessage_ValidJson_ParsesCorrectly()
{
    // Test JSON parsing
}

[Fact]
public void UpdateUwbsFromMessage_InvalidJson_HandlesGracefully()
{
    // Test error handling
}

[Fact]
public void Update_ReentrantCall_Skips()
{
    // Test re-entrancy protection
}
```

---

### Option 6: Program.cs Entry Point Tests (Lower Priority)
**Priority**: Low | **Effort**: High | **Impact**: +5-10% coverage

**Targets**:
- Configuration loading error handling
- Application startup sequence
- Graceful shutdown
- Update loop initialization

**Challenge**: Requires mocking many dependencies or integration testing

**Estimated Coverage Gain**: 5-10%

---

## Recommended Implementation Order

### Phase 1: Quick Wins (Target: 40-45% coverage)
1. ✅ **Option 1**: Unit tests for AppConfig, AppLogger, VersionInfo
2. ✅ **Option 3**: Algorithm tests for trilateration (core functionality)

**Estimated Time**: 4-6 hours  
**Coverage Target**: 40-45%

### Phase 2: Integration Testing (Target: 55-60% coverage)
3. ✅ **Option 2**: MQTT integration tests
4. ✅ **Option 5**: UWBManager integration tests

**Estimated Time**: 6-8 hours  
**Coverage Target**: 55-60%

### Phase 3: Comprehensive Coverage (Target: 70-75% coverage)
5. ✅ **Option 4**: WGS84Converter comprehensive tests
6. ✅ **Option 6**: Program.cs entry point tests (if needed)

**Estimated Time**: 6-8 hours  
**Coverage Target**: 70-75%

---

## Testing Tools & Setup

### Recommended Packages
- **xUnit** (already in use) - Test framework
- **Moq** or **NSubstitute** - Mocking framework for dependencies
- **FluentAssertions** (optional) - Better assertion syntax
- **MQTTnet.TestHelpers** - For MQTT integration tests

### Test Organization
```
tests/
├── Unit/                    # Fast, isolated unit tests
│   ├── AppConfigTests.cs
│   ├── AppLoggerTests.cs
│   ├── VersionInfoTests.cs
│   └── ...
├── Integration/             # Integration tests (may require external services)
│   ├── MQTTIntegrationTests.cs
│   ├── UWBManagerIntegrationTests.cs
│   └── ...
└── Algorithm/               # Algorithm-specific tests
    ├── TrilaterationTests.cs
    ├── RefinementTests.cs
    └── ...
```

---

## Coverage Goals

| Phase | Target Coverage | Priority Areas |
|-------|----------------|----------------|
| **Current** | 26.1% | Utility functions only |
| **Phase 1** | 40-45% | Core components + algorithms |
| **Phase 2** | 55-60% | Integration + network management |
| **Phase 3** | 70-75% | Comprehensive coverage |
| **Stretch** | 80%+ | All critical paths |

---

## Specific Test Cases Needed

### AppConfig Tests (5-8 tests)
- ✅ Load from valid appsettings.json
- ✅ Load with missing file (fallback to defaults)
- ✅ Load with invalid JSON (error handling)
- ✅ Environment variable overrides
- ✅ Beacon configuration loading

### AppLogger Tests (5-8 tests)
- ✅ ParseLogLevel with all valid levels
- ✅ ParseLogLevel with invalid input (defaults)
- ✅ Initialize creates logger factory
- ✅ GetLogger returns typed logger
- ✅ Dispose cleans up resources

### MQTTControl Tests (10-15 tests)
- ✅ Initialise connects successfully
- ✅ Initialise with retry on failure
- ✅ Publish message successfully
- ✅ Publish when disconnected (handles gracefully)
- ✅ Auto-reconnect on disconnect
- ✅ StopReconnect prevents reconnection
- ✅ DisconnectAsync cleans up
- ✅ Message received callback invoked
- ✅ Error handling in message processing

### UWBManager Tests (8-12 tests)
- ✅ Initialise sets up correctly
- ✅ UpdateUwbsFromMessage parses valid JSON
- ✅ UpdateUwbsFromMessage handles invalid JSON
- ✅ Update triggers processing
- ✅ Re-entrancy protection works
- ✅ Filters nodes with valid positions
- ✅ Sends network via MQTT

### Trilateration Tests (15-20 tests)
- ✅ ConvertUWBToPositions with 3 beacons
- ✅ ConvertUWBToPositions with >3 beacons
- ✅ ConvertUWBToPositions with <3 beacons (error)
- ✅ Collinear beacons (error handling)
- ✅ Iterative refinement improves positions
- ✅ InitializeBeacons applies to network
- ✅ ApplyConfiguredBeacons sets positions
- ✅ Edge cases: no edges, invalid distances, etc.

### WGS84Converter Tests (10-15 tests)
- ✅ LatLonAltEstimate with various offsets
- ✅ Extreme coordinates (poles, date line)
- ✅ Large distance calculations
- ✅ Altitude variations
- ✅ Coordinate system conversions

---

## Implementation Tips

1. **Start with AppConfig and AppLogger** - These are simple, pure functions, easy to test
2. **Use test fixtures** - Create reusable test data builders for UWB networks
3. **Mock external dependencies** - Use Moq for IMqttClient, file system, etc.
4. **Test error paths** - Don't just test happy paths, test failures too
5. **Use Theory/InlineData** - For testing multiple scenarios with same test logic
6. **Integration tests separately** - Keep them in separate project or use test categories

---

## CI Integration

Add coverage reporting to CI:
```yaml
- name: Generate Coverage Report
  run: |
    dotnet test --collect:"XPlat Code Coverage" --results-directory:./TestResults
    # Generate HTML report (optional)
    # Report coverage percentage in PR comments
```

---

## Next Steps

1. **Choose a phase** to start with (recommended: Phase 1)
2. **Set up test infrastructure** (mocking framework, test helpers)
3. **Create test fixtures** for common test data
4. **Implement tests incrementally** - one component at a time
5. **Track coverage** after each addition
6. **Aim for 70%+ coverage** on critical paths (algorithms, MQTT, configuration)

---

**Last Updated**: 2025-11-14  
**Current Coverage**: 26.1% lines, 7.1% branches  
**Target Coverage**: 70%+ lines, 50%+ branches

