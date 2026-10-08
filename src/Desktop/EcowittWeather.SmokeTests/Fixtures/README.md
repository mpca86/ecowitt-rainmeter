# GW3000 Local API test fixtures

Two anonymized snapshots from **Ecowitt GW3000 firmware 1.2.4** (`/get_livedata_info`). Values and identifier/section shapes are retained; sensor display names are replaced with generic CH names, and the unrelated diagnostic/debug section is removed. No IP addresses, MAC addresses, API keys or private station names are included.

- `gw3000_fw1_2_4_home.json`: gateway reports only `common_list` with outdoor readings and `wh25` with indoor readings/pressure; wind/rain/UV/solar and `ch_aisle` are absent. Missing fields must remain `null`, not zero.
- `gw3000_fw1_2_4_office.json`: includes wind, rain, solar, UV, and CH1–CH4. Observed real zeroes must remain numeric zero.

The missing sections are a **gateway response fact, not proof of a parser failure**. Both snapshots omit observation timestamps, so `ObservedAt` is expected to be null. Fixtures deliberately do not infer the cause of missing sensors or assert hardware model compatibility beyond these two samples.
