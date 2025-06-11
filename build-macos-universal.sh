#!/bin/bash

set -e

APP_NAME="FileTidy"

APP_VERSION=$(sed -n 's/.*<Version>\(.*\)<\/Version>.*/\1/p' Directory.Build.props)
if [ -z "$APP_VERSION" ]; then
  echo "❌ Failed to read version from Directory.Build.props"
  exit 1
fi

UNIVERSAL_DIR="macos-publish/universal"
ARM_DIR="macos-publish/arm64"
X64_DIR="macos-publish/x64"
DMG_TEMP="FileTidyDMG"
DMG_NAME="${APP_NAME}-${APP_VERSION}.dmg"
APP_BUNDLE="${APP_NAME}.app"

echo "▶ Cleaning previous builds..."
rm -rf $ARM_DIR $X64_DIR $UNIVERSAL_DIR $APP_BUNDLE $DMG_TEMP $DMG_NAME
find . -maxdepth 1 -name "rw.*.${APP_NAME}.dmg" -delete

mkdir -p $ARM_DIR $X64_DIR $UNIVERSAL_DIR

echo "▶ Publishing for arm64..."
dotnet publish FileTidy.GUI/FileTidy.GUI.csproj \
  -c Release -r osx-arm64 \
  --self-contained true \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true \
  -o $ARM_DIR

echo "▶ Publishing for x64..."
dotnet publish FileTidy.GUI/FileTidy.GUI.csproj \
  -c Release -r osx-x64 \
  --self-contained true \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true \
  -o $X64_DIR

echo "▶ Creating fat binary..."
lipo -create \
  $ARM_DIR/FileTidy.GUI \
  $X64_DIR/FileTidy.GUI \
  -output $UNIVERSAL_DIR/$APP_NAME

chmod +x $UNIVERSAL_DIR/$APP_NAME

echo "▶ Creating .app bundle..."
mkdir -p $APP_BUNDLE/Contents/{MacOS,Resources}
cp $UNIVERSAL_DIR/$APP_NAME $APP_BUNDLE/Contents/MacOS/
cp -R $ARM_DIR/Data $APP_BUNDLE/Contents/MacOS/
cp $ARM_DIR/*.dylib $APP_BUNDLE/Contents/MacOS/
cp macos-icon/FileTidy.icns $APP_BUNDLE/Contents/Resources/
cp macos-icon/Info.plist $APP_BUNDLE/Contents/

echo "▶ Creating DMG folder..."
mkdir -p $DMG_TEMP
cp -R $APP_BUNDLE $DMG_TEMP/

echo "▶ Building DMG..."
create-dmg \
  --volname "$APP_NAME" \
  --window-size 540 380 \
  --icon-size 100 \
  --background "FileTidyDMG-bg.png" \
  --icon "$APP_NAME.app" 100 120 \
  --icon "Applications" 380 120 \
  --hide-extension "$APP_NAME.app" \
  --app-drop-link 380 120 \
  "$DMG_NAME" \
  $DMG_TEMP/


# Unmount any leftover volumes from previous runs
MOUNTED_DEV=$(hdiutil info | grep "/Volumes/$APP_NAME" | awk '{ print $1 }')
if [ -n "$MOUNTED_DEV" ]; then
  echo "▶ Unmounting leftover volume..."
  hdiutil detach "$MOUNTED_DEV" -quiet || true
fi

echo "✅ Done: $DMG_NAME"
