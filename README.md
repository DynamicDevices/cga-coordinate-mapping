# CGA Coordinate Mapping

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

### MQTT Settings

Default MQTT configuration (can be modified in `MQTTControl.cs`):

- **Server**: `mqtt.dynamicdevices.co.uk`
- **Port**: `1883`
- **Receive Topic**: `DotnetMQTT/Test/in`
- **Send Topic**: `DotnetMQTT/Test/out`
- **Client ID**: `clientId-UwbManager-001`

To customize, modify the constants in `MQTTControl.cs` or pass parameters to `MQTTControl.Initialise()`.

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
- Each node should have edges connecting to other nodes with distance measurements
- Distances are in meters

## Running

```bash
# Run the application
./bin/Release/net8.0/linux-arm64/publish/InstDotNet

# Or with dotnet
dotnet run
```

The application will:
1. Connect to the MQTT broker
2. Subscribe to the receive topic
3. Process incoming UWB network updates
4. Calculate positions for unknown nodes
5. Publish updated network with GPS coordinates

Press `Ctrl+C` to exit gracefully.

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
├── Program.cs              # Main entry point
├── MQTTControl.cs          # MQTT client implementation
├── UWBManager.cs           # UWB network management
├── UWB2GPSConverter.cs     # Trilateration and position calculation
├── WGS84Converter.cs       # Geodetic coordinate conversions
├── VectorExtensions.cs    # Vector math utilities
├── UwbParser.py           # Python preprocessing script
├── TestNodes.json         # Sample test data
├── InstDotNet.csproj      # Project file
└── README.md              # This file
```

## Development

### Code Style

- Follow C# naming conventions
- Use async/await for I/O operations
- Include error handling for network operations
- Document public APIs

### Testing

Currently, testing is done manually with `TestNodes.json`. Future improvements should include:
- Unit tests for trilateration algorithms
- Integration tests for MQTT communication
- Validation tests for coordinate conversions

## Troubleshooting

### MQTT Connection Issues

- Verify MQTT broker is accessible
- Check firewall rules for port 1883
- Verify credentials if authentication is required

### Position Calculation Failures

- Ensure at least 3 beacons have `positionKnown: true`
- Verify beacon GPS coordinates are valid
- Check that distance measurements are reasonable
- Review console output for triangulation errors

### Build Issues

- Ensure .NET 8.0 SDK is installed
- Run `dotnet restore` to fetch dependencies
- Check that target runtime identifier is correct

## License

[Add your license here]

## Contributing

[Add contribution guidelines here]

## Authors

- CGA (Initial development)

## Acknowledgments

- WGS84 conversion routines based on work by James R. Clynch (NPS, 2003)
- Adapted to C# by Jen Laing (2020)

