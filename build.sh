#!/bin/bash

# Build script for PyMacInstall

echo "Building PyMacInstall for macOS..."

# Restore dependencies
echo "Restoring dependencies..."
dotnet restore

# Build for debug
echo "Building debug version..."
dotnet build -c Debug

echo "Build complete!"
echo "Run with: dotnet run"
echo ""
echo "To build for release:"
echo "  dotnet publish -f net8.0-maccatalyst -c Release -p:CreatePackage=true"
echo ""
echo "The release .app will be in:"
echo "  bin/Release/net8.0-maccatalyst/maccatalyst-arm64/PyMacInstall.app"
