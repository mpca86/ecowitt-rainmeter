# Packaging

Public release assets are built from `src/EcowittWeather`.

## RMSKIN

`packaging/RMSKIN.ini` is the Rainmeter Skin Installer manifest. The package:

- installs the root skin as `EcowittWeather`;
- opens `Settings/Settings.ini` after installation;
- uses `MergeSkins=1` so later `.rmskin` upgrades can preserve local user files that are not present in the package.

The release workflow uses `2bndy5/rmskin-action` to generate a validating `.rmskin` package with Rainmeter's required custom footer.

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
