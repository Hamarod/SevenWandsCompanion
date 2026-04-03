using System.Collections.ObjectModel;
using SevenwandsConsoleTool;

namespace SevenwandsCompanion
{
    public partial class MainPage : ContentPage
    {
        // Use logic paths or filenames that match how MauiAssets are deployed. 
        private const string IngredientsAssetPath = "Ingredients.json";
        private const string PotionsAssetPath = "Potions.json";

        // Cache des ingrédients pour le calcul des coûts
        private Dictionary<int, Ingredient> _ingredientsDictionary;

        public ObservableCollection<Potion> MesPotions { get; set; } = new ObservableCollection<Potion>();

        private ObservableCollection<Potion> _filteredPotions = new ObservableCollection<Potion>();
        public ObservableCollection<Potion> FilteredPotions
        {
            get => _filteredPotions;
            set
            {
                if (_filteredPotions != value)
                {
                    _filteredPotions = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _numberOfPotions = 1;
        public int NumberOfPotions
        {
            get => _numberOfPotions;
            set
            {
                if (_numberOfPotions != value)
                {
                    _numberOfPotions = value;
                    OnPropertyChanged();
                    // Mise à jour des affichages
                    _ = UpdateIngredientsAffichesAsync();
                    _ = UpdateRecetteAffichesAsync();
                    _ = UpdateCalculationsAsync(); // Combined update
                }
            }
        }

        private Potion _selectedPotion;
        public Potion SelectedPotion
        {
            get => _selectedPotion;
            set
            {
                if (_selectedPotion != value)
                {
                    _selectedPotion = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RecetteAffichee));

                    _ = UpdateIngredientsAffichesAsync();
                    _ = UpdateRecetteAffichesAsync();
                    _ = UpdateCalculationsAsync(); // Combined update
                }
            }
        }

        // Propriété dédiée pour l'affichage du coût total
        public float DisplayedCost => SelectedPotion?.Cost ?? 0;

        // Stockage de l'expérience calculée pour ne pas écraser la valeur de base du SelectedPotion
        private int _calculatedExperience;
        public int DisplayedExperience => _calculatedExperience;

        // Ajout des propriétés pour le Binding
        public float DisplayedSellPrice => _selectedPotion?.SellPrice * NumberOfPotions ?? 0;

        // On utilisera une propriété privée pour stocker le bénéfice calculé par les outils
        private float _calculatedBenefits;
        public float DisplayedBenefits => _calculatedBenefits;

        private List<Ingredient> _ingredientsAffiches = new List<Ingredient>();
        public List<Ingredient> IngredientsAffiches
        {
            get => _ingredientsAffiches;
            private set
            {
                if (_ingredientsAffiches != value)
                {
                    _ingredientsAffiches = value;
                    OnPropertyChanged(nameof(IngredientsAffiches));
                }
            }
        }

        private List<Ingredient> _recetteAffiches = new List<Ingredient>();
        public List<Ingredient> RecetteAffiches
        {
            get => _recetteAffiches;
            private set
            {
                if (_recetteAffiches != value)
                {
                    _recetteAffiches = value;
                    OnPropertyChanged(nameof(RecetteAffiches));
                }
            }
        }

        public List<RecipeIngredient> RecetteAffichee => SelectedPotion?.Recipe;

        public MainPage()
        {
            InitializeComponent();

            // --- CORRECTION : Suppression des appels d'écriture de fichier qui plantent sur mobile ---
            //SevenwandsTools.SaveIngredientsToJson(IngredientsAssetPath);
            //SevenwandsTools.SavePotionsToJson(PotionsAssetPath);
            // -------------------------------------------------------------------------------------

            BindingContext = this;
            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                // S'assurer que les fichiers existent dans AppDataDirectory (copie initiale si nécessaire)
                await EnsureDataFilesExist();

                // LOAD INGREDIENTS depuis AppDataDirectory uniquement
                string appDataIngredientsPath = Path.Combine(FileSystem.AppDataDirectory, IngredientsAssetPath);
                string ingredientsJson = await File.ReadAllTextAsync(appDataIngredientsPath);
                _ingredientsDictionary = SevenwandsTools.DeserializeIngredients(ingredientsJson);
                System.Diagnostics.Debug.WriteLine($"📦 MainPage: {_ingredientsDictionary.Count} ingrédients chargés depuis AppDataDirectory");

                // LOAD POTIONS depuis AppDataDirectory uniquement
                string appDataPotionsPath = Path.Combine(FileSystem.AppDataDirectory, PotionsAssetPath);
                string potionsJson = await File.ReadAllTextAsync(appDataPotionsPath);
                var potions = SevenwandsTools.DeserializePotions(potionsJson);
                MesPotions.Clear();
                // TRI PAR ORDER à l'initialisation
                foreach (var p in potions.OrderBy(x => x.Order)) MesPotions.Add(p);
                System.Diagnostics.Debug.WriteLine($"🧪 MainPage: {MesPotions.Count} potions chargées depuis AppDataDirectory");

                // Initialize filtered list with all potions
                UpdateFilteredPotions(string.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
                await DisplayAlert("Erreur", $"Impossible de charger les données: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// S'assure que les fichiers JSON existent dans AppDataDirectory.
        /// Crée des fichiers VIDES au premier lancement pour permettre à l'utilisateur de découvrir et ajouter progressivement les ingrédients et potions.
        /// </summary>
        private async Task EnsureDataFilesExist()
        {
            string ingredientsPath = Path.Combine(FileSystem.AppDataDirectory, IngredientsAssetPath);
            string potionsPath = Path.Combine(FileSystem.AppDataDirectory, PotionsAssetPath);

            // Créer Ingredients.json VIDE si absent
            if (!File.Exists(ingredientsPath))
            {
                try
                {
                    var emptyIngredientsDict = new Dictionary<int, Ingredient>();
                    await SevenwandsTools.SaveIngredientsToJson(ingredientsPath, emptyIngredientsDict);
                    System.Diagnostics.Debug.WriteLine($"🌱 MainPage: Création fichier vide Ingredients (l'utilisateur ajoutera les ingrédients)");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Erreur création Ingredients.json vide: {ex.Message}");
                    throw;
                }
            }

            // Créer Potions.json VIDE si absent
            if (!File.Exists(potionsPath))
            {
                try
                {
                    var emptyPotionsList = new List<Potion>();
                    await SevenwandsTools.SavePotionsToJson(potionsPath, emptyPotionsList);
                    System.Diagnostics.Debug.WriteLine($"🧪 MainPage: Création fichier vide Potions (l'utilisateur ajoutera les potions)");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Erreur création Potions.json vide: {ex.Message}");
                    throw;
                }
            }
        }

        // Helper private method to load ingredients if needed logic
        private async Task LoadIngredientsAsync()
        {
            if (_ingredientsDictionary == null)
            {
                string appDataPath = Path.Combine(FileSystem.AppDataDirectory, IngredientsAssetPath);
                string ingredientsJson = await File.ReadAllTextAsync(appDataPath);
                _ingredientsDictionary = SevenwandsTools.DeserializeIngredients(ingredientsJson);
            }
        }

        // Combined method to handle dependencies between calculations
        private async Task UpdateCalculationsAsync()
        {
            if (_selectedPotion != null)
            {
                if (_ingredientsDictionary == null)
                {
                    await LoadIngredientsAsync();
                }

                // 1. Calculate Ingredients & Cost
                var potionWithQuantities = _selectedPotion.CalculIngredientNeeded(NumberOfPotions, _ingredientsDictionary);

                // This updates .Cost and .Benefits on the temp object
                SevenwandsTools.CalculatePotionCosts(potionWithQuantities, _ingredientsDictionary, NumberOfPotions);

                _selectedPotion.Cost = potionWithQuantities.Cost;
                _calculatedBenefits = potionWithQuantities.Benefits;

                // 2. Calculate Experience
                // We use the base _selectedPotion logic or the temp object (both have the same per-unit XP now)
                _calculatedExperience = SevenwandsTools.CalculatePotionExperience(_selectedPotion, NumberOfPotions);

                // Notifications
                OnPropertyChanged(nameof(DisplayedCost));
                OnPropertyChanged(nameof(DisplayedSellPrice));
                OnPropertyChanged(nameof(DisplayedBenefits));
                OnPropertyChanged(nameof(DisplayedExperience));
            }
            else
            {
                _calculatedExperience = 0;
                OnPropertyChanged(nameof(DisplayedExperience));
            }
        }

        // Kept for compatibility if called individually, but redirected to main logic
        private async Task UpdateCostAsync() => await UpdateCalculationsAsync();
        private async Task UpdateExperienceAsync() => await UpdateCalculationsAsync();

        private async Task UpdateIngredientsAffichesAsync()
        {
            if (_selectedPotion != null)
            {
                if (_ingredientsDictionary == null) await LoadIngredientsAsync();

                if (_ingredientsDictionary != null)
                {
                    var result = SevenwandsTools.DisplayIngredientNeeds(_selectedPotion, _ingredientsDictionary, NumberOfPotions);
                    IngredientsAffiches = result.Where(i => i.Type == IngredientType.ingredient).ToList();
                }
            }
            else
            {
                IngredientsAffiches = new List<Ingredient>();
            }
            await Task.CompletedTask;
        }

        private async Task UpdateRecetteAffichesAsync()
        {
            if (_selectedPotion != null)
            {
                if (_ingredientsDictionary == null) await LoadIngredientsAsync();

                if (_ingredientsDictionary != null)
                {
                    RecetteAffiches = SevenwandsTools.DisplayIngredientNeeds(_selectedPotion, _ingredientsDictionary, 1);
                }
            }
            else
            {
                RecetteAffiches = new List<Ingredient>();
            }
            await Task.CompletedTask;
        }

        // Search functionality
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFilteredPotions(e.NewTextValue);
        }

        private void UpdateFilteredPotions(string searchText)
        {
            FilteredPotions.Clear();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Show all potions
                foreach (var potion in MesPotions)
                {
                    FilteredPotions.Add(potion);
                }
            }
            else
            {
                // Filter potions by name (case-insensitive)
                var filtered = MesPotions.Where(p =>
                    p.Name != null && p.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));

                foreach (var potion in filtered)
                {
                    FilteredPotions.Add(potion);
                }
            }
        }

        // Navigation vers PotionEditor
        private async void OnCreatePotionClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("PotionEditor");
        }

        private async void OnEditPotionClicked(object sender, EventArgs e)
        {
            if (SelectedPotion != null)
            {
                var editorPage = new PotionEditorPage(SelectedPotion);
                await Navigation.PushAsync(editorPage);
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Recharger les potions quand on revient sur la page
            await ReloadPotionsAsync();
        }

        private async Task ReloadPotionsAsync()
        {
            try
            {
                // RECHARGER LES INGRÉDIENTS depuis AppDataDirectory uniquement
                string appDataIngredientsPath = Path.Combine(FileSystem.AppDataDirectory, IngredientsAssetPath);
                string ingredientsJson = await File.ReadAllTextAsync(appDataIngredientsPath);
                _ingredientsDictionary = SevenwandsTools.DeserializeIngredients(ingredientsJson);
                System.Diagnostics.Debug.WriteLine($"🔄 MainPage: Ingrédients rechargés depuis AppDataDirectory");

                // RECHARGER LES POTIONS depuis AppDataDirectory uniquement
                string appDataPotionsPath = Path.Combine(FileSystem.AppDataDirectory, PotionsAssetPath);
                string potionsJson = await File.ReadAllTextAsync(appDataPotionsPath);
                var potions = SevenwandsTools.DeserializePotions(potionsJson);

                // TRI PAR ORDER pour affichage correct
                MesPotions.Clear();
                foreach (var p in potions.OrderBy(x => x.Order))
                {
                    MesPotions.Add(p);
                }
                System.Diagnostics.Debug.WriteLine($"🔄 MainPage: {MesPotions.Count} potions rechargées depuis AppDataDirectory (triées par Order)");

                UpdateFilteredPotions(string.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reloading data: {ex.Message}");
            }
        }
    }
}