using System.Collections.ObjectModel;
using SevenwandsConsoleTool;

namespace SevenwandsCompanion
{
    public partial class PotionEditorPage : ContentPage
    {
        private const string PotionsAssetPath = "Potions.json";
        private const string IngredientsAssetPath = "Ingredients.json";

        // Propriétés pour le binding
        private string _pageTitle = "🧪 NOUVELLE POTION";
        public string PageTitle
        {
            get => _pageTitle;
            set
            {
                _pageTitle = value;
                OnPropertyChanged();
            }
        }

        private string _pageSubtitle = "Créez une nouvelle potion magique";
        public string PageSubtitle
        {
            get => _pageSubtitle;
            set
            {
                _pageSubtitle = value;
                OnPropertyChanged();
            }
        }

        private Potion _editedPotion;
        public Potion EditedPotion
        {
            get => _editedPotion;
            set
            {
                _editedPotion = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<RecipeIngredientViewModel> RecipeIngredients { get; set; }
        public ObservableCollection<Ingredient> AllIngredients { get; set; }
        public ObservableCollection<string> Categories { get; set; }
        public ObservableCollection<string> PositionOptions { get; set; }

        private string _selectedPosition = "À la fin de la liste";
        public string SelectedPosition
        {
            get => _selectedPosition;
            set
            {
                _selectedPosition = value;
                OnPropertyChanged();
            }
        }

        private bool _isEditMode = false;
        private bool _isFirstAppearing = true; // Flag pour éviter le rechargement lors du premier affichage
        private List<Potion> _allPotions;

        public PotionEditorPage()
        {
            InitializeComponent();
            RecipeIngredients = new ObservableCollection<RecipeIngredientViewModel>();
            AllIngredients = new ObservableCollection<Ingredient>();
            PositionOptions = new ObservableCollection<string>();
            Categories = new ObservableCollection<string>
            {
                "Curative",
                "Alteration",
                "Aetheric",
                "Offensive",
                "Defensive",
                "Utility"
            };

            // Nouvelle potion par défaut
            EditedPotion = new Potion
            {
                Id = 0,
                Name = "",
                Description = "",
                Category = "Curative",
                MinimumLevel = 0,
                SellPrice = 0,
                Experience = 0,
                Order = 0,
                Recipe = new List<RecipeIngredient>()
            };

            BindingContext = this;
        }

        // Constructeur pour mode édition
        public PotionEditorPage(Potion potionToEdit) : this()
        {
            _isEditMode = true;
            PageTitle = "✏️ MODIFIER POTION";
            PageSubtitle = $"Modifiez les propriétés de {potionToEdit.Name}";

            // Clone la potion pour éviter de modifier l'original avant sauvegarde
            EditedPotion = new Potion
            {
                Id = potionToEdit.Id,
                Name = potionToEdit.Name,
                Description = potionToEdit.Description,
                Category = potionToEdit.Category,
                MinimumLevel = potionToEdit.MinimumLevel,
                SellPrice = potionToEdit.SellPrice,
                Experience = potionToEdit.Experience,
                Order = potionToEdit.Order,
                Recipe = new List<RecipeIngredient>(potionToEdit.Recipe)
            };

            System.Diagnostics.Debug.WriteLine($"🏗️ Constructeur Edit Mode: Recipe contient {EditedPotion.Recipe.Count} ingrédients");
        }

        // Utiliser OnNavigatedTo au lieu du constructeur pour l'initialisation async
        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);

            System.Diagnostics.Debug.WriteLine($"🚀 OnNavigatedTo: Début de l'initialisation");
            System.Diagnostics.Debug.WriteLine($"🚀 OnNavigatedTo: _isEditMode = {_isEditMode}");
            System.Diagnostics.Debug.WriteLine($"🚀 OnNavigatedTo: EditedPotion.Recipe.Count = {EditedPotion.Recipe?.Count ?? 0}");

            await InitializeDataAsync();

            System.Diagnostics.Debug.WriteLine($"✅ OnNavigatedTo: Initialisation terminée");
            System.Diagnostics.Debug.WriteLine($"✅ OnNavigatedTo: RecipeIngredients.Count = {RecipeIngredients.Count}");
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                // S'assurer que les fichiers existent dans AppDataDirectory (copie initiale si nécessaire)
                await EnsureDataFilesExist();

                // Charger les ingrédients UNIQUEMENT depuis AppDataDirectory (source unique de vérité)
                string appDataIngredientsPath = Path.Combine(FileSystem.AppDataDirectory, IngredientsAssetPath);
                string ingredientsJson = await File.ReadAllTextAsync(appDataIngredientsPath);

                var ingredientsDict = SevenwandsTools.DeserializeIngredients(ingredientsJson);
                AllIngredients.Clear();
                foreach (var ingredient in ingredientsDict.Values.OrderBy(i => i.Name))
                {
                    AllIngredients.Add(ingredient);
                }
                System.Diagnostics.Debug.WriteLine($"📦 {AllIngredients.Count} ingrédients chargés depuis AppDataDirectory");

                // Charger les potions UNIQUEMENT depuis AppDataDirectory
                string appDataPotionsPath = Path.Combine(FileSystem.AppDataDirectory, PotionsAssetPath);
                string potionsJson = await File.ReadAllTextAsync(appDataPotionsPath);

                _allPotions = SevenwandsTools.DeserializePotions(potionsJson);
                System.Diagnostics.Debug.WriteLine($"🧪 {_allPotions.Count} potions chargées depuis AppDataDirectory");

                // Générer les options de position
                BuildPositionOptions();

                // Générer un nouvel ID si mode création
                if (!_isEditMode)
                {
                    EditedPotion.Id = _allPotions.Any() ? _allPotions.Max(p => p.Id) + 1 : 1;
                }

                // Charger la recette si en mode édition (APRÈS avoir chargé les ingrédients)
                // Ajouter un petit délai pour s'assurer que l'UI a terminé le binding de AllIngredients
                if (_isEditMode && EditedPotion.Recipe != null)
                {
                    await Task.Delay(100); // Petit délai pour laisser l'UI se stabiliser
                    LoadRecipeFromPotion();
                    LoadCurrentPosition();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur lors du chargement des données: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
            }
        }

        private void BuildPositionOptions()
        {
            PositionOptions.Clear();

            // Options absolues (début et fin) regroupées ensemble
            PositionOptions.Add("🔝 Au début de la liste");
            PositionOptions.Add("🔚 À la fin de la liste");

            // Options relatives (après chaque potion existante, sauf celle qu'on édite)
            var potionsToShow = _allPotions
                .Where(p => !_isEditMode || p.Id != EditedPotion.Id)
                .OrderBy(p => p.Order);

            foreach (var potion in potionsToShow)
            {
                PositionOptions.Add($"📍 Après \"{potion.Name}\"");
            }

            // Sélection par défaut
            if (!_isEditMode)
            {
                SelectedPosition = "🔚 À la fin de la liste";
            }
        }

        private void LoadCurrentPosition()
        {
            // Trouver la potion juste avant celle-ci dans l'ordre
            var potionBefore = _allPotions
                .Where(p => p.Id != EditedPotion.Id && p.Order < EditedPotion.Order)
                .OrderByDescending(p => p.Order)
                .FirstOrDefault();

            if (potionBefore != null)
            {
                SelectedPosition = $"📍 Après \"{potionBefore.Name}\"";
            }
            else
            {
                // Si pas de potion avant, c'est au début
                SelectedPosition = "🔝 Au début de la liste";
            }
        }

        private void CalculateOrderFromPosition()
        {
            if (string.IsNullOrEmpty(SelectedPosition))
            {
                // Par défaut, mettre à la fin
                EditedPotion.Order = _allPotions.Any() ? _allPotions.Max(p => p.Order) + 10 : 10;
                return;
            }

            // Filtrer les potions (exclure celle en cours d'édition)
            var otherPotions = _allPotions.Where(p => p.Id != EditedPotion.Id).ToList();

            if (SelectedPosition.StartsWith("🔝"))
            {
                // Au début de la liste
                EditedPotion.Order = otherPotions.Any() ? otherPotions.Min(p => p.Order) - 10 : 10;
                System.Diagnostics.Debug.WriteLine($"📍 Position: Au début -> Order = {EditedPotion.Order}");
            }
            else if (SelectedPosition.StartsWith("📍 Après"))
            {
                // Extraire le nom de la potion de la chaîne "📍 Après "PotionName""
                int startIndex = SelectedPosition.IndexOf('"') + 1;
                int endIndex = SelectedPosition.LastIndexOf('"');

                if (startIndex > 0 && endIndex > startIndex)
                {
                    string potionName = SelectedPosition.Substring(startIndex, endIndex - startIndex);
                    var targetPotion = otherPotions.FirstOrDefault(p => p.Name == potionName);

                    if (targetPotion != null)
                    {
                        // Trouver la potion suivante pour calculer un ordre intermédiaire
                        var nextPotion = otherPotions
                            .Where(p => p.Order > targetPotion.Order)
                            .OrderBy(p => p.Order)
                            .FirstOrDefault();

                        if (nextPotion != null)
                        {
                            // Calculer un ordre intermédiaire sûr
                            float gap = nextPotion.Order - targetPotion.Order;
                            if (gap > 1)
                            {
                                // Espace suffisant : insérer au milieu
                                EditedPotion.Order = targetPotion.Order + (int)(gap / 2);
                            }
                            else
                            {
                                // Pas assez d'espace : décaler toutes les potions après
                                foreach (var p in otherPotions.Where(p => p.Order >= nextPotion.Order).OrderBy(p => p.Order))
                                {
                                    p.Order += 10;
                                }
                                EditedPotion.Order = targetPotion.Order + 1;
                            }
                        }
                        else
                        {
                            // Pas de potion après, ajouter après
                            EditedPotion.Order = targetPotion.Order + 10;
                        }

                        System.Diagnostics.Debug.WriteLine($"📍 Position: Après '{potionName}' (Order {targetPotion.Order}) -> Order = {EditedPotion.Order}");
                    }
                    else
                    {
                        // Potion de référence non trouvée, mettre à la fin
                        EditedPotion.Order = otherPotions.Any() ? otherPotions.Max(p => p.Order) + 10 : 10;
                        System.Diagnostics.Debug.WriteLine($"⚠️ Potion '{potionName}' non trouvée -> Order = {EditedPotion.Order}");
                    }
                }
                else
                {
                    // Erreur de parsing, mettre à la fin
                    EditedPotion.Order = otherPotions.Any() ? otherPotions.Max(p => p.Order) + 10 : 10;
                }
            }
            else if (SelectedPosition.StartsWith("🔚"))
            {
                // À la fin de la liste
                EditedPotion.Order = otherPotions.Any() ? otherPotions.Max(p => p.Order) + 10 : 10;
                System.Diagnostics.Debug.WriteLine($"📍 Position: À la fin -> Order = {EditedPotion.Order}");
            }
            else
            {
                // Option non reconnue, mettre à la fin
                EditedPotion.Order = otherPotions.Any() ? otherPotions.Max(p => p.Order) + 10 : 10;
            }
        }

        private void LoadRecipeFromPotion()
        {
            System.Diagnostics.Debug.WriteLine($"📜 LoadRecipeFromPotion: Début du chargement");
            System.Diagnostics.Debug.WriteLine($"📜 EditedPotion.Recipe contient {EditedPotion.Recipe?.Count ?? 0} ingrédients");
            System.Diagnostics.Debug.WriteLine($"📦 AllIngredients contient {AllIngredients.Count} ingrédients");

            RecipeIngredients.Clear();

            if (EditedPotion.Recipe == null || !EditedPotion.Recipe.Any())
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ La recette est vide ou null");
                return;
            }

            foreach (var recipeItem in EditedPotion.Recipe)
            {
                // Trouver l'instance EXACTE dans AllIngredients pour que le Picker binding fonctionne
                var ingredient = AllIngredients.FirstOrDefault(i => i.Id == recipeItem.IngredientId);

                if (ingredient != null)
                {
                    var viewModel = new RecipeIngredientViewModel
                    {
                        AvailableIngredients = AllIngredients, // Passer la collection complète
                        SelectedIngredient = ingredient, // Utilise l'instance exacte de AllIngredients
                        Quantity = recipeItem.Quantity,
                        IngredientId = ingredient.Id
                    };

                    RecipeIngredients.Add(viewModel);
                    System.Diagnostics.Debug.WriteLine($"✅ Ajouté à RecipeIngredients: {ingredient.Name} (ID: {ingredient.Id}, Qty: {recipeItem.Quantity})");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Ingrédient ID {recipeItem.IngredientId} non trouvé dans AllIngredients");
                }
            }

            System.Diagnostics.Debug.WriteLine($"📋 RecipeIngredients final: {RecipeIngredients.Count} items chargés");

            // Forcer la notification de changement pour l'UI
            OnPropertyChanged(nameof(RecipeIngredients));
        }

        /// <summary>
        /// S'assure que les fichiers JSON existent dans AppDataDirectory.
        /// Crée des fichiers VIDES au premier lancement pour permettre à l'utilisateur de découvrir et ajouter progressivement les ingrédients et potions.
        /// </summary>
        private async Task EnsureDataFilesExist()
        {
            string ingredientsPath = Path.Combine(FileSystem.AppDataDirectory, IngredientsAssetPath);
            string potionsPath = Path.Combine(FileSystem.AppDataDirectory, PotionsAssetPath);

            // Créer Ingredients.json VIDE si absent (l'utilisateur ajoutera les ingrédients)
            if (!File.Exists(ingredientsPath))
            {
                try
                {
                    // Créer un dictionnaire vide pour les ingrédients
                    var emptyIngredientsDict = new Dictionary<int, Ingredient>();
                    await SevenwandsTools.SaveIngredientsToJson(ingredientsPath, emptyIngredientsDict);
                    System.Diagnostics.Debug.WriteLine($"🌱 Création fichier vide: {IngredientsAssetPath} (l'utilisateur ajoutera les ingrédients)");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Erreur création Ingredients.json vide: {ex.Message}");
                    throw;
                }
            }

            // Créer Potions.json VIDE si absent (l'utilisateur ajoutera les potions)
            if (!File.Exists(potionsPath))
            {
                try
                {
                    // Créer une liste vide pour les potions
                    var emptyPotionsList = new List<Potion>();
                    await SevenwandsTools.SavePotionsToJson(potionsPath, emptyPotionsList);
                    System.Diagnostics.Debug.WriteLine($"🧪 Création fichier vide: {PotionsAssetPath} (l'utilisateur ajoutera les potions)");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Erreur création Potions.json vide: {ex.Message}");
                    throw;
                }
            }
        }

        private void OnAddIngredientClicked(object sender, EventArgs e)
        {
            if (AllIngredients.Any())
            {
                RecipeIngredients.Add(new RecipeIngredientViewModel
                {
                    AvailableIngredients = AllIngredients,
                    SelectedIngredient = AllIngredients.First(),
                    Quantity = 1
                });
            }
        }

        private void OnRemoveIngredientClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is RecipeIngredientViewModel item)
            {
                RecipeIngredients.Remove(item);
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(EditedPotion.Name))
            {
                await DisplayAlert("Erreur", "Le nom de la potion est obligatoire.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(EditedPotion.Category))
            {
                await DisplayAlert("Erreur", "La catégorie est obligatoire.", "OK");
                return;
            }

            // Construire la recette depuis RecipeIngredients
            EditedPotion.Recipe = RecipeIngredients
                .Where(ri => ri.SelectedIngredient != null && ri.Quantity > 0)
                .Select(ri => new RecipeIngredient(ri.SelectedIngredient.Id, ri.Quantity))
                .ToList();

            if (!EditedPotion.Recipe.Any())
            {
                await DisplayAlert("Erreur", "La potion doit avoir au moins un ingrédient dans sa recette.", "OK");
                return;
            }

            // Calculer l'ordre basé sur la position sélectionnée
            CalculateOrderFromPosition();

            try
            {
                // Mettre à jour ou ajouter la potion
                if (_isEditMode)
                {
                    var existingPotion = _allPotions.FirstOrDefault(p => p.Id == EditedPotion.Id);
                    if (existingPotion != null)
                    {
                        _allPotions.Remove(existingPotion);
                    }
                }

                _allPotions.Add(EditedPotion);

                // Sauvegarder dans AppDataDirectory (source unique de vérité, triée par Order)
                string appDataPath = Path.Combine(FileSystem.AppDataDirectory, PotionsAssetPath);
                var sortedPotions = _allPotions.OrderBy(p => p.Order).ToList();
                await SevenwandsTools.SavePotionsToJson(appDataPath, sortedPotions);
                System.Diagnostics.Debug.WriteLine($"💾 Potion sauvegardée ({sortedPotions.Count} potions): {appDataPath}");

                await DisplayAlert("Succès", $"La potion '{EditedPotion.Name}' a été {(_isEditMode ? "modifiée" : "créée")} avec succès !", "OK");

                // Retour à MainPage
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur lors de la sauvegarde: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"Save error: {ex.Message}");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Confirmation", "Voulez-vous vraiment annuler ? Les modifications seront perdues.", "Annuler", "Continuer l'édition");
            if (confirm)
            {
                await Shell.Current.GoToAsync("..");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        private async void OnManageIngredientsClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("IngredientEditor");
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            System.Diagnostics.Debug.WriteLine($"🎬 OnAppearing: _isFirstAppearing = {_isFirstAppearing}");
            System.Diagnostics.Debug.WriteLine($"🎬 OnAppearing: RecipeIngredients.Count = {RecipeIngredients.Count}");
            System.Diagnostics.Debug.WriteLine($"🎬 OnAppearing: AllIngredients.Count = {AllIngredients.Count}");

            // Ne recharger les ingrédients que si ce n'est pas la première apparition
            // (c'est-à-dire quand on revient de l'IngredientEditor)
            if (_isFirstAppearing)
            {
                _isFirstAppearing = false;
                System.Diagnostics.Debug.WriteLine("🎬 OnAppearing: Première apparition, pas de rechargement");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("🔄 OnAppearing: Retour de l'IngredientEditor, rechargement...");
                await ReloadIngredientsAsync();
            }
        }

        private async Task ReloadIngredientsAsync()
        {
            try
            {
                // Sauvegarder l'état actuel de la recette (ID + quantités)
                var currentRecipeState = RecipeIngredients
                    .Where(ri => ri.SelectedIngredient != null)
                    .Select(ri => new { IngredientId = ri.SelectedIngredient.Id, Quantity = ri.Quantity })
                    .ToList();

                // Essayer de charger depuis AppDataDirectory d'abord
                string appDataPath = Path.Combine(FileSystem.AppDataDirectory, IngredientsAssetPath);

                if (File.Exists(appDataPath))
                {
                    string ingredientsJson = await File.ReadAllTextAsync(appDataPath);
                    var ingredientsDict = SevenwandsTools.DeserializeIngredients(ingredientsJson);

                    AllIngredients.Clear();
                    foreach (var ingredient in ingredientsDict.Values.OrderBy(i => i.Name))
                    {
                        AllIngredients.Add(ingredient);
                    }

                    // Mettre à jour la référence AvailableIngredients dans toutes les lignes de recette existantes
                    foreach (var recipeVM in RecipeIngredients)
                    {
                        recipeVM.AvailableIngredients = AllIngredients;
                    }

                    System.Diagnostics.Debug.WriteLine($"🔄 Mis à jour AvailableIngredients pour {RecipeIngredients.Count} lignes de recette");

                    // Petit délai pour laisser l'UI rebinder AllIngredients dans les Pickers
                    await Task.Delay(50);

                    // Restaurer la recette avec les nouvelles instances d'ingrédients
                    if (currentRecipeState.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"🔄 Restauration de {currentRecipeState.Count} ingrédients dans la recette");

                        // Vider la recette actuelle
                        RecipeIngredients.Clear();

                        // Recréer les éléments de recette avec les nouvelles instances
                        foreach (var recipeItem in currentRecipeState)
                        {
                            var ingredient = AllIngredients.FirstOrDefault(i => i.Id == recipeItem.IngredientId);
                            if (ingredient != null)
                            {
                                RecipeIngredients.Add(new RecipeIngredientViewModel
                                {
                                    AvailableIngredients = AllIngredients,
                                    SelectedIngredient = ingredient,
                                    Quantity = recipeItem.Quantity,
                                    IngredientId = ingredient.Id
                                });
                                System.Diagnostics.Debug.WriteLine($"✅ Restauré: {ingredient.Name} x{recipeItem.Quantity}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"⚠️ Ingrédient ID {recipeItem.IngredientId} non trouvé après rechargement");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reloading ingredients: {ex.Message}");
            }
        }
    }

    // ViewModel pour un ingrédient de recette
    public class RecipeIngredientViewModel : BindableObject
    {
        // Référence à la collection complète d'ingrédients pour le Picker
        private ObservableCollection<Ingredient> _availableIngredients;
        public ObservableCollection<Ingredient> AvailableIngredients
        {
            get => _availableIngredients;
            set
            {
                _availableIngredients = value;
                OnPropertyChanged();
            }
        }

        private Ingredient _selectedIngredient;
        public Ingredient SelectedIngredient
        {
            get => _selectedIngredient;
            set
            {
                _selectedIngredient = value;
                OnPropertyChanged();
                if (value != null)
                {
                    IngredientId = value.Id;
                }
            }
        }

        private int _ingredientId;
        public int IngredientId
        {
            get => _ingredientId;
            set
            {
                _ingredientId = value;
                OnPropertyChanged();
            }
        }

        private int _quantity = 1;
        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged();
            }
        }
    }
}
