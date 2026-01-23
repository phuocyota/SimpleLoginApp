#!/usr/bin/env bash
set -euo pipefail

APP_NAME="Kido Teacher"
BINARY_NAME="KidoTeacher"
APP_SLUG="${APP_NAME// /-}"
CONFIG="${CONFIG:-Release}"
RID="${RID:-osx-arm64}"
BUNDLE_ID="${BUNDLE_ID:-com.kido.teacher}"
VERSION="${VERSION:-1.0.0}"

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PUBLISH_DIR="$ROOT_DIR/bin/$CONFIG/net8.0/$RID/publish"
DIST_DIR="$ROOT_DIR/dist/macos"
APP_DIR="$DIST_DIR/$APP_NAME.app"
MACOS_DIR="$APP_DIR/Contents/MacOS"
RES_DIR="$APP_DIR/Contents/Resources"

dotnet publish "$ROOT_DIR/SimpleLoginApp.csproj" -c "$CONFIG" -r "$RID" --self-contained true /p:PublishSingleFile=false

rm -rf "$APP_DIR"
mkdir -p "$MACOS_DIR" "$RES_DIR"

cp -R "$PUBLISH_DIR"/. "$MACOS_DIR/"

if [ -f "$MACOS_DIR/$BINARY_NAME" ]; then
  chmod +x "$MACOS_DIR/$BINARY_NAME"
else
  echo "Executable not found: $MACOS_DIR/$BINARY_NAME" >&2
  exit 1
fi

ICON_SRC="$ROOT_DIR/Assets/logo4.jpg"
ICONSET_DIR="$ROOT_DIR/.tmp_icon.iconset"
ICON_ICNS="$RES_DIR/$APP_NAME.icns"

if command -v iconutil >/dev/null 2>&1 && command -v sips >/dev/null 2>&1 && [ -f "$ICON_SRC" ]; then
  rm -rf "$ICONSET_DIR"
  mkdir -p "$ICONSET_DIR"
  for size in 16 32 64 128 256 512; do
    sips -s format png -z "$size" "$size" "$ICON_SRC" --out "$ICONSET_DIR/icon_${size}x${size}.png" >/dev/null
    sips -s format png -z "$((size * 2))" "$((size * 2))" "$ICON_SRC" --out "$ICONSET_DIR/icon_${size}x${size}@2x.png" >/dev/null
  done
  iconutil -c icns "$ICONSET_DIR" -o "$ICON_ICNS"
  rm -rf "$ICONSET_DIR"
fi

cat > "$APP_DIR/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key><string>$APP_NAME</string>
  <key>CFBundleDisplayName</key><string>$APP_NAME</string>
  <key>CFBundleIdentifier</key><string>$BUNDLE_ID</string>
  <key>CFBundleVersion</key><string>$VERSION</string>
  <key>CFBundleShortVersionString</key><string>$VERSION</string>
  <key>CFBundleExecutable</key><string>$BINARY_NAME</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>LSMinimumSystemVersion</key><string>11.0</string>
  <key>CFBundleIconFile</key><string>$APP_NAME</string>
</dict>
</plist>
PLIST

mkdir -p "$DIST_DIR"
(
  cd "$DIST_DIR"
  ZIP_NAME="${APP_SLUG}-macos.zip"
  ditto -c -k --sequesterRsrc --keepParent "$APP_NAME.app" "$ZIP_NAME"
)

echo "Created: $DIST_DIR/$APP_NAME.app"
echo "Created: $DIST_DIR/${APP_SLUG}-macos.zip"
