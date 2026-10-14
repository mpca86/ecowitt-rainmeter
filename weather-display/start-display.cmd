@echo off
setlocal
cd /d "%~dp0"
where py >nul 2>nul
if not errorlevel 1 (
  py -3 serve.py
) else (
  where python >nul 2>nul
  if errorlevel 1 (
    echo Python 3 is required. Install it and run this file again.
  ) else (
    python serve.py
  )
)
pause
