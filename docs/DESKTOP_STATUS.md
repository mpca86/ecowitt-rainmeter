# Desktop Edition – implementačný stav

Prvý funkčný *Cloud API vertical slice*:

```text
Ecowitt Web API v3
      |
      v
EcowittCloudSource
      |
      v
EcowittCloudParser
      |
      v
WeatherSnapshot (Core)
      |
      v
WeatherViewModel
      |
      +--> WeatherWidget 1
      +--> WeatherWidget 2
      +--> WeatherWidget N
```

`App` vlastní iba jeden HTTP klient a jeden plánovač dopytov. Každý otvorený
widget zdieľa `WeatherViewModel`; pridanie ďalšieho okna nezvyšuje počet
dotazov na Ecowitt Cloud API.

Pre zatiaľ jediný profil je podporované len Cloud API. Model `StationProfile`
už vyčleňuje `SourceMode` (Cloud / Local / Auto) a budúci Local API adaptér
implementuje rovnaké rozhranie `IWeatherSource`.

## Ďalšie kroky

1. Overiť WPF zostavenie na Windows a reálne Cloud API načítanie.
2. Pridať Local API adaptér pre gateway /get_livedata_info.
3. Pridať DataSourceManager s oddeleným pollingom pre viac profilov a automatickým fallbackom.
4. Pridať widget šablóny (Senzory, Tlak, Zrážky) a správcu rozloženia.
5. História SQLite, trendy, autostart, bezpečný export/import a desktop updater.

## Bezpečnosť

- API kľúče zostávajú v lokálnom DPAPI úložisku.
- Pri HTTP chybe neprezentujeme adresu požiadavky, keďže Ecowitt používa autentizáciu v query stringu.
- Žiadne reálne prihlasovacie údaje neboli vložené do GitHub súborov.
- Desktop Edition sa zatiaľ nevydáva ako verejný release.
