# Ecowitt Weather v1.11.0-beta.6

Šiesta verejná beta verzia Rainmeter skinu **Ecowitt Weather – Cloud API**.

## Čo je nové

- Do panela **Nastavenia** pribudol **EXPORT** a **IMPORT** kompletnej používateľskej konfigurácie.
- Export vytvára jeden prenosný súbor `.ecowittconfig`.
- Balík obsahuje Ecowitt Application Key, API Key, MAC adresu stanice, názvy senzorov, refresh interval a update kanál.
- Import pred prepísaním automaticky vytvorí zálohu aktuálnej konfigurácie.
- Po úspešnom importe sa Rainmeter automaticky obnoví.
- Import prijíma iba očakávané konfiguračné súbory a overuje základný manifest balíka.

## Bezpečnosť

Exportovaný `.ecowittconfig` obsahuje API kľúče a **nie je šifrovaný**. Uchovávaj ho ako citlivý súbor a nezverejňuj ho na GitHube ani vo verejnom úložisku.

## Aktualizácia

Používatelia verzie `v1.11.0-beta.5` môžu aktualizovať priamo cez:

`METEO CLOUD ●` → **Aktualizácie** → **AKTUALIZOVAŤ**
