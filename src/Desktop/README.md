# Ecowitt Weather – Desktop Edition (alpha)

Samostatná Windows aplikácia s podporou viacerých widgetov a viacerých staníc. Aktuálne alpha vydanie: **v0.2.0-alpha.3**.

**Aktuálne implementované:**
- .NET 8 / WPF widget bez rámu (aktuálne merania z Ecowitt Web API v3)
- Ecowitt Cloud API: teplota, vlhkosť, tlak, vietor, zrážky, UV, solárne žiarenie a CH1–CH8
- nastavenie MAC, Application Key a API Key; aliasy kanálov a interval načítania
- tlačidlo **Načítať stanice z Ecowitt**: zoznam meteorologických staníc priradených k API účtu, výber MAC bez prepisovania
- kontrastný tmavý formulár s trvalo dostupnými tlačidlami **Uložiť** a **Zrušiť**
- uloženie nastavení v JSON, **API kľúče chránené cez Windows DPAPI** pre aktuálne konto
- tray ikona s ovládaním a možnosťou pridať viac widgetov
- **viac nezávislých staníc a profilov** (pridanie / odstránenie v Nastaveniach)
- každý widget možno priradiť k ľubovoľnej stanici cez tray menu alebo cez **pravý klik na widget** → Zmeniť meteostanicu
- viac widgetov jednej stanice zdieľa **jediný polling cyklus**; odlišné stanice sa načítavajú samostatne
- presúvanie widgetov myšou a zapamätanie pozícií
- vlastný tmavý kontextový zoznam, svetlý kontrastný text a hlavička widgetu bez ikon
- zmena stanice, Nastavenia, Aktualizácie a zatvorenie z kontextovej ponuky (pravý klik)
- **samostatný Desktop alpha update kanál** oddelený od Rainmeter verzií; SHA-256, záloha a rollback skript
- Local API gateway cez /get_livedata_info a Auto režim s Web API zálohou
- centrálna tmavá WPF šablóna kontextových ponúk + regresné testy UI v CI
- Windows CI zostavenie, parser, hybrid a update smoke testy

**Zatiaľ neimplementované:**
- rôzne typy/layouty widgetov, SQLite história a grafy
- export/import Desktop konfigurácie a samostatný installer
- farebné UI stavy a autostart

## Inštalácia Desktop Alpha

V [GitHub Releases](https://github.com/mpca86/ecowitt-rainmeter/releases) si otvor
`desktop-v0.2.0-alpha.3` a stiahni `EcowittWeather-Desktop-v0.2.0-alpha.3-win-x64.zip`.
Rozbaľ do zapisovateľného priečinka (napr. `C:\\Apps\\EcowittWeather\\`) a spusti
`EcowittWeather.Desktop.exe`. Portable build obsahuje aj .NET runtime,
nie je potrebné mať Rainmeter ani Visual Studio.

## Spustenie zo zdrojového kódu na Windows 10/11

Na vývoj je potrebný .NET 8 SDK s Windows Desktop podporou (prípadne Visual Studio 2022).

```powershell
git clone --branch cloud-api https://github.com/mpca86/ecowitt-rainmeter.git
cd ecowitt-rainmeter

dotnet restore src/Desktop/EcowittWeather.sln
dotnet build src/Desktop/EcowittWeather.sln -c Release
dotnet run --project src/Desktop/EcowittWeather.Desktop -c Release
```

Pri prvom spustení sa otvorí karta **Začíname** s trojkrokovým sprievodcom.
K dispozícii sú samostatné karty **Stanice**, **Pripojenie**, **Senzory** a **Aplikácia**.
Na karte Pripojenie možno merania **otestovať pred uložením**.
Základný [slovenský návod pre prvé spustenie](../../docs/desktop/PRVE_SPUSTENIE.md)
vysvetľuje aj získanie Ecowitt API kľúčov.
Heslo účtu sa do aplikácie nikdy nezadáva; API kľúče zostávajú lokálne chránené pomocou Windows DPAPI.

Dvojklik na ikonu v oblasti oznámení zobrazí widgety, pravý klik otvorí menu
so zobrazením okien, pridaním widgetu, Nastaveniami, obnovením a ukončením.

### Zoznam staníc z Ecowitt

V Nastaveniach vyplň Application Key a API Key, potom stlač **Načítať stanice z Ecowitt**.
Aplikácia zavolá `GET /api/v3/device/list`, ktorý pre zoznam nevyžaduje MAC.
Zo zoznamu vyberie iba meteorologické stanice s platnou MAC adresou
(`type=1`), nie kamery ani jednotlivé WH31/ostatné bezdrôtové senzory.

Výber automaticky vyplní pole MAC; zmeny sa uložia až po kliknutí na **Uložiť**.
Ručné zadanie MAC zostáva dostupné. API kľúče sa kvôli tomuto dopytu
nezapisujú do logu ani Git repozitára.

### Viac staníc a widgetov

1. V **Nastaveniach** pri výbere profilu stlač **+ Pridať**.
2. Pomocou **Načítať stanice z Ecowitt** vyber inú stanicu alebo ručne zadaj jej MAC. Vyplň názov a stlač **Uložiť**.
3. Klikni pravým tlačidlom myši na ikonu v systémovej lište (tray) → **Pridať widget – vybrať stanicu** → vyber profil.
4. V otvorenom widgete klikni **pravým tlačidlom** a vyber **Zmeniť meteostanicu**.

Odlišné stanice používajú samostatné načítanie dát; dva widgety tej istej stanice zdieľajú jedno načítanie. Označenia CH1–CH8 sú nezávislé pre každú stanicu.

### Logické skupiny Nastavení

- **Začíname:** sprievodca nového používateľa, odkazy na Ecowitt a návod.
- **Stanice:** zoznam profilov, pridávanie/odoberanie a názov stanice.
- **Pripojenie:** Web / Local / Auto, požadované údaje pre daný režim,
  načítanie zoznamu staníc a test pripojenia.
- **Senzory:** používateľské názvy kanálov CH1–CH8.
- **Aplikácia:** interval obnovovania, aktualizácie, informácie o programe a bezpečnosti.

Už nastavený používateľ uvidí pri otvorení Nastavení rovno kartu **Stanice**.
Všetky zmeny sa potvrdzujú cez spoločné tlačidlo **Uložiť nastavenia**.

### Kontextová ponuka widgetu

Pravý klik na widget otvorí tmavú kontextovú ponuku: **Zmeniť meteostanicu**,
**Nastavenia**, **Aktualizácie** a **Zavrieť widget**.
Widget presunieš **ľavým potiahnutím**. Po prejdení myšou sa nezobrazuje rušivý tooltip. Ikony v hlavičke
boli odstránené, aby nezakrývali dlhšie názvy staníc.

### Zdroj údajov každej stanice

V **Nastaveniach → Stanice a profily** vyber konkrétny profil a nastav jeho režim:

- **Web API:** Ecowitt Application Key, API Key a MAC; funguje aj mimo LAN.
- **Local API:** IP adresa alebo lokálny názov gatewaya (napr. `192.168.1.100:80`).
  Kľúče ani MAC nie sú potrebné; dostupnosť gatewaya v LAN/VPN áno.
- **Auto:** zadáš lokálny gateway **aj** Cloud údaje. Najskôr sa použije Local.
  Pri chybe alebo nedostupnosti gatewaya sa použije Web API a lokálne spojenie
  sa po dvoch minútach skúsi znova.

Local API číta endpoint `/get_livedata_info` a normalizuje merania na spoločné
jednotky. Pätička widgetu ukazuje aktuálny zdroj vrátane označenia záložného Web API.

Tento režim zatiaľ podporuje typy lokálnych meraní podľa testovacej odpovede GW3000
z Rainmeter Edition. Pri iných gatewayoch môže byť potrebné doplniť mapovanie.

[Changelog Desktop Edition](CHANGELOG.md).

### Desktop alpha aktualizácie

- Pri spustení sa skontrolujú nové verejné vydania s tagom `desktop-v...`.
- V tray menu, vo widgete alebo v Nastaveniach je dostupný panel Aktualizácie.
- Aktualizátor stiahne samostatný ZIP a jeho SHA-256 súbor, porovná kontrolný súčet,
  rozbalí aktualizáciu mimo priečinka aplikácie a pri potvrdení spustí
  lokálny pomocný skript.
- Po ukončení aplikácie skript zálohuje existujúce súbory, vymení súbory
  a aplikáciu znova spustí; pri chybe sa pokúsi obnoviť zálohu.
- Dáta a API kľúče v `%APPDATA%\\EcowittWeather\\Desktop` sa neprepisujú.
- Priečinok aplikácie musí byť zapisovateľný, preto sa neodporúča `Program Files`.

**Aktualizácie sú dostupné od alpha.1; postup testujeme aj pri prechode alpha.2 → alpha.3.**
Automatické buildy a testy neznamenajú odskúšanie výmeny súborov na reálnom počítači.

### Migrácia existujúcej konfigurácie

Pri prvom spustení nového buildu sa starý `Profile` a `SensorAliases`
z `settings.json` automaticky prevedú do poľa `Profiles`.
Existujúce okná a ich pozície zostanú zachované; staré widgety sú naviazané
na pôvodnú stanicu. API kľúče zostávajú v chránenom súbore
`cloud-secrets.dat` a migrovať ich nie je potrebné.

### Ikona a informácie o programe

Desktop Edition má vlastnú ikonu počasia v spustiteľnom súbore, vo Windows tray
aj v titulku Nastavení. V tray menu a na spodku Nastavení je dostupné
**O programe** (autor, verzia, licencia a projektové odkazy).
Aktuálna verzia: `0.2.0-alpha.3`.

### Lokálne uložené dáta

```text
%APPDATA%\EcowittWeather\Desktop\
├─ settings.json          (bez API kľúčov)
└─ cloud-secrets.dat      (Windows DPAPI CurrentUser)
```

`cloud-secrets.dat` nie je prenosný medzi kontami ani počítačmi. Desktop export/import
bude mať vlastný chránený formát v ďalšej fáze.

## Build / testy

```powershell
dotnet build src/Desktop/EcowittWeather.sln -c Release
dotnet run --project src/Desktop/EcowittWeather.SmokeTests -c Release
```

CI workflow: [Desktop Edition Windows build](../../.github/workflows/desktop-build.yml).

Architektúra: [DESKTOP_ARCHITECTURE.md](../../docs/DESKTOP_ARCHITECTURE.md).

Rainmeter Edition ostáva v `src/EcowittWeather/` a jej vydávanie sa týmto nemení.
