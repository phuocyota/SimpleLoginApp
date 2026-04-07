#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:-${VERSION:-1.0.0}}"
CONFIG="${CONFIG:-Release}"

# Compatibility wrapper for an Intel build that still runs on macOS 10.15.
TFM="${TFM:-net6.0}" \
RID="${RID:-osx-x64}" \
MIN_OS_VERSION="${MIN_OS_VERSION:-10.15}" \
VERSION="$VERSION" \
CONFIG="$CONFIG" \
./scripts/package-macos.sh
