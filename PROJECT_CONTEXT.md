# Project Context: CGA Coordinate Mapping

## Project Overview

**Purpose**: Convert relative coordinates from Ultra-Wideband (UWB) devices into absolute GPS coordinates with confidence levels, using hardcoded beacon/tag locations as reference points.

**Status**: Active Development  
**Last Updated**: 2025-11-14  
**Repository**: `git@github.com:DynamicDevices/cga-coordinate-mapping.git`

## Core Functionality

### Input
- **Relative coordinates** from UWB devices
- **Distance measurements** between UWB nodes (edges)
- **Hardcoded beacon locations** with known GPS coordinates (lat/lon/alt)
- Data received via **MQTT** in JSON format

### Processing
1. **Trilateration**: Calculate 3D positions of unknown nodes using distance measurements from known beacon positions
2. **Coordinate Transformation**: Convert local 3D coordinates to WGS84 GPS coordinates (latitude, longitude, altitude)
3. **Iterative Refinement**: Apply gradient descent optimization to improve position accuracy
4. **Confidence Calculation**: Compute position accuracy metrics based on distance measurement errors

### Output
- **Absolute GPS coordinates** (WGS84 lat/lon/alt) for all nodes
- **Confidence levels** (position accuracy in meters)
- **Updated network** with calculated positions, published back via MQTT

## Architecture

### Key Components

#### 1. MQTTControl.cs
- **Purpose**: MQTT client for bidirectional communication
- **Input**: Receives UWB network JSON from topic `DotnetMQTT/Test/in`
- **Output**: Publishes updated network with GPS coordinates to topic `DotnetMQTT/Test/out`
- **Configuration**: 
  - Server: `mqtt.dynamicdevices.co.uk:1883`
  - Client ID: `clientId-UwbManager-001`

#### 2. UWBManager.cs
- **Purpose**: Manages UWB network lifecycle and update pipeline
- **Responsibilities**:
  - Parse incoming MQTT messages into network structure
  - Trigger position calculations
  - Filter nodes with valid positions for output
  - Serialize and publish results

#### 3. UWB2GPSConverter.cs
- **Purpose**: Core algorithm implementation
- **Algorithms**:
  - **3D Trilateration**: Calculates positions from 3+ known reference points
  - **Iterative Refinement**: Gradient descent optimization (max 10 iterations, learning rate 0.1)
  - **Error Calculation**: Computes position accuracy based on distance constraint violations
- **Requirements**: Minimum 3 beacons with `positionKnown: true` and valid GPS coordinates

#### 4. WGS84Converter.cs
- **Purpose**: Geodetic coordinate transformations
- **Transformations**:
  - Local 3D coordinates → ECEF (Earth-Centered, Earth-Fixed)
  - ECEF → WGS84 GPS (lat/lon/alt)
  - ENU (East-North-Up) tangent plane calculations
  - Unity coordinate system conversions
- **Reference**: Based on WGS84 ellipsoid constants (a=6378.137 km, f=1/298.257223563)

#### 5. VectorExtensions.cs
- **Purpose**: Vector math utilities
- **Operations**: Normalization, cross product, dot product, distance calculations

#### 6. UwbParser.py
- **Purpose**: Preprocessing tool for edge data
- **Function**: Converts edge list format to network JSON structure
- **Beacon Configuration**: Hardcoded beacon positions for B5A4, B57A, B98A

## System Architecture Diagram

> **⚠️ Important**: This diagram must be kept updated as the code changes. When modifying components, data flow, or architecture, update this diagram accordingly.

```mermaid
graph TB
    subgraph "Input"
        MQTT[MQTT Broker<br/>mqtt.dynamicdevices.co.uk:1883]
        JSON[UWB Network JSON<br/>Relative coordinates + distances]
    end
    
    subgraph "Application"
        MQTTCtrl[MQTTControl.cs<br/>MQTT Client]
        UWBManager[UWBManager.cs<br/>Network Manager]
        Converter[UWB2GPSConverter.cs<br/>Trilateration Engine]
        WGS84[WGS84Converter.cs<br/>Coordinate Transform]
        Vector[VectorExtensions.cs<br/>Math Utilities]
    end
    
    subgraph "Reference Data"
        Beacons[Hardcoded Beacons<br/>B5A4, B57A, B98A<br/>GPS Coordinates]
    end
    
    subgraph "Processing Pipeline"
        Parse[Parse Network JSON]
        Identify[Identify Known Beacons]
        Trilat[3D Trilateration<br/>Sphere Intersection]
        Refine[Iterative Refinement<br/>Gradient Descent]
        CalcConf[Calculate Confidence<br/>Position Accuracy]
        Filter[Filter Valid Nodes]
    end
    
    subgraph "Output"
        OutputJSON[Updated Network JSON<br/>GPS Coordinates + Confidence]
        MQTTOut[MQTT Publish]
    end
    
    MQTT -->|Subscribe| MQTTCtrl
    JSON -->|Receive| MQTTCtrl
    MQTTCtrl -->|Message| UWBManager
    UWBManager -->|Network Data| Parse
    Parse -->|Structured Network| Converter
    Beacons -->|Reference Points| Identify
    Identify -->|Known Positions| Trilat
    Converter -->|3D Positions| Trilat
    Trilat -->|Initial Positions| Refine
    Refine -->|Optimized Positions| CalcConf
    CalcConf -->|Positions + Accuracy| Filter
    Filter -->|Valid Nodes| UWBManager
    UWBManager -->|Serialized JSON| MQTTOut
    MQTTOut -->|Publish| MQTT
    
    Converter -.->|Uses| WGS84
    Converter -.->|Uses| Vector
    WGS84 -.->|Uses| Vector
    
    style MQTT fill:#e1f5ff
    style Beacons fill:#fff4e1
    style Converter fill:#e8f5e9
    style WGS84 fill:#e8f5e9
    style OutputJSON fill:#f3e5f5
```

## Data Flow

```
MQTT Message (JSON)
    ↓
UWBManager.UpdateUwbsFromMessage()
    ↓
Parse Network Structure
    ↓
UWB2GPSConverter.ConvertUWBToPositions()
    ↓
1. Identify 3+ Known Beacons (hardcoded GPS positions)
    ↓
2. Trilateration Loop:
   - For each unknown node
   - Find 3 known neighbors
   - Calculate 3D position using sphere intersection
   - Convert to GPS using WGS84Converter
    ↓
3. Iterative Refinement:
   - Gradient descent optimization
   - Minimize distance error
   - Update positions
    ↓
4. Calculate Confidence:
   - Position accuracy = sqrt(mean squared error)
   - Based on distance constraint violations
    ↓
Filter Valid Nodes (latLonAlt != null, accuracy != -1)
    ↓
Serialize to JSON
    ↓
MQTT Publish
```

## Hardcoded Beacon Locations

The system relies on **hardcoded beacon/tag locations** with known GPS coordinates:

| Beacon ID | Latitude | Longitude | Altitude (m) |
|-----------|----------|-----------|--------------|
| B5A4      | 53.48514639104522 | -2.191785053920114 | 0.0 |
| B57A      | 53.48545891792991 | -2.19232588314793 | 0.0 |
| B98A      | 53.485994341662628 | -2.192366069038485 | 0.0 |

**Location**: Manchester, UK area (approximately)

These beacons serve as **reference points** for the trilateration algorithm. All other node positions are calculated relative to these fixed points.

## Algorithm Details

### Trilateration Process

1. **Initial Setup**:
   - Identify all nodes with `positionKnown: true` (beacons)
   - Verify at least 3 beacons exist
   - Convert beacon GPS coordinates to local 3D space using WGS84 transformations

2. **Position Calculation**:
   - For each unknown node:
     - Find 3 known neighbors (beacons or previously calculated nodes)
     - Extract distance measurements from edges
     - Calculate sphere intersection in 3D space
     - Handle collinear cases (error if nodes are too close/collinear)

3. **Coordinate Conversion**:
   - Use reference beacon's GPS position as origin
   - Transform calculated 3D offset to GPS coordinates
   - Apply WGS84 ellipsoid corrections

4. **Refinement**:
   - Iterative gradient descent (up to 10 iterations)
   - Adjust positions to minimize distance measurement errors
   - Only accept improvements (reject if error increases)

### Confidence Calculation

**Position Accuracy** (`positionAccuracy`):
- Calculated as: `sqrt(mean_squared_error)`
- Where error = `|calculated_distance - measured_distance|` for each edge
- Units: meters
- Value of `-1` indicates invalid/uncalculated position

**Factors Affecting Confidence**:
- Number of distance measurements (edges)
- Geometric distribution of reference nodes
- Distance measurement accuracy
- Convergence of refinement algorithm

## Current Implementation Status

### ✅ Completed
- MQTT integration (receive/publish)
- Trilateration algorithm (3D sphere intersection)
- WGS84 coordinate conversion
- Iterative refinement (gradient descent)
- Confidence/accuracy calculation
- ARM64 native build support
- CI/CD pipeline (GitHub Actions)
- Python preprocessing script

### ⚠️ Known Issues
1. **Bug**: `isUpdating` flag never set to `true` in `UWBManager.cs` (re-entrancy guard ineffective)
2. **Bug**: `TryGetEndFromEdge` only checks `edge.end1`, missing cases where node is `end0`
3. **Typo**: `_usernname` should be `_username` in `MQTTControl.cs`
4. **Missing**: Null checks before processing network data
5. **Performance**: O(n²) neighbor lookups (should use dictionary)

### 🔄 Future Improvements
- Configuration file support (appsettings.json)
- Logging framework (replace Console.WriteLine)
- Unit tests for algorithms
- Retry logic for MQTT connections
- Health check endpoint
- Metrics/monitoring
- Support for dynamic beacon configuration (not just hardcoded)

## Build & Deployment

### Target Platform
- **Primary**: Linux ARM64 (embedded systems)
- **Build Tool**: .NET 8.0 SDK
- **CI/CD**: GitHub Actions (automated builds on push)

### Build Artifacts
- Native ARM64 executable: `InstDotNet`
- Managed assembly: `InstDotNet.dll`
- Dependencies: `MQTTnet.dll`
- Runtime config: `InstDotNet.runtimeconfig.json`

### Deployment
1. Download artifact from GitHub Actions
2. Extract to target system
3. Ensure .NET 8.0 runtime is installed (for framework-dependent build)
4. Configure MQTT broker connection (if different from defaults)
5. Run: `./InstDotNet`

## Testing

### Test Data
- **TestNodes.json**: Sample network with 10 nodes
  - 3 beacons (B5A4, B57A, B98A) with known positions
  - 7 unknown nodes with distance measurements
  - Various triage statuses for emergency response scenarios

### Manual Testing
1. Start MQTT broker
2. Run application
3. Publish test network JSON to receive topic
4. Verify GPS coordinates calculated for unknown nodes
5. Check confidence levels in output

## Related Files

- `README.md`: User-facing documentation
- `TestNodes.json`: Sample test data
- `.github/workflows/ci.yml`: CI/CD pipeline
- `UwbParser.py`: Data preprocessing tool

## Notes

- **Coordinate System**: Uses Unity-style coordinate system internally (Y-up), converts to ENU (East-North-Up) for GPS
- **Distance Units**: All distances in meters
- **Update Frequency**: 10ms update loop (configurable in `Program.cs`)
- **Error Handling**: Basic error logging to console, continues on failures
- **Thread Safety**: Limited (volatile flag for trigger, but `isUpdating` not thread-safe)

## Contact & Maintenance

- **Repository**: DynamicDevices/cga-coordinate-mapping
- **Initial Development**: CGA
- **Maintenance**: [To be assigned]

---

*This document should be updated as the project evolves. Key changes should be documented here for team reference.*

