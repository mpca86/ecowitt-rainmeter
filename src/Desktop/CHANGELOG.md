# Ecowitt Weather – Desktop Edition: Prehľad zmien

Tento súbor sa týka výlučne **Desktop Edition**. Rainmeter Edition má samostatný
changelog v koreňovom adresári repozitára.

## v0.2.0-alpha.2 — 2026-10-08

### Ovládanie a používateľské rozhranie
- Ľavé potiahnutie widget premiestni, pravé tlačidlo otvorí kontextovú ponuku.
- Odstránená položka Presunúť widget zo samotnej ponuky.
- Centrálna tmavá WPF šablóna pre hlavnú aj vnorenú kontextovú ponuku, jednotné farby textu, označení a zvýraznených položiek.
- Automatická regresná kontrola štýlov a gest myši vo Windows CI.

### Dátové zdroje
- Pridaný parser Ecowitt Local API pre `/get_livedata_info`.
- Pridaná normalizácia meraní vrátane prevodov Fahrenheit → Celzius, km/h → m/s, inHg → hPa a palcových zrážok → mm.
- Pridané nastavenie IP adresy/názvu a portu lokálneho gatewaya pre každý profil.
- Každá stanica môže používať Web, Local alebo Auto.
- Auto preferuje Local API, pri jeho výpadku prepne na Web API a po 2 minútach skúsi návrat k Local API.
- Pätička widgetu rozlišuje aktuálny zdroj a záložné Web API.
- Testy Local API dát, výpadku, záložného zdroja a obnovy LAN spojenia.

## v0.2.0-alpha.1 — 2026-10-08

- Prvé verejné vydanie samostatnej Windows aplikácie.
- Viac staníc a widgetov, jednotné Cloud API načítanie na profil.
- Zoznam priradených meteostaníc z Ecowitt Web API v3.
- Používateľské nastavenia a názvy senzorov pre každý profil.
- API kľúče lokálne šifrované pomocou Windows DPAPI.
- Vlastná ikona, systémová lišta, okno O programe.
- Alpha update kanál a samostatný GitHub Release workflow.
