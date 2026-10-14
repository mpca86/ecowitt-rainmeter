# Ecowitt Weather Display — Prototype 0.1

Samostatný webový displej so **simulovanými údajmi**. Nepripája sa k Ecowitt, nemá API kľúče ani backend. Verejné merania zatiaľ neposkytuje. Rainmeter a Desktop Edition nemení.

## Spustenie

Z koreňa repozitára spustite `python -m http.server 8080 --directory weather-display` a otvorte http://localhost:8080. Súbory musia byť obsluhované cez HTTP, nie file:// (ES modules). Python je potrebný iba na tento jednoduchý lokálny server; alternatívou je ľubovoľný statický hosting.

Pre televízor nastavte rozlíšenie 1920 × 1080, orientáciu na šírku, zoom prehliadača 100 % a vypnite overscan (Just Scan/Screen Fit). F alebo tlačidlo zapne fullscreen; F11 je alternatíva. Pohyb myšou alebo C zobrazí ovládanie. Na Windows možno vytvoriť skratku Chrome/Edge s argumentmi `--kiosk http://localhost:8080`; lokálny server musí bežať pred prehliadačom. Automatické spustenie a prevádzku po výpadku napájania treba nakonfigurovať na konkrétnom PC.

## Demo

Deň / Dážď / Noc mení simulované podmienky. Meranie sa obnovuje každých 15 sekúnd. Výpadok zastaví nové merania, ponechá posledné hodnoty a ich čas. Po 45 sekundách sú neaktuálne, po 180 sekundách sa zobrazí výpadok. Obnoviť spojenie ihneď načíta demo meranie. Demo označenie zostáva vždy viditeľné.

Graf má 577 vzoriek v päťminútových krokoch za 48 hodín. Denné extrémy sa počítajú iba za dnešný deň Europe/Bratislava; pri rovnakej hodnote zostáva prvý čas. História je syntetická, pri obnovení sa znovu vytvorí a nepredstavuje perzistentný záznam. Nočný scenár simuluje UV/žiarenie; nie je astronomickým modelom. Tlak, vietor a ostatné sekundárne hodnoty sú demo scenáre.

## Overenie prvého behu

- `node --check weather-display/display.js`: PASS.
- `node --test weather-display/model.test.mjs`: 8 PASS (48 h, hranica dňa/zhody, výpadok/zotavenie, chýbajúce dáta, stabilná história, neplatné/budúce vzorky, polnoc pri výpadku).
- Browser QA, fullscreen, reálny TV a dlhodobá prevádzka: NEOVERENÉ; podporované browser QA v pracovnom prostredí nebolo dostupné.
- Rozloženie používa proporčné CSS a celé okno; referenčné 16:9. Treba ručne overiť 1366×768, 1920×1080, 2560×1440 a 3840×2160, zoom aj škálovanie OS. Pri 32" overiť čitateľnosť z 2–3 m, orezanie a viditeľnosť ovládania.

## Ďalšie etapy

1. Vizuálne schváliť displej na skutočnej obrazovke; upraviť veľkosti podľa čitateľnosti.
2. Overiť kontrakt Local/Web API, jednotky, čas merania a intervaly; kľúče iba v serverovej službe.
3. Navrhnúť backend s nezávislým zberom, trvalou históriou, dennými extrémami a read-only endpointom. Lokálny aj vzdialený prístup musí ísť cez tento kontrakt, nie cez kľúče v prehliadači.
4. Reálne výpadky, zotavenie, chýbajúce senzory, DST, dátové medzery a 24–72 h kiosk test.
5. SK/EN/CS/PL, konfigurácia stanice, automatické spustenie, prípadné posúvanie obrazu.

Žiadne produkčné nasadenie, merge ani release nie sú súčasťou tohto behu.

## Úpravy prototypu 0.1

Názov stanice možno nastaviť cez `?station=Poprad%20-%20Veľká`; vstup sa zobrazuje ako text. Počiatočný scenár nastaví `&scenario=night` (day/rain/night). Bez parametrov zostávajú pôvodné hodnoty.

Pri výpadku cez polnoc sa dnešné extrémy vyprázdnia, kým nepríde dnešné meranie. Graf počas výpadku zostáva poslednou zachovanou demo históriou. Prekrývajúce sa vzorky sa pri aktualizácii nemenia; teplotný cyklus zohľadňuje aj minúty. Browser QA a TV test zostávajú neoverené.

## Rýchly test na Windows

1. Stiahnite ZIP tejto vývojovej vetvy z GitHubu (Code → Download ZIP) a rozbaľte ho.
2. V adresári `weather-display` spustite `start-display.cmd`. Potrebný je Python 3. Otvorí sa predvolený prehliadač; okno servera nechajte otvorené.
3. Stlačte F alebo F11. Server je dostupný iba na tomto PC na `http://127.0.0.1:8080/`.
4. Na ukončenie servera použite Ctrl+C v jeho okne. Pri obsadenom porte spustite z terminálu `python serve.py --port 8081`.

Launcher nemení automatické spustenie Windows ani nastavenia prehliadača. Windows launcher zatiaľ nebol otestovaný na Windows; Python server bol overený lokálne.

## Vizuálny test T01

Na monitore alebo TV nastavte zoom 100 % a postupne skontrolujte:

| Krok | Očakávaný výsledok |
| --- | --- |
| T01-A: fullscreen | Celý displej, žiadne posuvníky, viditeľná pätička a graf |
| T01-B: hodnoty | Teplota, min/max a časy čitateľné; tlak a žiarenie sa neprekrývajú s popismi |
| T01-C: ovládanie | Po 5 s bez pohybu sa skryje; pohyb myšou alebo C ho zobrazí |
| T01-D: scenáre | Deň, dážď a noc aktualizujú hodnoty; označenie DEMO zostáva |
| T01-E: výpadok | Po viac než 45 s neaktuálne dáta; po viac než 180 s výpadok; posledný čas merania zostáva |
| T01-F: zotavenie | Obnoviť spojenie ihneď vráti aktuálny demo stav |

Čitateľnosť na 32-palcovej TV overte z 2–3 metrov. Výsledok hláste napríklad `T01-A PASS, T01-B tlak príliš malý` spolu s rozlíšením a zoomom. Toto je plán manuálneho testu, nie záznam jeho vykonania.
