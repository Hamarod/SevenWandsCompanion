using System.Collections.ObjectModel;
using SevenwandsConsoleTool;

namespace SevenwandsCompanion
{
    // Écran d'édition de ressource dédié à Business (distinct de l'éditeur d'ingrédients
    // utilisé pour les recettes de potions). Ne propose que les types "stockables"
    // (ingredient/resource) : Fire/rotate/spell n'ont rien à faire ici.
    public partial class ResourceEditorPage : ContentPage
    {
        private const string IngredientsAssetPath = "Ingredients.json";
        private const string PotionsAssetPath = "Potions.json";
        private const string BusinessAssetPath = "Business.json";

        private bool _isEditMode;
        public bool IsEditMode => _isEditMode;

        private string _pageTitle = "🌿 NOUVELLE RESSOURCE";
        public string PageTitle
        {
            get => _pageTitle;
            set { _pageTitle = value; OnPropertyChanged(); }
        }

        private string _resourceName = "";
        public string ResourceName
        {
            get => _resourceName;
            set { _resourceName = value; OnPropertyChanged(); }
        }

        private string _description = "";
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public ObservableCollection<IngredientType> AvailableTypes { get; } = new()
        {
            IngredientType.ingredient,
            IngredientType.resource,
            IngredientType.ingredientAndResource,
            IngredientType.resourceAndProduct,
            IngredientType.ingredientAndResourceAndProduct
        };

        private IngredientType _selectedType = IngredientType.ingredient;
        public IngredientType SelectedType
        {
            get => _selectedType;
            set
            {
                _selectedType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsProductType));
            }
        }

        // Affiche la section "Recette" uniquement pour les types vendables directement comme
        // produit (resourceAndProduct/ingredientAndResourceAndProduct) : une ressource "pure"
        // n'a rien à fabriquer. Cette même ressource apparaît alors aussi côté Produits, classée
        // sous SES PROPRES catégories (CategoryToggles ci-dessous) : pas de catégorisation
        // séparée à refaire pour le même élément physique.
        public bool IsProductType => SelectedType.IsSellableAsProduct();

        // Liste combinée des composants sélectionnables dans la recette de cette ressource-
        // produit : les autres ingrédients (elle-même exclue) ET tous les produits (Potions).
        public ObservableCollection<RecipeComponentOption> AvailableComponents { get; set; } = new();
        public ObservableCollection<RecipeComponentViewModel> RecipeComponents { get; set; } = new();

        private bool _hasNoRecipeComponents = true;
        public bool HasNoRecipeComponents
        {
            get => _hasNoRecipeComponents;
            set { _hasNoRecipeComponents = value; OnPropertyChanged(); }
        }

        // Price reste float? (utilisé pour la sauvegarde), mais l'Entry se lie à PriceText
        // (string) : un binding direct Entry.Text <-> float? ne sait pas représenter "vide" (une
        // chaîne vide ne convertit pas vers null), donc MAUI annule la saisie et réaffiche
        // l'ancienne valeur dès qu'on efface le champ. PriceText gère "vide" explicitement.
        private float? _price;
        public float? Price
        {
            get => _price;
            set
            {
                if (_price != value)
                {
                    _price = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PriceText));
                }
            }
        }

        public string PriceText
        {
            get => _price?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "";
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    Price = null;
                }
                else if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, out var parsed)
                    || float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsed))
                {
                    Price = parsed;
                }
                // Saisie partielle/invalide (ex: "-", "1.") : on n'écrase pas Price, l'utilisateur
                // continue de taper.
            }
        }

        // Catégories métier assignables à cette ressource (une ressource peut appartenir
        // à plusieurs catégories, ex: Forge ET Auberge).
        public ObservableCollection<CategoryToggleOption> CategoryToggles { get; set; } = new();

        private int _resourceId;
        private Dictionary<int, Ingredient> _allResources = new();
        private List<Potion> _allProducts = new();
        private BusinessData _businessData = new();

        public ResourceEditorPage()
        {
            InitializeComponent();
            BindingContext = this;
            _ = InitializeDataAsync(null);
        }

        public ResourceEditorPage(IngredientViewModel resourceToEdit)
        {
            InitializeComponent();
            BindingContext = this;
            _ = InitializeDataAsync(resourceToEdit);
        }

        private async Task InitializeDataAsync(IngredientViewModel? resourceToEdit)
        {
            try
            {
                string appDataPath = Path.Combine(FileSystem.AppDataDirectory, IngredientsAssetPath);
                if (!File.Exists(appDataPath))
                {
                    await SevenwandsTools.SaveIngredientsToJson(appDataPath, new Dictionary<int, Ingredient>());
                }

                _allResources = SevenwandsTools.DeserializeIngredients(await File.ReadAllTextAsync(appDataPath));

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

                List<int> assignedCategoryIds = new();
                List<RecipeIngredient> existingProductRecipe = new();

                if (resourceToEdit != null)
                {
                    _isEditMode = true;
                    OnPropertyChanged(nameof(IsEditMode));
                    PageTitle = "✏️ MODIFIER RESSOURCE";

                    _resourceId = resourceToEdit.Id;
                    ResourceName = resourceToEdit.Name ?? "";
                    Description = resourceToEdit.Description ?? "";
                    SelectedType = resourceToEdit.Type;
                    Price = resourceToEdit.Price;

                    var existingStock = _businessData.IngredientStocks.FirstOrDefault(s => s.IngredientId == _resourceId);
                    if (existingStock != null)
                    {
                        assignedCategoryIds = existingStock.CategoryIds.Any()
                            ? existingStock.CategoryIds
                            : (existingStock.LegacyCategoryId.HasValue ? new List<int> { existingStock.LegacyCategoryId.Value } : new List<int>());
                        existingProductRecipe = existingStock.ProductRecipe;
                    }
                }
                else
                {
                    _resourceId = _allResources.Any() ? _allResources.Keys.Max() + 10 : 10;
                }

                CategoryToggles.Clear();
                foreach (var category in _businessData.Categories.OrderBy(c => c.Name))
                {
                    CategoryToggles.Add(new CategoryToggleOption(category, assignedCategoryIds.Contains(category.Id)));
                }

                // Composants sélectionnables dans la recette de cette ressource-produit : les
                // autres ingrédients (elle-même exclue, ne peut pas se référencer elle-même) et
                // tous les produits.
                AvailableComponents.Clear();
                foreach (var otherIngredient in _allResources.Values.Where(i => i.Id != _resourceId).OrderBy(i => i.Name))
                {
                    AvailableComponents.Add(new RecipeComponentOption
                    {
                        Kind = RecipeComponentKind.Ingredient,
                        IngredientId = otherIngredient.Id,
                        Name = otherIngredient.Name ?? ""
                    });
                }
                foreach (var product in _allProducts.OrderBy(p => p.Name))
                {
                    AvailableComponents.Add(new RecipeComponentOption
                    {
                        Kind = RecipeComponentKind.Product,
                        PotionId = product.Id,
                        Name = product.Name ?? ""
                    });
                }

                RecipeComponents.Clear();
                foreach (var recipeItem in existingProductRecipe)
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
                HasNoRecipeComponents = RecipeComponents.Count == 0;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur lors du chargement: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"Error loading resource editor data: {ex.Message}");
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ResourceName))
            {
                await DisplayAlert("Erreur", "Le nom de la ressource est obligatoire.", "OK");
                return;
            }

            try
            {
                var resource = new Ingredient(
                    id: _resourceId,
                    name: ResourceName.Trim(),
                    type: SelectedType,
                    description: Description ?? "",
                    price: Price);

                _allResources[resource.Id] = resource;

                string appDataPath = Path.Combine(FileSystem.AppDataDirectory, IngredientsAssetPath);
                await SevenwandsTools.SaveIngredientsToJson(appDataPath, _allResources);

                // Met à jour les catégories assignées et la recette "produit" dans Business.json,
                // en conservant la quantité possédée, le prix de revente et la catégorie produit
                // déjà existants (gérés depuis l'écran Business, pas ici) — sinon chaque
                // enregistrement depuis cet écran les écraserait à zéro.
                var selectedCategoryIds = CategoryToggles.Where(t => t.IsSelected).Select(t => t.Category.Id).ToList();
                var existingStock = _businessData.IngredientStocks.FirstOrDefault(s => s.IngredientId == resource.Id);

                var productRecipe = RecipeComponents
                    .Where(rc => rc.SelectedComponent != null && rc.Quantity > 0)
                    .Select(rc => rc.SelectedComponent!.Kind == RecipeComponentKind.Ingredient
                        ? new RecipeIngredient(rc.SelectedComponent.IngredientId!.Value, rc.Quantity)
                        : RecipeIngredient.ForProduct(rc.SelectedComponent.PotionId!.Value, rc.Quantity))
                    .ToList();

                var newStock = new IngredientStock(resource.Id, existingStock?.QuantityOwned ?? 0, selectedCategoryIds)
                {
                    ResalePrice = existingStock?.ResalePrice ?? 0,
                    ProductRecipe = productRecipe
                };

                _businessData.IngredientStocks.RemoveAll(s => s.IngredientId == resource.Id);
                _businessData.IngredientStocks.Add(newStock);

                string businessPath = Path.Combine(FileSystem.AppDataDirectory, BusinessAssetPath);
                await SevenwandsTools.SaveBusinessDataToJson(businessPath, _businessData);

                await DisplayAlert("Succès", $"La ressource '{resource.Name}' a été {(_isEditMode ? "modifiée" : "créée")} avec succès !", "OK");
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur lors de la sauvegarde: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"Save error: {ex.Message}");
            }
        }

        private void OnAddRecipeComponentClicked(object sender, EventArgs e)
        {
            if (AvailableComponents.Any())
            {
                RecipeComponents.Add(new RecipeComponentViewModel
                {
                    AvailableComponents = AvailableComponents,
                    SelectedComponent = AvailableComponents.First(),
                    Quantity = 1
                });
                HasNoRecipeComponents = false;
            }
        }

        private void OnRemoveRecipeComponentClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is RecipeComponentViewModel item)
            {
                RecipeComponents.Remove(item);
                HasNoRecipeComponents = RecipeComponents.Count == 0;
            }
        }

        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            if (!_isEditMode) return;

            bool confirm = await DisplayAlert(
                "Confirmation",
                $"Voulez-vous vraiment supprimer la ressource '{ResourceName}' ?\n\n⚠️ Attention: Les potions utilisant cette ressource pourraient devenir invalides.",
                "Supprimer",
                "Annuler");

            if (!confirm) return;

            try
            {
                _allResources.Remove(_resourceId);

                string appDataPath = Path.Combine(FileSystem.AppDataDirectory, IngredientsAssetPath);
                await SevenwandsTools.SaveIngredientsToJson(appDataPath, _allResources);

                _businessData.IngredientStocks.RemoveAll(s => s.IngredientId == _resourceId);
                string businessPath = Path.Combine(FileSystem.AppDataDirectory, BusinessAssetPath);
                await SevenwandsTools.SaveBusinessDataToJson(businessPath, _businessData);

                await DisplayAlert("Succès", "La ressource a été supprimée.", "OK");
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
