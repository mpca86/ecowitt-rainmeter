# Cloud API branch

The `cloud-api` branch is intended for users who cannot directly reach an Ecowitt gateway over LAN/VLAN.

## Planned source

Ecowitt Web API v3 real-time device data.

Documentation:
https://doc.ecowitt.net/web/#/apiv3en?page_id=17

## Planned configuration

- Application Key
- API Key
- station MAC address
- units
- refresh interval

Credentials must remain in a local user configuration file and must never be committed to the repository.

## Design target

```text
Ecowitt Web API v3
    ↓
cloud adapter
    ↓
shared normalized model
    ↓
shared history / trends
    ↓
shared Rainmeter UI
```

The Cloud API implementation should remain separate from the Local API adapter while sharing common rendering and history logic wherever practical.
