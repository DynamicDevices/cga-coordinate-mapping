# Yocto Build Artifact Locations

This document compares the build output locations between the main branch and the ajl/development branch to help configure Yocto recipes correctly.

## Branch Structure Comparison

### Main Branch
- **Source files location**: Root directory (`/`)
  - `InstDotNet.csproj`
  - `Program.cs`
  - `MQTTControl.cs`
  - `UWB2GPSConverter.cs`
  - `UWBManager.cs`
  - `VectorExtensions.cs`
  - `WGS84Converter.cs`

- **Build output location**: 
  ```
  bin/Release/net8.0/linux-arm64/publish/
  ```

- **Main executable**: 
  ```
  bin/Release/net8.0/linux-arm64/publish/InstDotNet
  ```

### Development Branch (ajl/development)
- **Source files location**: `src/` subdirectory
  - `src/InstDotNet.csproj`
  - `src/Program.cs`
  - `src/MQTTControl.cs`
  - `src/UWB2GPSConverter.cs`
  - `src/UWBManager.cs`
  - `src/VectorExtensions.cs`
  - `src/WGS84Converter.cs`
  - Plus additional files: `src/AppConfig.cs`, `src/HealthCheck.cs`, etc.

- **Build output location**:
  ```
  src/bin/Release/net8.0/linux-arm64/publish/
  ```

- **Main executable**:
  ```
  src/bin/Release/net8.0/linux-arm64/publish/InstDotNet
  ```

## Yocto Recipe Configuration

### For Main Branch

When building from the main branch, Yocto recipes should:

1. **Build command**:
   ```bash
   dotnet publish InstDotNet.csproj \
       -c Release \
       -r linux-arm64 \
       --self-contained true \
       -p:PublishSingleFile=true \
       -p:IncludeNativeLibrariesForSelfExtract=true \
       -o ${WORKDIR}/build/bin/Release/net8.0/linux-arm64/publish
   ```

2. **Copy artifacts** from:
   ```
  ${WORKDIR}/build/bin/Release/net8.0/linux-arm64/publish/
  ```

3. **Install executable** to target:
   ```
  ${D}${bindir}/InstDotNet
  ```

### For Development Branch

When building from the ajl/development branch:

1. **Build command**:
   ```bash
   dotnet publish src/InstDotNet.csproj \
       -c Release \
       -r linux-arm64 \
       --self-contained true \
       -p:PublishSingleFile=true \
       -p:IncludeNativeLibrariesForSelfExtract=true \
       -o ${WORKDIR}/build/src/bin/Release/net8.0/linux-arm64/publish
   ```

2. **Copy artifacts** from:
   ```
  ${WORKDIR}/build/src/bin/Release/net8.0/linux-arm64/publish/
  ```

## Key Differences

| Aspect | Main Branch | Development Branch |
|--------|-------------|-------------------|
| Project file | `InstDotNet.csproj` (root) | `src/InstDotNet.csproj` |
| Build directory | `bin/Release/...` | `src/bin/Release/...` |
| Relative path | `bin/Release/net8.0/linux-arm64/publish/` | `src/bin/Release/net8.0/linux-arm64/publish/` |

## Dockerfile Reference

The Dockerfile in `/data_drive/dd/containers/cga-coordinate-mapping/Dockerfile` currently expects:
- Source: `src/InstDotNet.csproj` (development branch structure)
- Output: `/app/publish/InstDotNet`

This matches the development branch structure. If using the main branch, update the Dockerfile to:
- Source: `InstDotNet.csproj` (root)
- Output: `/app/publish/InstDotNet`

## Build Artifacts

Both branches produce the same artifacts:
- `InstDotNet` - Main executable (self-contained)
- `InstDotNet.deps.json` - Dependency manifest
- `InstDotNet.runtimeconfig.json` - Runtime configuration
- Native libraries (if not single-file): `libcoreclr.so`, `libclrjit.so`, `libclrgc.so`
- `createdump` - Core dump utility

## Notes

- The main branch uses a simpler, flatter directory structure
- The development branch uses a more organized `src/` structure
- Both produce identical executables, just from different source locations
- Yocto recipes need to account for the different source/build paths

