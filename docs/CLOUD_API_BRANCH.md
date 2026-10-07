# cloud-api branch

This branch implements the Ecowitt Web API v3 data-source adapter.

## Rules

- Do not store Application Key / API Key values in Git.
- Keep the normalized output compatible with the Local API semantic model.
- Reuse shared history / trend / UI components where practical.
- Default cloud polling should be conservative and configurable.

## First implementation milestone

1. Cloud Settings fields for Application Key, API Key and station MAC.
2. Connection test.
3. `device/real_time` request.
4. Cloud response normalizer.
5. Mapping into the shared Rainmeter variables.
