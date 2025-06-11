#!/bin/bash

set -e

APP_NAME="FileTidy"
APP_VERSION="1.0.0"
RUNTIME="linux-x64"
PUBLISH_DIR="linux-publish"
PACKAGE_DIR="${APP_NAME}-${APP_VERSION}-linux"
ARCHIVE_NAME="${PACKAGE_DIR}.tar.gz"

echo "▶ Cleaning previous builds..."
rm -rf $PUBLISH_DIR $PACKAGE_DIR $ARCHIVE_NAME

echo "▶ Publishing .NET app for Linux..."
dotnet publish FileTidy.GUI/FileTidy.GUI.csproj \
  -c Release -r $RUNTIME \
  --self-contained true \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true \
  -o $PUBLISH_DIR

echo "▶ Creating package folder..."
mkdir -p $PACKAGE_DIR
cp -R $PUBLISH_DIR/* $PACKAGE_DIR/
chmod +x $PACKAGE_DIR/FileTidy.GUI
mv $PACKAGE_DIR/FileTidy.GUI $PACKAGE_DIR/FileTidy

#  add .desktop file for Linux launchers
echo "▶ Creating .desktop launcher..."
cat <<EOF > $PACKAGE_DIR/${APP_NAME}.desktop
[Desktop Entry]
Type=Application
Name=FileTidy
Exec=./FileTidy
Icon=filetidy
Terminal=false
Categories=Utility;
EOF

# include icon
cp macos-icon/FileTidy.icns $PACKAGE_DIR/filetidy.icns

echo "▶ Compressing to $ARCHIVE_NAME..."
tar -czf $ARCHIVE_NAME $PACKAGE_DIR

echo "✅ Done: $ARCHIVE_NAME"
