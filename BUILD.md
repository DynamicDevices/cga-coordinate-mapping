# Build Instructions for linux-arm64

This document describes how to build InstDotNet for linux-arm64 targets, typically used with Yocto-based embedded Linux systems.

## Automated Builds

The project includes a GitHub Actions CI workflow (`.github/workflows/ci.yml`) that automatically builds linux-arm64 artifacts on every push to the main branch. The artifacts are available as GitHub Actions artifacts and can be downloaded from the Actions tab.

## Prerequisites

- .NET 8.0 SDK installed
- Linux ARM64 runtime available (usually installed with SDK)

## Building for linux-arm64

### Using the Build Script

The simplest way to build for linux-arm64 is to use the provided build script:

```bash
./build-linux-arm64.sh
```

This script will:
1. Clean previous build artifacts
2. Build and publish the application as a self-contained single-file executable
3. Output artifacts to `bin/Release/net8.0/linux-arm64/publish/`

### Manual Build

You can also build manually using dotnet CLI:

```bash
dotnet publish InstDotNet.csproj \
    -c Release \
    -r linux-arm64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o bin/Release/net8.0/linux-arm64/publish
```

## Build Output Location

The build artifacts are placed in:
```
bin/Release/net8.0/linux-arm64/publish/
```

### Key Artifacts

- **InstDotNet** - Main executable (self-contained single file)
- **InstDotNet.deps.json** - Dependency manifest
- **InstDotNet.runtimeconfig.json** - Runtime configuration
- **libcoreclr.so**, **libclrjit.so**, **libclrgc.so** - .NET runtime libraries (if not single-file)
- **createdump** - Core dump utility

## Yocto Integration

For Yocto recipes, the build output location `bin/Release/net8.0/linux-arm64/publish/` should be used as the source for copying artifacts to the target filesystem.

### Comparison with Development Branch

- **Main branch**: Files are in root directory, output: `bin/Release/net8.0/linux-arm64/publish/`
- **Development branch**: Files are in `src/` directory, output: `src/bin/Release/net8.0/linux-arm64/publish/`

Yocto recipes should be updated to match the branch structure being used.

## Build Configuration

The project file (`InstDotNet.csproj`) is configured for:
- Target Framework: .NET 8.0
- Runtime: linux-arm64
- Self-contained: true (includes .NET runtime)
- Single-file: true (packs everything into one executable)
- Native libraries: Extracted automatically

## Troubleshooting

### Build Errors

If you encounter duplicate assembly attribute errors:
1. Clean build artifacts: `rm -rf obj bin`
2. Run `dotnet clean`
3. Rebuild

### Missing Runtime

If the linux-arm64 runtime is not available:
```bash
dotnet --list-runtimes
```

Install the runtime if needed:
```bash
# The runtime should be included with the SDK, but if missing:
# Follow .NET installation instructions for your platform
```

## Testing the Build

After building, you can verify the executable:
```bash
file bin/Release/net8.0/linux-arm64/publish/InstDotNet
# Should show: ELF 64-bit LSB executable, ARM aarch64

# Check dependencies (should be minimal for self-contained):
ldd bin/Release/net8.0/linux-arm64/publish/InstDotNet
```

