# Ecowitt Weather – Desktop Edition: Prehľad zmien

Tento súbor sa týka výlučne **Desktop Edition**. Rainmeter Edition má samostatný
changelog v koreňovom adresári repozitára.

## v0.2.0-alpha.3 — 2026-10-08

### Prvé spustenie a nastavenia
- Odstránený rušivý tooltip zobrazovaný pri prejdení myšou nad widgetom.
- Nastavenia preusporiadané do piatich logických častí: **Začíname**, **Stanice**, **Pripojenie**, **Senzory** a **Aplikácia**.
- Pri prvej konfigurácii sa otvára jednoduchý sprievodca s vysvetlením režimov Web API / Local API / Auto.
- V Pripojení sa zobrazujú iba polia potrebné pre vybraný režim.
- Nové tlačidlo **Otestovať pripojenie** načíta meranie bez uloženia konfigurácie.
- Priame odkazy na oficiálny Ecowitt web a slovenský návod.
- Prvotného sprievodcu možno kedykoľvek znovu otvoriť z tray menu cez **Prvé nastavenie / Pomoc**.
- Jeden centrálny tmavý štýl aj pre karty Nastavení.
- Rozšírené regresné testy na prítomnosť sekcií, pomocníka a neprítomnosť tooltipu.

### Dokumentácia a bezpečnosť
- [Slovenský návod pre prvé spustenie](../../docs/desktop/PRVE_SPUSTENIE.md) vrátane krokov na získanie Application Key a API Key.
- Aplikácia **nežiada prihlasovacie meno ani heslo** do ecowitt.net.
- Kľúče sa získavajú priamo z používateľského profilu na oficiálnom webe a ukladajú lokálne cez Windows DPAPI.
- Návod rozlišuje Web, Local a Auto a uvádza najčastejšie problémy s pripojením.

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
