#!/bin/bash
set -euo pipefail
project_dir="$(cd "$(dirname "$0")/.." && pwd)"
app_path="${1:-$project_dir/outputs/Sunward.app}"
dmg_path="${2:-$project_dir/outputs/Sunward.dmg}"
[[ -d "$app_path/Contents" ]] || { echo "Build Sunward.app first: $app_path" >&2; exit 1; }
mkdir -p "$project_dir/work" "$(dirname "$dmg_path")"
stage_dir="$(mktemp -d "$project_dir/work/dmg.XXXXXX")"
trap 'rm -rf "$stage_dir"' EXIT
ditto "$app_path" "$stage_dir/Sunward.app"
ln -s /Applications "$stage_dir/Applications"
mkdir "$stage_dir/Licenses"
cp "$project_dir/LICENSE" "$stage_dir/Licenses/Sunward-MIT.txt"
cp "$project_dir/THIRD_PARTY_NOTICES.md" "$stage_dir/Licenses/THIRD_PARTY_NOTICES.md"
cp "$project_dir/ThirdParty/"* "$stage_dir/Licenses/"
printf '%s\n' 'SUNWARD — A RACING GAME BY GPT-6 ASTRA' '' 'Drag Sunward to Applications, then launch it.' 'This release: Apple Silicon Mac, macOS 12 or later.' 'P: Photo Mode. Space: save photo. Esc: pause.' '' 'https://github.com/yjrocks712/Sunward-by-GPT-6-Astra' 'See the release notes for controls and first-launch signing limitations.' '' '— Gpt-6 Astra' > "$stage_dir/READ ME.txt"
hdiutil create -ov -volname Sunward -srcfolder "$stage_dir" -fs HFS+ -format UDZO -imagekey zlib-level=9 "$dmg_path"
hdiutil verify "$dmg_path"
shasum -a 256 "$dmg_path"
