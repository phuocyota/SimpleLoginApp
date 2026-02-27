#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:-${VERSION:-1.0.0}}"
CONFIG="${CONFIG:-Release}"

# Legacy Intel build for older macOS.
TFM="${TFM:-net6.0}" \
RID="${RID:-osx-x64}" \
MIN_OS_VERSION="${MIN_OS_VERSION:-10.15}" \
VERSION="$VERSION" \
CONFIG="$CONFIG" \
./scripts/package-macos.sh
