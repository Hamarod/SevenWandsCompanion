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
            IngredientType.ingredientAndResource
        };

        private IngredientType _selectedType = IngredientType.ingredient;
        public IngredientType SelectedType
        {
            get => _selectedType;
            set { _selectedType = value; OnPropertyChanged(); }
        }

        private float? _price;
        public float? Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(); }
        }

        // Catégories métier assignables à cette ressource (une ressource peut appartenir
        // à plusieurs catégories, ex: Forge ET Auberge).
        public ObservableCollection<CategoryToggleOption> CategoryToggles { get; set; } = new();

        private int _resourceId;
        private Dictionary<int, Ingredient> _allResources = new();
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

                string businessPath = Path.Combine(FileSystem.AppDataDirectory, BusinessAssetPath);
                _businessData = File.Exists(businessPath)
                    ? await SevenwandsTools.LoadBusinessDataFromJson(businessPath)
                    : new BusinessData();

                List<int> assignedCategoryIds = new();

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

                // Met à jour les catégories assignées dans Business.json, en conservant
                // la quantité déjà possédée (gérée depuis l'écran Business, pas ici).
                var selectedCategoryIds = CategoryToggles.Where(t => t.IsSelected).Select(t => t.Category.Id).ToList();
                var existingStock = _businessData.IngredientStocks.FirstOrDefault(s => s.IngredientId == resource.Id);
                int quantityOwned = existingStock?.QuantityOwned ?? 0;

                _businessData.IngredientStocks.RemoveAll(s => s.IngredientId == resource.Id);
                _businessData.IngredientStocks.Add(new IngredientStock(resource.Id, quantityOwned, selectedCategoryIds));

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
