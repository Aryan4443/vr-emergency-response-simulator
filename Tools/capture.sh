#!/bin/sh
# Plays the fire evacuation drill and writes screenshots to Build/Capture.
set -e
UNITY="/Applications/Unity/Hub/Editor/6000.0.83f1/Unity.app/Contents/MacOS/Unity"
PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
LOGFILE="$PROJECT/Build/capture.log"
mkdir -p "$PROJECT/Build"
rm -rf "$PROJECT/Build/Capture"

set +e
"$UNITY" -batchmode -projectPath "$PROJECT" \
  -runTests -testPlatform PlayMode \
  -testFilter "VRSim.Tests.SceneCaptureTests" \
  -testResults "$PROJECT/Build/testresults-Capture.xml" -logFile "$LOGFILE"
STATUS=$?
set -e

grep -E "error CS|\[Capture\]" "$LOGFILE" | sort -u
ls -1 "$PROJECT/Build/Capture" 2>/dev/null
exit $STATUS
