# 🎨 Direction Artistique Sevenwands

## 🌟 Vue d'Ensemble

L'application Sevenwands Potion Maker utilise une **direction artistique sombre et élégante** inspirée de l'univers magique, avec une palette de couleurs dorées et mystérieuses.

---

## 🎨 Palette de Couleurs

### Couleurs Principales

| Couleur | Code Hex | Usage | Aperçu |
|---------|----------|-------|--------|
| **Background Dark** | `#1A1D29` | Fond principal de l'application | ![#1A1D29](https://via.placeholder.com/50x30/1A1D29/1A1D29.png) |
| **Background Card** | `#252836` | Fond des cartes et conteneurs | ![#252836](https://via.placeholder.com/50x30/252836/252836.png) |
| **Border Color** | `#2D3142` | Bordures et séparateurs | ![#2D3142](https://via.placeholder.com/50x30/2D3142/2D3142.png) |

### Couleurs de Texte

| Couleur | Code Hex | Usage |
|---------|----------|-------|
| **Text Primary** | `White` | Textes principaux |
| **Text Secondary** | `#8B8B8B` | Textes secondaires et labels |
| **Text Accent** | `#D4AF37` | Titres et éléments importants (doré) |

### Couleurs d'Accent

| Couleur | Code Hex | Usage | Aperçu |
|---------|----------|-------|--------|
| **Accent Gold** | `#D4AF37` | Titres, éléments premium | ![#D4AF37](https://via.placeholder.com/50x30/D4AF37/D4AF37.png) |
| **Accent Orange** | `#FFA500` | Barres de progression, prix | ![#FFA500](https://via.placeholder.com/50x30/FFA500/FFA500.png) |
| **Accent Green** | `#4CAF50` | Bénéfices positifs, succès | ![#4CAF50](https://via.placeholder.com/50x30/4CAF50/4CAF50.png) |
| **Accent Blue** | `#2196F3` | Expérience, informations | ![#2196F3](https://via.placeholder.com/50x30/2196F3/2196F3.png) |
| **Accent Red** | `#F44336` | Erreurs, alertes | ![#F44336](https://via.placeholder.com/50x30/F44336/F44336.png) |

---

## 📐 Composants Stylisés

### 1. **Cartes (Borders)**

```xaml
<Border Style="{StaticResource CardStyle}">
    <!-- Contenu -->
</Border>
```

**Propriétés :**
- Stroke: `#2D3142`
- Background: `#252836`
- StrokeThickness: `1`
- StrokeShape: `RoundRectangle 8`

### 2. **Titres Principaux**

```xaml
<Label Text="TITRE PRINCIPAL" 
       Style="{StaticResource TitleStyle}" />
```

**Propriétés :**
- FontSize: `24`
- FontAttributes: `Bold`
- TextColor: `#D4AF37` (doré)

### 3. **Titres de Section**

```xaml
<Label Text="Section" 
       Style="{StaticResource SectionTitleStyle}" />
```

**Propriétés :**
- FontSize: `16`
- FontAttributes: `Bold`
- TextColor: `#D4AF37`

### 4. **Boutons**

```xaml
<Button Text="Action" 
        Style="{StaticResource PrimaryButtonStyle}" />
```

**Propriétés :**
- Background: `#2D3142`
- TextColor: `White`
- CornerRadius: `8`
- Padding: `20,10`

### 5. **Barres de Progression**

```xaml
<ProgressBar Progress="0.5" 
             Style="{StaticResource ProgressStyle}" />
```

**Propriétés :**
- ProgressColor: `#FFA500` (orange)
- BackgroundColor: `#2D3142`
- HeightRequest: `6`

---

## 🎯 Principes de Design

### 1. **Hiérarchie Visuelle**

```
┌─────────────────────────────────────┐
│ 📊 TITRE PRINCIPAL (#D4AF37)       │  ← Doré, 24px, Bold
├─────────────────────────────────────┤
│ Sous-titre (#8B8B8B)               │  ← Gris, 14px
├─────────────────────────────────────┤
│ ┌─────────────────────────────┐   │
│ │ 🧪 Section (#D4AF37)        │   │  ← Doré, 16px, Bold
│ │ ┌─────────────────────────┐ │   │
│ │ │ Contenu (White)         │ │   │  ← Blanc, 14px
│ │ │ Label (#8B8B8B)         │ │   │  ← Gris, 12px
│ │ └─────────────────────────┘ │   │
│ └─────────────────────────────┘   │
└─────────────────────────────────────┘
```

### 2. **Espacement Cohérent**

- **Padding pages** : `20`
- **Espacement sections** : `20`
- **Espacement éléments** : `10`
- **Espacement petits éléments** : `5`

### 3. **Arrondi des Coins**

- **Cartes** : `8px`
- **Boutons** : `8px`
- **Boutons ronds** : `25px` (cercle)

---

## 🖼️ Exemples de Mise en Page

### Page Type 1 : Liste + Détails (MainPage)

```
┌────────────────────────────────────────────────────────┐
│ ⚗️ TITRE PRINCIPAL                                    │
│ Description de la page                                 │
├─────────────┬─────────────┬─────────────────────────┤
│ 🧪 LISTE    │ 🌿 DÉTAILS  │ 📜 RÉSULTATS            │
│ ┌─────────┐ │ ┌─────────┐ │ ┌───────────────────┐   │
│ │ Item 1  │ │ │ Info 1  │ │ │ 💰 Vente: 100    │   │
│ │ Item 2  │ │ │ Info 2  │ │ │ 📈 Bénéfice: 50  │   │
│ │ Item 3  │ │ │ Info 3  │ │ │ ⭐ XP: 25        │   │
│ └─────────┘ │ └─────────┘ │ └───────────────────┘   │
└─────────────┴─────────────┴─────────────────────────┘
```

### Page Type 2 : Statistiques + Liste (TokenTrackingPage)

```
┌────────────────────────────────────────────────────────┐
│ 📊 TITRE PRINCIPAL                  [🔄]               │
│ Description                                            │
├────────────────────────────────────────────────────────┤
│ [Sélecteur d'année]                                   │
├─────────┬─────────┬─────────┬─────────────────────┤
│ 📊 21%  │ 🏆 0/7  │ 📚 7    │ ⚠️ 4               │
│ Prog.   │ Complet │ Total   │ Priorité            │
├─────────┴─────────┴─────────┴─────────────────────┤
│ TROISIÈME ANNÉE                  42 / 200           │
│ [████████░░░░░░░░] 21%                             │
│                                                     │
│ ┌──────────────────────────────────────────────┐   │
│ │ 🌿 ALCHIMIE - BOTANIQUE                      │   │
│ │ [████░░░░░░] 6 / 30                          │   │
│ │ [-] [+] [+5]    Il te reste 24 points       │   │
│ └──────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────┘
```

---

## 🎨 Icônes Utilisées

| Émoji | Usage | Contexte |
|-------|-------|----------|
| ⚗️ | Potions | Titre principal page potions |
| 🧪 | Liste potions | Section liste |
| 🌿 | Ingrédients | Section ingrédients, botanique |
| 📜 | Recette | Section recette, histoire |
| 💰 | Coût | Affichage des prix |
| 💵 | Vente | Prix de vente |
| 📈 | Bénéfice | Profits |
| ⭐ | Expérience | Points d'expérience |
| 📊 | Progression | Statistiques, graphiques |
| 🏆 | Succès | Cours complétés |
| 📚 | Total | Total des cours |
| ⚠️ | Priorité | Haute priorité |
| ✨ | Sorts | Sorts magiques |
| 🐉 | Créatures | Créatures magiques |
| ⚡ | Divers | Activités diverses |
| 🔄 | Réinitialiser | Bouton de reset |

---

## 📝 Conventions de Nommage

### Styles

- **TitleStyle** : Titres principaux (24px, doré)
- **SectionTitleStyle** : Titres de sections (16px, doré)
- **SubtitleStyle** : Sous-titres (14px, gris)
- **CardStyle** : Cartes/Borders
- **PrimaryButtonStyle** : Boutons principaux
- **SmallButtonStyle** : Petits boutons d'action
- **ProgressStyle** : Barres de progression
- **EntryStyle** : Champs de saisie
- **PickerStyle** : Sélecteurs
- **ValueLabelStyle** : Valeurs numériques importantes
- **IconStyle** : Icônes/Emojis

### Couleurs

- **Background{Type}** : Couleurs de fond
- **Text{Type}** : Couleurs de texte
- **Accent{Color}** : Couleurs d'accent
- **Button{Type}** : Couleurs de boutons

---

## 🔧 Comment Appliquer le Style

### Dans App.xaml

```xaml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="Resources/Styles/SevenwandsStyles.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

### Dans une Page

```xaml
<ContentPage BackgroundColor="{StaticResource BackgroundDark}">
    <Grid Padding="20">
        <Label Text="Titre" 
               Style="{StaticResource TitleStyle}" />
        <Border Style="{StaticResource CardStyle}">
            <Label Text="Contenu" 
                   TextColor="{StaticResource TextPrimary}" />
        </Border>
    </Grid>
</ContentPage>
```

---

## 🎯 Checklist pour Nouvelle Page

Lors de la création d'une nouvelle page :

- [ ] BackgroundColor="{StaticResource BackgroundDark}"
- [ ] Padding="20" sur Grid principal
- [ ] Titre avec Style="{StaticResource TitleStyle}"
- [ ] Sous-titre avec TextColor="{StaticResource TextSecondary}"
- [ ] Cartes avec Style="{StaticResource CardStyle}"
- [ ] Titres de section avec Style="{StaticResource SectionTitleStyle}"
- [ ] Textes principaux avec TextColor="{StaticResource TextPrimary}"
- [ ] Icônes emoji pour chaque section
- [ ] Espacement cohérent (RowSpacing="20", ColumnSpacing="15")
- [ ] Boutons avec Style="{StaticResource PrimaryButtonStyle}"

---

## 💡 Tips Design

### 1. **Toujours Utiliser les StaticResource**
❌ Mauvais :
```xaml
<Label TextColor="#D4AF37" />
```

✅ Bon :
```xaml
<Label TextColor="{StaticResource AccentGold}" />
```

### 2. **Hiérarchie Visuelle Claire**
```
Doré (#D4AF37) → Éléments les plus importants
Blanc (White) → Contenu principal
Gris (#8B8B8B) → Informations secondaires
```

### 3. **Espacement Uniforme**
```
Grid Padding="20"
RowSpacing="20" (entre sections)
RowSpacing="10" (dans sections)
RowSpacing="5" (petits éléments)
```

### 4. **Toujours des Icônes**
Chaque section doit avoir une icône emoji pour :
- Identité visuelle forte
- Navigation intuitive
- Esthétique cohérente

---

## 📱 Responsive Design

Le design s'adapte automatiquement :

- **Desktop** : 3 colonnes (Liste | Détails | Résultats)
- **Tablet** : 2 colonnes si nécessaire
- **Mobile** : 1 colonne avec ScrollView

---

## 🎨 Inspirations

- **Harry Potter / Hogwarts Legacy** : Couleurs dorées, ambiance magique
- **Dark Mode Premium** : Élégance, lisibilité
- **Material Design Dark** : Espacement, hiérarchie
- **Steam Dark Theme** : Cartes, bordures subtiles

---

**Auteur** : GitHub Copilot  
**Date** : 2025  
**Version** : 1.0
