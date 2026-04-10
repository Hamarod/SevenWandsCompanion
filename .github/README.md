# 🤖 GitHub Actions - SevenWandsCompanion

Ce dossier contient les workflows GitHub Actions pour automatiser le déploiement de l'application.

## 📄 Fichiers

### `publish.yml`
Workflow principal qui :
- ✅ Builder l'application Android (APK signé)
- ✅ Builder l'application Windows (ZIP + MSIX)
- ✅ Créer une release GitHub automatiquement
- ✅ Uploader les fichiers buildés

**Déclencheur** : Push d'un tag de version (ex: `v1.0.3`)

### `DEPLOYMENT_GUIDE.md`
Guide complet pour :
- ⚙️ Configurer les secrets GitHub
- 🔐 Créer et encoder le keystore Android
- 🚀 Créer des releases
- 🐛 Dépanner les problèmes

---

## 🚀 Utilisation Rapide

```bash
# 1. Créer un tag
git tag v1.0.3

# 2. Pousser le tag
git push origin v1.0.3

# 3. ✅ La release est créée automatiquement !
```

---

## 📚 Documentation Complète

Consultez [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md) pour les instructions détaillées.
