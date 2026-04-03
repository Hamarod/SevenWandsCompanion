# Script pour créer un certificat auto-signé et signer l'application
# Exécuter en tant qu'Administrateur

# Paramètres
$certName = "SevenwandsCompanion"
$certPassword = "YourPassword123!" # À changer
$publishFolder = "..\SevenwandsCompanion\bin\Release\net10.0-windows10.0.19041.0\win10-x64\publish"
$exeName = "SevenwandsCompanion.exe"

Write-Host "🔐 Création d'un certificat auto-signé..." -ForegroundColor Green

# Créer un certificat auto-signé
$cert = New-SelfSignedCertificate `
    -Type CodeSigning `
    -Subject "CN=$certName, O=Sevenwands, C=FR" `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -Provider "Microsoft Enhanced RSA and AES Cryptographic Provider" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter (Get-Date).AddYears(5)

Write-Host "✅ Certificat créé: $($cert.Thumbprint)" -ForegroundColor Green

# Exporter le certificat avec mot de passe
$certPath = "$PSScriptRoot\$certName.pfx"
$securePwd = ConvertTo-SecureString -String $certPassword -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath $certPath -Password $securePwd | Out-Null

Write-Host "✅ Certificat exporté vers: $certPath" -ForegroundColor Green

# Copier le certificat dans le magasin des autorités racines de confiance
Write-Host "🔑 Installation du certificat en tant qu'autorité racine de confiance..." -ForegroundColor Yellow
$store = New-Object System.Security.Cryptography.X509Certificates.X509Store("Root", "CurrentUser")
$store.Open("ReadWrite")
$store.Add($cert)
$store.Close()

Write-Host "✅ Certificat installé comme autorité de confiance" -ForegroundColor Green

# Fonction pour signer un fichier
function Sign-Application {
    param (
        [string]$FilePath,
        [string]$CertThumbprint
    )
    
    if (Test-Path $FilePath) {
        Write-Host "📝 Signature de: $FilePath" -ForegroundColor Cyan
        
        # Utiliser signtool.exe (inclus dans Windows SDK)
        $signtool = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe"
        
        if (Test-Path $signtool) {
            & $signtool sign /sha1 $CertThumbprint /fd SHA256 /t http://timestamp.digicert.com $FilePath
            Write-Host "✅ Fichier signé avec succès" -ForegroundColor Green
        } else {
            Write-Host "⚠️ signtool.exe introuvable. Installez Windows SDK." -ForegroundColor Yellow
            Write-Host "   Téléchargez depuis: https://developer.microsoft.com/windows/downloads/windows-sdk/" -ForegroundColor Yellow
        }
    } else {
        Write-Host "❌ Fichier introuvable: $FilePath" -ForegroundColor Red
    }
}

# Instructions pour signer après la publication
Write-Host "`n📋 INSTRUCTIONS:" -ForegroundColor Yellow
Write-Host "1. Publiez votre application:" -ForegroundColor White
Write-Host "   dotnet publish -c Release -f net10.0-windows10.0.19041.0 -r win10-x64" -ForegroundColor Cyan
Write-Host "`n2. Exécutez cette commande pour signer:" -ForegroundColor White
Write-Host "   .\Sign-Application.ps1 -Sign" -ForegroundColor Cyan
Write-Host "`n3. Pour vos amis, fournissez aussi le certificat .cer:" -ForegroundColor White
Write-Host "   Ils devront l'installer dans 'Autorités racines de confiance'" -ForegroundColor White

# Exporter le certificat public (.cer) pour distribution
$cerPath = "$PSScriptRoot\$certName.cer"
Export-Certificate -Cert $cert -FilePath $cerPath | Out-Null
Write-Host "`n✅ Certificat public exporté: $cerPath" -ForegroundColor Green
Write-Host "   Partagez ce fichier avec vos amis pour qu'ils puissent faire confiance à vos applications" -ForegroundColor White

Write-Host "`n🎯 Thumbprint du certificat: $($cert.Thumbprint)" -ForegroundColor Green
Write-Host "Sauvegardez ce thumbprint pour signer vos applications ultérieurement" -ForegroundColor White

# Si le paramètre -Sign est présent, signer l'application
if ($args -contains "-Sign") {
    Write-Host "`n🔐 Signature de l'application..." -ForegroundColor Green
    $exePath = Join-Path $publishFolder $exeName
    Sign-Application -FilePath $exePath -CertThumbprint $cert.Thumbprint
}
