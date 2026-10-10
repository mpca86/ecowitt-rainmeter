# T11-A – Ecowitt Rainmeter project website MVP

Status: **bilingual staging proposal deployed only to preview.mpca86.sk via the feature branch**. Draft PR #14 remains unmerged. Do not merge into `hostcreators-test` without approval.

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

## Staging and release process

- Base branch: `hostcreators-test` (HostCreators document root: `site`).
- Feature branch: `feature/t11-site-mvp`. Only changes to this branch until approved.
- Deploy contract: `bash deploy/preflight.sh` in HostCreators; GitHub Actions check `static-site-preflight` on PR.
- Staging pages contain `robots: noindex, nofollow` and must stay that way until a proper production-domain decision.
- Merge only after content/design feedback, screenshots decision, access/security review, CI PASS and user approval.
- Production `main` with Windows Rainmeter skin remains unchanged.

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
