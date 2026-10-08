# Ecowitt Weather Desktop – prvé spustenie a pripojenie stanice

Tento návod patrí k **Ecowitt Weather Desktop Edition** pre Windows. Funguje aj
bez Rainmetera. Po nainštalovaní aplikácie otvor **Nastavenia → Začíname**.

> **Bezpečnosť:** Do aplikácie nikdy nezadávaj prihlasovacie heslo do Ecowitt.
> Pre Web API sa používajú samostatné **Application Key** a **API Key**.
> Návod nežiada odoslať kľúče autorovi, na GitHub ani do e-mailu.

## 1. Vyber spôsob pripojenia

V aplikácii otvor **Nastavenia → Pripojenie** a vyber režim:

| Režim | Čo potrebuješ | Kedy použiť |
| --- | --- | --- |
| **Web API** | Ecowitt Application Key, API Key a MAC stanice | Stanica mimo tvojej siete, notebook alebo viac lokalít |
| **Local API** | Lokálnu IP adresu alebo názov Ecowitt gatewaya | Počítač a gateway sú v jednej LAN alebo dostupnej VPN; funguje aj bez internetu |
| **Auto (Local + Web)** | Oba vyššie uvedené typy nastavení | Primárne LAN, pri výpadku prepnúť na Cloud |

Ak používaš iba Local API, **nepotrebuješ API kľúče ani prihlasovanie**.

## 2. Web API – kde získam kľúče?

1. Vo **vlastnom internetovom prehliadači** otvor [www.ecowitt.net](https://www.ecowitt.net/).
2. Prihlás sa priamo na oficiálnej stránke Ecowitt. Aplikácia samotná tvoje
   používateľské meno ani heslo nevyžaduje.
3. Klikni na **ikonu používateľského účtu vpravo hore** a otvor
   **User Profile / Používateľský profil**.
4. V používateľskom profile vyhľadaj sekciu kľúčov API. Tu môžeš vytvoriť
   alebo získať **Application Key** a **API Key**. Dostupný býva tiež súbor
   dokumentácie `api.doc`.
5. Vráť sa do **Ecowitt Weather → Nastavenia → Pripojenie** a vlož oba kľúče
   do rovnako pomenovaných polí.
6. Klikni **Načítať stanice z Ecowitt**. Zobrazí sa zoznam meteorologických
   staníc dostupných cez tieto API kľúče.
7. Vyber stanicu zo zoznamu. **MAC adresa sa doplní automaticky.**
8. Klikni **Otestovať pripojenie**. Ak sa zobrazia merania, klikni
   **Uložiť nastavenia**.

*Ak sa názvy tlačidiel na Ecowitt stránke časom zmenia, riadi sa ich aktuálnou
podobou. Dôležitým bodom je používateľský profil na webovom portáli,
nie mobilná aplikácia.*

Oficiálny podklad:
[Ecowitt – How do I find my app & api keys?](https://ecowitt-subgqs9hhsx.gorgias.help/en-US/how-do-i-find-my-app-and-api-keys-564812)

## 3. Local API – bez cloudu

1. Zisti IP adresu Ecowitt gatewaya vo svojej sieti, napríklad v zozname
   zariadení routera. Príklad adresy: `192.168.1.100`.
2. V časti **Pripojenie** vyber **Local API**.
3. Do poľa **IP adresa alebo názov gatewaya** napíš IP, prípadne
   `192.168.1.100:80`. Aplikácia automaticky doplní endpoint
   `/get_livedata_info`.
4. Klikni **Otestovať pripojenie**. Ak PC gateway nevidí, prever Wi-Fi,
   oddelené VLAN, firewall alebo VPN.
5. Po úspešnom teste ulož nastavenia.

**Poznámka:** Local API nezisťuje všetky stanice na internete.
Vyžaduje podporovaný lokálny Ecowitt gateway, nie iba bezdrôtový senzor.

## 4. Auto – Local s Web API zálohou

1. Vyber režim **Auto**.
2. Zadaj IP lokálneho gatewaya.
3. Zadaj Application Key, API Key a vyber MAC zo zoznamu.
4. Otestuj pripojenie a ulož.

Aplikácia uprednostní Local API. Keď lokálny gateway prestane odpovedať,
skúsi Web API; návrat k Local API kontroluje po približne 2 minútach.
V spodnej časti widgetu vidíš, ktorý zdroj bol naozaj použitý.

## 5. Druhá stanica a ďalšie widgety

- V **Nastavenia → Stanice** klikni na **+ Pridať**, pomenuj nový profil.
- V **Pripojenie** mu nastav samostatnú MAC/Local IP a zdroj dát.
- V **Senzory** podľa potreby pomenuj kanály CH1–CH8.
- Z ponuky ikony v systémovej lište vyber **Pridať widget – vybrať stanicu**.

Jeden profil môže byť Local a druhý Web. Ak dva widgety zobrazujú tú istú
stanicu, aplikácia jej dáta načítava spoločným pollingom.

## 6. Ovládanie a aktualizácie

- **Ľavé potiahnutie widgetu** – premiestnenie po obrazovke.
- **Pravý klik na widget** – zmena stanice, Nastavenia, Aktualizácie alebo zatvorenie.
- **Pravý klik na ikonu v systémovej lište** – pridanie widgetov a správa aplikácie.

## 7. Kam sa ukladajú údaje?

- Bežné nastavenia: `%APPDATA%\EcowittWeather\Desktop\settings.json`
- Kľúče API: `%APPDATA%\EcowittWeather\Desktop\cloud-secrets.dat`

API kľúče sú chránené Windows DPAPI pre aktuálneho používateľa; súbor s
kľúčmi nemožno jednoducho preniesť do druhého PC. Aplikácia neposiela
kľúče autorovi. Ecowitt Web API ich používa iba pri prístupe k službe
Ecowitt.

**Nikdy nezverejňuj kľúče na screenshotoch, v logoch, v GitHub Issues
ani v konverzácii.** Ak kľúč unikne, v používateľskom profile Ecowitt ho
zruš/obnov a nastav nový.

## Riešenie problémov

| Problém | Čo overiť |
| --- | --- |
| Nevidím stanice v zozname | Prihlasovací účet Ecowitt, oprávnenia a obidva API kľúče |
| Web API nefunguje | Kľúče, MAC a internet |
| Local API nefunguje | IP adresa, dostupnosť gatewaya, VLAN / Wi-Fi / firewall |
| Auto používa iba Web | Dostupnosť lokálneho gatewaya; po obnovení LAN počkaj na ďalší pokus |
| Nezobrazujú sa niektoré senzory | Podporu senzora v Local odpovedi gatewaya a názvy kanálov CH1–CH8 |

Opravy a novinky sú uvedené v [slovenskom changelogu Desktop Edition](../../src/Desktop/CHANGELOG.md).

---

Návod je verzovaný spoločne so zdrojovým kódom, aby bol vždy dostupný
aj pre staršie verzie aplikácie. Aktuálny stav sa môže líšiť od vývojových
obrázkov a zobrazenia portálu Ecowitt.
