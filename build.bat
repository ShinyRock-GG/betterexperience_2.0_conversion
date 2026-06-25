@echo off
:: ============================================================
:: build.bat
:: Builds all four DLLs and copies them to the game's plugin folder.
:: Requires: GameRefs.props configured (run 01-first-time-setup.bat)
::           dotnet SDK installed (https://dot.net)
:: ============================================================

set SLN=%~dp0BetterExperience\2.0\BetterExperience_2.0.sln

if not exist "%SLN%" (
    echo ERROR: Solution not found at %SLN%
    pause
    exit /b 1
)

if not exist "%~dp0GameRefs.props" (
    echo WARNING: GameRefs.props not found. Build will use fallback 23.1\ folder.
    echo Run 01-first-time-setup.bat to configure your game path.
    echo.
)

echo Building BetterExperience 2.0...
dotnet build "%SLN%" -c Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo BUILD SUCCEEDED
    echo DLLs are in your game's BepInEx\plugins\BetterExperience\ folder.
) else (
    echo.
    echo BUILD FAILED - check errors above.
)
pause
