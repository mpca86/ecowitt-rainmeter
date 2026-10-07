# Ecowitt Weather v1.11.0-beta.4

Fourth public beta of the Ecowitt **Cloud API** Rainmeter skin.

## Purpose of this release

This release is the first one intended to validate the complete **in-skin beta update flow** from beta.3.

Expected path:

`METEO CLOUD ●` → **Aktualizácie** → **AKTUALIZOVAŤ** → backup → download → SHA256 verification → install → Rainmeter refresh.

## Changes

- Bumped the package and all Rainmeter metadata to `v1.11.0-beta.4`.
- Forced UTF-8 stdout from Windows PowerShell 5.1 so Rainmeter `RunCommand` receives clean updater output.
- Keeps the fixed GitHub prerelease discovery introduced in beta.3.
- Preserves `CloudSecrets.inc`, `UserVariables.inc`, history, diagnostics and update state during self-update.

## Test

On a beta.3 installation with the update channel set to **Beta**, open the Meteo skin or the Update panel. It should detect `v1.11.0-beta.4`, show the update indicator beside **METEO CLOUD**, and allow the update without manually installing a new `.rmskin`.
