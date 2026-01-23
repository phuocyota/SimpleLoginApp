#!/usr/bin/env bash
set -euo pipefail

APP_NAME="Kido Teacher"
BINARY_NAME="KidoTeacher"
APP_SLUG="${APP_NAME// /-}"
CONFIG="${CONFIG:-Release}"
BUNDLE_ID="${BUNDLE_ID:-com.kido.teacher}"
VERSION="${VERSION:-1.0.0}"

# Build both Apple Silicon + Intel by default
RIDS_DEFAULT=("osx-arm64" "osx-x64")
# If user sets RID env, only build that one
if [[ "${RID:-}" != "" ]]; then
  RIDS=("$RID")
else
  RIDS=("${RIDS_DEFAULT[@]}")
fi

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT_DIR/SimpleLoginApp.csproj"

DIST_ROOT="$ROOT_DIR/dist/macos"
ICON_SRC="$ROOT_DIR/Assets/logo4.jpg"

make_icns () {
  local app_dir="$1"
  local res_dir="$app_dir/Contents/Resources"
  local iconset_dir="$ROOT_DIR/.tmp_icon.iconset"
  local icon_icns="$res_dir/$APP_NAME.icns"

  mkdir -p "$res_dir"

  if command -v iconutil >/dev/null 2>&1 \
    && command -v sips >/dev/null 2>&1 \
    && [[ -f "$ICON_SRC" ]]; then

    rm -rf "$iconset_dir"
    mkdir -p "$iconset_dir"
    for size in 16 32 64 128 256 512; do
      sips -s format png -z "$size" "$size" "$ICON_SRC" --out "$iconset_dir/icon_${size}x${size}.png" >/dev/null
      sips -s format png -z "$((size * 2))" "$((size * 2))" "$ICON_SRC" --out "$iconset_dir/icon_${size}x${size}@2x.png" >/dev/null
    done
    iconutil -c icns "$iconset_dir" -o "$icon_icns"
    rm -rf "$iconset_dir"
  fi
}

write_plist () {
  local app_dir="$1"
  cat > "$app_dir/Contents/Info.plist" <<PLIST
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
}

package_one () {
  local rid="$1"

  echo "==> Publishing for $rid ..."
  dotnet publish "$PROJECT" -c "$CONFIG" -r "$rid" --self-contained true /p:PublishSingleFile=false

  # Avalonia output folder pattern:
  local publish_dir="$ROOT_DIR/bin/$CONFIG/net8.0/$rid/publish"

  local arch_label
  if [[ "$rid" == *"arm64"* ]]; then
    arch_label="arm64"
  else
    arch_label="x64"
  fi

  # Create separate output per arch so they don't overwrite each other
  local dist_dir="$DIST_ROOT/$arch_label"
  local app_dir="$dist_dir/$APP_NAME.app"
  local macos_dir="$app_dir/Contents/MacOS"
  local res_dir="$app_dir/Contents/Resources"

  rm -rf "$app_dir"
  mkdir -p "$macos_dir" "$res_dir"

  # Copy publish output into .app
  cp -R "$publish_dir"/. "$macos_dir/"

  if [[ -f "$macos_dir/$BINARY_NAME" ]]; then
    chmod +x "$macos_dir/$BINARY_NAME"
  else
    echo "Executable not found: $macos_dir/$BINARY_NAME" >&2
    echo "Check BINARY_NAME and publish output content." >&2
    exit 1
  fi

  make_icns "$app_dir"
  write_plist "$app_dir"

  # Zip with ditto (best for mac apps)
  mkdir -p "$dist_dir"
  (
    cd "$dist_dir"
    local zip_name="${APP_SLUG}-macos-${arch_label}.zip"
    ditto -c -k --sequesterRsrc --keepParent "$(basename "$app_dir")" "$zip_name"
    echo "Created: $dist_dir/$zip_name"
  )

  echo "Created: $app_dir"
  echo
}

main () {
  rm -rf "$DIST_ROOT"
  mkdir -p "$DIST_ROOT"

  for rid in "${RIDS[@]}"; do
    package_one "$rid"
  done

  echo "✅ Done. Output in: $DIST_ROOT"
}

main
