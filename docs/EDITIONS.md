# Ecowitt Weather – edície

Projekt sa rozdeľuje na dve používateľské edície.

## Rainmeter Edition

Aktuálna produkčná/beta implementácia:

```text
src/EcowittWeather/
```

Tento adresár sa zatiaľ nepresúva, aby sme zachovali kompatibilitu existujúceho updatera, GitHub Releases a nainštalovaných beta verzií.

Dlhodobý cieľ:

```text
src/Rainmeter/EcowittWeather/
```

Presun sa urobí až spolu s migráciou release workflow a self-updatera.

## Desktop Edition

Nová natívna Windows aplikácia:

```text
src/Desktop/
```

Desktop Edition je od začiatku navrhnutá pre Local API, Web API, hybridný režim, viac staníc a viac widgetov.

Pozri [DESKTOP_ARCHITECTURE.md](DESKTOP_ARCHITECTURE.md).

## Aktuálny stav Desktop Edition

Prvý prototyp je implementovaný ako .NET 8 WPF solution s Cloud API,
kompaktným widgetom, Windows tray, nastaveniami a viacerými oknami
zdieľajúcimi jeden HTTP polling. Testovanie prebieha na Windows CI.

Aktuálny stav a ďalšie kroky sú v
[DESKTOP_STATUS.md](DESKTOP_STATUS.md).
