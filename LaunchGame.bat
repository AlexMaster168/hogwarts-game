@echo off
setlocal
cd /d "%~dp0"
if not exist "Build\HogwartsMini.exe" (
  echo Game build not found. Run BuildGame.bat first.
  pause
  exit /b 1
)
start "Hogwarts Mini" "Build\HogwartsMini.exe" -screen-fullscreen 0 %*
exit /b 0
