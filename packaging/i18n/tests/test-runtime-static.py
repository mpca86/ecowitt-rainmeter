#!/usr/bin/env python3
"""Static guardrails for the Rainmeter SK/EN/CS/PL runtime integration."""
import json
import re
from pathlib import Path

root=Path("src/EcowittWeather")
catalogs=Path("packaging/i18n/catalogs")
langs=("sk","en","cs","pl")
reference=json.loads((catalogs/"en.json").read_text("utf-8"))
actual_langs=set()
for language in langs:
    data=json.loads((catalogs/f"{language}.json").read_text("utf-8"))
    assert set(data)==set(reference), f"{language}: keys differ"
    ini=(root/"@Resources"/"Languages"/f"{language}.inc").read_bytes()
    assert ini.startswith(b"\xff\xfe"), f"{language}: no UTF-16 LE BOM"
    decoded=ini.decode("utf-16")
    assert decoded.startswith("[Variables]"), f"{language}: not an INI variable include"
    for key, value in data.items():
        actual=f"L_{key.replace('.', '_')}={value}\r\n"
        assert actual in decoded, f"{language}:{key}: compiled content differs"
    actual_langs.add(language)
assert actual_langs==set(langs)
state=(root/"@Resources"/"Includes"/"LanguageState.inc").read_bytes()
assert state.startswith(b"\xff\xfe")
assert "ResolvedUiLanguage=en" in state.decode("utf-16")
variables=(root/"@Resources"/"Includes"/"Variables.inc").read_text("utf-16")
assert "\nUiLanguage=auto\n" in variables

used=set()
for skin in ("Meteo","Settings","Sensors","Diagnostics","Update"):
    ini=(root/skin/f"{skin}.ini").read_bytes()
    assert ini.startswith(b"\xff\xfe"), f"{skin}: encoding changed"
    src=ini.decode("utf-16")
    ordered=(
        "@IncludeVariables=#@#Includes\\Variables.inc",
        "@IncludeUserVariables=#@#Includes\\UserVariables.inc",
        "@IncludeLanguageState=#@#Includes\\LanguageState.inc",
        "@IncludeEnglish=#@#Languages\\en.inc",
        "@IncludeLocale=#@#Languages\\#ResolvedUiLanguage#.inc",
    )
    indices=[src.index(item) for item in ordered]
    assert indices==sorted(indices), f"{skin}: language include order invalid"
    assert "ScriptFile=#@#Scripts\\language_bootstrap.lua" in src
    assert "#UiLanguage#.inc" not in src, f"{skin}: unchecked locale path"
    assert "[MeasureRainmeterLanguageWow6432]" in src, f"{skin}: WOW6432Node fallback measure missing"
    assert "RegKey=SOFTWARE\\WOW6432Node\\Rainmeter" in src, f"{skin}: WOW6432Node key missing"
    assert "[MeasureRainmeterLanguageNative]" in src, f"{skin}: native fallback measure missing"
    assert "RegKey=SOFTWARE\\Rainmeter" in src, f"{skin}: native key missing"
    assert src.index("[MeasureRainmeterLanguageWow6432]") < src.index("[MeasureLanguageBootstrap]"), f"{skin}: Registry measures must precede Lua bootstrap"
    used.update(re.findall(r"#(L_[a-z0-9_]+)#",src))
lua=(root/"@Resources"/"Scripts"/"main_cloud.lua").read_text("utf-8")
used.update(name for name in re.findall(r"'(L_[a-z0-9_]+)'",lua) if name != "L_wind_")
assert all("L_wind_" + direction in {"L_" + k.replace(".", "_") for k in reference} for direction in ("n", "ne", "e", "se", "s", "sw", "w", "nw"))
known={"L_"+key.replace(".","_") for key in reference}
assert used<=known, f"Unknown runtime translations: {sorted(used-known)}"

settings=(root/"Settings"/"Settings.ini").read_text("utf-16")
for lang in ("auto",)+langs:
    assert f"[MeterLang{lang.upper()}]" in settings
    assert f'UiLanguage "{lang}"' in settings
assert "LanguageState.inc" in (root/"@Resources"/"Scripts"/"language_bootstrap.lua").read_text("utf-8")
bootstrap=(root/"@Resources"/"Scripts"/"language_bootstrap.lua").read_text("utf-8")
assert (root/"@Resources"/"Scripts"/"language_bootstrap.lua").read_bytes().isascii()
assert "registry_measure_names()" in bootstrap
assert "GetMeasure(name)" in bootstrap
assert "alternateNumber and alternateNumber ~= 0 then return alternateValue" in bootstrap
assert "if iniLanguage ~= nil then return locale_for_lcid(iniLanguage) or 'en' end" in bootstrap
assert "PROCESSOR_ARCHITEW6432" in bootstrap
assert "powershell" not in bootstrap.lower() and "os.execute" not in bootstrap.lower()
print(f"PASS: 5 UTF-16 panels, 4 catalogs, {len(used)} wired translation names, allowlisted locale path")
