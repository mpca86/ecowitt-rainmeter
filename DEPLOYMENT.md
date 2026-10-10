# HostCreators – Git Build & Deploy (testgit.mpca86.sk)

> **Rozsah:** izolovaný testovací web projektu Ecowitt Rainmeter, nie samotný Rainmeter skin (aplikácia pre Windows).
> Tento dokument platí pre vetvu `hostcreators-test`. Zmeny v `main` nie sú súčasťou tohto testovacieho deployu.

## 1. Overená konfigurácia

| Nastavenie | Hodnota |
|---|---|
| Hosting | HostCreators |
| Web | https://testgit.mpca86.sk/ |
| GitHub repozitár | `mpca86/ecowitt-rainmeter` |
| Automaticky nasadzovaná vetva | `hostcreators-test` |
| Public/document root, relatívne ku koreňu repozitára | `site` |
| Stránka | `site/index.html` |
| Build krok – cieľový stabilný príkaz | `bash deploy/preflight.sh` |
| Nasadenie | štandardný krok `Deploy` v HostCreators, po úspešnom BASH kroku |
| Upozornenia | e-mailové výstupy zapnuté v HostCreators; adresu a tajomstvá neukladať do Git repozitára |

**Prechodný stav T09:** Pred zmenou v HostCreators je v Build & Deploy nastavený starý príkaz
`bash deploy/t06-build-check.sh`. Nový skript `deploy/preflight.sh` sa má najprv otestovať a až potom možno starý T06 skript odstrániť.

## 2. Prvé nasadenie alebo kontrola nastavení

1. Vo WebAdmine otvor GIT aplikáciu pre `testgit.mpca86.sk`.
2. Over repozitár `mpca86/ecowitt-rainmeter`, vetvu `hostcreators-test` a relatívnu public cestu `site`.
3. V časti **Build & deploy** nastav **BASH** krok `bash deploy/preflight.sh` medzi `Spustenie` a `Deploy`; starý T06 BASH krok nahraď, nepridávaj oba.
4. Ulož nastavenia a urob jeden kontrolný manuálny deploy (iba pri migrácii konfigurácie).
5. V logu musí byť `[PREFLIGHT] PASS: static site is ready for deployment` a následne úspešné nasadenie.
6. Skontroluj, že stránka vracia obsah `Testovacia verzia 1` a dorazí úspešný e-mail.

Po overení funguje automatický deploy po commite do `hostcreators-test`. **Neprepínaj na `main`** a nenastavuj root repozitára ako public adresár.

## 3. Bežný vývoj a publikovanie

- Pracuj v samostatnej vývojovej vetve. Test `test-branch-isolation` potvrdil, že jej commit sa na web nenasadí.
- Skontroluj diff (žiadne tajomstvá, nesprávne súbory či nedokončené zmeny), až potom zmeny začleň do `hostcreators-test`.
- GitHub push do `hostcreators-test` aktivuje HostCreators webhook a nový build.
- Over: (a) commit SHA v Histórii HostCreators, (b) `[PREFLIGHT] PASS` v logu, (c) úspešné nasadenie, (d) obsah webu, (e) e-mailové hlásenie.
- Na test webu použi URL s jednorazovým query parametrom, aby sa znížil vplyv cache. Parametrizácia URL **nezaručuje** vymazanie všetkých vrstiev cache.

## 4. Rollback (bez prepisovania histórie)

HostCreators používa samostatné `release_*` adresáre a symlink `current`, ale podľa dokumentácie uchováva iba posledný zostavený release. **Nevychádzaj z predpokladu, že vieš prepnutím symlinku obnoviť ľubovoľný historický release.**

Pre statickú stránku obnov posledný overený obsah novým Git commitom (bez `git push --force`):

```bash
git switch hostcreators-test
git pull --ff-only
git restore --source=<LAST_GOOD_COMMIT> -- site/
git add site/
git commit -m "revert(site): restore last known good content"
git push origin hostcreators-test
```

`<LAST_GOOD_COMMIT>` nahraď **reálnym** overeným SHA z GitHubu/Histórie. Tento príklad vracia iba `site/`, nie build konfiguráciu. Ak chyba vznikla v skripte, obnov aj príslušný súbor `deploy/preflight.sh` alebo oprav konfiguráciu v HostCreators.

Po rollbacku vždy skontroluj log, aktuálnu stránku a notifikáciu. Databázy a používateľské súbory sú mimo rozsahu tohto testu; vyžadujú samostatné zálohy a obnovovací plán.

## 5. Incidenty a diagnostika

| Príznak | Diagnostika / akcia |
|---|---|
| Nová verzia sa neobjaví | Over push do správnej vetvy, Históriu a SHA; nespúšťaj manuálne skôr, než zistíš, či webhook zlyhal |
| `[PREFLIGHT] FAIL` | Čítaj presný dôvod chyby v logu; oprav súbor alebo štruktúru; až potom nový commit |
| BASH krok nie je spustený | Skontroluj, či je v HostCreators príkaz `bash deploy/preflight.sh` a či stojí pred `Deploy` |
| Build zlyhal | Nový release sa nemá aktivovať. Over funkčnosť posledného webu a chybový e-mail |
| GitHub ukazuje nový commit, web starý | Skontroluj Históriu, public cestu `site`, CDN/browser cache a obsah nasadeného súboru |
| E-mail nepríde | Over adresu v nastaveniach, spam a log v HostCreators |

Pri naliehavom incidente nemeň naraz viac vecí. Najprv identifikuj posledný funkčný commit, vykonaj izolovanú opravu alebo Git rollback a potvrď funkčnosť webu.

## 6. Bezpečnosť a prevádzkové pravidlá

- `site/` je verejne prístupný webový adresár; drž v ňom iba súbory určené na publikovanie.
- API kľúče, heslá, privátne SSH kľúče, osobné údaje a produkčné `.env` **nikdy** necommituj; používaj zabezpečené nastavenia hostingu / správcu tajomstiev.
- `deploy/preflight.sh` kontroluje len niekoľko konkrétnych rizík pre statický web; nie je to komplexný audit repozitára.
- Nevkladaj tajomstvá do build výpisu; build log sa môže posielať e-mailom. Nepoužívaj `set -x` pri práci s tajomstvami.
- `<meta name="robots" content="noindex, nofollow">` nie je ochrana prístupu. Pre neverejné dáta použi autentifikáciu.
- Pred nasadením PHP/MySQL aplikácie (napr. Lovci Mrazov) samostatne rieš migrácie, zálohy DB, trvalé uploady, cron úlohy, oprávnenia a staging prístup.

## 7. História testov (10. 10. 2026)

| Test | Výsledok | Poznámka |
|---|---|---|
| T01 – prvé nasadenie | PASS | `28c49dc` |
| T02/T03 – GitHub update a automatický deploy | PASS | `2e98048`, verzia 2 |
| T04 – Git rollback | PASS | `2c498be`, opäť verzia 1 |
| T05 – izolácia vetiev | PASS | `5698887` v inej vetve nemal vplyv na web |
| T06 – kontrolovaný neúspešný build | PASS | `ae8ccd3`, `exit 42`, starý web zostal dostupný |
| T07 – zotavenie | PASS | `086587e`, automatický úspešný deploy |
| T08 – e-mailové hlásenia | PASS (doručenie) | úspech aj chyba, obnovenie cez `54ebf62`; detail chybového e-mailu nebol úplne skontrolovaný |
| T09 – stabilizácia | IN PROGRESS | nový `deploy/preflight.sh`, dokumentácia; treba dokončiť zmenu BASH príkazu v HostCreators a overiť |

## Dokumentácia HostCreators

- https://www.hostcreators.sk/pomoc/git-build-deploy/
- https://www.hostcreators.sk/pomoc/git-build-deploy/ako-funguje-git-build-deploy.html
- https://www.hostcreators.sk/pomoc/git-build-deploy/deploy-git-aplikacie.html
