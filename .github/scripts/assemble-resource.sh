#!/usr/bin/env bash
#
# Assembles the shippable FiveM resources from the monorepo build outputs.
#
# Prerequisite: `pnpm build` has already run, so the following exist:
#   apps/client-script/dist/*        (managed client assemblies)
#   apps/server-script/dist/*        (managed server assemblies + pdb)
#   apps/user-interface/dist/index.html   (NUI page -> client/index.html / ui_page)
#   apps/video-player/dist/index.html     (DUI page -> wwwroot/index.html)
#
# Usage: .github/scripts/assemble-resource.sh <version>
#   <version>  e.g. 2.0.0 - used only for the archive name.
#
# Output:
#   dist/hypnonema/            assembled main resource
#   dist/hypnonema-map/        assembled example map resource
#   dist/hypnonema-<version>.zip   both of the above, ready as a release asset

set -euo pipefail

VERSION="${1:-0.0.0}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

OUT="dist"
RES="$OUT/hypnonema"
MAP="$OUT/hypnonema-map"

rm -rf "$OUT"
mkdir -p "$RES/client" "$RES/server" "$RES/wwwroot" "$RES/config" "$RES/stream"

echo "==> main resource"

# Manifest + top-level files - stamp the release version into the manifest
sed -E "s/^([[:space:]]*version[[:space:]]+)'[^']*'/\1'${VERSION}'/" fxmanifest.lua > "$RES/fxmanifest.lua"
cp README.md LICENSE.md "$RES/"
cp config/permissions.cfg "$RES/"          # README documents `exec @hypnonema/permissions.cfg`

# Runtime configuration (server reads <resource>/config/*.yaml)
cp config/config.yaml config/models.yaml config/screens.yaml "$RES/config/"

# Scaleform / texture-renderer stream assets (server reads <resource>/stream)
cp assets/stream/. "$RES/stream/" -r

# Managed assemblies
cp apps/client-script/dist/. "$RES/client/" -r
cp apps/server-script/dist/. "$RES/server/" -r
rm -f "$RES"/client/CitizenFX.Core.Client.dll "$RES"/server/CitizenFX.Core.Server.dll

# Web UIs (single-file bundles)
cp apps/user-interface/dist/index.html "$RES/client/index.html"
cp apps/video-player/dist/index.html "$RES/wwwroot/index.html"

echo "==> example map resource"
mkdir -p "$MAP"
cp assets/hypnonema-map/. "$MAP/" -r

echo "==> archive"
(
  cd "$OUT"
  if command -v 7z >/dev/null; then
    7z a -bd -tzip "hypnonema-${VERSION}.zip" "hypnonema" "hypnonema-map" >/dev/null
  elif command -v zip >/dev/null; then
    zip -qr "hypnonema-${VERSION}.zip" "hypnonema" "hypnonema-map"
  else
    powershell -NoProfile -Command "Compress-Archive -Path hypnonema,hypnonema-map -DestinationPath hypnonema-${VERSION}.zip -Force"
  fi
)

echo "==> done: $OUT/hypnonema-${VERSION}.zip"
find "$RES" "$MAP" -type f | sort
