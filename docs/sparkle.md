# Sparkle Integration

This repo now includes the packaging and release-side pieces needed for Sparkle on macOS:

- `scripts/package-macos.sh`
  - injects Sparkle keys into `Info.plist`
  - can embed `Sparkle.framework` and `Autoupdate.app` from `SPARKLE_DIR`
- `scripts/release.sh`
  - can generate `appcast.xml` when `SPARKLE_GENERATE_APPCAST` is provided
  - uploads the appcast alongside release archives

## What is still required

Sparkle does not become active only by embedding the framework.
The macOS app still needs a native Sparkle bridge that:

- creates a `SPUStandardUpdaterController`
- points Sparkle at the signed app bundle
- wires a "Check for Updates..." action from the app menu

For a pure Avalonia/.NET app, that bridge is typically implemented in a small native macOS host layer.

## Required environment variables

### Packaging

- `SPARKLE_DIR`
  Path containing `Sparkle.framework` and optionally `Autoupdate.app`
- `SPARKLE_FEED_URL`
  Public URL to `appcast.xml`
- `SPARKLE_PUBLIC_ED_KEY`
  Sparkle EdDSA public key
- `SPARKLE_AUTO_CHECKS`
  `true` or `false`
- `SPARKLE_AUTO_DOWNLOADS`
  `true` or `false`

### Release

- `SPARKLE_GENERATE_APPCAST`
  Path to Sparkle's `generate_appcast` binary
- `SPARKLE_ED_KEY_FILE`
  Path to the Sparkle private EdDSA key file

## Example

```bash
export SPARKLE_DIR="$HOME/Downloads/Sparkle-2.6.4/bin"
export SPARKLE_FEED_URL="https://github.com/phuocyota/SimpleLoginApp/releases/latest/download/appcast.xml"
export SPARKLE_PUBLIC_ED_KEY="YOUR_PUBLIC_ED25519_KEY"
export SPARKLE_GENERATE_APPCAST="$SPARKLE_DIR/generate_appcast"
export SPARKLE_ED_KEY_FILE="$HOME/.config/sparkle/ed25519_key"

./scripts/release.sh 1.0.0
```

## Signing and notarization

For production distribution on macOS, sign and notarize the `.app` before publishing archives.
Sparkle updates are expected to be distributed from signed and notarized builds.
