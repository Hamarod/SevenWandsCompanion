# 📊 Token Tracking - Système de Sauvegarde

## 🎯 Fonctionnalités Implémentées

### 1. **Sauvegarde Automatique des Données**
Chaque fois que vous incrémentez ou décrémentez le nombre de jetons avec les boutons **+**, **−** ou **+5**, les données sont automatiquement sauvegardées.

### 2. **Deux Niveaux de Sauvegarde**

#### 📁 **Sauvegarde Utilisateur (AppDataDirectory)**
- **Emplacement** : `FileSystem.AppDataDirectory/TokenTracking.json`
- **Objectif** : Persiste les modifications de l'utilisateur entre les sessions
- **Priorité** : Chargé en premier au démarrage de l'application

#### 🔧 **Sauvegarde Source (Mode DEBUG uniquement)**
- **Emplacement** : `Src/TokenTracking.json` (fichier source du projet)
- **Objectif** : Facilite le développement en mettant à jour le fichier source
- **Condition** : Activé uniquement en mode DEBUG (`#if DEBUG`)

### 3. **Chargement Intelligent des Données**

L'application charge les données dans cet ordre :
1. ✅ **AppDataDirectory** : Si des données modifiées existent
2. ✅ **Resources/Raw** : Sinon, charge les données d'origine
3. ✅ **Copie initiale** : Copie automatiquement les données d'origine dans AppDataDirectory lors du premier lancement

```csharp
// Flux de chargement
AppDataDirectory/TokenTracking.json (existe?)
    ├─ OUI → Charger depuis AppDataDirectory
    └─ NON → Charger depuis Resources/Raw/TokenTracking.json
              └─ Copier dans AppDataDirectory pour futures modifications
```

### 4. **Indicateur Visuel de Sauvegarde**

Un indicateur **"Sauvegarde..."** avec une roue de chargement apparaît pendant 2 secondes lors de chaque sauvegarde.

```xaml
<ActivityIndicator IsRunning="{Binding IsSaving}" Color="#D4AF37"/>
<Label Text="Sauvegarde..." TextColor="#D4AF37"/>
```

### 5. **Bouton de Réinitialisation 🔄**

Un bouton circulaire dans l'en-tête permet de réinitialiser toutes les données aux valeurs d'origine.

**Fonctionnement** :
1. Clic sur le bouton 🔄
2. Confirmation : "Voulez-vous réinitialiser toutes les données...?"
3. Si OUI → Supprime `AppDataDirectory/TokenTracking.json`
4. Recharge les données depuis `Resources/Raw/TokenTracking.json`

## 📝 Code Clé

### Sauvegarde Automatique

```csharp
public void OnIncrementClicked(object sender, EventArgs e)
{
    if (sender is Button button && button.CommandParameter is Course course)
    {
        course.CurrentPoints += 1;
        SelectedYear?.RefreshCalculations();
        UpdateStatistics();
        _ = SaveDataAsync(); // ✅ Sauvegarde automatique
    }
}
```

### Méthode SaveDataAsync()

```csharp
private async Task SaveDataAsync()
{
    IsSaving = true; // Affiche l'indicateur
    
    // Sauvegarde utilisateur
    var userDataPath = Path.Combine(FileSystem.AppDataDirectory, TokenTrackingAssetPath);
    await SevenwandsTools.SaveTokenTrackingToJson(userDataPath, Years.ToList());
    
#if DEBUG
    // Sauvegarde source (DEBUG uniquement)
    var projectPath = Path.GetFullPath(Path.Combine(..., "Src", TokenTrackingAssetPath));
    await SevenwandsTools.SaveTokenTrackingToJson(projectPath, Years.ToList());
#endif
    
    await Task.Delay(2000); // Affiche l'indicateur 2 secondes
    IsSaving = false;
}
```

### Réinitialisation

```csharp
public async Task ResetToDefaultDataAsync()
{
    bool confirm = await DisplayAlert("Réinitialiser", "...", "Oui", "Non");
    
    if (confirm)
    {
        var userDataPath = Path.Combine(FileSystem.AppDataDirectory, TokenTrackingAssetPath);
        if (File.Exists(userDataPath))
            File.Delete(userDataPath);
        
        await InitializeDataAsync(); // Recharge depuis Resources/Raw
    }
}
```

## 🧪 Tests

### Test 1 : Sauvegarde Automatique
1. ✅ Ouvrir l'application
2. ✅ Incrémenter un cours avec le bouton **+**
3. ✅ Vérifier que l'indicateur "Sauvegarde..." apparaît
4. ✅ Fermer et rouvrir l'application
5. ✅ Vérifier que les données sont conservées

### Test 2 : Mode DEBUG - Sauvegarde Source
1. ✅ Compiler en mode DEBUG
2. ✅ Modifier des jetons
3. ✅ Vérifier que `Src/TokenTracking.json` est mis à jour
4. ✅ Vérifier les logs : `✅ Token tracking data also saved to source: ...`

### Test 3 : Réinitialisation
1. ✅ Modifier plusieurs cours
2. ✅ Cliquer sur le bouton 🔄
3. ✅ Confirmer la réinitialisation
4. ✅ Vérifier que toutes les données reviennent aux valeurs d'origine

## 📊 Logs de Débogage

Les logs suivants sont affichés dans la console :

```
✅ Token tracking data saved to: [AppDataDirectory]/TokenTracking.json
✅ Token tracking data also saved to source: [ProjectPath]/Src/TokenTracking.json
⚠️ Could not save to source file (DEBUG mode): [Error]
❌ Error saving TokenTracking data: [Error]
```

## 🎨 Interface Utilisateur

### Avant la Sauvegarde
```
┌─────────────────────────────────────────┐
│ SUIVI DES JETONS              [🔄]      │
│ Gérez votre progression académique      │
└─────────────────────────────────────────┘
```

### Pendant la Sauvegarde
```
┌─────────────────────────────────────────┐
│ SUIVI DES JETONS   [⏳ Sauvegarde...] [🔄]│
│ Gérez votre progression académique      │
└─────────────────────────────────────────┘
```

## 🚀 Améliorations Futures

- [ ] Synchronisation cloud (OneDrive, Google Drive)
- [ ] Export/Import JSON manuel
- [ ] Historique des modifications
- [ ] Statistiques de progression (graphiques)
- [ ] Notifications de rappel pour les cours à compléter

## 📦 Fichiers Modifiés

1. ✅ `TokenTrackingPage.xaml.cs` - Logique de sauvegarde
2. ✅ `TokenTrackingPage.xaml` - Interface utilisateur
3. ✅ `TokenTrackingModels.cs` - Modèles de données
4. ✅ `SevenwandsTools.cs` - Méthodes de sérialisation

---

**Auteur** : GitHub Copilot  
**Date** : 2025  
**Version** : 1.0
