# 🎯 Guide Ultra-Simplifié : Partager Votre Application

## Pour les Pressés ⚡

**Vous voulez juste partager votre app avec un ami ?**

### Option 1 : Sans Signature (5 minutes)

1. **Ouvrir PowerShell** dans `SevenwandsConsoleTool/Scripts`
   ```powershell
   cd D:\dev\dotnet\SevenwandsConsoleTool\Scripts
   ```

2. **Double-cliquer sur** `QuickBuild.bat`

3. **Choisir l'option [3]** : Build complet

4. **Partager** le fichier ZIP créé dans `Release/`

5. **Envoyer** aussi le fichier `INSTALLATION_GUIDE.md`

**Dire à votre ami :**
> "Clic droit sur le ZIP > Propriétés > Décocher 'Bloquer' > Extraire > Lancer l'exe > Cliquer sur 'Plus d'infos' > 'Exécuter quand même'"

---

### Option 2 : Avec Signature (10 minutes) ⭐ **RECOMMANDÉ**

**Une seule fois :**
1. Double-cliquer sur `QuickBuild.bat` (en Administrateur)
2. Choisir [2] : Créer un certificat
3. Attendre la création

**Pour chaque build :**
1. Double-cliquer sur `QuickBuild.bat` (en Administrateur)
2. Choisir [3] : Build complet
3. Partager le ZIP + le fichier `.cer`

**Dire à votre ami :**
> "1. Installer le certificat .cer (double-clic > Installer > Autorités racines)
>  2. Lancer l'exe"

---

## Comparaison Rapide

| Méthode | Temps | Complexité | Confiance Windows |
|---------|-------|------------|-------------------|
| Sans signature | 5 min | ⭐ Facile | ⚠️ Bloqué |
| Avec signature | 10 min | ⭐⭐ Moyen | ✅ Accepté après install certificat |
| MSIX | 15 min | ⭐⭐⭐ Avancé | ✅✅ Installation propre |

---

## Schéma Visuel

### Sans Signature
```
┌─────────────┐
│ Vous        │
│             │
│ 1. Build    │────► [ZIP]
│ 2. Partager │
└─────────────┘
                      │
                      ▼
                ┌───────────┐
                │ Ami       │
                │           │
                │ Débloquer │
                │ Extraire  │────► ⚠️ "Windows a protégé..."
                │ Lancer    │────► Clic "Plus d'infos"
                └───────────┘     ► "Exécuter quand même"
```

### Avec Signature
```
┌─────────────┐
│ Vous        │
│             │
│ 1. Cert     │────► [.cer]
│ 2. Build    │────► [ZIP + .cer]
│ 3. Partager │
└─────────────┘
                      │
                      ▼
                ┌───────────┐
                │ Ami       │
                │           │
                │ Install   │────► 🔐 Certificat installé
                │  .cer     │
                │           │
                │ Lancer    │────► ✅ Aucun avertissement!
                │  .exe     │
                └───────────┘
```

---

## FAQ Ultra-Rapide

### Q: Mon ami voit "Windows a protégé votre PC", c'est grave ?
**R:** Non ! C'est normal pour les apps non signées. Dites-lui :
1. Clic sur "Informations complémentaires"
2. Clic sur "Exécuter quand même"

### Q: Comment éviter ce message ?
**R:** Créer un certificat et signer l'app (Option 2 ci-dessus)

### Q: Ça marche sur Mac/Linux ?
**R:** Non, seulement Windows 10/11

### Q: Mon ami a besoin d'installer quelque chose ?
**R:** Oui, .NET 10 Runtime (s'installe automatiquement en général)

### Q: Le certificat dure combien de temps ?
**R:** 5 ans

### Q: C'est légal de créer un certificat ?
**R:** Oui, totalement légal pour distribuer vos propres applications !

---

## Raccourcis Clavier (QuickBuild.bat)

- `1` → Tester l'environnement
- `2` → Créer certificat (1 fois)
- `3` → Build complet
- `4` → Signer une app existante
- `5` → Créer MSIX
- `0` → Quitter

---

## En Cas de Problème

### "Accès refusé"
► Clic droit sur `QuickBuild.bat` > "Exécuter en tant qu'administrateur"

### "signtool.exe introuvable"
► Installez Windows SDK : https://aka.ms/winsdk

### "Mon ami ne peut pas ouvrir le ZIP"
► Dites-lui d'utiliser 7-Zip ou WinRAR

### "L'application crash au démarrage"
► Votre ami doit installer .NET 10 Runtime : https://aka.ms/dotnet10

---

## Checklist Avant Partage

- [ ] Application testée sur votre PC
- [ ] ZIP créé avec `QuickBuild.bat`
- [ ] Certificat `.cer` inclus (si signature)
- [ ] `INSTALLATION_GUIDE.md` envoyé
- [ ] Instructions données à votre ami

---

## Modèle de Message pour Vos Amis

**Sans signature :**
```
Hey ! Je t'envoie mon app Sevenwands Potion Maker.

📦 Instructions :
1. Clic droit sur le ZIP > Propriétés > Cocher "Débloquer" > OK
2. Extraire le ZIP
3. Lancer SevenwandsCompanion.exe
4. Si Windows bloque : "Plus d'infos" > "Exécuter quand même"

Amusez-vous bien ! 🧪
```

**Avec signature :**
```
Hey ! Je t'envoie mon app Sevenwands Potion Maker.

📦 Instructions :
1. Double-cliquer sur SevenwandsCompanion.cer
2. "Installer le certificat" > "Autorités racines de confiance"
3. Extraire le ZIP
4. Lancer SevenwandsCompanion.exe
5. Pas de message Windows ! ✅

Amusez-vous bien ! 🧪
```

---

**Temps total : 5-10 minutes**  
**Niveau : Débutant**  
**Succès : 100%** 🎉
