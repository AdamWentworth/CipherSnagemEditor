#!/usr/bin/env bash
set -euo pipefail

tool="${1:-Colosseum}"
runtime="${2:-linux-x64}"
package_version="${3:-0.1.12}"
configuration="${CONFIGURATION:-Release}"
output_root="${OUTPUT_ROOT:-artifacts}"

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

case "$tool" in
  Colosseum)
    tool_slug="colosseum-tool"
    app_name="Colosseum Tool"
    app_comment="Pokemon Colosseum modding editor"
    package_name="cipher-snagem-colosseum-tool"
    project_path="src/CipherSnagemEditor.ColosseumTool/CipherSnagemEditor.ColosseumTool.csproj"
    launcher_executable="ColosseumTool"
    icon_path="assets/ui/app-icons/colosseum/icon-256.png"
    ;;
  GoD)
    tool_slug="god-tool"
    app_name="GoD Tool"
    app_comment="Pokemon XD Gale of Darkness modding editor"
    package_name="cipher-snagem-god-tool"
    project_path="src/CipherSnagemEditor.GoDTool/CipherSnagemEditor.GoDTool.csproj"
    launcher_executable="GoDTool"
    icon_path="assets/ui/app-icons/xd/icon-32.png"
    ;;
  *)
    echo "Unknown tool: $tool" >&2
    exit 2
    ;;
esac

icon_name="$package_name"
publish_dir="$output_root/publish-$tool_slug-$runtime"
package_dir="$output_root/packages/$tool_slug-$runtime"
archive_path="$output_root/packages/$tool_slug-$runtime.tar.gz"
runtime_label="${runtime#linux-}"
archive_path="$output_root/packages/$tool_slug-linux-portable-$runtime_label.tar.gz"
deb_path="$output_root/packages/$tool_slug-ubuntu-debian-$runtime_label.deb"
archive_file="$(basename "$archive_path")"
deb_file="$(basename "$deb_path")"

rm -rf "$publish_dir" "$package_dir"
rm -f "$archive_path" "$deb_path"
mkdir -p "$output_root/packages"

dotnet publish "$project_path" \
  -c "$configuration" \
  -r "$runtime" \
  --self-contained true \
  -p:PublishReadyToRun=false \
  -o "$publish_dir"

mkdir -p "$package_dir"
cp -a "$publish_dir"/. "$package_dir"/
cp "packaging/linux/run-cipher-snagem-editor.sh" "$package_dir/"
cp "packaging/linux/install-linux-user.sh" "$package_dir/"
cp "packaging/linux/README-linux.txt" "$package_dir/"
cp "packaging/linux/cipher-snagem-editor.desktop" "$package_dir/$package_name.desktop"

export APP_NAME="$app_name"
export APP_COMMENT="$app_comment"
export APP_SLUG="$package_name"
export EXECUTABLE="$launcher_executable"
export ICON_NAME="$icon_name"
export DEB_FILE="$deb_file"
export ARCHIVE_FILE="$archive_file"
export PACKAGE_DIR="$tool_slug-$runtime"

for template in \
  "$package_dir/run-cipher-snagem-editor.sh" \
  "$package_dir/install-linux-user.sh" \
  "$package_dir/README-linux.txt" \
  "$package_dir/$package_name.desktop"
do
  perl -0pi -e '
    s/\@APP_NAME\@/$ENV{APP_NAME}/g;
    s/\@APP_COMMENT\@/$ENV{APP_COMMENT}/g;
    s/\@APP_SLUG\@/$ENV{APP_SLUG}/g;
    s/\@EXECUTABLE\@/$ENV{EXECUTABLE}/g;
    s/\@ICON_NAME\@/$ENV{ICON_NAME}/g;
    s/\@DEB_FILE\@/$ENV{DEB_FILE}/g;
    s/\@ARCHIVE_FILE\@/$ENV{ARCHIVE_FILE}/g;
    s/\@PACKAGE_DIR\@/$ENV{PACKAGE_DIR}/g;
  ' "$template"
done

mkdir -p "$package_dir/resources"
cp "$icon_path" "$package_dir/resources/$icon_name.png"

tar -czf "$archive_path" -C "$output_root/packages" "$tool_slug-$runtime"

python scripts/create-linux-deb.py \
  --package-dir "$package_dir" \
  --output "$deb_path" \
  --runtime "$runtime" \
  --version "$package_version" \
  --package-name "$package_name" \
  --install-root "/opt/$package_name" \
  --app-name "$app_name" \
  --comment "$app_comment" \
  --icon-name "$icon_name" \
  --executable "$launcher_executable"

echo "Linux publish complete."
echo "Tool: $tool"
echo "Runtime: $runtime"
echo "Package directory: $package_dir"
echo "Archive: $archive_path"
echo "Ubuntu package: $deb_path"
