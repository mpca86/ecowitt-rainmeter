# Desktop Edition – architektúra

## Cieľ

Desktop Edition bude samostatná Windows aplikácia postavená na .NET 8 + WPF. Nemá byť závislá od Rainmetera a má podporovať viac nezávislých widgetov, staníc a zdrojov dát.

Rainmeter Edition zostáva samostatnou distribučnou vetvou produktu. Existujúce verejné balíky a updater ostávajú kompatibilné.

## Edície

- **Rainmeter Edition** – aktuálny skin a updater.
- **Desktop Edition** – natívna Windows aplikácia s tray ikonou, widgetmi, nastaveniami, históriou a aktualizáciami.

Repozitár sa zatiaľ nepremenúva, aby sa nerozbili existujúce odkazy a self-updater Rainmeter edície.

## Dátové zdroje Desktop Edition

Každý profil / stanica môže používať jeden z režimov:

1. **Local API**
   - priame čítanie gatewayu v LAN,
   - nízka latencia,
   - funguje bez internetu.

2. **Web API**
   - Ecowitt Web API v3,
   - funguje mimo lokálnej siete,
   - vyžaduje Application Key, API Key a MAC.

3. **Auto / hybrid**
   - preferuje Local API,
   - pri nedostupnosti lokálneho gatewayu prejde na Web API,
   - po návrate lokálneho zdroja sa môže vrátiť späť.

Zdroj dát je oddelený od UI. Widget nepozná HTTP endpoint; pracuje iba s normalizovaným modelom počasia.

## Viac staníc a viac widgetov

Desktop Edition bude navrhnutá ako multi-profile / multi-widget systém.

Príklad:

- Profil **Kancelária**
  - Web API
  - stanica A

- Profil **Dom**
  - Auto: Local API + Web API fallback
  - stanica B

- Profil **Mrazová dolina**
  - Local API
  - stanica C

Používateľ môže vytvoriť viac widgetov a každý widget môže používať iný profil a inú šablónu.

Príklad desktopu:

- Widget 1: Kancelária – kompaktné aktuálne počasie
- Widget 2: Dom – interiérové senzory
- Widget 3: Dom – tlak + trend + zrážky
- Widget 4: Mrazová dolina – minimum / maximum / trend

Nie je nutné mať jeden veľký widget so všetkými údajmi.

## Navrhované vrstvy

```text
src/Desktop/
├─ EcowittWeather.Core/
│  ├─ Models/
│  ├─ Abstractions/
│  ├─ Sensors/
│  └─ Trends/
│
├─ EcowittWeather.Infrastructure/
│  ├─ Ecowitt/
│  │  ├─ Local/
│  │  └─ Cloud/
│  ├─ Storage/
│  ├─ Security/
│  └─ Updates/
│
└─ EcowittWeather.Desktop/
   ├─ Views/
   ├─ ViewModels/
   ├─ Widgets/
   ├─ Tray/
   └─ Services/
```

## Normalizovaný model

Oba API adaptéry mapujú svoje odpovede do spoločného modelu, napr.:

```text
WeatherSnapshot
├─ Timestamp
├─ Source
├─ Outdoor
│  ├─ Temperature
│  └─ Humidity
├─ Indoor / Gateway
├─ Pressure
├─ Wind
├─ Rain
├─ Solar
├─ UV
└─ Sensors[]
   ├─ Channel
   ├─ Name
   ├─ Temperature
   ├─ Humidity
   └─ Battery
```

Widgety potom nerozlišujú, či údaje prišli z Local API alebo Web API.

## Profily

Navrhovaný model konfigurácie:

```json
{
  "profiles": [
    {
      "id": "office",
      "name": "Kancelária",
      "sourceMode": "Web",
      "cloud": {
        "mac": "..."
      }
    },
    {
      "id": "home",
      "name": "Dom",
      "sourceMode": "Auto",
      "local": {
        "host": "192.168.1.50"
      },
      "cloud": {
        "mac": "..."
      }
    }
  ]
}
```

API kľúče sa nemajú ukladať v plaintext JSON. Budú uložené cez Windows DPAPI / Windows credential storage.

## Widget model

Každý widget bude samostatná konfigurácia:

```json
{
  "id": "widget-office-main",
  "profileId": "office",
  "template": "CurrentWeather",
  "position": { "x": 40, "y": 80 },
  "alwaysOnTop": false,
  "locked": true,
  "opacity": 0.9
}
```

Widgety môžu mať rôzne typy:

- Current Weather
- Sensors
- Pressure
- Rain
- Wind
- Min / Max
- History / graph
- Custom dashboard

## Aplikačný model

Jedna aplikácia beží v tray, ale spravuje ľubovoľný počet widget okien.

```text
EcowittWeather.Desktop.exe
        │
        ├─ Tray
        ├─ Profile manager
        ├─ Data source manager
        ├─ History service
        ├─ Update service
        │
        ├─ Widget window #1
        ├─ Widget window #2
        └─ Widget window #N
```

Takto nebude každé okno samostatný proces a rovnaká stanica nebude zbytočne dotazovaná viackrát.

## Cache a polling

DataSourceManager bude zdieľať jednu dátovú reláciu na profil:

- jeden HTTP polling cyklus pre profil,
- výsledný WeatherSnapshot sa publikuje všetkým widgetom,
- jednotlivé widgety sa iba prekreslia.

To je dôležité najmä pri Web API kvôli limitom a pri viacerých widgetoch.

## História

Desktop Edition bude používať SQLite.

Minimálne tabuľky:

- profiles
- observations
- sensor_observations
- widget_layouts

To umožní:

- denné minimum / maximum,
- čas minima / maxima,
- 15 min trend teploty,
- 3 h tendencia tlaku,
- 24 h / 7 d / 30 d grafy,
- export CSV.

## Fázy vývoja

### Fáza 1 – MVP
- WPF shell
- tray ikona
- jeden profil
- Web API
- jeden Current Weather widget
- Settings

### Fáza 2
- Local API
- Auto / hybrid režim
- viac profilov
- viac widgetov

### Fáza 3
- SQLite história
- grafy
- min/max
- diagnostika

### Fáza 4
- updater
- export/import konfigurácie
- vlastné widget templates
- stabilná Desktop Edition
