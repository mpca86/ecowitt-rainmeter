# Changelog

## v1.11.0-beta.1 — 2026-10-07

- First public beta of the Ecowitt Cloud API branch.
- Added Ecowitt Web API v3 real-time data source with Application Key, API Key and station MAC configuration.
- Added dynamic CH1–CH8 temperature / humidity channels and local sensor aliases.
- Added Cloud Settings, Sensors, Diagnostics and Update panels.
- Added safe local separation of user settings and Cloud API credentials.
- Added automatic update checks with Stable / Beta / Development channels.
- Added update availability indicator beside the METEO CLOUD title.
- Added GitHub release packaging for ZIP and validated .rmskin assets.
- Added first Rainmeter Skin Installer package metadata.


## v1.10.0-alpha

- Fixed UV index rendering and Slovak UV category labels.
- Added dynamic CH1–CH8 visualization.
- Added automatic vertical layout and panel resizing for newly detected sensors.
- Added channel aliases with API-name fallback.
- Kept 15-minute temperature trends and 3-hour pressure tendency.
- Continued modular Local API parser architecture.

## Earlier alpha milestones

- Replaced regex JSON parsing with a real Lua JSON decoder.
- Added generic Ecowitt sensor registry.
- Added Settings and Diagnostics skins.
- Added configurable gateway host and port.
- Added first-run configuration guards.
