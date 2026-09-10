#!/bin/sh
# Records the fire evacuation drill and encodes it to Docs/demo.mp4.
#
# Unity renders a numbered frame sequence offline with a fixed timestep, so the result plays back
# at a steady 30 fps no matter how long each frame took to render. ffmpeg then encodes it.
set -e

UNITY="/Applications/Unity/Hub/Editor/6000.0.83f1/Unity.app/Contents/MacOS/Unity"
FFMPEG="${FFMPEG:-$HOME/.local/bin/ffmpeg}"
PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
FRAMES="$PROJECT/Build/DemoFrames"
LOGFILE="$PROJECT/Build/record-demo.log"
OUTPUT="$PROJECT/Docs/demo.mp4"

mkdir -p "$PROJECT/Build" "$PROJECT/Docs"

echo "Rendering frames..."
set +e
"$UNITY" -batchmode -projectPath "$PROJECT" \
  -runTests -testPlatform PlayMode \
  -testFilter "VRSim.Tests.DemoVideoCaptureTests" \
  -testResults "$PROJECT/Build/testresults-Demo.xml" -logFile "$LOGFILE"
STATUS=$?
set -e

if grep -q "error CS" "$LOGFILE"; then
  echo "COMPILE ERRORS:"
  grep "error CS" "$LOGFILE" | sort -u | head -20
  exit 1
fi

COUNT=$(ls -1 "$FRAMES"/frame_*.png 2>/dev/null | wc -l | tr -d ' ')
if [ "$COUNT" -eq 0 ]; then
  echo "No frames were rendered (unity exit $STATUS). Last log lines:"
  tail -20 "$LOGFILE"
  exit 1
fi
echo "Rendered $COUNT frames."

echo "Encoding..."
"$FFMPEG" -y -loglevel error \
  -framerate 30 -i "$FRAMES/frame_%05d.png" \
  -c:v libx264 -pix_fmt yuv420p -crf 20 -preset slow \
  -movflags +faststart \
  "$OUTPUT"

ls -lh "$OUTPUT"
echo "Done: $OUTPUT"
