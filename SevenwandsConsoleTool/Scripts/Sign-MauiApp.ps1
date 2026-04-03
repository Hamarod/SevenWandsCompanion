# Script pour signer l'application après publication
# Utilise le certificat créé par Create-CodeSigningCertificate.ps1

param(
    [string]$ExePath = "..\SevenwandsCompanion\bin\Release\net10.0-windows10.0.19041.0\win10-x64\publish\SevenwandsCompanion.exe",
    [string]$CertThumbprint = "" # Laissez vide pour auto-détecter
)

Write-Host "🔐 Signature de l'application MAUI..." -ForegroundColor Green

# Si le thumbprint n'est pas fourni, chercher le certificat
if ([string]::IsNullOrEmpty($CertThumbprint)) {
    Write-Host "🔍 Recherche du certificat SevenwandsCompanion..." -ForegroundColor Cyan
    $cert = Get-ChildItem -Path Cert:\CurrentUser\My | Where-Object { $_.Subject -like "*SevenwandsCompanion*" } | Select-Object -First 1
    
    if ($cert) {
        $CertThumbprint = $cert.Thumbprint
        Write-Host "✅ Certificat trouvé: $CertThumbprint" -ForegroundColor Green
    } else {
        Write-Host "❌ Aucun certificat trouvé. Exécutez d'abord Create-CodeSigningCertificate.ps1" -ForegroundColor Red
        exit 1
    }
}

# Vérifier que le fichier existe
if (-not (Test-Path $ExePath)) {
    Write-Host "❌ Fichier introuvable: $ExePath" -ForegroundColor Red
    Write-Host "   Publiez d'abord l'application avec:" -ForegroundColor Yellow
    Write-Host "   dotnet publish -c Release -f net10.0-windows10.0.19041.0 -r win10-x64" -ForegroundColor Cyan
    exit 1
}

# Trouver signtool.exe
$windowsKitsPaths = @(
    "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe",
    "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22000.0\x64\signtool.exe",
    "C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\signtool.exe",
    "C:\Program Files (x86)\Windows Kits\10\App Certification Kit\signtool.exe"
)

$signtool = $null
foreach ($path in $windowsKitsPaths) {
    if (Test-Path $path) {
        $signtool = $path
        break
    }
}

if (-not $signtool) {
    Write-Host "❌ signtool.exe introuvable" -ForegroundColor Red
    Write-Host "   Installez Windows SDK depuis:" -ForegroundColor Yellow
    Write-Host "   https://developer.microsoft.com/windows/downloads/windows-sdk/" -ForegroundColor Cyan
    exit 1
}

Write-Host "📝 Signature en cours..." -ForegroundColor Cyan
Write-Host "   Fichier: $ExePath" -ForegroundColor White
Write-Host "   Certificat: $CertThumbprint" -ForegroundColor White

# Signer le fichier
& $signtool sign /sha1 $CertThumbprint /fd SHA256 /td SHA256 /tr http://timestamp.digicert.com $ExePath

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Application signée avec succès!" -ForegroundColor Green
    
    # Vérifier la signature
    Write-Host "`n🔍 Vérification de la signature..." -ForegroundColor Cyan
    & $signtool verify /pa $ExePath
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Signature vérifiée!" -ForegroundColor Green
    }
} else {
    Write-Host "❌ Erreur lors de la signature (code: $LASTEXITCODE)" -ForegroundColor Red
}

Write-Host "`n📦 Votre application est maintenant signée et prête à être distribuée!" -ForegroundColor Green
Write-Host "   N'oubliez pas de partager aussi le fichier .cer avec vos amis:" -ForegroundColor Yellow
Write-Host "   Scripts\SevenwandsCompanion.cer" -ForegroundColor Cyan
