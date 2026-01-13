#!/bin/bash

# Build and package script for PyMacInstall

echo "Building PyMacInstall for distribution..."

# Clean previous builds
echo "Cleaning previous builds..."
dotnet clean

# Restore dependencies
echo "Restoring dependencies..."
dotnet restore

# Build for release
echo "Building release version..."
dotnet publish -f net8.0-maccatalyst -c Release -p:CreatePackage=true

if [ $? -eq 0 ]; then
    echo ""
    echo "Build successful!"
    echo ""
    echo "Application bundle location:"
    echo "  bin/Release/net8.0-maccatalyst/maccatalyst-arm64/PyMacInstall.app"
    echo ""
    echo "To install to Applications folder:"
    echo "  cp -r bin/Release/net8.0-maccatalyst/maccatalyst-arm64/PyMacInstall.app ~/Applications/"
    echo ""
    echo "To create a DMG for distribution:"
    echo "  hdiutil create -volname PyMacInstall -srcfolder bin/Release/net8.0-maccatalyst/maccatalyst-arm64/PyMacInstall.app -ov -format UDZO PyMacInstall.dmg"
else
    echo ""
    echo "Build failed!"
    exit 1
fi
