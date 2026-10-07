# Ecowitt Weather v1.11.0-beta.2

Second public beta of the Ecowitt **Cloud API** Rainmeter skin.

## What changed since beta.1

- Version bump to `v1.11.0-beta.2` for a real end-to-end in-skin update test.
- Keeps the same Cloud API data model and user configuration split introduced in beta.1.
- Preserves `CloudSecrets.inc`, `UserVariables.inc`, history and diagnostics during self-update.
- Beta channel remains the default for beta installations.

## Test goal

This release is intentionally small so beta.1 installations can verify the complete updater flow:

`METEO CLOUD ●` → **Aktualizácie** → **AKTUALIZOVAŤ** → backup → download → SHA256 → install → Rainmeter refresh.

## Installation

New users can install `EcowittWeather-v1.11.0-beta.2.rmskin` directly with Rainmeter.

Existing beta.1 users should switch to the **Beta** update channel and use the in-skin updater.

API credentials remain local in `CloudSecrets.inc` and are excluded from release packages.
