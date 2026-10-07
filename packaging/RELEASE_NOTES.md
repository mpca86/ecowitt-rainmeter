# Ecowitt Weather v1.11.0-beta.1

First public beta of the Ecowitt **Cloud API** Rainmeter skin.

## Highlights

- Ecowitt Web API v3 real-time data source.
- Outdoor temperature / humidity, wind, solar radiation, UV and rainfall.
- Indoor gateway temperature / humidity and pressure.
- Dynamic CH1–CH8 temperature / humidity channels.
- User-defined sensor aliases.
- 15-minute temperature trends and 3-hour pressure tendency.
- Persistent local history.
- Settings, Sensors, Diagnostics and Update panels.
- Safe local storage for Application Key, API Key and station MAC.
- Stable / Beta / Development update channels.
- Automatic update check with a notification beside **METEO CLOUD**.
- First `.rmskin` installer package.

## Installation

Download `EcowittWeather-v1.11.0-beta.1.rmskin`, open it with Rainmeter and install the package. The installer opens **Settings** after installation.

Enter:
- Application Key
- API Key
- station MAC address

Confirm every field with **Enter**, then run **TEST CLOUD API**.

## Notes

This is a beta release. Cloud API channel names are mapped locally through the Sensors panel because Ecowitt real-time data exposes CH1–CH8 but does not reliably expose the user's custom sensor aliases.

API credentials are stored only in the local `CloudSecrets.inc` file and are excluded from release packages and Git.
