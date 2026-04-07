#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:-}"
if [ -z "$VERSION" ]; then
  echo "Usage: $0 <version>" >&2
  exit 1
fi

APP_NAME="Kido Teacher"
APP_SLUG="${APP_NAME// /-}"
REPO="phuocyota/SimpleLoginApp"
TAP="phuocyota/teacher"
CASK_NAME="kido-teacher"

# ---- Helpers ----
die() { echo "$*" >&2; exit 1; }

if ! command -v gh >/dev/null 2>&1; then
  die "Missing 'gh' (GitHub CLI). Install then run: gh auth login"
fi

if ! gh auth status -h github.com >/dev/null 2>&1; then
  die "GitHub CLI not authenticated. Run: gh auth login"
fi

if ! command -v brew >/dev/null 2>&1; then
  die "Missing 'brew' (Homebrew). Install Homebrew first."
fi

# Check working tree clean (includes untracked files too)
if [ -n "$(git status --porcelain)" ]; then
  die "Working tree is dirty (including untracked). Commit/stash/clean before releasing."
fi

# ---- Build/Package ----
VERSION="$VERSION" ./scripts/package-macos.sh

ZIP_ARM="dist/macos/arm64/${APP_SLUG}-macos-arm64.zip"
ZIP_INTEL="dist/macos/x64/${APP_SLUG}-macos-x64.zip"

[ -f "$ZIP_ARM" ] || die "Zip not found: $ZIP_ARM"
[ -f "$ZIP_INTEL" ] || die "Zip not found: $ZIP_INTEL"

SHA_ARM="$(shasum -a 256 "$ZIP_ARM" | awk '{print $1}')"
SHA_INTEL="$(shasum -a 256 "$ZIP_INTEL" | awk '{print $1}')"
echo "Built: $ZIP_ARM"
echo "SHA256 (arm64): $SHA_ARM"
echo "Built: $ZIP_INTEL"
echo "SHA256 (x64): $SHA_INTEL"

# ---- Push source + tag ----
git push origin HEAD

# Ensure annotated tag exists then push it
if git rev-parse "v$VERSION" >/dev/null 2>&1; then
  echo "Tag v$VERSION already exists."
else
  git tag -a "v$VERSION" -m "Release v$VERSION"
fi
git push origin "v$VERSION"

# ---- GitHub Release ----
if gh release view "v$VERSION" -R "$REPO" >/dev/null 2>&1; then
  gh release upload "v$VERSION" "$ZIP_ARM" "$ZIP_INTEL" -R "$REPO" --clobber
else
  gh release create "v$VERSION" "$ZIP_ARM" "$ZIP_INTEL" -R "$REPO" \
    -t "$APP_NAME $VERSION" \
    -n "macOS build"
fi

# ---- Update Homebrew Tap Cask ----
TAP_DIR="$(brew --repo "$TAP" 2>/dev/null || true)"
if [ -z "$TAP_DIR" ]; then
  brew tap "$TAP"
  TAP_DIR="$(brew --repo "$TAP")"
fi

CASK_FILE="$TAP_DIR/Casks/$CASK_NAME.rb"
[ -f "$CASK_FILE" ] || die "Cask not found: $CASK_FILE"

VERSION="$VERSION" SHA_ARM="$SHA_ARM" SHA_INTEL="$SHA_INTEL" APP_SLUG="$APP_SLUG" CASK_FILE="$CASK_FILE" python3 - <<'PY'
import os
from pathlib import Path

version = os.environ["VERSION"]
sha_arm = os.environ["SHA_ARM"]
sha_intel = os.environ["SHA_INTEL"]
app_slug = os.environ["APP_SLUG"]

path = Path(os.environ["CASK_FILE"])

content = f'''cask "kido-teacher" do
  version "{version}"

  on_arm do
    sha256 "{sha_arm}"
    url "https://github.com/phuocyota/SimpleLoginApp/releases/download/v#{{version}}/{app_slug}-macos-arm64.zip"
  end

  on_intel do
    sha256 "{sha_intel}"
    url "https://github.com/phuocyota/SimpleLoginApp/releases/download/v#{{version}}/{app_slug}-macos-x64.zip"
  end

  name "Kido Teacher"
  desc "Kido Teacher desktop app"
  homepage "https://github.com/phuocyota/SimpleLoginApp"

  app "Kido Teacher.app"
end
'''

path.write_text(content, encoding="utf-8")
PY

git -C "$TAP_DIR" add "Casks/$CASK_NAME.rb"
git -C "$TAP_DIR" commit -m "Update $CASK_NAME to v$VERSION" || true
git -C "$TAP_DIR" push

echo "Release v$VERSION done."
echo "Install/upgrade: brew install --cask $CASK_NAME"
