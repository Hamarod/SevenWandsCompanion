@echo off
setlocal EnableDelayedExpansion

:: ╔════════════════════════════════════════════════════════════╗
:: ║   🧪 Sevenwands Potion Maker - Quick Build Script        ║
:: ╚════════════════════════════════════════════════════════════╝

title Sevenwands Potion Maker - Build Script

echo.
echo ╔════════════════════════════════════════════════════════════╗
echo ║                                                            ║
echo ║     🧪 Sevenwands Potion Maker - Quick Build             ║
echo ║                                                            ║
echo ╚════════════════════════════════════════════════════════════╝
echo.

:: Vérifier si exécuté en tant qu'Administrateur
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ⚠️ Ce script nécessite des droits Administrateur
    echo.
    echo Clic droit sur ce fichier ^> "Exécuter en tant qu'administrateur"
    echo.
    pause
    exit /b 1
)

echo ✅ Exécution en tant qu'Administrateur
echo.

:: Vérifier PowerShell
where powershell >nul 2>&1
if %errorLevel% neq 0 (
    echo ❌ PowerShell introuvable!
    pause
    exit /b 1
)

echo ✅ PowerShell détecté
echo.

:: Menu principal
:MENU
cls
echo.
echo ╔════════════════════════════════════════════════════════════╗
echo ║                    🎯 MENU PRINCIPAL                      ║
echo ╚════════════════════════════════════════════════════════════╝
echo.
echo   [1] 🧪 Tester l'environnement
echo   [2] 🔐 Créer un certificat de signature
echo   [3] 🔨 Build complet (Build + Sign + Package)
echo   [4] ✍️ Signer une application existante
echo   [5] 📦 Créer un package MSIX
echo.
echo   [0] ❌ Quitter
echo.
set /p choice="Votre choix: "

if "%choice%"=="1" goto TEST
if "%choice%"=="2" goto CERT
if "%choice%"=="3" goto BUILD
if "%choice%"=="4" goto SIGN
if "%choice%"=="5" goto MSIX
if "%choice%"=="0" goto EXIT
echo.
echo ⚠️ Choix invalide
timeout /t 2 >nul
goto MENU

:TEST
cls
echo.
echo 🧪 Test de l'environnement...
echo.
powershell -ExecutionPolicy Bypass -File "Test-Environment.ps1"
echo.
pause
goto MENU

:CERT
cls
echo.
echo 🔐 Création d'un certificat de signature...
echo.
powershell -ExecutionPolicy Bypass -File "Create-CodeSigningCertificate.ps1"
echo.
pause
goto MENU

:BUILD
cls
echo.
echo 🔨 Build complet en cours...
echo.
powershell -ExecutionPolicy Bypass -File "Build-And-Package.ps1"
echo.
pause
goto MENU

:SIGN
cls
echo.
echo ✍️ Signature de l'application...
echo.
powershell -ExecutionPolicy Bypass -File "Sign-MauiApp.ps1"
echo.
pause
goto MENU

:MSIX
cls
echo.
echo 📦 Création d'un package MSIX...
echo.
powershell -ExecutionPolicy Bypass -File "Create-MsixPackage.ps1"
echo.
pause
goto MENU

:EXIT
cls
echo.
echo 👋 Au revoir!
echo.
timeout /t 2 >nul
exit /b 0
