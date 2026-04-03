# 📜 Scripts de Build et Distribution

Ce dossier contient tous les scripts nécessaires pour compiler, signer et distribuer l'application Sevenwands Potion Maker.

---

## 🚀 Utilisation Rapide

### Premier Build Complet

```powershell
# 1. Tester l'environnement
.\Test-Environment.ps1

# 2. Créer le certificat (une seule fois)
.\Create-CodeSigningCertificate.ps1

# 3. Build et package complet
.\Build-And-Package.ps1
```

**Résultat :** Un fichier ZIP prêt à partager dans `Release\`

---

## 📁 Liste des Scripts

### 1. `Test-Environment.ps1`
**Usage :** Tester votre environnement avant le build

```powershell
.\Test-Environment.ps1
```

**Vérifie :**
- ✅ .NET 10 SDK installé
- ✅ Projet MAUI accessible
- ✅ Windows SDK (signtool)
- ✅ Certificats existants
- ✅ Espace disque disponible

---

### 2. `Create-CodeSigningCertificate.ps1`
**Usage :** Créer un certificat auto-signé pour signer l'application

```powershell
# Exécuter en Administrateur
.\Create-CodeSigningCertificate.ps1
```

**Crée :**
- 🔐 Certificat auto-signé (valide 5 ans)
- 📄 Fichier `.pfx` (avec mot de passe)
- 📄 Fichier `.cer` (pour vos amis)
- 📦 Installation dans "Autorités racines de confiance"

**À exécuter :** 1 seule fois (ou tous les 5 ans)

---

### 3. `Sign-MauiApp.ps1`
**Usage :** Signer l'application après compilation

```powershell
.\Sign-MauiApp.ps1

# Ou spécifier le chemin
.\Sign-MauiApp.ps1 -ExePath "chemin\vers\app.exe" -CertThumbprint "ABC123..."
```

**Utilise :**
- Le certificat créé par `Create-CodeSigningCertificate.ps1`
- `signtool.exe` du Windows SDK
- Timestamp server pour validation

---

### 4. `Create-MsixPackage.ps1`
**Usage :** Créer un package MSIX (Windows Installer)

```powershell
.\Create-MsixPackage.ps1
```

**Avantages :**
- ✅ Installation propre dans le menu Démarrer
- ✅ Désinstallation facile
- ✅ Mises à jour automatiques possibles
- ✅ Mieux accepté par Windows

---

### 5. `Build-And-Package.ps1` ⭐ **RECOMMANDÉ**
**Usage :** Script tout-en-un pour build, signature et packaging

```powershell
# Build complet
.\Build-And-Package.ps1

# Sans rebuild (si déjà compilé)
.\Build-And-Package.ps1 -SkipBuild

# Sans signature (pour test rapide)
.\Build-And-Package.ps1 -SkipSign

# Personnaliser le dossier de sortie
.\Build-And-Package.ps1 -OutputFolder "D:\Releases"
```

**Fait tout automatiquement :**
1. 🔨 Compile l'application en Release
2. 🔐 Vérifie/crée le certificat
3. ✍️ Signe l'exécutable
4. 📦 Crée un dossier de distribution
5. 📄 Génère un README.txt
6. 🗜️ Compresse en ZIP
7. 📊 Affiche un résumé

**Résultat :** `Release\SevenwandsCompanion-vYYYYMMDD.zip`

---

## 🔧 Prérequis

### Obligatoires
- ✅ Windows 10/11 (64-bit)
- ✅ .NET 10 SDK
- ✅ PowerShell 5.1 ou supérieur

### Optionnels (pour signature)
- ⭐ Windows SDK (pour `signtool.exe`)
- ⭐ Droits Administrateur (pour créer certificats)

**Installer Windows SDK :**
https://developer.microsoft.com/windows/downloads/windows-sdk/

---

## 📋 Workflow Recommandé

### Développement Quotidien

```powershell
# Juste compiler et tester
dotnet run --project ..\SevenwandsCompanion\SevenwandsCompanion.csproj
```

### Partager avec des Amis (Rapide)

```powershell
# Build simple sans signature
dotnet publish -c Release -r win10-x64
# Compresser manuellement le dossier publish
```

### Distribution Officielle (Complète)

```powershell
# Build + Signature + Package
.\Build-And-Package.ps1
```

---

## 🎯 Exemples d'Utilisation

### Exemple 1 : Premier Build
```powershell
PS> cd D:\dev\dotnet\SevenwandsConsoleTool\Scripts

# Tester l'environnement
PS> .\Test-Environment.ps1
🧪 Test de l'environnement de build...
[1/5] Vérification de .NET 10...
  ✅ .NET 10 installé: 10.0.100
...

# Créer le certificat
PS> .\Create-CodeSigningCertificate.ps1
🔐 Création d'un certificat auto-signé...
✅ Certificat créé: ABC123456789...

# Build et package
PS> .\Build-And-Package.ps1
[1/5] 🔨 Building application...
...
✅ BUILD COMPLETE
📦 Package: Release\SevenwandsCompanion-v20250115.zip
```

### Exemple 2 : Build Rapide (déjà compilé)
```powershell
# Juste packager sans recompiler
PS> .\Build-And-Package.ps1 -SkipBuild
[1/5] ⏭️ Skipping build...
[2/5] 🔐 Checking certificate...
✅ Certificate found
...
```

### Exemple 3 : Réutiliser un Certificat Existant
```powershell
# Le script détecte automatiquement le certificat
PS> .\Sign-MauiApp.ps1
🔍 Recherche du certificat SevenwandsCompanion...
✅ Certificat trouvé: ABC123456789
📝 Signature en cours...
✅ Application signée!
```

---

## ⚠️ Résolution de Problèmes

### Erreur : "Accès refusé"
**Solution :** Exécuter PowerShell en Administrateur
```powershell
# Clic droit sur PowerShell > "Exécuter en tant qu'administrateur"
```

### Erreur : "signtool.exe introuvable"
**Solution :** Installer Windows SDK
- Télécharger : https://developer.microsoft.com/windows/downloads/windows-sdk/
- Installer uniquement : "Windows App Certification Kit"

### Erreur : "Certificat expiré"
**Solution :** Recréer un certificat
```powershell
# Supprimer l'ancien
Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -like "*SevenwandsCompanion*" } | Remove-Item

# Recréer
.\Create-CodeSigningCertificate.ps1
```

### Erreur : "dotnet command not found"
**Solution :** Installer .NET 10 SDK
- Télécharger : https://dotnet.microsoft.com/download/dotnet/10.0

### Mon ami ne peut pas installer le certificat
**Solution :** Fournir ces instructions :
1. Double-cliquer sur `SevenwandsCompanion.cer`
2. "Installer le certificat"
3. "Ordinateur local" (ou "Utilisateur actuel")
4. "Placer tous les certificats dans le magasin suivant"
5. Parcourir > "Autorités de certification racines de confiance"
6. OK > Suivant > Terminer

---

## 📚 Documentation Complémentaire

- **INSTALLATION_GUIDE.md** : Guide pour vos amis
- **DISTRIBUTION_GUIDE.md** : Guide complet de distribution
- **TOKEN_TRACKING_README.md** : Documentation technique

---

## 🔒 Sécurité

### Les certificats auto-signés sont-ils sûrs ?
Oui, **SI** vous êtes le développeur et que vous faites confiance à vous-même ! 😄

Les certificats auto-signés :
- ✅ Prouvent que le fichier n'a pas été modifié
- ✅ Prouvent l'identité du signataire (vous)
- ⚠️ Ne sont pas reconnus automatiquement par Windows
- ⚠️ Nécessitent une installation manuelle

### Différence avec un certificat officiel ?
- **Officiel** (EV Code Signing) :
  - Prix : 300-1000€/an
  - Reconnu automatiquement par Windows
  - Nécessite une vérification d'identité

- **Auto-signé** :
  - Prix : Gratuit
  - Nécessite installation manuelle
  - Parfait pour distribution privée

---

## 💡 Astuces Pro

### Astuce 1 : Build Plus Rapide
```powershell
# Désactiver ReadyToRun pour build plus rapide (dev)
dotnet publish -c Release -p:PublishReadyToRun=false
```

### Astuce 2 : Build Single-File
```powershell
# Tout dans un seul .exe
dotnet publish -p:PublishSingleFile=true
```

### Astuce 3 : Build Self-Contained
```powershell
# Inclure .NET Runtime (pas besoin de .NET installé)
dotnet publish --self-contained true
```

### Astuce 4 : Build Trimmed (plus petit)
```powershell
# Supprimer le code inutilisé (risqué!)
dotnet publish -p:PublishTrimmed=true
```

---

## 📞 Support

Si vous rencontrez des problèmes :

1. **Vérifier l'environnement** : `.\Test-Environment.ps1`
2. **Lire les logs** : Les erreurs sont affichées en rouge
3. **Vérifier les prérequis** : .NET 10, Windows SDK
4. **Redémarrer PowerShell** : En Administrateur

---

**Dernière mise à jour :** 2025  
**Auteur :** GitHub Copilot  
**Version :** 1.0
