# Ecowitt Rainmeter

Rainmeter skin for displaying weather data from Ecowitt gateways and sensors.

**Author:** Martin Šturcel (mpca86)  
**Website:** https://martinsturcel.sk  
**Repository:** https://github.com/mpca86/ecowitt-rainmeter

## Status

Current development baseline: **v1.10.0-alpha**.

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
