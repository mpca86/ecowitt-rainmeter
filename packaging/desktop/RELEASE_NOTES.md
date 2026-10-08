# Ecowitt Weather Desktop v0.2.0-alpha.2

Druhá verejná alpha verzia samostatnej aplikácie pre Windows 10/11 (x64).

## Opravené ovládanie a vzhľad

- **Ľavým tlačidlom myši** možno widget priamo potiahnuť na nové miesto.
- **Pravým tlačidlom myši** sa otvorí kontextová ponuka.
- Z kontextovej ponuky bolo odstránené nadbytočné tlačidlo na presun widgetu.
- Hlavná ponuka aj vnorený zoznam meteostaníc používajú **jednu spoločnú tmavú WPF šablónu** vrátane pozadia, zaškrtávacích značiek a aktívnych položiek.
- Pridaný automatický test spoločnej témy, ovládania myšou a absencie rušivých tlačidiel vo widgete.

## Local API a automatický hybridný režim

- Každý profil má vlastnú voľbu **Web API / Local API / Auto**.
- Local API získava živé merania z dostupného Ecowitt gatewaya cez `http://HOST:PORT/get_livedata_info`.
- Parser podporuje teplotu, vlhkosť, tlak, vietor, nárazy, zrážky, UV, slnečné žiarenie a kanály CH1–CH8.
- Rozpoznané jednotky sa prepočítavajú na °C, hPa, m/s a mm.
- Local API nepotrebuje Ecowitt Application Key ani API Key.
- **Auto** prioritne používa Local API. Pri chybe gatewaya prepne na Web API; lokálne pripojenie skúša opäť po dvoch minútach.
- Aktuálny zdroj dát je uvedený v pätičke widgetu vrátane informácie o záložnom Web API.
- Lokálny zdroj akceptuje IP adresy privátnej siete alebo názvy lokálnych gatewayov, s voliteľným portom.

## Aktualizácia

Ak používaš `v0.2.0-alpha.1`, otvor kontextovú ponuku widgetu → **Aktualizácie** → **Skontrolovať** → **Stiahnuť a aktualizovať**. Alpha.2 je určená aj na prvý praktický test zabudovaného aktualizátora.

Doterajšie stanice, widgety, pozície a API kľúče sa zachovávajú.

## Známe obmedzenia

- Local API bolo overené automatickými testami podľa vzorovej odpovede z Rainmeter Edition, **reálne spojenie na konkrétny gateway ešte treba odskúšať**.
- Rôzne typy widgetov, grafy, história SQLite a samostatný inštalátor zatiaľ nie sú dostupné.
- Automatická výmena programových súborov s rollbackom sa musí overiť na testovacom Windows PC.
