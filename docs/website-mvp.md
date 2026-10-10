# T11-A – Ecowitt Rainmeter project website MVP

Status (11 Oct 2026): **bilingual SK/EN website deployed separately to preview (`website-preview`) and production (`website-production`)**. The original Draft PR #14 remains unmerged and must not be merged into `hostcreators-test`. See [T11-C1 – separated deployments](T11-C1-DEPLOYMENT.md) for exact environment roots, CI checks, change history and rollback steps.

## Purpose and accuracy

This is a landing site for the **Windows Rainmeter skin**, not a weather application running in the browser. The current project README on `main` identifies development baseline `v1.10.0-alpha`, Local API `/get_livedata_info`, and a separate Cloud API v3 adapter under development. The page does not imply that a packaged `.rmskin` release is already available. The hero uses an authentic, user-provided cropped METEO CLOUD screenshot from the Cloud API beta (displayed label `v1.11.0-beta.6`), **not** an illustration or simulated readings. It contains no visible API key fields or device MAC. No API credentials or cloud requests are used by the website. The `v1.10.0-alpha` label elsewhere refers to the independently documented baseline in `main`.

The Rainmeter project license is **CC BY-NC-SA 3.0**. This should not be described as OSI-approved open-source licensing. Any future screenshots and third-party logos/graphics require suitable rights and attribution.

## Content and structure

| Path | Content |
|---|---|
| `site/index.html` | Slovak landing page, `lang=sk` |
| `site/en/index.html` | English version, `lang=en` |
| `site/assets/css/site.css` | Shared responsive CSS, no external dependencies |
| `site/assets/ecowitt-meteo-cloud-screenshot.webp` | Authentic 265 × 335 WebP for the hero, user-provided, screened for visible secrets |
| `site/assets/ecowitt-cloud-beta-screenshot.webp` | Additional user-provided larger beta capture (not displayed on the landing page yet) |
| `docs/website-mvp.md` | This developer note; outside the public document root |

Both pages include: headline, honest alpha status, authentic METEO CLOUD beta screenshot, 6 capability cards, separate Local / Cloud state, 3 high-level getting-started steps, FAQ, GitHub + GitHub Issues, author and project-license credit. Locale switching uses normal links and does not rely on JavaScript. Navigation is semantic and keyboard accessible. No trackers or external web fonts.

## Website environments and release process (T11-C1)

- `website-preview` → HostCreators `preview.mpca86.sk` → `/mpca86.sk/sub/preview/current/site`; required GitHub Actions check `website-preview-preflight`.
- `website-production` → HostCreators `rainmeter.mpca86.sk` → `/mpca86.sk/sub/rainmeter/current/site`; required check `website-production-preflight`.
- `hostcreators-test` → `testgit.mpca86.sk` remains an isolated T10 Git deployment test. The former feature `feature/t11-site-mvp` is not tracked by HostCreators anymore; its original Draft PR #14 to `hostcreators-test` remains unmerged.
- Both website branches have active GitHub Rulesets: PR required, approvals 0, required CI, deletion/force-push protection. HostCreators runs `bash deploy/preflight.sh` before deploy. Always check deployed commit SHA separately.
- The production website sources were introduced by merged PR #17 (commit `5f34916`); isolation test PR #18 succeeded across SK/EN preview vs production; PR #19 removed the test marker from preview Git sources (commit `518def3`). **Live post-cleanup preview deployment confirmation is pending.**
- SK and EN web pages retain `robots: noindex, nofollow`; review before a separate public launch. Production changes require separate approval and a release PR; do not merge ordinary preview-only changes to production automatically.
- `main`, holding the Windows Rainmeter project, remains separate. See [T11-C1 deployment record](T11-C1-DEPLOYMENT.md) for runbook, test evidence and rollback.

## Local review (without HostCreators deployment)

With the repository checked out at `feature/t11-site-mvp`, use any static HTTP server at the repository `site/` folder:

```bash
cd site
python3 -m http.server 8000
```

Then open `http://localhost:8000/` (SK) and `http://localhost:8000/en/` (EN). Python 3 is only used for the temporary local review server, not required by HostCreators. A file-manager double-click of `site/index.html` also works for basic review because the website uses relative links and assets.

## Review checklist before merge

- [ ] SK and EN wording, content and tone approved
- [x] One authentic METEO CLOUD beta screenshot provided and integrated; inspect the screenshot for privacy before any public production deployment
- [ ] Verify current software status `v1.10.0-alpha` at publication time
- [ ] Verify availability of real, safe release download; never invent a `.rmskin` link
- [ ] Verify external GitHub and Rainmeter links
- [ ] Keyboard navigation, mobile widths, responsive layout and contrast inspected visually
- [ ] Ensure no API keys/secrets/personal data are published
- [ ] GitHub Actions `static-site-preflight` is successful
- [ ] HostCreators deploy and rollback procedure reviewed

Related planning: [T11 – Issue #13](https://github.com/mpca86/ecowitt-rainmeter/issues/13). Previous process: [DEPLOYMENT.md](../DEPLOYMENT.md).

## Next phase proposal (T11-B)

After feedback, verify the new authentic beta screenshot on narrow and wide screens, add detailed installation documentation based on a verified install test, and possibly prepare separate Markdown documentation pages. Keep the screenshot at its native resolution to avoid blur. Place new authentic captures into a gallery only after privacy review. Keep the public website static and never put Ecowitt cloud secrets in the browser.
