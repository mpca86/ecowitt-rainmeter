# Ecowitt Weather – Desktop Edition (prototyp)

Prvý spustiteľný základ samostatnej Windows aplikácie bez Rainmetera.

**Aktuálne implementované:**
- .NET 8 / WPF widget bez rámu (aktuálne merania z Ecowitt Web API v3)
- Ecowitt Cloud API: teplota, vlhkosť, tlak, vietor, zrážky, UV, solárne žiarenie a CH1–CH8
- nastavenie MAC, Application Key a API Key; aliasy kanálov a interval načítania
- tlačidlo **Načítať stanice z Ecowitt**: zoznam meteorologických staníc priradených k API účtu, výber MAC bez prepisovania
- kontrastný tmavý formulár s trvalo dostupnými tlačidlami **Uložiť** a **Zrušiť**
- uloženie nastavení v JSON, **API kľúče chránené cez Windows DPAPI** pre aktuálne konto
- tray ikona s ovládaním a možnosťou pridať viac widgetov
- **viac nezávislých staníc a profilov** (pridanie / odstránenie v Nastaveniach)
- každý widget možno priradiť k ľubovoľnej stanici z profilov cez tray menu alebo prepínač **⇄** v hlavičke
- viac widgetov jednej stanice zdieľa **jediný polling cyklus**; odlišné stanice sa načítavajú samostatne
- presúvanie widgetov myšou a zapamätanie pozícií
- Windows CI zostavenie a parser smoke testy

**Zatiaľ neimplementované:**
- Local API a Auto/fallback medzi zdrojmi
- rôzne typy/layouty widgetov, SQLite história, grafy, desktop updater
- export/import Desktop konfigurácie a samostatný installer
- farebné UI stavy a autostart

## Spustenie na Windows 10/11

Potrebný je .NET 8 SDK s Windows Desktop podporou (prípadne Visual Studio 2022).

```powershell
git clone --branch cloud-api https://github.com/mpca86/ecowitt-rainmeter.git
cd ecowitt-rainmeter

dotnet restore src/Desktop/EcowittWeather.sln
dotnet build src/Desktop/EcowittWeather.sln -c Release
dotnet run --project src/Desktop/EcowittWeather.Desktop -c Release
```

Pri prvom spustení sa otvorí formulár, do ktorého sa API kľúče zadávajú **len lokálne**.
Nikdy ich neukladaj do Git repozitára.

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
4. V otvorenom widgete možno stanicu kedykoľvek zmeniť cez **⇄**.

Odlišné stanice používajú samostatné načítanie dát; dva widgety tej istej stanice zdieľajú jedno načítanie. Označenia CH1–CH8 sú nezávislé pre každú stanicu.

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
Aktuálna vývojová verzia: `0.2.0-preview.1`.

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
