# Ecowitt Weather v1.11.0-beta.3

Third public beta of the Ecowitt **Cloud API** Rainmeter skin.

## Fixed

- Fixed GitHub prerelease discovery on **Windows PowerShell 5.1**.
- Added explicit TLS 1.2 and GitHub API version headers for older Windows PowerShell environments.
- Update checks now correctly enumerate GitHub Releases instead of returning `CURRENT|--` when beta releases exist.

## Why beta.3 exists

Beta.1 and beta.2 can query GitHub but may fail to enumerate prereleases correctly on Windows PowerShell 5.1. Because that bug lives inside the updater itself, one manual bridge install to beta.3 is required for affected systems.

After beta.3 is installed, the next beta release will be used to validate the complete in-skin update flow.

## Installation

Download and install `EcowittWeather-v1.11.0-beta.3.rmskin` manually once if you are on beta.1 or beta.2 and update detection shows no beta release.

Your local `CloudSecrets.inc`, `UserVariables.inc`, history and diagnostics remain outside distributable files.
