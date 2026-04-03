# 🎯 Sevenwands Companion

Application .NET MAUI pour suivre et gérer les potions et jetons du jeu Sevenwands.

Credit : Pour la gestion des jetons tout l'inspiration viens de Bubulle allez soutenir ici : https://kaldeo.alwaysdata.net/jetons

---

## 🚀 Démarrage Rapide

### Prérequis
- .NET 10 SDK
- Visual Studio 2026 ou supérieur
- Windows SDK (pour la signature de code)

### Installation
1. Cloner le repository
```bash
git clone https://github.com/Hamarod/SevenWandsCompanion
```

2. Ouvrir la solution dans Visual Studio
3. Restaurer les packages NuGet
4. Compiler et exécuter

---

## 📦 Structure du Projet

### SevenwandsCompanion (Application MAUI)
Application principale avec interface utilisateur pour :
- 📊 Suivi des jetons (Token Tracking)
- 🧪 Éditeur de potions
- 🌿 Éditeur d'ingrédients

### SevenwandsConsoleTool
Outils de build et scripts pour la distribution de l'application.

---

## 🎨 Direction Artistique

L'application utilise une palette sombre et élégante inspirée de l'univers magique :
- **Fond principal** : `#1A1D29`
- **Cartes/Conteneurs** : `#252836`
- **Accent doré** : `#D4AF37`
- **Texte** : Blanc et `#8B8B8B`

---

## 📊 Fonctionnalités Token Tracking

- ✅ Sauvegarde automatique des données
- ✅ Deux niveaux de sauvegarde (AppDataDirectory + Source en DEBUG)
- ✅ Chargement intelligent des données
- ✅ Incrémentation/Décrémentation rapide (+1, -1, +5)

---

## 🔨 Build et Distribution

### Build Rapide (Sans Signature)
```powershell
cd SevenwandsConsoleTool\Scripts
.\QuickBuild.bat
# Choisir option [3]
```

### Build avec Signature (Recommandé)
```powershell
# 1. Créer le certificat (une seule fois, en Administrateur)
.\Create-CodeSigningCertificate.ps1

# 2. Build complet
.\Build-And-Package.ps1
```

---

## 🤝 Contribution

Les contributions sont les bienvenues ! N'hésitez pas à ouvrir une issue ou une pull request.

---

## 📄 Licence

Projet personnel - Tous droits réservés
