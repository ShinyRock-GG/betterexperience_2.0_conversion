@echo off
:: ============================================================
:: 02-initial-commit-and-push.bat
:: Does the first commit and push to GitHub.
::
:: BEFORE running this you must:
::   1. Add the deploy key public key to GitHub:
::      Repo Settings > Deploy keys > Add deploy key
::      Title: betterexperience_deploy
::      Key:   ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIOICiI73MSfhr24NMuLNOAwEcix+GNQduXrq8FRqmtkM betterexperience_deploy
::      Check "Allow write access"
::
::   2. (Optional) Run 01-first-time-setup.bat first to configure GameRefs.props
:: ============================================================

set REPO=%~dp0
set SSH_KEY=F:\Games\AAA\better_experience_working\.ssh\deploy_key
set GIT_SSH_COMMAND=ssh -i "%SSH_KEY%" -o StrictHostKeyChecking=no

cd /d "%REPO%"

:: Verify key exists
if not exist "%SSH_KEY%" (
    echo ERROR: Deploy key not found at %SSH_KEY%
    echo Run the SSH key generation step first.
    pause
    exit /b 1
)

:: Remove stale lock if present
if exist ".git\index.lock" del /f ".git\index.lock"

:: Configure git identity
git config user.email "rockwell2006@gmail.com"
git config user.name "ShinyRock-GG"

:: Ensure remote is SSH
git remote set-url origin git@github.com:ShinyRock-GG/betterexperience_2.0_conversion.git

:: Stage and commit
git add -A
git status

echo.
echo About to commit as "Initial project structure: csproj files, solution, gitignore"
echo Press Ctrl+C to cancel, or
pause

git commit -m "Initial project structure: csproj files, solution, gitignore"

echo.
echo Pushing to GitHub...
git push -u origin main

if %ERRORLEVEL% EQU 0 (
    echo.
    echo SUCCESS - pushed to GitHub.
) else (
    echo.
    echo FAILED. Check that the deploy key has been added to the GitHub repo.
    echo Repo Settings ^> Deploy keys ^> Add deploy key
)
pause
