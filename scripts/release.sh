#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:-}"
if [ -z "$VERSION" ]; then
  echo "Usage: $0 <version>" >&2
  exit 1
fi

APP_NAME="Kido Teacher"
APP_SLUG="${APP_NAME// /-}"
ZIP_NAME="${APP_SLUG}-macos.zip"
ZIP_PATH="dist/macos/$ZIP_NAME"
REPO="phuocyota/SimpleLoginApp"
TAP="phuocyota/teacher"
CASK_NAME="kido-teacher"

if ! gh auth status -h github.com >/dev/null 2>&1; then
  echo "GitHub CLI not authenticated. Run: gh auth login" >&2
  exit 1
fi

if ! git diff --quiet || ! git diff --cached --quiet; then
  echo "Working tree is dirty. Commit or stash changes before releasing." >&2
  exit 1
fi

VERSION="$VERSION" ./scripts/package-macos.sh

if [ ! -f "$ZIP_PATH" ]; then
  echo "Zip not found: $ZIP_PATH" >&2
  exit 1
fi

SHA256="$(shasum -a 256 "$ZIP_PATH" | awk '{print $1}')"

git push origin HEAD

if gh release view "v$VERSION" -R "$REPO" >/dev/null 2>&1; then
  gh release upload "v$VERSION" "$ZIP_PATH" -R "$REPO" --clobber
else
  gh release create "v$VERSION" "$ZIP_PATH" -R "$REPO" -t "$APP_NAME $VERSION" -n "macOS build"
fi

TAP_DIR="$(brew --repo "$TAP" 2>/dev/null || true)"
if [ -z "$TAP_DIR" ]; then
  brew tap "$TAP"
  TAP_DIR="$(brew --repo "$TAP")"
fi

CASK_FILE="$TAP_DIR/Casks/$CASK_NAME.rb"
if [ ! -f "$CASK_FILE" ]; then
  echo "Cask not found: $CASK_FILE" >&2
  exit 1
fi

VERSION="$VERSION" SHA256="$SHA256" ZIP_NAME="$ZIP_NAME" CASK_FILE="$CASK_FILE" python3 - <<'PY'
import os
import re
from pathlib import Path

version = os.environ["VERSION"]
sha256 = os.environ["SHA256"]
zip_name = os.environ["ZIP_NAME"]

path = Path(os.environ["CASK_FILE"])
text = path.read_text()
text = re.sub(r'^\\s*version \\".*\\"', f'version \"{version}\"', text, flags=re.M)
text = re.sub(r'^\\s*sha256 \\".*\\"', f'sha256 \"{sha256}\"', text, flags=re.M)
url_line = f'url \"https://github.com/phuocyota/SimpleLoginApp/releases/download/v{version}/{zip_name}\"'
text = re.sub(r'^\\s*url \\".*\\"', url_line, text, flags=re.M)
path.write_text(text)
PY

git -C "$TAP_DIR" add "Casks/$CASK_NAME.rb"
git -C "$TAP_DIR" commit -m "Update $CASK_NAME to v$VERSION" || true
git -C "$TAP_DIR" push

echo "Release v$VERSION done."
echo "Install/upgrade: brew install --cask $CASK_NAME"
