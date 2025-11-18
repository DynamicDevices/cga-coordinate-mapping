#!/bin/bash
# Build script for linux-arm64 target
# This script builds the application for Yocto/embedded Linux ARM64 targets
#
# Output location: bin/Release/net8.0/linux-arm64/publish/
# This matches the structure expected by Yocto recipes which typically
# copy artifacts from the build output directory.

set -e

echo "Building InstDotNet for linux-arm64..."
echo "Cleaning previous build artifacts..."

# Clean previous builds to avoid conflicts
dotnet clean InstDotNet.csproj -c Release 2>/dev/null || true
rm -rf bin/Release/net8.0/linux-arm64 obj/Release/net8.0/linux-arm64

echo "Building and publishing for linux-arm64..."

# Build and publish for linux-arm64 as self-contained
dotnet publish InstDotNet.csproj \
    -c Release \
    -r linux-arm64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o bin/Release/net8.0/linux-arm64/publish

echo ""
echo "Build completed successfully!"
echo ""
echo "Output location: bin/Release/net8.0/linux-arm64/publish/"
echo ""
echo "Build artifacts:"
ls -lh bin/Release/net8.0/linux-arm64/publish/ | grep -E "InstDotNet|\.so|createdump" || ls -lh bin/Release/net8.0/linux-arm64/publish/
echo ""
echo "Main executable: bin/Release/net8.0/linux-arm64/publish/InstDotNet"

