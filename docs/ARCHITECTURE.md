# Architecture

## Goal

Keep the visual Rainmeter skin independent from the data source.

The current Local API implementation is split into four layers:

1. **JSON decoder** — converts the raw API response into a Lua table.
2. **Ecowitt parser / registry** — normalizes gateway-specific fields into semantic values.
3. **History engine** — stores sampled values and provides historical lookup for trends.
4. **Rainmeter adapter / UI** — sets Rainmeter variables, colors, labels, visibility and layout.

## Local API flow

```text
Ecowitt gateway
    ↓
/get_livedata_info
    ↓
WebParser
    ↓
json.lua
    ↓
ecowitt_parser.lua
    ↓
normalized values
    ↓
history.lua + main.lua
    ↓
Rainmeter meters
```

## Dynamic channels

`ch_aisle` channels are indexed generically. The current UI is designed to support CH1–CH8 and to hide channels that are not present.

## Data-source abstraction

The planned Cloud API branch will normalize Ecowitt Web API v3 responses into the same semantic model. The goal is to share history, trends and UI code between Local API and Cloud API implementations.

## Runtime files

Files such as `meteo_history.csv`, diagnostics output and user-specific secrets must not be committed.
