@echo off
:: ============================================================
:: commit-and-push.bat
:: Stages all changes, prompts for commit message, pushes.
:: ============================================================

set REPO=%~dp0
set SSH_KEY=F:\Games\AAA\better_experience_working\.ssh\deploy_key
set GIT_SSH_COMMAND=ssh -i "%SSH_KEY%" -o StrictHostKeyChecking=no

cd /d "%REPO%"

if exist ".git\index.lock" del /f ".git\index.lock"

echo Current changes:
git status --short
echo.

set /p MSG="Commit message: "
if "%MSG%"=="" (
    echo No message entered. Aborting.
    pause
    exit /b 1
)

git add -A
git commit -m "%MSG%"
git push

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Pushed successfully.
) else (
    echo.
    echo Push failed. Check SSH key and GitHub access.
)
pause
