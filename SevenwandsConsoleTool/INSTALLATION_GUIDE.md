# 🛡️ Guide d'Installation - Sevenwands Potion Maker

## ⚠️ Message de Windows : "Windows a protégé votre PC"

Ce message est **NORMAL** et **ATTENDU** pour les applications :
- ✅ Non signées avec un certificat Microsoft officiel (coûte 300-1000€/an)
- ✅ Non distribuées via le Microsoft Store
- ✅ Nouvelles ou peu téléchargées

**Votre application est sûre** et créée par un développeur de confiance. Voici comment l'installer.

---

## 🚀 Méthode 1 : Installation Simple (Recommandée)

### Étape 1 : Débloquer le fichier ZIP
1. Faites un **clic droit** sur le fichier ZIP téléchargé
2. Sélectionnez **Propriétés**
3. Cochez la case **"Débloquer"** en bas
4. Cliquez sur **OK**
5. Extrayez le ZIP

### Étape 2 : Lancer l'application
1. Allez dans le dossier extrait
2. Double-cliquez sur `SevenwandsCompanion.exe`
3. Si Windows affiche "Windows a protégé votre PC" :
   - Cliquez sur **"Informations complémentaires"**
   - Cliquez sur **"Exécuter quand même"**

![Windows Protected](https://i.imgur.com/example.png)

---

## 🔐 Méthode 2 : Avec Certificat (Plus Sûre)

Si j'ai fourni un fichier `SevenwandsCompanion.cer` :

### Étape 1 : Installer le certificat
1. **Double-cliquez** sur le fichier `SevenwandsCompanion.cer`
2. Cliquez sur **"Installer le certificat..."**
3. Sélectionnez **"Ordinateur local"** ou **"Utilisateur actuel"**
4. Cliquez sur **"Suivant"**
5. Sélectionnez **"Placer tous les certificats dans le magasin suivant"**
6. Cliquez sur **"Parcourir"**
7. Sélectionnez **"Autorités de certification racines de confiance"**
8. Cliquez sur **"OK"** puis **"Suivant"** et **"Terminer"**
9. Confirmez l'installation du certificat

### Étape 2 : Lancer l'application
L'application ne sera plus bloquée par Windows !

---

## 🎯 Méthode 3 : PowerShell (Pour les Utilisateurs Avancés)

Ouvrez PowerShell dans le dossier de l'application et exécutez :

```powershell
# Débloquer tous les fichiers
Get-ChildItem -Recurse | Unblock-File

# Lancer l'application
.\SevenwandsCompanion.exe
```

---

## ❓ FAQ

### Pourquoi Windows bloque-t-il l'application ?
Windows SmartScreen bloque automatiquement les applications qui :
- Ne sont pas signées avec un certificat officiel
- N'ont pas été téléchargées via le Microsoft Store
- Sont peu téléchargées (nouveau)

C'est une mesure de sécurité normale.

### L'application est-elle sûre ?
Oui ! Cette application a été créée par un développeur de confiance. Le code source est disponible et l'application ne contient aucun malware.

### Dois-je faire confiance au certificat ?
Si vous faites confiance à la personne qui vous a partagé l'application, oui. Le certificat garantit que l'application n'a pas été modifiée depuis sa signature.

### L'application fonctionne-t-elle hors ligne ?
Oui ! L'application est entièrement autonome et ne nécessite pas de connexion Internet.

---

## 🆘 Problèmes Courants

### "L'application ne démarre pas"
1. Vérifiez que vous avez **.NET 10 Runtime** installé
2. Téléchargez depuis : https://dotnet.microsoft.com/download/dotnet/10.0
3. Installez le **Desktop Runtime** pour Windows

### "Erreur de DLL manquante"
1. Installez **Visual C++ Redistributable** :
   - https://aka.ms/vs/17/release/vc_redist.x64.exe

### "Le certificat a expiré"
Le certificat est valable 5 ans. Si expiré, contactez le développeur pour obtenir une nouvelle version.

---

## 📞 Support

En cas de problème, contactez :
- Email : votre@email.com
- GitHub : https://github.com/votre-repo

---

**Version** : 1.0  
**Dernière mise à jour** : 2025
