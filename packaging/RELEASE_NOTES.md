# Ecowitt Weather v1.11.0-beta.4

Štvrtá verejná beta verzia Rainmeter skinu **Ecowitt Weather – Cloud API**.

## Účel vydania

Táto verzia bola prvým vydaním určeným na overenie kompletného procesu aktualizácie priamo zo skinu z beta.3.

Očakávaný postup:

`METEO CLOUD ●` → **Aktualizácie** → **AKTUALIZOVAŤ** → záloha → stiahnutie → overenie SHA256 → inštalácia → obnovenie Rainmetera.

## Zmeny

- Aktualizované číslo balíka a všetky Rainmeter metadata na `v1.11.0-beta.4`.
- Vynútený UTF-8 výstup z Windows PowerShell 5.1, aby Rainmeter RunCommand dostával správne dekódovaný text.
- Zachovaná oprava vyhľadávania GitHub prerelease verzií z beta.3.
- Pri aktualizácii sa zachovávajú `CloudSecrets.inc`, `UserVariables.inc`, história, diagnostika a stav aktualizácií.

## Test

Na inštalácii beta.3 s kanálom **Beta** má skin rozpoznať `v1.11.0-beta.4`, zobraziť indikátor pri **METEO CLOUD** a umožniť aktualizáciu bez ručnej inštalácie nového `.rmskin` balíka.
