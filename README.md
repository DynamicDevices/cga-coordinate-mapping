# CGA Coordinate Mapping

[![CI Build and Publish](https://github.com/DynamicDevices/cga-coordinate-mapping/actions/workflows/ci.yml/badge.svg)](https://github.com/DynamicDevices/cga-coordinate-mapping/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/platform-linux--arm64-lightgrey)](https://github.com/DynamicDevices/cga-coordinate-mapping)
[![Code Coverage](https://img.shields.io/badge/coverage-58%25-green)](https://github.com/DynamicDevices/cga-coordinate-mapping/actions)

A .NET 8.0 application that converts Ultra-Wideband (UWB) sensor network data into GPS coordinates using trilateration algorithms. Designed for real-time position tracking and emergency response scenarios.

## Overview

This application processes UWB sensor network data received via MQTT, calculates positions of unknown nodes using trilateration from known beacon positions, and converts the results to WGS84 GPS coordinates. It includes iterative refinement algorithms to improve position accuracy.

## Features

- **Real-time UWB Network Processing**: Receives UWB node distance measurements via MQTT
- **Trilateration Algorithm**: Calculates 3D positions from distance measurements
- **GPS Coordinate Conversion**: Converts local coordinates to WGS84 lat/lon/alt
- **Iterative Refinement**: Gradient descent optimization for improved accuracy
- **MQTT Integration**: Bidirectional MQTT communication for network updates
- **ARM64 Native Build**: Optimized for embedded Linux ARM64 systems

## Architecture

- **MQTTControl.cs**: MQTT client for receiving/sending network data
- **UWBManager.cs**: Manages UWB network updates and conversion pipeline
- **UWB2GPSConverter.cs**: Core trilateration and position calculation logic
- **WGS84Converter.cs**: Geodetic coordinate conversion (ECEF, ENU transformations)
- **VectorExtensions.cs**: Vector math utilities
- **UwbParser.py**: Python script for preprocessing edge data into network format

For a detailed system architecture diagram, see [PROJECT_CONTEXT.md](PROJECT_CONTEXT.md#system-architecture-diagram).

## Requirements

- .NET 8.0 SDK or Runtime
- MQTT broker (default: `mqtt.dynamicdevices.co.uk:1883`)
- Linux ARM64 target platform (or compatible)

## Building

### Prerequisites

Install the .NET 8.0 SDK:
```bash
# On Ubuntu/Debian
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --version 8.0.121
```

### Build for Linux ARM64

```bash
# Restore dependencies
dotnet restore

# Build Release version for ARM64
dotnet build -c Release -r linux-arm64

# Publish self-contained (includes runtime)
dotnet publish -c Release -r linux-arm64 --self-contained true

# Publish framework-dependent (requires .NET runtime)
dotnet publish -c Release -r linux-arm64 --self-contained false
```

Output will be in: `bin/Release/net8.0/linux-arm64/publish/`

### Build for Other Platforms

```bash
# Linux x64
dotnet publish -c Release -r linux-x64

# Windows x64
dotnet publish -c Release -r win-x64

# macOS ARM64
dotnet publish -c Release -r osx-arm64
```

## Configuration

### Configuration Files

The application uses `appsettings.json` for configuration. A development override file `appsettings.Development.json` is also supported.

**Location**: `src/appsettings.json`

**Configuration Sections**:
- **MQTT**: Server address, port, topics, credentials, retry settings, auto-reconnect, TLS/SSL settings
- **Application**: Update interval, log level
- **Algorithm**: Max iterations, learning rate, refinement enabled/disabled
- **Beacons**: Optional beacon GPS coordinates (can be empty - beacons can be provided via MQTT data instead)

**Environment Variables**: All settings can be overridden via environment variables (e.g., `MQTT__ServerAddress`, `Application__LogLevel`).

### MQTT Settings

Default MQTT configuration (from `appsettings.json`):

- **Server**: `mqtt.dynamicdevices.co.uk`
- **Port**: `1883`
- **Receive Topic**: `DotnetMQTT/Test/in`
- **Send Topic**: `DotnetMQTT/Test/out`
- **Client ID**: `clientId-UwbManager-001`
- **Retry Attempts**: 5 (with exponential backoff)
- **Auto Reconnect**: Enabled

To customize, edit `appsettings.json` or set environment variables.

### UWB Network Format

The application expects JSON messages in the following format:

```json
{
  "uwbs": [
    {
      "id": "B5A4",
      "triageStatus": 5,
      "position": {"x": 0, "y": 0, "z": 0},
      "latLonAlt": [53.485, -2.192, 0.0],
      "positionKnown": true,
      "lastPositionUpdateTime": 0.0,
      "edges": [
        {
          "end0": "B5A4",
          "end1": "B57A",
          "distance": 49.83
        }
      ],
      "positionAccuracy": 0.0
    }
  ]
}
```

**Requirements:**
- At least 3 nodes must have `positionKnown: true` with valid `latLonAlt` coordinates (beacons)
  - Beacons can be provided via MQTT data (recommended) or pre-configured in `appsettings.json`
  - If using MQTT data, set `positionKnown: true` and include `latLonAlt: [latitude, longitude, altitude]` for each beacon
- Each node should have edges connecting to other nodes with distance measurements
- Distances are in meters

## Running

```bash
# Run the application (from project root)
./bin/Release/net8.0/linux-arm64/publish/InstDotNet

# Or with dotnet
dotnet run --project src/InstDotNet.csproj
```

The application will:
1. Display version information (version, build date, git commit hash)
2. Connect to the MQTT broker
3. Subscribe to the receive topic
4. Process incoming UWB network updates
5. Calculate positions for unknown nodes
6. Publish updated network with GPS coordinates

Press `Ctrl+C` to exit gracefully.

## Versioning

This project uses [Semantic Versioning](https://semver.org/) (MAJOR.MINOR.PATCH).

- **Version**: Defined in `InstDotNet.csproj` and `VERSION` file
- **Build Date**: Automatically set at build time (UTC)
- **Git Commit Hash**: Automatically extracted from git repository (short hash)

Version information is displayed when the application starts and is embedded in the assembly metadata. To update the version, modify the `<Version>` property in `InstDotNet.csproj` and the `VERSION` file.

## Python Parser

The `UwbParser.py` script can preprocess edge data into the network format:

```bash
python3 UwbParser.py
```

This reads edge data from `test_uwbs.json` and generates `uwb_network.json`.

## Algorithm Details

### Trilateration

The system uses 3D trilateration to calculate positions:
1. Requires 3 known reference points (beacons) with GPS coordinates
   - Beacons can be provided via MQTT data (set `positionKnown: true` with `latLonAlt` coordinates)
   - Or pre-configured in `appsettings.json` (optional)
2. Uses distance measurements from unknown nodes to known nodes
3. Calculates intersection of spheres to determine position
4. Converts local 3D coordinates to GPS using WGS84 transformations

### Iterative Refinement

After initial trilateration, the system applies gradient descent optimization:
- Maximum 10 iterations
- Learning rate: 0.1
- Minimizes distance error between calculated and measured distances

## Project Structure

```
.
├── bin/                            # Build outputs (gitignored)
├── obj/                            # Build artifacts (gitignored)
├── src/                            # Source code
│   ├── Program.cs                  # Main entry point
│   ├── MQTTControl.cs              # MQTT client implementation
│   ├── UWBManager.cs               # UWB network management
│   ├── UWB2GPSConverter.cs         # Trilateration and position calculation
│   ├── WGS84Converter.cs           # Geodetic coordinate conversions
│   ├── VectorExtensions.cs          # Vector math utilities
│   ├── Logger.cs                    # Logging framework
│   ├── VersionInfo.cs               # Version information
│   ├── AppConfig.cs                 # Configuration model
│   ├── appsettings.json             # Configuration file
│   ├── appsettings.Development.json # Development overrides
│   └── InstDotNet.csproj            # Project file
├── tests/                           # Unit tests
│   ├── VectorExtensionsTests.cs
│   ├── UWB2GPSConverterTests.cs
│   ├── WGS84ConverterTests.cs
│   ├── GlobalUsings.cs
│   └── InstDotNet.Tests.csproj
├── Directory.Build.props            # Centralized build configuration
├── UwbParser.py                     # Python preprocessing script
├── TestNodes.json                   # Sample test data
├── InstDotNet.sln                   # Solution file
├── LICENSE                          # GPLv3 license
├── CONTRIBUTING.md                  # Contribution guidelines
└── README.md                        # This file
```

## Development

### Code Style

- Follow C# naming conventions
- Use async/await for I/O operations
- Include error handling for network operations
- Document public APIs

### Testing

**Unit Tests**: Comprehensive test suite with **92 tests** (all passing ✅)
- Vector math operations (11 tests)
- Trilateration algorithms (13 tests)
- Coordinate conversions (6 tests)
- Configuration loading (6 tests)
- Logging framework (7 tests)
- Version information (6 tests)
- Edge handling and error calculations (8 tests)

**CI Integration**: All tests run automatically in GitHub Actions for both linux-arm64 and linux-x64 platforms.

**Run Tests Locally**:
```bash
dotnet test
```

**Future Improvements**:
- Integration tests for MQTT communication
- Additional edge case coverage (see CODE_COVERAGE_IMPROVEMENTS.md)

## Troubleshooting

### MQTT Connection Issues

- Verify MQTT broker is accessible
- Check firewall rules for port 1883
- Verify credentials if authentication is required

### Position Calculation Failures

- Ensure at least 3 beacons have `positionKnown: true` with valid `latLonAlt` coordinates
- Beacons can be provided via MQTT data or pre-configured in `appsettings.json`
- Verify beacon GPS coordinates are valid
- Check that distance measurements are reasonable
- Review console output for triangulation errors

### Build Issues

- Ensure .NET 8.0 SDK is installed
- Run `dotnet restore` to fetch dependencies
- Check that target runtime identifier is correct

## License

This project is licensed under the GNU General Public License v3.0 (GPLv3). See the [LICENSE](LICENSE) file for details.

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

**Important Notes:**
- All contributions must be submitted via Pull Request (PR)
- Copyright must be assigned to the project maintainers
- Please ensure all tests pass and documentation is updated

## Authors

- CGA (Initial development)
- Alex J Lennon (AI Wrangling)

## Acknowledgments

- WGS84 conversion routines based on work by James R. Clynch (NPS, 2003)
- Adapted to C# by Jen Laing (2020)

