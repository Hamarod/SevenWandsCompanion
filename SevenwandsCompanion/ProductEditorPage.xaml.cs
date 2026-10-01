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

        // Liste combinée des composants sélectionnables dans une recette : ingrédients bruts ET
        // autres produits finis (ex: "Alcool de fée" peut être choisi directement comme composant
        // de "Praline", au lieu de devoir exister en double comme ressource ET comme produit).
        public ObservableCollection<RecipeComponentOption> AvailableComponents { get; set; } = new();
        public ObservableCollection<RecipeComponentViewModel> RecipeComponents { get; set; } = new();

        private bool _hasNoComponents = true;
        public bool HasNoComponents
        {
            get => _hasNoComponents;
            set { _hasNoComponents = value; OnPropertyChanged(); }
        }

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

                // Composants sélectionnables dans la recette : tous les ingrédients, plus tous les
                // autres produits (un produit ne peut pas se référencer lui-même). Un produit et
                // une ressource homonymes (ex: "Alcool de fée") apparaissent l'un à côté de
                // l'autre, le badge (📦/🧪) permettant de les distinguer.
                AvailableComponents.Clear();
                foreach (var ingredient in AvailableIngredients)
                {
                    AvailableComponents.Add(new RecipeComponentOption
                    {
                        Kind = RecipeComponentKind.Ingredient,
                        IngredientId = ingredient.Id,
                        Name = ingredient.Name ?? ""
                    });
                }
                foreach (var otherProduct in _allProducts.Where(p => p.Id != _productId).OrderBy(p => p.Name))
                {
                    AvailableComponents.Add(new RecipeComponentOption
                    {
                        Kind = RecipeComponentKind.Product,
                        PotionId = otherProduct.Id,
                        Name = otherProduct.Name ?? ""
                    });
                }

                RecipeComponents.Clear();
                if (productToEdit != null)
                {
                    foreach (var recipeItem in productToEdit.Recipe)
                    {
                        RecipeComponentOption? option = recipeItem.PotionId.HasValue
                            ? AvailableComponents.FirstOrDefault(c => c.Kind == RecipeComponentKind.Product && c.PotionId == recipeItem.PotionId.Value)
                            : AvailableComponents.FirstOrDefault(c => c.Kind == RecipeComponentKind.Ingredient && c.IngredientId == recipeItem.IngredientId);

                        if (option != null)
                        {
                            RecipeComponents.Add(new RecipeComponentViewModel
                            {
                                AvailableComponents = AvailableComponents,
                                SelectedComponent = option,
                                Quantity = recipeItem.Quantity
                            });
                        }
                    }
                }
                HasNoComponents = RecipeComponents.Count == 0;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur lors du chargement: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"Error loading product editor data: {ex.Message}");
            }
        }

        private void OnAddComponentClicked(object sender, EventArgs e)
        {
            if (AvailableComponents.Any())
            {
                RecipeComponents.Add(new RecipeComponentViewModel
                {
                    AvailableComponents = AvailableComponents,
                    SelectedComponent = AvailableComponents.First(),
                    Quantity = 1
                });
                HasNoComponents = false;
            }
        }

        private void OnRemoveComponentClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is RecipeComponentViewModel item)
            {
                RecipeComponents.Remove(item);
                HasNoComponents = RecipeComponents.Count == 0;
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ProductName))
            {
                await DisplayAlert("Erreur", "Le nom du produit est obligatoire.", "OK");
                return;
            }

            var recipe = RecipeComponents
                .Where(rc => rc.SelectedComponent != null && rc.Quantity > 0)
                .Select(rc => rc.SelectedComponent!.Kind == RecipeComponentKind.Ingredient
                    ? new RecipeIngredient(rc.SelectedComponent.IngredientId!.Value, rc.Quantity)
                    : RecipeIngredient.ForProduct(rc.SelectedComponent.PotionId!.Value, rc.Quantity))
                .ToList();

            if (!recipe.Any())
            {
                await DisplayAlert("Erreur", "Le produit doit avoir au moins un composant (ingrédient ou produit) dans sa recette.", "OK");
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
                IsBusinessProduct = true,
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

    // Type d'un composant sélectionnable dans la recette d'un produit.
    public enum RecipeComponentKind
    {
        Ingredient,
        Product
    }

    // Une option sélectionnable dans le Picker d'une ligne de recette : soit un ingrédient
    // (Ingredients.json), soit un autre produit fini (Potions.json). Permet à un produit d'entrer
    // directement dans la recette d'un autre sans avoir à exister en double comme ressource ET
    // comme produit (ex: "Alcool de fée").
    public class RecipeComponentOption
    {
        public RecipeComponentKind Kind { get; set; }
        public int? IngredientId { get; set; }
        public int? PotionId { get; set; }
        public string Name { get; set; } = "";

        public string DisplayName => Kind == RecipeComponentKind.Ingredient ? $"📦 {Name}" : $"🧪 {Name}";
    }

    // ViewModel pour une ligne de recette du ProductEditorPage (distinct de RecipeIngredientViewModel
    // utilisé par le Créateur de Potions d'origine, qui ne gère que les ingrédients).
    public class RecipeComponentViewModel : BindableObject
    {
        private ObservableCollection<RecipeComponentOption> _availableComponents = new();
        public ObservableCollection<RecipeComponentOption> AvailableComponents
        {
            get => _availableComponents;
            set { _availableComponents = value; OnPropertyChanged(); }
        }

        private RecipeComponentOption? _selectedComponent;
        public RecipeComponentOption? SelectedComponent
        {
            get => _selectedComponent;
            set { _selectedComponent = value; OnPropertyChanged(); }
        }

        private int _quantity = 1;
        public int Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(); }
        }
    }
}
