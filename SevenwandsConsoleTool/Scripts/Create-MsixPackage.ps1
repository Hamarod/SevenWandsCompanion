# Script pour créer un package MSIX auto-signé
# Le package MSIX est mieux accepté par Windows

param(
    [string]$ProjectPath = "..\SevenwandsCompanion\SevenwandsCompanion.csproj",
    [string]$PublisherName = "CN=Sevenwands"
)

Write-Host "📦 Création d'un package MSIX..." -ForegroundColor Green

# Étape 1: Créer un certificat pour MSIX si nécessaire
$certName = "SevenwandsCompanion_MSIX"
$cert = Get-ChildItem -Path Cert:\CurrentUser\My | Where-Object { $_.Subject -like "*$certName*" } | Select-Object -First 1

if (-not $cert) {
    Write-Host "🔐 Création d'un nouveau certificat pour MSIX..." -ForegroundColor Cyan
    $cert = New-SelfSignedCertificate `
        -Type Custom `
        -Subject $PublisherName `
        -KeyUsage DigitalSignature `
        -FriendlyName $certName `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}") `
        -NotAfter (Get-Date).AddYears(5)
    
    Write-Host "✅ Certificat créé: $($cert.Thumbprint)" -ForegroundColor Green
}

# Étape 2: Publier l'application avec MSIX
Write-Host "`n📝 Publication de l'application MSIX..." -ForegroundColor Cyan

$command = @"
dotnet publish $ProjectPath `
    -c Release `
    -f net10.0-windows10.0.19041.0 `
    -r win10-x64 `
    -p:RuntimeIdentifierOverride=win10-x64 `
    -p:Platform=x64 `
    -p:GenerateAppxPackageOnBuild=true `
    -p:AppxPackageSigningEnabled=true `
    -p:PackageCertificateThumbprint=$($cert.Thumbprint)
"@

Write-Host $command -ForegroundColor Yellow
Invoke-Expression $command

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Package MSIX créé avec succès!" -ForegroundColor Green
    Write-Host "`n📋 Pour installer sur un autre PC:" -ForegroundColor Yellow
    Write-Host "1. Exportez le certificat:" -ForegroundColor White
    
    $cerPath = ".\SevenwandsCompanion_MSIX.cer"
    Export-Certificate -Cert $cert -FilePath $cerPath | Out-Null
    Write-Host "   ✅ Certificat exporté: $cerPath" -ForegroundColor Green
    
    Write-Host "`n2. Sur le PC de votre ami:" -ForegroundColor White
    Write-Host "   - Double-cliquer sur le .cer" -ForegroundColor Cyan
    Write-Host "   - Installer le certificat dans 'Ordinateur local > Personnes autorisées'" -ForegroundColor Cyan
    Write-Host "   - Installer le package .msix" -ForegroundColor Cyan
} else {
    Write-Host "`n❌ Erreur lors de la création du package (code: $LASTEXITCODE)" -ForegroundColor Red
}
