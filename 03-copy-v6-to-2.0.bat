@echo off
:: ============================================================
:: 03-copy-v6-to-2.0.bat
:: Copies v6 decompiled source into the 2.0 build project folders.
:: Safe to re-run — xcopy skips unchanged files.
:: ============================================================

set REPO=%~dp0
set SRC=%REPO%BetterExperience\update60e\decompiled
set DST=%REPO%BetterExperience\2.0

echo Copying BetterExperience...
xcopy /E /I /Y /Q "%SRC%\BetterExperience" "%DST%\BetterExperience"

echo Copying Better_Cloth...
xcopy /E /I /Y /Q "%SRC%\Better_Cloth" "%DST%\Better_Cloth"

echo Copying Better_Scene...
xcopy /E /I /Y /Q "%SRC%\Better_Scene" "%DST%\Better_Scene"

echo Copying Better_Story...
xcopy /E /I /Y /Q "%SRC%\Better_Story" "%DST%\Better_Story"

echo.
echo Done. Tell Claude you ran step 1.
pause
