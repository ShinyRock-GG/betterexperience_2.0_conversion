@echo off
:: ============================================================
:: build-and-deploy.bat
:: Builds all four DLLs and copies them to 23.1\BepInEx\plugins\BetterExperience\
:: ============================================================

set SLN=%~dp0BetterExperience\2.0\BetterExperience_2.0.sln
set DEST=%~dp023.1\BepInEx\plugins\BetterExperience

if not exist "%SLN%" (
    echo ERROR: Solution not found at %SLN%
    pause
    exit /b 1
)

echo Building BetterExperience 2.0...
dotnet build "%SLN%" -c Release

if not %ERRORLEVEL% EQU 0 (
    echo.
    echo BUILD FAILED - check errors above.
    pause
    exit /b 1
)

echo.
echo Deploying DLLs to %DEST%...
if not exist "%DEST%" mkdir "%DEST%"

copy /Y "%~dp0BetterExperience\2.0\BetterExperience\bin\Release\net472\BetterExperience.dll" "%DEST%\"
copy /Y "%~dp0BetterExperience\2.0\Better_Cloth\bin\Release\net472\Better_Cloth.dll"         "%DEST%\"
copy /Y "%~dp0BetterExperience\2.0\Better_Scene\bin\Release\net472\Better_Scene.dll"         "%DEST%\"
copy /Y "%~dp0BetterExperience\2.0\Better_Story\bin\Release\net472\Better_Story.dll"         "%DEST%\"

echo.
echo Done. Launch the game and check BepInEx\LogOutput.log.
pause
