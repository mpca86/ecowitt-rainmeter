#!/usr/bin/env python3
"""Stage the legacy Ecowitt Weather Local API alpha for a Rainmeter .rmskin.

This intentionally stages only specific main-branch source files. The output
uses a distinct Rainmeter root config, so installation cannot replace the
Cloud API beta EcowittWeather skin. No release is published by this script.
"""
from __future__ import annotations

import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "src" / "EcowittWeather"
OUT = ROOT / "build" / "local-rmskin"
SKIN = OUT / "Skins" / "EcowittWeatherLocalAlpha"

SOURCE_FILES = [
    "Settings/Settings.ini",
    "Meteo/Meteo.ini",
    "Diagnostics/Diagnostics.ini",
    "@Resources/Includes/Variables.inc",
    "@Resources/Scripts/json.lua",
    "@Resources/Scripts/ecowitt_parser.lua",
    "@Resources/Scripts/history.lua",
    "@Resources/Scripts/main.lua",
]

if OUT.exists():
    shutil.rmtree(OUT)
SKIN.mkdir(parents=True)
for rel in SOURCE_FILES:
    source = SRC / rel
    if not source.is_file():
        raise SystemExit(f"Required Local API source file missing: {rel}")
    dest = SKIN / rel
    dest.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(source, dest)

# Remove developer-specific defaults, not the config variable names or units.
variables = SKIN / "@Resources/Includes/Variables.inc"
raw = variables.read_bytes()
if raw.startswith(b"\xff\xfe") or raw.startswith(b"\xfe\xff"):
    codec = "utf-16"
elif raw.startswith(b"\xef\xbb\xbf"):
    codec = "utf-8-sig"
else:
    codec = "utf-8"
text = raw.decode(codec)
for name in [f"Channel{i}Label" for i in range(1, 9)] + ["GatewayLabel"]:
    updated, count = re.subn(
        rf"(?m)^{name}=.*$",
        f"{name}=",
        text,
    )
    if count != 1:
        raise SystemExit(f"Unexpected {name} occurrence count: {count}")
    text = updated
for name, value in [("EcowittHost", ""), ("EcowittPort", "80"), ("DebugParser", "0")]:
    text, count = re.subn(rf"(?m)^{name}=.*$", f"{name}={value}", text)
    if count != 1:
        raise SystemExit(f"Unexpected {name} count: {count}")
variables.write_bytes(text.encode(codec))

(SKIN / "@Resources/Diagnostics").mkdir(parents=True, exist_ok=True)
(SKIN / "@Resources/Diagnostics/README.txt").write_text(
    "Rainmeter Local API diagnostics are written here at runtime when DebugParser=1.\n",
    encoding="utf-8",
)
(SKIN / "README_LOCAL_ALPHA.txt").write_text(
    "Ecowitt Weather Local API v1.10.0-alpha - LEGACY ALPHA\n"
    "This is an older, no-longer-actively-developed Rainmeter source line.\n"
    "Active Rainmeter work moved to the newer Cloud API beta branch.\n"
    "The package is for legacy Local API testing and is not a stable release.\n"
    "The skin root is EcowittWeatherLocalAlpha to avoid overwriting Cloud beta.\n"
    "Start: Load EcowittWeatherLocalAlpha/Settings/Settings.ini in Rainmeter.\n"
    "Enter only your supported gateway IP or hostname and port (default 80).\n"
    "No Ecowitt cloud API key or MAC address is needed for Local API.\n"
    "Never forward the gateway HTTP endpoint to the public internet.\n",
    encoding="utf-8",
)
OUT.mkdir(parents=True, exist_ok=True)
(OUT / "RMSKIN.ini").write_text(
    "[rmskin]\n"
    "Name=Ecowitt Weather Local API Alpha\n"
    "Author=Martin Sturcel (mpca86)\n"
    "Version=1.10.0-alpha\n"
    "LoadType=Skin\n"
    "Load=EcowittWeatherLocalAlpha\\Settings\\Settings.ini\n"
    "MinimumRainmeter=4.5\n"
    "MinimumWindows=10.0\n"
    "MergeSkins=0\n",
    encoding="utf-8",
)

all_files = sorted(f.relative_to(OUT).as_posix() for f in OUT.rglob("*") if f.is_file())
expected = len(SOURCE_FILES) + 3
if len(all_files) != expected:
    raise SystemExit(f"Unexpected staged file count: {len(all_files)} != {expected}")
for name in all_files:
    if any(term in name.lower() for term in (
        "cloudsecrets", "uservariables", "meteo_history.csv", "ecowitt_debug.txt", ".ecowittconfig",
        "sample_gw3000", ".env", "token", "password"
    )):
        raise SystemExit(f"Unwanted private/runtime file in package: {name}")
for f in OUT.rglob("*"):
    if f.is_file():
        content = f.read_bytes()
        if b"Kancel" in content or b"Predaj" in content or b"Diel" in content:
            raise SystemExit(f"Unexpected personal developer labels remain in: {f}")

print("Staged Rainmeter Local API alpha package safely:")
for name in all_files:
    print(" -", name)
