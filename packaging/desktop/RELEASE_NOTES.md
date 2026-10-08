# Ecowitt Weather Desktop v0.2.0-alpha.3

Tretie verejné alpha vydanie. Toto vydanie sa sústreďuje na **jednoduché
prvé spustenie a prehľadné Nastavenia**, bez zmeny spôsobu uloženia
existujúcich staníc a API kľúčov.

## Novinky

- Odstránený rušivý bledý tooltip pri prejdení myšou nad widgetom.
- Nastavenia sú rozdelené do piatich kariet:
  **Začíname · Stanice · Pripojenie · Senzory · Aplikácia**.
- Pri nenastavenej aplikácii sa automaticky zobrazí jednoduchý
  **trojkrokový úvodný sprievodca**.
- Nastavenia pripojenia zobrazujú len polia potrebné pre vybraný režim:
  Web API, Local API alebo Auto.
- Nové tlačidlo **Otestovať pripojenie** umožňuje preveriť skutočné
  merania bez nutnosti ukladať rozpracované nastavenia.
- Priame odkazy na oficiálnu stránku Ecowitt a
  [slovenský návod na prvé spustenie](https://github.com/mpca86/ecowitt-rainmeter/blob/cloud-api/docs/desktop/PRVE_SPUSTENIE.md).
- Stabilná tmavá podkladová šablóna kariet Nastavení vo Windows.
- CI regresné kontroly neprítomnosti tooltipu, rozloženia kariet a
  ovládacích prvkov pomocníka.
- Aktualizovaný samostatný [changelog Desktop Edition](https://github.com/mpca86/ecowitt-rainmeter/blob/cloud-api/src/Desktop/CHANGELOG.md).

## Bezpečnosť a ochrana súkromia

Ecowitt prihlasovacie meno a heslo **nikdy nezadávaj do Desktop aplikácie**.
Application Key a API Key vytvoríš vo svojom používateľskom profile na
oficiálnej stránke ecowitt.net. Desktop aplikácia ich používa pri prístupe
k Web API a chráni ich cez Windows DPAPI.

Režim **Local API** nevyžaduje žiadne Ecowitt API kľúče.

## Ako aktualizovať

V nainštalovanej alpha.2 vyber **pravý klik na widget → Aktualizácie**,
alebo použi ikonu aplikácie v systémovej lište.

**Existujúce nastavenia, widgety, API kľúče a rozloženie zostávajú zachované.**

Ak aktualizácia zo ZIP zlyhá, stiahni prenosný ZIP z tohto vydania,
ukonči aplikáciu a rozbaľ ho do pôvodného priečinka aplikácie.

## Známe obmedzenia

- Aktualizáciu a vzhľad treba overiť aj v používateľskom prostredí
  Windows 10/11; automatické testy nepokrývajú všetky motívy systému.
- Local API je podporované podľa odpovede kompatibilných Ecowitt gatewayov,
  nie každého možného senzora.
- História SQLite, grafy a samostatný inštalátor sú stále vo vývoji.
