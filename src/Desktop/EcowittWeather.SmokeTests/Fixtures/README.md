# GW3000 Local API test fixtures

Two anonymized snapshots from **Ecowitt GW3000 firmware 1.2.4** (`/get_livedata_info`). Values and identifier/section shapes are retained; sensor display names are replaced with generic CH names, and the unrelated diagnostic/debug section is removed. No IP addresses, MAC addresses, API keys or private station names are included.

- `gw3000_fw1_2_4_home.json`: **GW3000 + WH32 outdoor temperature/humidity sensor only**, confirmed by station owner. Gateway reports only `common_list` with outdoor readings and `wh25` with indoor readings/pressure; wind/rain/UV/solar and `ch_aisle` are absent as expected for this hardware configuration. Missing fields must remain `null`, not zero.
- `gw3000_fw1_2_4_office.json`: includes wind, rain, solar, UV, and CH1–CH4. Observed real zeroes must remain numeric zero.

The missing sections are a **gateway response fact, not proof of a parser failure**. Both snapshots omit observation timestamps, so `ObservedAt` is expected to be null. Fixtures deliberately do not infer the cause of missing sensors or assert hardware model compatibility beyond these two samples.

## Verified GW3000 field identifiers (Live Data vs Local JSON)

- `common_list/0x0B`: current wind speed (Wind Speed)
- `common_list/0x0C`: **current wind gust** (Gust Speed), normalized to `WindGustMs`
- `common_list/0x19`: daily wind maximum (Day Wind Max), **not** the current gust
- `common_list/0x0A`: current wind direction; `0x6D`: 10-minute average direction
- `common_list/3` (decimal text ID): feels-like temperature; `5` (decimal text ID): VPD (kPa)
- `common_list/0x03` (hex text ID): dew point; must not be matched by decimal `3`

The office live capture confirmed current wind speed 0.3 m/s, gust 0.5 m/s, and day maximum 7.7 m/s. Regression tests check these values separately without storing an unredacted Live Data screenshot or the user's LAN address. Desktop currently displays only the existing normalized fields; this patch does not add new widget types or fields.
