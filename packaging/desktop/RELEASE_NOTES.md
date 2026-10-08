# Ecowitt Weather Desktop v0.2.0-alpha.1

Prvé samostatné **alpha vydanie Desktop Edition** pre Windows 10/11 (64-bit).
Rainmeter nie je potrebný.

## Novinky

- Samostatná aplikácia vo Windows systémovej lište.
- Podpora viacerých meteorologických staníc Ecowitt a viacerých widgetov.
- Cloud API v3 a načítanie staníc priradených k API účtu.
- Jeden dotaz na stanicu obsluhuje všetky widgety, ktoré ju zobrazujú.
- Každý widget má vlastné priradenie stanice a názvy senzorov CH1–CH8.
- **Ľavý klik na widget otvorí kontextovú ponuku**: zmeniť stanicu, nastavenia, aktualizácie, presunúť alebo zatvoriť.
- Odstránené rušivé ikony z hlavičky widgetov.
- Kontrastný tmavý výber staníc a tmavá kontextová ponuka.
- Vlastná ikona počasia, metadáta autora a slovenská sekcia O programe.
- Zabudovaná **kontrola alpha aktualizácií** z GitHub Releases.
- Overené stiahnutie ZIP cez SHA-256, zálohovanie pred výmenou súborov a pokus o obnovu pri chybe.
- Automatická migrácia nastavení zo starších preview verzií.
- API kľúče sú uložené v šifrovanom lokálnom súbore chránenom Windows DPAPI.

## Inštalácia

1. Stiahni `EcowittWeather-Desktop-v0.2.0-alpha.1-win-x64.zip`.
2. Rozbaľ ZIP do vlastného priečinka, napríklad `C:\Apps\EcowittWeather\`.
3. Spusti `EcowittWeather.Desktop.exe`.
4. Vyplň Ecowitt Application Key a API Key, načítaj zoznam staníc a ulož nastavenia.

Neodporúčame spúšťať aplikáciu priamo z otvoreného ZIP ani z chráneného priečinka Program Files. Aktualizátor musí mať oprávnenie zapisovať do priečinka aplikácie.

## Ovládanie

- Ľavý klik na plochu widgetu: kontextová ponuka.
- Shift + ľavé potiahnutie: presun widgetu.
- Pravý klik na ikonu v systémovej lište: správa všetkých widgetov, nastavenia, aktualizácie a O programe.

## Stav alpha

V tejto verzii funguje **Ecowitt Web API**. Local API, automatické prepínanie Local/Web, grafy a SQLite história sú plánované.

Aktualizátor je súčasťou tejto alpha verzie; jeho kompletný proces bude možné prakticky otestovať pri nasledujúcom alpha vydaní.
