BASE="$1"
REMOTE="$2"
LOCAL="$3"
MERGED="$4"
BASEDIR=$(dirname "$0")
cd "$BASEDIR"
./UnityMergeTool merge "$BASE" "$LOCAL" "$REMOTE" "$MERGED" > log.txt