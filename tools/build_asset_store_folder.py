"""Builds the Asset Store version of the package as a plain folder inside the 2022.3 upload project.

    python -I tools/build_asset_store_folder.py

Source:  com.locasync.glyph-scanner/   (the UPM package, the single source of truth)
Output:  AssetStoreUpload/Assets/LocaSync Glyph Scanner/
           Editor/          code and asmdefs, with the package's own .meta files (same GUIDs as the UPM package)
           Samples/         the sample CSV, from Samples~/ (folders ending in ~ are hidden from Unity)
           Documentation/   README.md and CHANGELOG.md

Tests and package.json are left out. LICENSE.md is left out as well: the Asset Store version is distributed
under the Asset Store EULA, while the GitHub version is MIT.

Files that have no .meta in the source (the sample, the Documentation copies, new folders) get a .meta with a
GUID derived from their path. The GUIDs then stay the same between builds, which keeps store updates from breaking
references in users' projects.
"""

import hashlib
import os
import shutil
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SOURCE = os.path.join(ROOT, "com.locasync.glyph-scanner")
OUTPUT_PARENT = os.path.join(ROOT, "AssetStoreUpload", "Assets")
FOLDER_NAME = "LocaSync Glyph Scanner"
OUTPUT = os.path.join(OUTPUT_PARENT, FOLDER_NAME)

# (source path relative to SOURCE, destination path relative to OUTPUT)
COPIES = [
    ("Editor", "Editor"),
    ("Samples~/SampleCsv", "Samples/Sample CSV"),
    ("README.md", "Documentation/README.md"),
    ("CHANGELOG.md", "Documentation/CHANGELOG.md"),
]

FOLDER_META = "fileFormatVersion: 2\nguid: {guid}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
TEXT_META = "fileFormatVersion: 2\nguid: {guid}\nTextScriptImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"


def stable_guid(relative_path):
    return hashlib.md5(("locasync-glyph-scanner/asset-store/" + relative_path.replace("\\", "/")).encode("utf-8")).hexdigest()


def ensure_meta(path, relative_path):
    meta = path + ".meta"
    if os.path.exists(meta):
        return
    template = FOLDER_META if os.path.isdir(path) else TEXT_META
    with open(meta, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(template.format(guid=stable_guid(relative_path)))


def main():
    if not os.path.isdir(SOURCE):
        sys.exit("Package source not found: " + SOURCE)

    if os.path.isdir(OUTPUT):
        shutil.rmtree(OUTPUT)
    if os.path.exists(OUTPUT + ".meta"):
        os.remove(OUTPUT + ".meta")
    os.makedirs(OUTPUT)

    for source_rel, dest_rel in COPIES:
        source = os.path.join(SOURCE, source_rel)
        dest = os.path.join(OUTPUT, dest_rel)
        os.makedirs(os.path.dirname(dest), exist_ok=True)
        if os.path.isdir(source):
            shutil.copytree(source, dest)
        else:
            shutil.copy2(source, dest)

    # Give every file and folder without a .meta a stable one, including the root folder itself.
    ensure_meta(OUTPUT, FOLDER_NAME)
    for directory, subdirs, files in os.walk(OUTPUT):
        for name in subdirs + files:
            if name.endswith(".meta"):
                continue
            path = os.path.join(directory, name)
            ensure_meta(path, os.path.relpath(path, OUTPUT_PARENT))

    longest = max(
        (os.path.relpath(os.path.join(d, f), os.path.dirname(OUTPUT_PARENT)) for d, _, fs in os.walk(OUTPUT) for f in fs),
        key=len,
    )
    count = sum(len([f for f in fs if not f.endswith(".meta")]) for _, _, fs in os.walk(OUTPUT))
    print("Built", OUTPUT)
    print(count, "files; longest path", len(longest), "chars:", longest)


if __name__ == "__main__":
    main()
