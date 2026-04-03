# Script de test rapide pour vérifier que tout fonctionne

Write-Host "🧪 Test de l'environnement de build..." -ForegroundColor Cyan

# Test 1: Vérifier .NET
Write-Host "`n[1/5] Vérification de .NET 10..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version
if ($dotnetVersion -like "10.*") {
    Write-Host "  ✅ .NET 10 installé: $dotnetVersion" -ForegroundColor Green
} else {
    Write-Host "  ⚠️ .NET 10 non détecté. Version actuelle: $dotnetVersion" -ForegroundColor Yellow
    Write-Host "     Téléchargez depuis: https://dotnet.microsoft.com/download/dotnet/10.0" -ForegroundColor Gray
}

# Test 2: Vérifier le projet
Write-Host "`n[2/5] Vérification du projet..." -ForegroundColor Yellow
$projectPath = "..\SevenwandsCompanion\SevenwandsCompanion.csproj"
if (Test-Path $projectPath) {
    Write-Host "  ✅ Projet trouvé: $projectPath" -ForegroundColor Green
} else {
    Write-Host "  ❌ Projet introuvable!" -ForegroundColor Red
    exit 1
}

# Test 3: Vérifier Windows SDK
Write-Host "`n[3/5] Vérification de Windows SDK..." -ForegroundColor Yellow
$signtoolPaths = @(
    "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe",
    "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22000.0\x64\signtool.exe",
    "C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\signtool.exe"
)

$signtoolFound = $false
foreach ($path in $signtoolPaths) {
    if (Test-Path $path) {
        Write-Host "  ✅ signtool.exe trouvé: $path" -ForegroundColor Green
        $signtoolFound = $true
        break
    }
}

if (-not $signtoolFound) {
    Write-Host "  ⚠️ signtool.exe non trouvé" -ForegroundColor Yellow
    Write-Host "     Pour signer vos applications, installez Windows SDK:" -ForegroundColor Gray
    Write-Host "     https://developer.microsoft.com/windows/downloads/windows-sdk/" -ForegroundColor Gray
}

# Test 4: Vérifier les certificats existants
Write-Host "`n[4/5] Vérification des certificats..." -ForegroundColor Yellow
$certs = Get-ChildItem -Path Cert:\CurrentUser\My | Where-Object { $_.Subject -like "*SevenwandsCompanion*" }
if ($certs) {
    Write-Host "  ✅ Certificat(s) trouvé(s):" -ForegroundColor Green
    foreach ($cert in $certs) {
        Write-Host "     - Subject: $($cert.Subject)" -ForegroundColor Gray
        Write-Host "     - Thumbprint: $($cert.Thumbprint)" -ForegroundColor Gray
        Write-Host "     - Expire le: $($cert.NotAfter.ToString('dd/MM/yyyy'))" -ForegroundColor Gray
    }
} else {
    Write-Host "  ℹ️ Aucun certificat trouvé" -ForegroundColor Cyan
    Write-Host "     Exécutez: .\Create-CodeSigningCertificate.ps1" -ForegroundColor Gray
}

# Test 5: Vérifier l'espace disque
Write-Host "`n[5/5] Vérification de l'espace disque..." -ForegroundColor Yellow
$drive = (Get-Location).Drive
$freeSpace = [math]::Round((Get-PSDrive $drive.Name).Free / 1GB, 2)
if ($freeSpace -gt 5) {
    Write-Host "  ✅ Espace disque disponible: $freeSpace GB" -ForegroundColor Green
} else {
    Write-Host "  ⚠️ Espace disque faible: $freeSpace GB" -ForegroundColor Yellow
}

# Résumé
Write-Host "`n" + "="*60 -ForegroundColor Cyan
Write-Host "📋 RÉSUMÉ" -ForegroundColor Cyan
Write-Host "="*60 -ForegroundColor Cyan

$allGood = $true

if ($dotnetVersion -like "10.*") {
    Write-Host "✅ .NET 10" -ForegroundColor Green
} else {
    Write-Host "❌ .NET 10 manquant" -ForegroundColor Red
    $allGood = $false
}

if (Test-Path $projectPath) {
    Write-Host "✅ Projet MAUI" -ForegroundColor Green
} else {
    Write-Host "❌ Projet introuvable" -ForegroundColor Red
    $allGood = $false
}

if ($signtoolFound) {
    Write-Host "✅ Windows SDK (signtool)" -ForegroundColor Green
} else {
    Write-Host "⚠️ Windows SDK (optionnel)" -ForegroundColor Yellow
}

if ($certs) {
    Write-Host "✅ Certificat de signature" -ForegroundColor Green
} else {
    Write-Host "ℹ️ Certificat à créer" -ForegroundColor Cyan
}

Write-Host "`n" + "="*60 -ForegroundColor Cyan

if ($allGood) {
    Write-Host "`n🎉 Tout est prêt pour le build!" -ForegroundColor Green
    Write-Host "`nProchaines étapes:" -ForegroundColor Yellow
    Write-Host "  1. Si pas de certificat: .\Create-CodeSigningCertificate.ps1" -ForegroundColor White
    Write-Host "  2. Pour builder et packager: .\Build-And-Package.ps1" -ForegroundColor White
} else {
    Write-Host "`n⚠️ Certains prérequis manquent" -ForegroundColor Yellow
    Write-Host "Installez les éléments manquants avant de continuer." -ForegroundColor White
}

Write-Host "`nPress any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
