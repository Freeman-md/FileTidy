#!/bin/bash

set -e

APP_NAME="FileTidy"
RUNTIME="win-x64"

APP_VERSION=$(sed -n 's/.*<Version>\(.*\)<\/Version>.*/\1/p' Directory.Build.props)
if [ -z "$APP_VERSION" ]; then
  echo "❌ Failed to read version from Directory.Build.props"
  exit 1
fi

PUBLISH_DIR="windows-publish"
PACKAGE_DIR="${APP_NAME}-${APP_VERSION}-win"
ARCHIVE_NAME="${PACKAGE_DIR}.zip"

echo "▶ Cleaning previous builds..."
rm -rf $PUBLISH_DIR $PACKAGE_DIR $ARCHIVE_NAME

echo "▶ Publishing .NET app for Windows..."
dotnet publish FileTidy.GUI/FileTidy.GUI.csproj \
  -c Release -r $RUNTIME \
  --self-contained true \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true \
  -o $PUBLISH_DIR

echo "▶ Creating package folder..."
mkdir -p $PACKAGE_DIR
cp -R $PUBLISH_DIR/* $PACKAGE_DIR/

chmod +x $PACKAGE_DIR/FileTidy.GUI.exe
mv $PACKAGE_DIR/FileTidy.GUI.exe $PACKAGE_DIR/FileTidy.exe

echo "▶ Zipping package..."
zip -r $ARCHIVE_NAME $PACKAGE_DIR > /dev/null

echo "✅ Done: $ARCHIVE_NAME"
