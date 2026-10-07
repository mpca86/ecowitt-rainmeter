# Ecowitt Rainmeter

Rainmeter skin for displaying weather data from Ecowitt gateways and sensors.

**Author:** Martin Šturcel (mpca86)  
**Website:** https://martinsturcel.sk  
**Repository:** https://github.com/mpca86/ecowitt-rainmeter

## Status

Current Cloud API development baseline: **v1.10.2-cloud-alpha**.

The project currently has two data-source directions:

- **Local API** — reads `/get_livedata_info` directly from an Ecowitt gateway on the local network.
- **Cloud API** — separate development branch using Ecowitt Web API v3 for users without direct LAN access to the gateway.

## Branches

- `main` — current tested Local API baseline
- `develop` — ongoing development
- `cloud-api` — Ecowitt Web API v3 adapter

## Repository layout

```text
src/EcowittWeather/
├─ Meteo/
├─ Settings/
├─ Diagnostics/
└─ @Resources/
   ├─ Scripts/
   ├─ Includes/
   └─ Diagnostics/

docs/
samples/
packaging/
```

The Rainmeter package is being prepared for distribution as an `.rmskin` installer.

## Current Local API features

- real JSON decoder instead of regex-based JSON parsing
- generic Ecowitt sensor registry
- dynamic CH1–CH8 sensor display
- outdoor temperature / humidity
- wind speed, gust and direction
- solar radiation
- UV index with category
- rainfall / daily rainfall
- indoor temperature / humidity
- absolute and relative pressure
- 15-minute temperature trends
- 3-hour pressure tendency
- persistent CSV history
- Settings and Diagnostics skins

## License

Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported. See [LICENSE.md](LICENSE.md).


## Self-updater

The Cloud API branch includes a Rainmeter-native update UI under `Update/Update.ini`.

Update channels:

- `stable` — latest non-prerelease GitHub Release.
- `beta` — latest prerelease GitHub Release.
- `development` — latest `cloud-api` branch archive.

The updater preserves local user state:

- `@Resources/Includes/CloudSecrets.inc`
- `@Resources/Includes/UserVariables.inc`
- `Meteo/meteo_history.csv`
- `@Resources/Diagnostics/ecowitt_cloud_debug.txt`

Before installing an update it creates a ZIP backup in:

`%LOCALAPPDATA%\EcowittRainmeter\Backups`

Git-managed development checkouts are detected and are not overwritten by the public self-updater.

### Releases

Pushing a version tag such as `v1.11.0` or `v1.11.0-beta.1` triggers the GitHub Actions release workflow. It builds:

- `EcowittWeather-v<version>.zip`
- matching `.sha256`

Tags containing `alpha`, `beta`, or `rc` are published as GitHub prereleases.
