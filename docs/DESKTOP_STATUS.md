# Desktop Edition – implementačný stav

## v0.2.0-alpha.2

Implementované:

- WPF desktop aplikácia a viaceré widgety / profily Ecowitt staníc
- Cloud API v3 vrátane načítania zoznamu staníc podľa kľúčov
- Local API (`/get_livedata_info`) podľa vzorovej odpovede GW3000
- hybridný režim Auto, preferujúci LAN a prechádzajúci na Web API pri výpadku
- periodický návrat k Local API s dvojminútovým odstupom pri nedostupnosti
- normalizácia meraní teploty, vlhkosti, vetra, tlaku, zrážok, UV, žiarenia, CH1–CH8
- zdieľané načítanie dát pre widgety jednej stanice
- pravý klik na widget pre kontextovú ponuku; ľavé potiahnutie pre presun
- jednotná tmavá šablóna hlavnej aj vnorenej ponuky s automatickým UI kontraktovým testom
- Windows DPAPI pre API kľúče, samostatný Desktop GitHub alpha updater a changelog

## Dátový tok

```text
StationProfile (Cloud / Local / Auto)
    |
    v
StationWeatherRouter
    |        |
    v        v
Local API   Ecowitt Cloud API v3
    |        |
    +----+---+
         |
         v
WeatherSnapshot
         |
         v
WeatherViewModel
    |         |
    v         v
Widget 1    Widget N
```

## Overenie

Windows CI: zostavenie, parser, Local gateway URL, výpadok a návrat hybridného režimu,
migrácia nastavení, výber alpha release, šablóny WPF a gestá myši.

Automatické testy **nenahrádzajú test na reálnom gatewayi a vizuálnu kontrolu** v používateľskom Windows prostredí.

## Ďalšie priority

1. Prakticky otestovať alpha.1 → alpha.2 cez zabudovaný aktualizátor.
2. Otestovať Local API s fyzickým gatewayom vrátane režimu Auto a výpadku siete.
3. Pridať ďalšie šablóny widgetov a panel diagnostiky pripojení.
4. História SQLite, grafy a trendové ukazovatele.
5. Voliteľný štart s Windows a bezpečný export/import Desktop konfigurácie.
