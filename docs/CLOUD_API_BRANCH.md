# cloud-api branch

This branch implements the Ecowitt Web API v3 data-source adapter.

## Current status

Initial cloud alpha is wired to:

`GET https://api.ecowitt.net/api/v3/device/real_time`

The branch now contains:

- `cloud_parser.lua` — normalizes Ecowitt Web API v3 real-time JSON.
- `main_cloud.lua` — shared Rainmeter UI/history adapter for cloud data.
- Cloud Settings UI for Application Key, API Key, MAC and refresh interval.
- Cloud diagnostics output: `ecowitt_cloud_debug.txt`.
- Dynamic `temp_and_humidity_ch1` … `temp_and_humidity_ch8` support.

## Credentials

Never commit real credentials.

Copy:

`src/EcowittWeather/@Resources/Includes/CloudSecrets.inc.example`

to:

`src/EcowittWeather/@Resources/Includes/CloudSecrets.inc`

Then enter the values locally through the Settings skin.

`CloudSecrets.inc` is ignored by Git.

## API request

The current alpha requests all available fields and pins metric units:

- temperature: Celsius
- pressure: hPa
- wind: metric
- rainfall: metric
- solar irradiance: metric

The cloud parser additionally inspects returned unit strings and normalizes wind to m/s and rainfall to mm where possible.

## First test

1. Create local `CloudSecrets.inc` from the example.
2. Load `Settings/Settings.ini`.
3. Enter Application Key, API Key and station MAC.
4. Confirm every field with **Enter**.
5. Keep refresh at 60 seconds for the first test.
6. Click **TEST CLOUD API**.
7. Enable `DebugParser=1` in `Variables.inc`.
8. Load the Meteo skin.
9. Inspect `@Resources/Diagnostics/ecowitt_cloud_debug.txt`.

Do not share the Application Key or API Key in screenshots, logs or GitHub issues.

## Expected normalization

The adapter currently maps these Ecowitt Web API sections:

- `outdoor`
- `indoor`
- `wind`
- `pressure`
- `rainfall`
- `solar_and_uvi`
- `temp_and_humidity_ch1` … `temp_and_humidity_ch8`

The actual payload from the first live test will be used to validate field names, units and timestamps before this branch is considered beta.
