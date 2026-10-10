# T11-C1 – Oddelenie preview a produkcie (Ecowitt Rainmeter)

**Stav k 11. 10. 2026:** architektúra oddelená, test izolácie **PASS** na oboch jazykových verziách (potvrdené používateľom v prehliadači). Testovacia značka následne odstránená z Git vetvy `website-preview` cez PR #19; **výsledok nového HostCreators deployu a zmiznutie značky na živom preview po PR #19 ešte vyžadujú používateľské potvrdenie**. Dokumentácia vznikla v samostatnom Draft PR a jej merge nie je súčasťou tohto kroku.

## Architektúra a hranice

| Úloha | Git vetva | HostCreators Git aplikácia / subdoména | Výsledná verejná cesta |
|---|---|---|---|
| Preview – ďalší vývoj a schvaľovanie | `website-preview` | `https://preview.mpca86.sk/` | `/mpca86.sk/sub/preview/current/site` |
| Produkcia – len schválené vydania | `website-production` | `https://rainmeter.mpca86.sk/` | `/mpca86.sk/sub/rainmeter/current/site` |
| Izolovaný CI/CD experiment T10 | `hostcreators-test` | `https://testgit.mpca86.sk/` | samostatná testovacia aplikácia |

- Všetky tri používajú repozitár `mpca86/ecowitt-rainmeter`; produkčné a preview publikovanie má **rozdielne Git vetvy, samostatné HostCreators aplikácie aj samostatné release adresáre**.
- Verejný statický web je v adresári `site/`. Build preflight: `bash deploy/preflight.sh` pred krokom Deploy; HostCreators prepína symlink `current` na úspešný release.
- V nastaveniach subdomény HostCreators je základný adresár `/mpca86.sk/sub/rainmeter/`. HostCreators sám zobrazuje „Výsledná cesta ku GIT aplikácii“ `/mpca86.sk/sub/rainmeter/current/site`. **Nezadávať** túto odvodenú cestu do základného adresára ešte raz.
- `main` je primárne zdrojová vetva aplikácie Rainmeter. `feature/t11-site-mvp` je pôvodná vývojová vetva; **už nie je** sledovaná preview Git aplikáciou.
- Pôvodný PR #14 (`feature/t11-site-mvp` → `hostcreators-test`) zostáva Draft, bez merge. Nezlúčiť omylom do T10.

## GitHub ochrana vetiev a CI

| Vetva | GitHub Actions workflow | Názov povinnej kontroly |
|---|---|---|
| `website-preview` | `.github/workflows/website-preview-ci.yml` | `website-preview-preflight` |
| `website-production` | `.github/workflows/website-production-ci.yml` | `website-production-preflight` |
| `hostcreators-test` | `.github/workflows/hostcreators-preflight.yml` | `static-site-preflight` |

Rulesety **Protect website-preview** a **Protect website-production** sú aktívne. Vyžadujú Pull Request, **0** povinných externých schválení, úspešný príslušný status check, vyriešené konverzácie; blokujú force push a odstránenie vetvy; bypass list je prázdny. Nové workflowy majú read-only oprávnenia a nenasadzujú na HostCreators priamo – nasadenie spúšťa HostCreators cez branch-push webhook.

**CI PASS nie je to isté ako úspešné nasadenie.** GitHub Actions overuje základný statický preflight. HostCreators build log, živé URL, funkčné jazykové odkazy a výsledný commit treba preveriť samostatne.

## História realizácie a dôkazy

1. **Audit:** Zoznam HostCreators pôvodne potvrdil iba aplikácie `testgit` a `preview`; subdoména `rainmeter` používala dokumentový koreň `/mpca86.sk/sub/preview/current/site/`. Produkcia a preview teda pôvodne zobrazovali rovnaké nasadenie.
2. **Vetvy:** `website-preview` založená z otestovanej `feature/t11-site-mvp`; `website-production` založená zo stabilného `hostcreators-test`. Bez zásahu do `main`.
3. **CI:** PR [#15](https://github.com/mpca86/ecowitt-rainmeter/pull/15) (preview) → squash `d333a28`; PR [#16](https://github.com/mpca86/ecowitt-rainmeter/pull/16) (produkcia) → squash `21b77e8`. Používateľ potvrdil zelené *push* workflow behy po merge.
4. **Ochrana:** Active Rulesety oboch webových vetiev vrátane samostatných povinných preflight kontrol.
5. **Produkčný web:** Draft PR [#17](https://github.com/mpca86/ecowitt-rainmeter/pull/17) po kontrole zlúčený do `website-production`, commit [`5f34916`](https://github.com/mpca86/ecowitt-rainmeter/commit/5f349166b7b3887c6c70ff83b7523b9db86e40fe). Obsah: SK/EN statický web, CSS, dva autentické obrázky a poznámky; preview-only workflow sa nepreniesol. HostCreators log potvrdil rovnaký commit, `[PREFLIGHT] PASS`, úspešný deploy za ~2 s.
6. **Oddelené nasadenie:** Nová produkčná Git aplikácia HostCreators používa samostatný adresár `/mpca86.sk/sub/rainmeter/` a vetvu `website-production`. Používateľ potvrdil funkčné URL produkcia SK, produkcia EN aj preview. Nastavenia subdomény potvrdili odvodený document root `.../rainmeter/current/site`.
7. **Preview prechod:** HostCreators aplikácia `preview` bola prepnutá z `feature/t11-site-mvp` na `website-preview`; History ukázala úspešné spustenie pre commit `d333a28`.
8. **Test izolácie:** PR [#18](https://github.com/mpca86/ecowitt-rainmeter/pull/18) zmenil presne dve HTML pätičky s `PREVIEW · T11-C1.6`. Po CI PASS bol zlúčený iba do `website-preview`, squash commit [`2bef702`](https://github.com/mpca86/ecowitt-rainmeter/commit/2bef702e328199330f6f5a88d2089f49649280de). Používateľ na živých stránkach potvrdil: značka je na preview **SK aj EN**, na produkcii **SK ani EN nie je** – výsledok **PASS**.
9. **Upratanie:** PR [#19](https://github.com/mpca86/ecowitt-rainmeter/pull/19) odstránil presne dve značky, CI pred merge PASS; schválený squash commit [`518def3`](https://github.com/mpca86/ecowitt-rainmeter/commit/518def3d927b4f24f61bca204db82c1aa724b741) v `website-preview`. GitHub obsah teraz nemá značku v SK/EN preview; produkčná vetva nebola zmenená.

## Bežná prevádzka: vývoj → preview → produkcia

1. Vytvor feature vetvu z `website-preview`. V PR do `website-preview` over diff a check `website-preview-preflight`.
2. Po schválení vykonaj merge; HostCreators automaticky nasadí **len preview**. Skontroluj jeho Históriu, deploy SHA, preflight a stránky SK (`/`), EN (`/en/`) vrátane CSS/obrázkov.
3. Keď je zmena pripravená na verejné vydanie, priprav **samostatný release PR do `website-production`**, iba so schváleným obsahom. Neprepisuj produkčný workflow preview-only konfiguráciou.
4. Potvrď `website-production-preflight`, skontroluj rozdiely, vyžiadaj si výslovný súhlas s merge. Po merge sleduj produkčný HostCreators deploy a skontroluj produkčné URL v oboch jazykoch.
5. Práca s `website-production`, DNS, HostCreators a PR #14 je oddelená od bežných preview zmien.

## Rollback a kontrola incidentu

- Pri chybe na **preview** alebo **produkcii** najprv otvor HostCreators **História** a deploy log; zisti aktuálnu Git vetvu a SHA, preflight log a stav `current`.
- Obnov posledný známy dobrý release cez podporovaný rollback mechanizmus HostCreators (ak je dostupný), inak priprav **revert commit cez PR do príslušnej chránenej vetvy**, po PASS CI ho schváľ a mergni. Neobchádzaj Ruleset force pushom.
- Pôvodný odkaz `/mpca86.sk/sub/preview/current/site/` bol len predmigračné smerovanie a **nie je vhodný trvalý produkčný rollback**; prepájal by produkciu späť s vývojom.
- Pre produkciu kontroluj `/mpca86.sk/sub/rainmeter/current/site`, nie `/mpca86.sk/sub/rainmeter/` ako ručne zadaný výsledný document root; odvodzuje ho Git aplikácia.
- Pri nedostupnosti jednej domény over HTTPS, nastavenie subdomény a dokumentový koreň samostatne; neprepájaj náhodne dva `current` odkazy.

## Kontrola dokončenia T11-C1

- [x] Oddelené vetvy, GitHub Rulesety a nezávislé CI kontroly.
- [x] Samostatné HostCreators Git aplikácie a základné adresáre; potvrdené produkčné `current/site`.
- [x] Produkčný deploy správneho commitu a funkčné SK/EN URL, HTTPS podľa používateľa.
- [x] Preview deploy po prepnutí vetvy; úspešné testovacie označenie na preview SK a EN, absencia na produkcii SK a EN.
- [x] Testovacie označenie odstránené z Git vetvy `website-preview` PR #19.
- [ ] Po PR #19 overiť v **HostCreators preview História** nasadenie SHA `518def3` a preflight PASS.
- [ ] Po PR #19 skontrolovať, že značka zmizla aj na **živom** `preview.mpca86.sk/` a `preview.mpca86.sk/en/` (obnoviť bez cache); produkcia nezmenená.
- [ ] Dokumentačný PR schváliť a zlúčiť samostatne.

## Publikačný stav a obsahové poznámky

SK/EN stránky majú `noindex, nofollow`. To ponechať až do samostatne schváleného verejného spustenia. Text webu rozlišuje README vývojový základ `v1.10.0-alpha` a autentický screenshot Cloud API beta `v1.11.0-beta.6`; pred verejným release preveriť aktuálnu verziu a oficiálny .rmskin inštalátor. Nikdy nepublikovať API kľúče, MAC adresy, tokeny ani webhook podpisy.

Ďalší krok mimo rozsahu T11-C1: zmysluplné vydania a dokumentácia inštalácie Local/Cloud API. Súvisiaca stránková špecifikácia: [website-mvp.md](website-mvp.md).
