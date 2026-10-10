#!/usr/bin/env bash
# HostCreators T06: controlled pre-deploy failure simulation.
# This script is intentionally safe by default. It only fails when
# .t06-fail-build exists at the repository root (outside site/).
set -eu

echo "[T06] Running pre-deploy check"

if [ ! -f "site/index.html" ]; then
  echo "[T06] ERROR: site/index.html is missing" >&2
  exit 43
fi

if [ -f ".t06-fail-build" ]; then
  echo "[T06] INTENTIONAL BUILD FAILURE: .t06-fail-build marker detected" >&2
  exit 42
fi

echo "[T06] PASS: no failure marker; deploy may continue"
