#!/bin/sh
# Renders the earthquake level to Build/CaptureEarthquake.
set -e
UNITY="/Applications/Unity/Hub/Editor/6000.0.83f1/Unity.app/Contents/MacOS/Unity"
PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
LOGFILE="$PROJECT/Build/capture-earthquake.log"
mkdir -p "$PROJECT/Build"
rm -rf "$PROJECT/Build/CaptureEarthquake"

set +e
"$UNITY" -batchmode -projectPath "$PROJECT" \
  -runTests -testPlatform PlayMode \
  -testFilter "VRSim.Tests.EarthquakeSceneCaptureTests" \
  -testResults "$PROJECT/Build/testresults-CaptureEarthquake.xml" -logFile "$LOGFILE"
STATUS=$?
set -e

grep -E "error CS|\[Capture\]" "$LOGFILE" | sort -u
ls -1 "$PROJECT/Build/CaptureEarthquake" 2>/dev/null
exit $STATUS
