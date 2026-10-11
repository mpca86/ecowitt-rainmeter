#!/usr/bin/env python3
"""Normalize Rainmeter's Unicode-sensitive Cloud API Lua script in a staged skin.

Git tracks readable UTF-8 source. Rainmeter's Lua ScriptFile requires UTF-16 LE
with a BOM whenever Lua or the skin passes non-ASCII characters through Lua.
Never run this on the source tree: run it on a copied/staged skin directory.
"""
from __future__ import annotations

import argparse
from pathlib import Path

UTF16_LE_BOM = b"\xff\xfe"
TARGET = Path("@Resources") / "Scripts" / "main_cloud.lua"


def normalize_file(path: Path) -> None:
    data = path.read_bytes()
    if data.startswith(UTF16_LE_BOM):
        text = data[2:].decode("utf-16-le", errors="strict")
    elif data.startswith(b"\xfe\xff"):
        raise ValueError(f"Unsupported UTF-16 BE: {path}")
    else:
        text = data.decode("utf-8-sig", errors="strict")

    if "function Update()" not in text or "function Initialize()" not in text:
        raise ValueError(f"Not an expected Rainmeter Cloud Lua script: {path}")

    expected = UTF16_LE_BOM + text.encode("utf-16-le")
    if data != expected:
        path.write_bytes(expected)

    actual = path.read_bytes()
    if actual != expected or not actual.startswith(UTF16_LE_BOM):
        raise ValueError(f"Staging failed Unicode encoding verification: {path}")
    if actual[2:].decode("utf-16-le") != text:
        raise ValueError(f"UTF-16 round-trip failed: {path}")

    print(f"PASS: {path} is UTF-16 LE with BOM; Unicode round-trip verified")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("skin_root", type=Path, help="Staged Rainmeter skin root")
    args = parser.parse_args()
    normalize_file(args.skin_root / TARGET)


if __name__ == "__main__":
    main()
