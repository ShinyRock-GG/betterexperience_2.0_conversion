@echo off
:: ============================================================
:: 01-first-time-setup.bat
:: Run once after cloning. Sets up your local GameRefs.props.
:: ============================================================

set REPO=%~dp0
set GAMEREFSEXAMPLE=%REPO%GameRefs.props.example
set GAMEREFS=%REPO%GameRefs.props

if exist "%GAMEREFS%" (
    echo GameRefs.props already exists. Opening for review...
    notepad "%GAMEREFS%"
    goto :done
)

copy "%GAMEREFSEXAMPLE%" "%GAMEREFS%"
echo.
echo Created GameRefs.props from example.
echo Opening in Notepad — set GameDir to your SMA 23.1 install path.
echo.
notepad "%GAMEREFS%"

:done
echo.
echo Done. GameRefs.props is ready.
pause
