#!/bin/bash
# First-time setup on macOS: installs Unity Hub and Unity 2021.3.26f1 with WebGL support.
# The editor goes into ~/Unity (one folder, easy to remove). Safe to run again; it skips
# whatever is already installed. Takes ~15-30 min the first time (about 8 GB download).
set -e

UNITY_VERSION=2021.3.26f1
CHANGESET=a16dc32e0ff2
EDITOR_DIR="$HOME/Unity/Editors"
HUB="/Applications/Unity Hub.app/Contents/MacOS/Unity Hub"
EDITOR="$EDITOR_DIR/$UNITY_VERSION/Unity.app"

if ! command -v brew >/dev/null; then
  echo "Homebrew is needed first. Install it from https://brew.sh, then run this again."
  exit 1
fi

if [ ! -d "/Applications/Unity Hub.app" ]; then
  echo "Installing Unity Hub..."
  brew install --cask unity-hub
fi

mkdir -p "$EDITOR_DIR"
"$HUB" -- --headless install-path -s "$EDITOR_DIR" >/dev/null

if [ ! -d "$EDITOR" ]; then
  echo "Installing Unity $UNITY_VERSION with WebGL support into $EDITOR_DIR (large download)..."
  "$HUB" -- --headless install --version "$UNITY_VERSION" --changeset "$CHANGESET" --module webgl
elif [ ! -d "$EDITOR_DIR/$UNITY_VERSION/PlaybackEngines/WebGLSupport" ]; then
  echo "Adding WebGL support to Unity $UNITY_VERSION..."
  "$HUB" -- --headless install-modules --version "$UNITY_VERSION" --module webgl
fi

echo
echo "Unity $UNITY_VERSION is installed in $EDITOR_DIR."
echo "One manual step left: open Unity Hub, sign in with a Unity account, then"
echo "  Preferences > Licenses > Add > Get a free personal license"
echo "After that, run: tools/run-simulator.sh"
