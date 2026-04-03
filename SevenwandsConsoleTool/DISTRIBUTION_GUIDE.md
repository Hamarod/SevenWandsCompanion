# 🛡️ Guide Complet : Contourner le Blocage Windows

## 📋 Résumé des Solutions

| Solution | Complexité | Recommandée | Avantages |
|----------|-----------|-------------|-----------|
| Débloquer les fichiers | ⭐ Facile | ✅ Pour tests rapides | Simple, rapide |
| Auto-signature | ⭐⭐ Moyenne | ✅ Pour amis proches | Certificat de confiance |
| Package MSIX | ⭐⭐⭐ Avancée | ✅ Pour distribution | Installation propre |
| Microsoft Store | ⭐⭐⭐⭐ Complexe | ❌ Trop complexe | Confiance absolue |

---

## 🚀 SOLUTION RECOMMANDÉE : Build & Package Script

### Étape 1 : Créer et Signer l'Application

```powershell
# Dans le dossier SevenwandsConsoleTool
cd Scripts

# Exécuter en tant qu'Administrateur
.\Build-And-Package.ps1
```

**Ce script va automatiquement :**
1. ✅ Compiler l'application en mode Release
2. ✅ Créer un certificat auto-signé
3. ✅ Signer l'exécutable
4. ✅ Créer un package ZIP avec certificat et README
5. ✅ Tout préparer pour la distribution

### Étape 2 : Partager avec Vos Amis

**Envoyez-leur :**
- 📦 Le fichier `SevenwandsCompanion-vXXXXXXXX.zip`
- 📄 Le fichier `INSTALLATION_GUIDE.md`

**Dites-leur de :**
1. Débloquer le ZIP (clic droit > Propriétés > Débloquer)
2. Extraire le ZIP
3. Installer le certificat `SevenwandsCompanion.cer`
4. Lancer `SevenwandsCompanion.exe`

---

## 🔧 SOLUTIONS ALTERNATIVES

### Solution 1 : Débloquer Manuellement (Le Plus Simple)

**Pour vous (lors du packaging) :**
```powershell
# Débloquer tous les fichiers du dossier bin
cd ..\SevenwandsCompanion\bin\Release\net10.0-windows10.0.19041.0\win10-x64\publish
Get-ChildItem -Recurse | Unblock-File
```

**Pour vos amis :**
1. Clic droit sur le ZIP > Propriétés
2. Cocher "Débloquer" > OK
3. Extraire et lancer

---

### Solution 2 : Créer un Certificat Unique

```powershell
# Une seule fois
.\Create-CodeSigningCertificate.ps1

# Puis, après chaque build
.\Sign-MauiApp.ps1
```

**Avantages :**
- ✅ Le certificat reste valide 5 ans
- ✅ Vos amis l'installent une fois
- ✅ Toutes vos futures apps seront acceptées

---

### Solution 3 : Package MSIX (Le Plus Professionnel)

```powershell
.\Create-MsixPackage.ps1
```

**Avantages :**
- ✅ Installation propre via Windows Installer
- ✅ Désinstallation facile
- ✅ Mises à jour automatiques possibles
- ✅ Moins de méfiance de Windows

---

## 📝 CHECKLIST AVANT DISTRIBUTION

### Avant de Partager :
- [ ] Application compilée en **Release** (pas Debug)
- [ ] Application testée sur votre machine
- [ ] Certificat créé et application signée
- [ ] Package ZIP créé avec certificat et README
- [ ] Guide d'installation préparé

### À Partager :
- [ ] `SevenwandsCompanion-vXXXXXXXX.zip`
- [ ] `INSTALLATION_GUIDE.md`
- [ ] Message : "Débloquez le ZIP avant d'extraire!"

---

## 🎯 SCRIPT D'UTILISATION COMPLÈTE

### Build Initial (1 seule fois)

```powershell
# Ouvrir PowerShell en Administrateur
cd D:\dev\dotnet\SevenwandsConsoleTool\Scripts

# Créer le certificat
.\Create-CodeSigningCertificate.ps1

# Build et package complet
.\Build-And-Package.ps1
```

### Builds Suivants

```powershell
# Juste rebuild et package
.\Build-And-Package.ps1
```

### Si Changement Mineur (pas besoin de rebuild)

```powershell
# Juste signer
.\Sign-MauiApp.ps1

# Ou package sans rebuild
.\Build-And-Package.ps1 -SkipBuild
```

---

## ⚠️ PROBLÈMES COURANTS

### "Accès refusé" lors de la création du certificat
**Solution :** Exécuter PowerShell en tant qu'Administrateur

### "signtool.exe introuvable"
**Solution :** Installer Windows SDK
- https://developer.microsoft.com/windows/downloads/windows-sdk/
- Installer uniquement "Windows App Certification Kit"

### "Le certificat n'est pas approuvé"
**Solution :** Votre ami doit installer le `.cer` dans "Autorités racines de confiance"

### "L'application ne démarre pas chez mon ami"
**Solutions :**
1. Vérifier qu'il a .NET 10 Runtime installé
2. Télécharger : https://dotnet.microsoft.com/download/dotnet/10.0
3. Installer le "Desktop Runtime" pour Windows

---

## 🔒 SÉCURITÉ

### Est-ce que mon application est vraiment sûre ?
Oui ! Vous êtes le développeur. Vous savez qu'il n'y a pas de malware.

### Pourquoi créer un certificat auto-signé ?
Pour prouver que l'application vient bien de vous et n'a pas été modifiée.

### Mon ami doit-il faire confiance au certificat ?
Oui, mais uniquement s'il vous fait confiance. Expliquez-lui que :
- C'est votre application
- Vous l'avez créée
- Le code est sûr

### Quelle est la différence avec un certificat officiel ?
- **Officiel** : Payant (300-1000€/an), reconnu automatiquement par Windows
- **Auto-signé** : Gratuit, nécessite installation manuelle du certificat

---

## 💡 CONSEILS PRO

### 1. Créez un ZIP "Portable"
```powershell
# Dans le dossier publish
.\Build-And-Package.ps1

# Résultat: Application standalone avec tout inclus
```

### 2. Utilisez ClickOnce (Alternative)
```powershell
# Publier avec ClickOnce
dotnet publish -p:PublishProtocol=ClickOnce
```

### 3. Créez un Installer avec InnoSetup
- Plus professionnel
- Installation guidée
- Icônes dans le menu Démarrer

---

## 📞 SUPPORT

Si vous rencontrez des problèmes :

1. **Vérifier les logs** :
   ```powershell
   Get-Content $env:LOCALAPPDATA\SevenwandsCompanion\logs\*.log
   ```

2. **Tester en mode Administrateur** :
   ```powershell
   # Clic droit sur PowerShell > Exécuter en tant qu'administrateur
   ```

3. **Désactiver temporairement Windows Defender** (test uniquement) :
   ```powershell
   # Panneau de configuration > Sécurité Windows > Protection contre les virus
   ```

---

## ✅ RÉSUMÉ RAPIDE

Pour distribuer votre application à des amis :

```powershell
# 1. Compiler (en Administrateur)
cd Scripts
.\Build-And-Package.ps1

# 2. Partager
#    - SevenwandsCompanion-vXXXXXXXX.zip
#    - INSTALLATION_GUIDE.md

# 3. Dire à vos amis de:
#    - Débloquer le ZIP
#    - Installer le certificat .cer
#    - Lancer l'exe
```

**C'est tout ! 🎉**

---

**Dernière mise à jour :** 2025  
**Auteur :** GitHub Copilot
