#!/usr/bin/env python3
"""Validate and compile Cloud API Rainmeter language catalogs.

Sources remain UTF-8 JSON in Git. Generated Rainmeter .inc files use
UTF-16 LE with BOM, so Slovak, Czech and Polish diacritics remain intact.
This tool DOES NOT edit the tracked Rainmeter source INI or user files.
"""
from __future__ import annotations

import argparse
import json
import re
import string
from pathlib import Path

LOCALES = ("sk", "en", "cs", "pl")
DEFAULT_LOCALE = "sk"
KEY_RE = re.compile(r"^[a-z][a-z0-9]*(?:[._][a-z0-9]+)*$")
PLACEHOLDER_RE = re.compile(r"^[a-z][a-z0-9_]*$")
FORMATTER = string.Formatter()


def tokens(text: str, key: str, locale: str) -> set[str]:
    found = set()
    try:
        items = FORMATTER.parse(text)
        for _literal, field, spec, conversion in items:
            if field is not None:
                if not PLACEHOLDER_RE.fullmatch(field) or spec or conversion:
                    raise ValueError(
                        f"{locale}:{key}: unsupported interpolation {{{field}}}"
                    )
                found.add(field)
    except ValueError as exc:
        raise ValueError(f"{locale}:{key}: invalid placeholders: {exc}") from exc
    return found


def load_catalogs(source: Path) -> dict[str, dict[str, str]]:
    catalogs = {}
    for locale in LOCALES:
        path = source / f"{locale}.json"
        content = path.read_text(encoding="utf-8-sig")
        # object_pairs_hook rejects duplicate JSON object keys.
        def unique_pairs(pairs: list[tuple[str, str]]) -> dict[str, str]:
            result = {}
            for k, v in pairs:
                if k in result:
                    raise ValueError(f"{path}: duplicate key {k}")
                result[k] = v
            return result

        values = json.loads(content, object_pairs_hook=unique_pairs)
        if not isinstance(values, dict):
            raise ValueError(f"{path}: expected a flat key/string dictionary")
        for key, value in values.items():
            if not KEY_RE.fullmatch(key):
                raise ValueError(f"{path}: unsafe key {key!r}")
            if not isinstance(value, str) or not value.strip():
                raise ValueError(f"{path}: empty or non-string translation for {key}")
            if any(c in value for c in "\r\n\x00#"):
                raise ValueError(f"{path}: Rainmeter-unsafe value in {key}")
            tokens(value, key, locale)
        catalogs[locale] = values

    reference = catalogs[DEFAULT_LOCALE]
    for locale, values in catalogs.items():
        missing = sorted(reference.keys() - values.keys())
        extra = sorted(values.keys() - reference.keys())
        if missing or extra:
            raise ValueError(f"{locale}: missing keys={missing}, extra keys={extra}")
        for key in reference:
            a = tokens(reference[key], key, DEFAULT_LOCALE)
            b = tokens(values[key], key, locale)
            if a != b:
                raise ValueError(
                    f"{locale}:{key}: placeholders {sorted(b)} "
                    f"do not match {DEFAULT_LOCALE} {sorted(a)}"
                )
    return catalogs


def var_name(key: str) -> str:
    return "L_" + re.sub(r"[._]", "_", key)


def compile_catalogs(source: Path, destination: Path) -> None:
    catalogs = load_catalogs(source)
    names = {var_name(k) for k in catalogs[DEFAULT_LOCALE]}
    if len(names) != len(catalogs[DEFAULT_LOCALE]):
        raise ValueError("Variable collisions after name normalization")

    destination.mkdir(parents=True, exist_ok=True)
    for locale, catalog in catalogs.items():
        lines = [
            "; Generated from packaging/i18n/catalogs by compile.py",
            "; DO NOT EDIT. Each file must be UTF-16 LE with BOM for Rainmeter.",
            "[Variables]",
        ]
        for key in sorted(catalog):
            lines.append(f"{var_name(key)}={catalog[key]}")
        expected = "\r\n".join(lines) + "\r\n"
        target = destination / f"{locale}.inc"
        target.write_bytes(b"\xff\xfe" + expected.encode("utf-16-le"))
        actual = target.read_bytes()
        assert actual.startswith(b"\xff\xfe")
        assert actual[2:].decode("utf-16-le") == expected
        print(f"PASS: {locale.upper()} — {len(catalog)} strings; UTF-16 LE with BOM")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path,
                        default=Path(__file__).resolve().parent / "catalogs")
    parser.add_argument("--output", type=Path,
                        help="Staged Rainmeter @Resources/Languages directory")
    args = parser.parse_args()
    if args.output:
        compile_catalogs(args.source, args.output)
    else:
        catalogs = load_catalogs(args.source)
        print(f"PASS: {len(catalogs['sk'])} keys validated for {', '.join(LOCALES)}")


if __name__ == "__main__":
    main()
