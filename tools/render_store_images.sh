#!/usr/bin/env bash
# Renders the Asset Store marketing images from docs/asset-store/images-src/*.html with headless Chrome.
#   bash tools/render_store_images.sh
# Output: docs/asset-store/images/*.png at the exact sizes the Publisher Portal asks for.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SRC="$ROOT/docs/asset-store/images-src"
OUT="$ROOT/docs/asset-store/images"
CHROME="${CHROME:-/c/Program Files/Google/Chrome/Application/chrome.exe}"
PROFILE="$(mktemp -d)"
trap 'rm -rf "$PROFILE"' EXIT
mkdir -p "$OUT"

to_url() { echo "file:///$(cygpath -m "$1")"; }

render() { # name width height
  local page="$SRC/$1.html" png="$OUT/$1.png"
  "$CHROME" --headless=new --disable-gpu --hide-scrollbars --force-device-scale-factor=1 \
    --user-data-dir="$(cygpath -w "$PROFILE")" --window-size="$2,$3" \
    --screenshot="$(cygpath -w "$png")" "$(to_url "$page")" >/dev/null 2>&1
  echo "$1.png ($2x$3)"
}

render icon 160 160
render card 420 280
render cover 1950 1300
render social 1200 630
render shot-results 1920 1080
render shot-setup 1920 1080
render publisher-banner 1950 1300

# Offline manual (the Asset Store asks for PDF/RTF documentation when a package contains code).
MANUAL_SRC="$ROOT/docs/asset-store/manual-src/manual.html"
MANUAL_PDF="$ROOT/docs/asset-store/manual/LocaSync Glyph Scanner Manual.pdf"
mkdir -p "$(dirname "$MANUAL_PDF")"
"$CHROME" --headless=new --disable-gpu --no-pdf-header-footer \
  --user-data-dir="$(cygpath -w "$PROFILE")" \
  --print-to-pdf="$(cygpath -w "$MANUAL_PDF")" "$(to_url "$MANUAL_SRC")" >/dev/null 2>&1
echo "manual: $(basename "$MANUAL_PDF")"
