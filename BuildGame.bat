@echo off
setlocal
cd /d "%~dp0"
set "HM_UNITY=%ProgramFiles%\Unity\Hub\Editor\6000.0.77f1\Editor\Unity.exe"
if not "%~1"=="" set "HM_UNITY=%~1"
if not exist "%HM_UNITY%" (
  echo Unity 6000.0.77f1 was not found.
  echo Usage: BuildGame.bat "full path to Unity.exe"
  pause
  exit /b 1
)
if not exist "Build\Verification" mkdir "Build\Verification"
echo Close this project in Unity before building.
echo Build log: Build\Verification\bat-build.log
start "Unity build" /wait "%HM_UNITY%" -batchmode -nographics -projectPath "%CD%" -executeMethod ProjectSetup.Build -logFile "%CD%\Build\Verification\bat-build.log"
set "HM_RESULT=%ERRORLEVEL%"
if not "%HM_RESULT%"=="0" (
  echo Build failed. See the log.
  pause
  exit /b %HM_RESULT%
)
echo Build completed: Build\HogwartsMini.exe
pause
exit /b 0
