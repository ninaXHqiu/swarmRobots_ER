#!/bin/bash
# Builds the routine simulator for the browser (only when scripts changed since the last
# build) and serves it at http://localhost:8002, then opens it in Chrome.
# First build takes ~10 min, later builds a few minutes. Ctrl-C stops the server.
#   tools/run-simulator.sh            build if needed, then serve
#   tools/run-simulator.sh --rebuild  always build first
set -e
cd "$(dirname "$0")/.."

UNITY="$HOME/Unity/Editors/2021.3.26f1/Unity.app/Contents/MacOS/Unity"
LOG="$HOME/Unity/build.log"
PORT=8002

if [ ! -x "$UNITY" ]; then
  echo "Unity not found. Run tools/first-time-setup.sh first."
  exit 1
fi

needs_build=no
if [ "$1" = "--rebuild" ] || [ ! -f SimPreview/index.html ]; then
  needs_build=yes
elif [ -n "$(find Assets -name '*.cs' -newer SimPreview/index.html | head -1)" ]; then
  needs_build=yes
fi

if [ "$needs_build" = yes ]; then
  echo "Building the simulator (first build ~10 min, later a few). Log: $LOG"
  if ! "$UNITY" -batchmode -nographics -projectPath "$PWD" -executeMethod WebBuild.BuildSimulator -logFile "$LOG"; then
    if grep -q "No valid Unity Editor license" "$LOG"; then
      echo "Unity has no licence yet: open Unity Hub, sign in, Preferences > Licenses > Add > free personal license."
    else
      echo "Build failed. Errors:"
      grep -E "error CS[0-9]+" "$LOG" | sort -u | head -20
    fi
    exit 1
  fi
fi

cd SimPreview
python3 -m http.server "$PORT" >/dev/null 2>&1 &
SERVER=$!
trap 'kill $SERVER 2>/dev/null' EXIT
sleep 1
URL="http://localhost:$PORT/"
open -a "Google Chrome" "$URL" 2>/dev/null || echo "Open $URL in Chrome"
echo "Simulator running at $URL  (Ctrl-C to stop)"
wait $SERVER
