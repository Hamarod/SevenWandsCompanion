using System.Collections.ObjectModel;
using SevenwandsConsoleTool;

namespace SevenwandsCompanion
{
    // Écran d'édition de produit dédié à Business (distinct du Créateur de Potions).
    // Édite les données coeur du produit (Potions.json) ainsi que sa catégorie métier
    // et son prix de revente joueur (Business.json).
    public partial class ProductEditorPage : ContentPage
    {
        private const string PotionsAssetPath = "Potions.json";
        private const string IngredientsAssetPath = "Ingredients.json";
        private const string BusinessAssetPath = "Business.json";
        private const int UncategorizedCategoryId = -1;

        private readonly BusinessCategory _uncategorizedOption = new(UncategorizedCategoryId, "— Non catégorisé —");

        private bool _isEditMode;
        public bool IsEditMode => _isEditMode;

        private string _pageTitle = "📦 NOUVEAU PRODUIT";
        public string PageTitle
        {
            get => _pageTitle;
            set { _pageTitle = value; OnPropertyChanged(); }
        }

        private string _productName = "";
        public string ProductName
        {
            get => _productName;
            set { _productName = value; OnPropertyChanged(); }
        }

        private string _description = "";
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        private float _sellPrice;
        public float SellPrice
        {
            get => _sellPrice;
            set { _sellPrice = value; OnPropertyChanged(); }
        }

        private float _resalePrice;
        public float ResalePrice
        {
            get => _resalePrice;
            set { _resalePrice = value; OnPropertyChanged(); }
        }

        public ObservableCollection<BusinessCategory> AvailableCategories { get; set; } = new();

        private BusinessCategory? _selectedCategory;
        public BusinessCategory? SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Ingredient> AvailableIngredients { get; set; } = new();
        public ObservableCollection<RecipeIngredientViewModel> RecipeIngredients { get; set; } = new();

        // Champs du produit non exposés dans ce formulaire (spécifiques au système de potions)
        // mais préservés tels quels pour ne pas corrompre les données existantes.
        private int _productId;
        private int _existingOrder;
        private int _existingMinimumLevel;
        private string? _existingCategory;
        private int? _existingExperience;

        private List<Potion> _allProducts = new();
        private BusinessData _businessData = new();

        public ProductEditorPage()
        {
            InitializeComponent();
            BindingContext = this;
            _ = InitializeDataAsync(null);
        }

        public ProductEditorPage(Potion productToEdit)
        {
            InitializeComponent();
            BindingContext = this;
            _ = InitializeDataAsync(productToEdit);
        }

        private async Task InitializeDataAsync(Potion? productToEdit)
        {
            try
            {
                string ingredientsPath = Path.Combine(FileSystem.AppDataDirectory, IngredientsAssetPath);
                if (!File.Exists(ingredientsPath))
                {
                    await SevenwandsTools.SaveIngredientsToJson(ingredientsPath, new Dictionary<int, Ingredient>());
                }
                var ingredientsDict = SevenwandsTools.DeserializeIngredients(await File.ReadAllTextAsync(ingredientsPath));
                AvailableIngredients.Clear();
                foreach (var ingredient in ingredientsDict.Values.OrderBy(i => i.Name))
                {
                    AvailableIngredients.Add(ingredient);
                }

                string productsPath = Path.Combine(FileSystem.AppDataDirectory, PotionsAssetPath);
                if (!File.Exists(productsPath))
                {
                    await SevenwandsTools.SavePotionsToJson(productsPath, new List<Potion>());
                }
                _allProducts = SevenwandsTools.DeserializePotions(await File.ReadAllTextAsync(productsPath));

                string businessPath = Path.Combine(FileSystem.AppDataDirectory, BusinessAssetPath);
                _businessData = File.Exists(businessPath)
                    ? await SevenwandsTools.LoadBusinessDataFromJson(businessPath)
                    : new BusinessData();

                AvailableCategories.Clear();
                AvailableCategories.Add(_uncategorizedOption);
                foreach (var category in _businessData.Categories.OrderBy(c => c.Name))
                {
                    AvailableCategories.Add(category);
                }

                if (productToEdit != null)
                {
                    _isEditMode = true;
                    OnPropertyChanged(nameof(IsEditMode));
                    PageTitle = "✏️ MODIFIER PRODUIT";

                    _productId = productToEdit.Id;
                    _existingOrder = productToEdit.Order;
                    _existingMinimumLevel = productToEdit.MinimumLevel;
                    _existingCategory = productToEdit.Category;
                    _existingExperience = productToEdit.Experience;

                    ProductName = productToEdit.Name ?? "";
                    Description = productToEdit.Description ?? "";
                    SellPrice = productToEdit.SellPrice;

                    var resaleEntry = _businessData.PotionResalePrices.FirstOrDefault(p => p.PotionId == productToEdit.Id);
                    ResalePrice = resaleEntry?.ResalePrice ?? 0;
                    SelectedCategory = resaleEntry?.CategoryId is int catId
                        ? AvailableCategories.FirstOrDefault(c => c.Id == catId) ?? _uncategorizedOption
                        : _uncategorizedOption;

                    RecipeIngredients.Clear();
                    foreach (var recipeItem in productToEdit.Recipe)
                    {
                        var ingredient = AvailableIngredients.FirstOrDefault(i => i.Id == recipeItem.IngredientId);
                        if (ingredient != null)
                        {
                            RecipeIngredients.Add(new RecipeIngredientViewModel
                            {
                                AvailableIngredients = AvailableIngredients,
                                SelectedIngredient = ingredient,
                                Quantity = recipeItem.Quantity,
                                IngredientId = ingredient.Id
                            });
                        }
                    }
                }
                else
                {
                    _productId = _allProducts.Any() ? _allProducts.Max(p => p.Id) + 1 : 1;
                    _existingOrder = _allProducts.Any() ? _allProducts.Max(p => p.Order) + 10 : 10;
                    _existingMinimumLevel = 0;
                    _existingCategory = "Curative";
                    _existingExperience = 0;
                    SelectedCategory = _uncategorizedOption;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur lors du chargement: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"Error loading product editor data: {ex.Message}");
            }
        }

        private void OnAddIngredientClicked(object sender, EventArgs e)
        {
            if (AvailableIngredients.Any())
            {
                RecipeIngredients.Add(new RecipeIngredientViewModel
                {
                    AvailableIngredients = AvailableIngredients,
                    SelectedIngredient = AvailableIngredients.First(),
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
            if (string.IsNullOrWhiteSpace(ProductName))
            {
                await DisplayAlert("Erreur", "Le nom du produit est obligatoire.", "OK");
                return;
            }

            var recipe = RecipeIngredients
                .Where(ri => ri.SelectedIngredient != null && ri.Quantity > 0)
                .Select(ri => new RecipeIngredient(ri.SelectedIngredient.Id, ri.Quantity))
                .ToList();

            if (!recipe.Any())
            {
                await DisplayAlert("Erreur", "Le produit doit avoir au moins un ingrédient dans sa recette.", "OK");
                return;
            }

            var product = new Potion
            {
                Id = _productId,
                Name = ProductName.Trim(),
                Description = Description ?? "",
                Category = _existingCategory ?? "Curative",
                MinimumLevel = _existingMinimumLevel,
                SellPrice = SellPrice,
                Experience = _existingExperience,
                Order = _existingOrder,
                Recipe = recipe
            };

            try
            {
                _allProducts.RemoveAll(p => p.Id == product.Id);
                _allProducts.Add(product);

                string productsPath = Path.Combine(FileSystem.AppDataDirectory, PotionsAssetPath);
                await SevenwandsTools.SavePotionsToJson(productsPath, _allProducts.OrderBy(p => p.Order).ToList());

                int? categoryId = (SelectedCategory == null || SelectedCategory.Id == UncategorizedCategoryId)
                    ? null
                    : SelectedCategory.Id;

                _businessData.PotionResalePrices.RemoveAll(p => p.PotionId == product.Id);
                _businessData.PotionResalePrices.Add(new PotionResalePrice(product.Id, ResalePrice, categoryId));

                string businessPath = Path.Combine(FileSystem.AppDataDirectory, BusinessAssetPath);
                await SevenwandsTools.SaveBusinessDataToJson(businessPath, _businessData);

                await DisplayAlert("Succès", $"Le produit '{product.Name}' a été {(_isEditMode ? "modifié" : "créé")} avec succès !", "OK");
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur lors de la sauvegarde: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"Save error: {ex.Message}");
            }
        }

        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            if (!_isEditMode) return;

            bool confirm = await DisplayAlert(
                "Confirmation",
                $"Voulez-vous vraiment supprimer le produit '{ProductName}' ?",
                "Supprimer",
                "Annuler");

            if (!confirm) return;

            try
            {
                _allProducts.RemoveAll(p => p.Id == _productId);
                string productsPath = Path.Combine(FileSystem.AppDataDirectory, PotionsAssetPath);
                await SevenwandsTools.SavePotionsToJson(productsPath, _allProducts.OrderBy(p => p.Order).ToList());

                _businessData.PotionResalePrices.RemoveAll(p => p.PotionId == _productId);
                string businessPath = Path.Combine(FileSystem.AppDataDirectory, BusinessAssetPath);
                await SevenwandsTools.SaveBusinessDataToJson(businessPath, _businessData);

                await DisplayAlert("Succès", "Le produit a été supprimé.", "OK");
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur lors de la suppression: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"Delete error: {ex.Message}");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert(
                "Confirmation",
                "Voulez-vous vraiment annuler ? Les modifications seront perdues.",
                "Annuler",
                "Continuer l'édition");

            if (confirm)
            {
                await Shell.Current.GoToAsync("..");
            }
        }
    }
}
