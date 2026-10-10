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
| Build krok – overený stabilný príkaz | `bash deploy/preflight.sh` |
| GitHub CI | `.github/workflows/hostcreators-preflight.yml`, job `static-site-preflight` |
| Ochrana vetvy | Ruleset `Protect hostcreators-test` (Active), Pull Request povinný, 0 schválení, required check `static-site-preflight` |
| Nasadenie | štandardný krok `Deploy` v HostCreators, po úspešnom BASH kroku |
| Upozornenia | e-mailové výstupy zapnuté v HostCreators; adresu a tajomstvá neukladať do Git repozitára |

**T09 stabilizácia (10. 10. 2026):** Build krok HostCreators bol zmenený na `bash deploy/preflight.sh` a manuálne overený: commit `3283f40`, `[PREFLIGHT] PASS`, úspešné dokončenie za 2,55 s. Starý testovací súbor `deploy/t06-build-check.sh` bol následne odstránený commitom `274bdb6`. Aktuálne zmeny pri údržbe dokumentácie sa automaticky publikujú cez webhook; stav posledného deployu vždy over v Histórii HostCreators.

## 2. Prvé nasadenie alebo kontrola nastavení

1. Vo WebAdmine otvor GIT aplikáciu pre `testgit.mpca86.sk`.
2. Over repozitár `mpca86/ecowitt-rainmeter`, vetvu `hostcreators-test` a relatívnu public cestu `site`.
3. V časti **Build & deploy** musí byť **jediný BASH** krok `bash deploy/preflight.sh` medzi `Spustenie` a `Deploy`. Starý testovací skript bol z repozitára odstránený; nesmie byť nakonfigurovaný ako build krok.
4. Ulož nastavenia a urob jeden kontrolný manuálny deploy (iba pri migrácii konfigurácie).
5. V logu musí byť `[PREFLIGHT] PASS: static site is ready for deployment` a následne úspešné nasadenie.
6. Skontroluj, že stránka zodpovedá poslednému schválenému PR (po T10 má text `Testovacia verzia T10`) a dorazí úspešný e-mail.

Po overení funguje automatický deploy po commite do `hostcreators-test`. Táto vetva je od T10 chránená GitHub Rulesetom: nové zmeny posielaj cez Pull Request s úspešným `static-site-preflight`. **Neprepínaj na `main`** a nenastavuj root repozitára ako public adresár.

## 3. Bežný vývoj a publikovanie

1. Z vetvy `hostcreators-test` vytvor `feature/*` alebo `docs/*` vetvu. Test T05 potvrdil, že jej samostatné commity nenasadzujú web.
2. Urob a skontroluj zmeny v tejto vetve. Priprav Pull Request s **base `hostcreators-test`**, nie `main`.
3. Na PR musí prebehnúť GitHub Actions workflow **HostCreators preflight** s úspešným jobom **`static-site-preflight`**. Pri zlyhaní oprav zdroj a počkaj na nové CI PASS. Workflow je v `.github/workflows/hostcreators-preflight.yml` a volá `bash deploy/preflight.sh`.
4. Skontroluj `Files changed`, nepovoľ prenos tajomstiev ani nežiaduce zmeny konfigurácie. Ruleset `Protect hostcreators-test` je aktívny: vyžaduje PR a CI, nevyžaduje druhého schvaľovateľa (`Required approvals = 0`); blokuje force push aj odstránenie vetvy.
5. Merge vykonaj vedome až po CI PASS; pre samostatné textové zmeny preferuj **Squash and merge**. Test T10 použil PR #11 a squash commit `47ae8c7`.
6. Merge do `hostcreators-test` aktivuje HostCreators webhook a nový build. Over: (a) commit SHA v Histórii HostCreators, (b) `[PREFLIGHT] PASS` v logu, (c) úspešné nasadenie, (d) obsah webu, (e) e-mailové hlásenie.

Poznámka: T10 experimentom preukázal zlyhanie CI a následný úspešný run; individuálne kliknutie na blokované tlačidlo **Merge** počas FAIL nebolo samostatne zaznamenané. Nezamieňaj overené zlyhanie CI s priamym testom blokovania UI.
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
| T09 – stabilizácia | PASS | `3283f40`: `[PREFLIGHT] PASS`, 2,55 s; následne odstránený testovací skript `274bdb6` a finalizovaná dokumentácia `4fa5ba3` (potvrdený automatický deploy v HostCreators). |

| T10 – Pull Request + CI/CD | PASS | PR [#11](https://github.com/mpca86/ecowitt-rainmeter/pull/11): feature vetva, povinné CI, úmyselný CI FAIL a následný PASS; squash merge `47ae8c7`. HostCreators, webová verzia T10 aj e-mail potvrdené (10. 10. 2026). |

## 8. T10 – výsledky GitHub Pull Request workflow

- **GitHub Ruleset:** `Protect hostcreators-test`, stav Active, cieľová vetva `hostcreators-test`. Nastavené `Require a pull request before merging`, `Required approvals: 0`, `Require conversation resolution`, `Restrict deletions`, `Block force pushes` a `Require status checks to pass: static-site-preflight` (GitHub Actions). Bez povinného externého review.
- **CI workflow:** `.github/workflows/hostcreators-preflight.yml`, `pull_request` do `hostcreators-test` a `push` do tejto vetvy, oprávnenia iba `contents: read`, GitHub Actions checkout s pripnutým SHA, `bash deploy/preflight.sh`.
- **Pozitívny CI test:** run [38082743079](https://github.com/mpca86/ecowitt-rainmeter/actions/runs/38082743079) ukončený úspešne.
- **Negatívny CI test:** commit `8f77a10` zámerne upravil doctype iba na feature vetve; run [38083245628](https://github.com/mpca86/ecowitt-rainmeter/actions/runs/38083245628) skončil očakávaným FAIL. Cieľová vetva ani web sa nezmenili.
- **Obnova CI:** commit `068b5f5` vrátil správny doctype; run [38083289706](https://github.com/mpca86/ecowitt-rainmeter/actions/runs/38083289706) skončil PASS.
- **Schválený merge:** PR [#11](https://github.com/mpca86/ecowitt-rainmeter/pull/11) zlúčený metódou squash, výsledný commit `47ae8c72142f3d6b5b5446b43a449eb1c302ef49`. Cieľová vetva obsahuje web `Testovacia verzia T10` a CI workflow.
- **Deploy po merge:** podľa manuálne potvrdeného výsledku z HostCreators bol commit úspešne nasadený, na https://testgit.mpca86.sk/ sa zobrazila verzia T10 a e-mailové hlásenie dorazilo.
- **Nevykonané testy:** samostatné overenie neaktivovaného Merge tlačidla pri CI FAIL, bezpečnosť GitHub Ruleset bypass pravidiel pod inými aktérmi, PHP build, databázy a súbežné deploye.

## 9. Nasledujúca etapa

T11 – projektový web **Ecowitt Rainmeter**. Web bude dokumentačná a produktová prezentácia Windows Rainmeter skinu, nie samotná aplikácia ani serverový Ecowitt API proxy. Dizajn a obsah sa pripravia najprv v samostatnej feature vetve, potom PR + preflight, nakoniec staging deploy. Rozsah funkcií a právny text uvádza projektový plán T11; overiť ich pred publikovaním.

## Dokumentácia HostCreators

- https://www.hostcreators.sk/pomoc/git-build-deploy/
- https://www.hostcreators.sk/pomoc/git-build-deploy/ako-funguje-git-build-deploy.html
- https://www.hostcreators.sk/pomoc/git-build-deploy/deploy-git-aplikacie.html
