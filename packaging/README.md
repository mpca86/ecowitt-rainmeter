# Packaging

Public release assets are built from `src/EcowittWeather`.

## RMSKIN

`packaging/RMSKIN.ini` is the Rainmeter Skin Installer manifest. The package:

- installs the root skin as `EcowittWeather`;
- opens `Settings/Settings.ini` after installation;
- uses `MergeSkins=1` so later `.rmskin` upgrades can preserve local user files that are not present in the package.

The release workflow uses `2bndy5/rmskin-action` to generate a validating `.rmskin` package with Rainmeter's required custom footer.

## Unicode in the Cloud API Lua script

Rainmeter requires UTF-16 LE with a BOM for a Lua `ScriptFile` that
uses Unicode text or passes non-ASCII skin variables. The source file
`src/EcowittWeather/@Resources/Scripts/main_cloud.lua` is kept in
reviewable UTF-8 in Git. **Do not copy this source file directly over an
installed Rainmeter script** if Slovak text or user sensor names matter.

The GitHub Release workflow stages the skin and runs
`python3 packaging/prepare-rainmeter-lua.py build/rmskin/Skins/EcowittWeather`
before creating both ZIP and RMSKIN files. The local
`packaging/build-release.ps1` ZIP builder performs the equivalent staged
conversion without requiring Python on Windows. Neither path modifies
the source file in Git.

A separate non-release CI workflow builds a temporary test patch containing
only the UTF-16 LE `main_cloud.lua` and its SHA-256. Before applying it to
an existing `v1.11.0-beta.6` installation, back up the original Lua file
and unload the METEO skin. Do not overwrite user settings or API keys.
Test the tooltip, rain labels, non-ASCII sensor aliases and automatic refresh
on Windows before publishing any new beta.

Official reference: https://docs.rainmeter.net/tips/unicode-in-rainmeter/

## Release assets

Each release contains:

- `EcowittWeather-v<version>.rmskin`
- `EcowittWeather-v<version>.rmskin.sha256`
- `EcowittWeather-v<version>.zip`
- `EcowittWeather-v<version>.zip.sha256`

Runtime and secret files are excluded.

## Publishing

The workflow runs on `cloud-api` only when the head commit message starts with `release:`.

Example: `release: v1.11.0-beta.1`
