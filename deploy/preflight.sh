#!/usr/bin/env bash
# HostCreators Git Build & Deploy - static site preflight for testgit.mpca86.sk
# Intended command in HostCreators: bash deploy/preflight.sh
# Keep this script in the build phase, BEFORE the Deploy step.
set -euo pipefail

echo "[PREFLIGHT] Starting checks for site/"

fail() {
  echo "[PREFLIGHT] FAIL: $1" >&2
  exit 1
}

if [ ! -d "site" ] || [ -L "site" ]; then
  fail "site/ must exist as a regular directory"
fi

if [ ! -f "site/index.html" ] || [ ! -s "site/index.html" ]; then
  fail "site/index.html must exist and must not be empty"
fi

if ! grep -Eiq '<!doctype[[:space:]]+html' site/index.html; then
  fail "site/index.html does not contain an HTML doctype"
fi

# Never publish local configuration, Git metadata or private keys as web content.
for sensitive in \
  "site/.env" \
  "site/.env.local" \
  "site/.env.production" \
  "site/.git" \
  "site/id_rsa" \
  "site/id_ed25519"; do
  if [ -e "$sensitive" ] || [ -L "$sensitive" ]; then
    fail "potentially sensitive file/directory in the public web root: $sensitive"
  fi
done

echo "[PREFLIGHT] PASS: static site is ready for deployment"
