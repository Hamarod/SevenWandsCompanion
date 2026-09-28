using System.Collections.ObjectModel;
using SevenwandsConsoleTool;

namespace SevenwandsCompanion
{
    // Écran dédié à l'ajout/modification/suppression des ressources de stock (type
    // "ingredient" ou "resource"), distinct de l'éditeur d'ingrédients utilisé pour
    // construire les recettes de potions (qui gère aussi Fire/rotate/spell).
    public partial class ResourcesManagerPage : ContentPage
    {
        private const string IngredientsAssetPath = "Ingredients.json";

        public ObservableCollection<IngredientViewModel> AllResources { get; set; } = new();
        public ObservableCollection<IngredientViewModel> FilteredResources { get; set; } = new();

        private IngredientViewModel? _selectedResource;
        public IngredientViewModel? SelectedResource
        {
            get => _selectedResource;
            set
            {
                if (_selectedResource != value)
                {
                    _selectedResource = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    UpdateFilteredResources(value);
                }
            }
        }

        public ResourcesManagerPage()
        {
            InitializeComponent();
            BindingContext = this;
            _ = InitializeDataAsync();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                string appDataPath = Path.Combine(FileSystem.AppDataDirectory, IngredientsAssetPath);
                if (!File.Exists(appDataPath))
                {
                    await SevenwandsTools.SaveIngredientsToJson(appDataPath, new Dictionary<int, Ingredient>());
                }

                string json = await File.ReadAllTextAsync(appDataPath);
                var ingredientsDict = SevenwandsTools.DeserializeIngredients(json);

                AllResources.Clear();
                foreach (var ingredient in ingredientsDict.Values
                    .Where(i => i.Type.IsStockable())
                    .OrderBy(i => i.Name))
                {
                    AllResources.Add(new IngredientViewModel(ingredient));
                }

                UpdateFilteredResources(SearchText);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Impossible de charger les ressources: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"Error loading resources: {ex.Message}");
            }
        }

        private void UpdateFilteredResources(string searchText)
        {
            FilteredResources.Clear();

            IEnumerable<IngredientViewModel> source = AllResources;
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                source = source.Where(r => r.Name != null && r.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var resource in source)
            {
                FilteredResources.Add(resource);
            }
        }

        private async void OnNewResourceClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ResourceEditorPage());
        }

        private async void OnEditResourceClicked(object sender, EventArgs e)
        {
            if (SelectedResource != null)
            {
                await Navigation.PushAsync(new ResourceEditorPage(SelectedResource));
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
