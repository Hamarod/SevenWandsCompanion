# Script complet pour build, signer et packager l'application
# Exécuter en tant qu'Administrateur

param(
    [switch]$SkipBuild,
    [switch]$SkipSign,
    [string]$OutputFolder = ".\Release"
)

$ErrorActionPreference = "Stop"

Write-Host @"
╔════════════════════════════════════════════════════════════╗
║                                                            ║
║   🧪 Sevenwands Potion Maker - Build & Package Script   ║
║                                                            ║
╚════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

$projectPath = "..\SevenwandsCompanion\SevenwandsCompanion.csproj"
$appName = "SevenwandsCompanion"

# Étape 1: Build l'application
if (-not $SkipBuild) {
    Write-Host "`n[1/5] 🔨 Building application..." -ForegroundColor Yellow
    
    dotnet publish $projectPath `
        -c Release `
        -f net10.0-windows10.0.19041.0 `
        -r win10-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:PublishReadyToRun=true
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✅ Build completed successfully!" -ForegroundColor Green
} else {
    Write-Host "`n[1/5] ⏭️ Skipping build..." -ForegroundColor Gray
}

# Étape 2: Vérifier le certificat
Write-Host "`n[2/5] 🔐 Checking code signing certificate..." -ForegroundColor Yellow

$cert = Get-ChildItem -Path Cert:\CurrentUser\My | Where-Object { $_.Subject -like "*$appName*" } | Select-Object -First 1

if (-not $cert) {
    Write-Host "⚠️ No certificate found. Creating one..." -ForegroundColor Yellow
    
    $cert = New-SelfSignedCertificate `
        -Type CodeSigning `
        -Subject "CN=$appName, O=Sevenwands, C=FR" `
        -KeyAlgorithm RSA `
        -KeyLength 2048 `
        -Provider "Microsoft Enhanced RSA and AES Cryptographic Provider" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -NotAfter (Get-Date).AddYears(5)
    
    # Installer dans les autorités racines
    $store = New-Object System.Security.Cryptography.X509Certificates.X509Store("Root", "CurrentUser")
    $store.Open("ReadWrite")
    $store.Add($cert)
    $store.Close()
    
    Write-Host "✅ Certificate created and installed!" -ForegroundColor Green
} else {
    Write-Host "✅ Certificate found: $($cert.Thumbprint)" -ForegroundColor Green
}

# Étape 3: Signer l'application
if (-not $SkipSign) {
    Write-Host "`n[3/5] ✍️ Signing application..." -ForegroundColor Yellow
    
    $exePath = "..\SevenwandsCompanion\bin\Release\net10.0-windows10.0.19041.0\win10-x64\publish\$appName.exe"
    
    if (-not (Test-Path $exePath)) {
        Write-Host "❌ Executable not found: $exePath" -ForegroundColor Red
        exit 1
    }
    
    # Trouver signtool
    $signtool = Get-ChildItem "C:\Program Files (x86)\Windows Kits\10\bin" -Recurse -Filter "signtool.exe" -ErrorAction SilentlyContinue | 
                Select-Object -First 1 -ExpandProperty FullName
    
    if ($signtool) {
        & $signtool sign /sha1 $cert.Thumbprint /fd SHA256 /td SHA256 /tr http://timestamp.digicert.com $exePath
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Application signed successfully!" -ForegroundColor Green
        } else {
            Write-Host "⚠️ Signature failed (non-critical)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "⚠️ signtool not found. Skipping signature." -ForegroundColor Yellow
        Write-Host "   Install Windows SDK: https://developer.microsoft.com/windows/downloads/windows-sdk/" -ForegroundColor Gray
    }
} else {
    Write-Host "`n[3/5] ⏭️ Skipping signature..." -ForegroundColor Gray
}

# Étape 4: Créer le dossier de distribution
Write-Host "`n[4/5] 📦 Creating distribution package..." -ForegroundColor Yellow

$publishFolder = "..\SevenwandsCompanion\bin\Release\net10.0-windows10.0.19041.0\win10-x64\publish"
$version = (Get-Date -Format "yyyyMMdd")
$distFolder = "$OutputFolder\$appName-v$version"

# Nettoyer et créer le dossier de sortie
if (Test-Path $distFolder) {
    Remove-Item $distFolder -Recurse -Force
}
New-Item -ItemType Directory -Path $distFolder | Out-Null

# Copier les fichiers
Copy-Item "$publishFolder\*" -Destination $distFolder -Recurse -Force

# Exporter le certificat
$cerPath = "$distFolder\$appName.cer"
Export-Certificate -Cert $cert -FilePath $cerPath | Out-Null

# Créer un fichier README
$readmeContent = @"
╔════════════════════════════════════════════════════════════╗
║   🧪 Sevenwands Potion Maker - Installation Guide        ║
╚════════════════════════════════════════════════════════════╝

📦 Contenu du package:
  ✓ $appName.exe - Application principale
  ✓ $appName.cer - Certificat de sécurité
  ✓ README.txt - Ce fichier

🚀 INSTALLATION RAPIDE:

1. DÉBLOQUER LE ZIP (IMPORTANT!):
   - Clic droit sur le ZIP téléchargé
   - Propriétés > Cocher "Débloquer" > OK
   - Extraire le ZIP

2. INSTALLER LE CERTIFICAT (Recommandé):
   - Double-cliquer sur "$appName.cer"
   - Installer le certificat > "Ordinateur local"
   - Placer dans "Autorités de certification racines de confiance"
   - Confirmer l'installation

3. LANCER L'APPLICATION:
   - Double-cliquer sur "$appName.exe"
   - Si Windows bloque: "Informations complémentaires" > "Exécuter quand même"

⚠️ Prérequis:
  • Windows 10/11 (64-bit)
  • .NET 10 Runtime (installé automatiquement si besoin)

📞 Support: votre@email.com
🌐 GitHub: https://github.com/votre-repo

Version: $version
"@

Set-Content -Path "$distFolder\README.txt" -Value $readmeContent -Encoding UTF8

# Créer un ZIP
Write-Host "`n[5/5] 🗜️ Creating ZIP archive..." -ForegroundColor Yellow

$zipPath = "$OutputFolder\$appName-v$version.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Compress-Archive -Path $distFolder\* -DestinationPath $zipPath -CompressionLevel Optimal

Write-Host "✅ Package created successfully!" -ForegroundColor Green

# Résumé
Write-Host @"

╔════════════════════════════════════════════════════════════╗
║                    ✅ BUILD COMPLETE                       ║
╚════════════════════════════════════════════════════════════╝

📦 Package location:
   $zipPath

📏 Package size:
   $([math]::Round((Get-Item $zipPath).Length / 1MB, 2)) MB

📋 Next steps:
   1. Test l'application sur votre machine
   2. Partagez le ZIP avec vos amis
   3. Partagez aussi le guide: INSTALLATION_GUIDE.md

🎯 Tips pour vos amis:
   • Débloquer le ZIP avant d'extraire (Important!)
   • Installer le certificat .cer
   • Accepter "Exécuter quand même" si demandé

"@ -ForegroundColor Green

Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
