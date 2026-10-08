# Prehľad zmien

## v1.11.0-beta.6 — 2026-10-08

- Do panela Nastavenia pridaný export a import kompletnej používateľskej konfigurácie.
- Export vytvára jeden súbor `.ecowittconfig` s API kľúčmi, MAC adresou, názvami senzorov a používateľskými nastaveniami.
- Import pred prepísaním automaticky zálohuje existujúce lokálne nastavenia.
- Import spracuje iba očakávané konfiguračné súbory a po dokončení obnoví Rainmeter.
- Exportovaný balík nie je šifrovaný a treba ho uchovávať ako citlivý súbor.

## v1.11.0-beta.5 — 2026-10-07

- Do panela Aktualizácie pridaná sekcia ČO JE NOVÉ.
- Poznámky k vydaniu sa načítavajú z GitHub Release a zobrazujú priamo v Rainmeteri.
- Pridané tlačidlo OTVORIŤ CELÝ CHANGELOG.
- Opravené zobrazovanie času poslednej kontroly aktualizácií.
- Changelog a release notes sú odteraz písané po slovensky.
- Runtime súbor Changelog.txt je vylúčený z Git repozitára a distribučných balíkov.

## v1.11.0-beta.4 — 2026-10-07

- Štvrtá verejná beta verzia vetvy Cloud API.
- Prvé vydanie určené na overenie kompletného procesu aktualizácie priamo zo skinu z beta.3 na beta.4.
- Vynútený UTF-8 výstup z Windows PowerShell 5.1 pre správnu komunikáciu s Rainmeter RunCommand.
- Zachovaná oprava vyhľadávania GitHub prerelease verzií z beta.3.

## v1.11.0-beta.3 — 2026-10-07

- Opravené načítanie GitHub prerelease verzií vo Windows PowerShell 5.1.
- Do updatera pridané explicitné TLS 1.2 a verzia GitHub API.
- Kontrola beta aktualizácií už správne rozpoznáva publikované prerelease verzie.
- Jednorazová prechodová verzia pre používateľov beta.1 a beta.2, ktorých sa týkala chyba kontroly aktualizácií.

## v1.11.0-beta.2 — 2026-10-07

- Druhá verejná beta verzia Cloud API.
- Aktualizované všetky metadata na v1.11.0-beta.2.
- Vydanie určené na prvý praktický test aktualizácie beta.1 → beta.2 priamo zo skinu.
- Používateľské API údaje, aliasy senzorov, história a diagnostika zostávajú mimo distribučných súborov.

## v1.11.0-beta.1 — 2026-10-07

- Prvá verejná beta verzia Ecowitt Cloud API.
- Pridaný zdroj dát Ecowitt Web API v3 s Application Key, API Key a MAC adresou stanice.
- Pridané dynamické kanály CH1–CH8 pre teplotu a vlhkosť a lokálne aliasy senzorov.
- Pridané panely Nastavenia, Názvy senzorov, Diagnostika a Aktualizácie.
- Oddelené lokálne používateľské nastavenia a citlivé Cloud API údaje.
- Pridané aktualizačné kanály Stable / Beta / Development.
- Pridaný indikátor dostupnej aktualizácie pri nadpise METEO CLOUD.
- Pridané automatické zostavenie ZIP a validovaného .rmskin balíka cez GitHub Releases.
- Pridaný prvý Rainmeter Skin Installer balík.

## v1.10.0-alpha

- Opravené zobrazovanie UV indexu a slovenských kategórií UV.
- Pridané dynamické zobrazenie CH1–CH8.
- Pridané automatické vertikálne rozloženie a zmena výšky panela podľa počtu senzorov.
- Pridané aliasy kanálov s fallback názvom z API.
- Zachované 15-minútové trendy teploty a 3-hodinová tendencia tlaku.
- Pokračovanie modulárnej architektúry parsera Local API.

## Staršie alpha míľniky

- Regexové spracovanie JSON nahradené plnohodnotným Lua JSON parserom.
- Pridaný generický register Ecowitt senzorov.
- Pridané skiny Nastavenia a Diagnostika.
- Pridaná konfigurovateľná adresa a port gatewayu.
- Pridané ochrany pri prvom spustení bez konfigurácie.
