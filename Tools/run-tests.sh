#!/bin/sh
# Runs the edit-mode suite headless and prints a one-line summary.
set -e
UNITY="/Applications/Unity/Hub/Editor/6000.0.83f1/Unity.app/Contents/MacOS/Unity"
PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
PLATFORM="${1:-EditMode}"
RESULTS="$PROJECT/Build/testresults-$PLATFORM.xml"
LOGFILE="$PROJECT/Build/testrun-$PLATFORM.log"
mkdir -p "$PROJECT/Build"

# Delete the previous results so a run that never starts cannot report stale ones as fresh.
rm -f "$RESULTS"

# Play-mode tests instantiate world-space canvases, which need a graphics device. Edit-mode
# tests do not, and run faster without one.
GRAPHICS="-nographics"
if [ "$PLATFORM" = "PlayMode" ]; then
  GRAPHICS=""
fi

set +e
# shellcheck disable=SC2086
"$UNITY" -batchmode $GRAPHICS -projectPath "$PROJECT" \
  -runTests -testPlatform "$PLATFORM" \
  -testResults "$RESULTS" -logFile "$LOGFILE"
STATUS=$?
set -e

if grep -q "error CS" "$LOGFILE"; then
  echo "COMPILE ERRORS:"
  grep "error CS" "$LOGFILE" | sort -u | head -20
  exit 1
fi

if grep -q "Multiple Unity instances cannot open the same project" "$LOGFILE"; then
  echo "RUN DID NOT START: another Unity instance holds the project lock."
  exit 1
fi

if [ ! -f "$RESULTS" ]; then
  echo "RUN DID NOT PRODUCE RESULTS (unity exit $STATUS). Last log lines:"
  tail -15 "$LOGFILE"
  exit 1
fi

python3 - "$RESULTS" <<'PY'
import sys, xml.etree.ElementTree as ET
root = ET.parse(sys.argv[1]).getroot()
print("result={} total={} passed={} failed={} skipped={}".format(
    root.get('result'), root.get('total'), root.get('passed'),
    root.get('failed'), root.get('skipped')))
for case in root.iter('test-case'):
    if case.get('result') != 'Passed':
        print("  {} -> {}".format(case.get('fullname'), case.get('result')))
        msg = case.find('.//message')
        if msg is not None and msg.text:
            print("    " + msg.text.strip().splitlines()[0])
PY
exit $STATUS
