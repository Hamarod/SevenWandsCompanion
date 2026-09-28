using System.Collections.ObjectModel;
using SevenwandsConsoleTool;

namespace SevenwandsCompanion
{
    // Écran dédié à l'ajout/modification/suppression des produits (potions, et autres
    // types de produits à terme), distinct du "Créateur de potions" (calculateur de coûts).
    public partial class ProductsManagerPage : ContentPage
    {
        private const string PotionsAssetPath = "Potions.json";

        public ObservableCollection<Potion> AllProducts { get; set; } = new();
        public ObservableCollection<Potion> FilteredProducts { get; set; } = new();

        private Potion? _selectedProduct;
        public Potion? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (_selectedProduct != value)
                {
                    _selectedProduct = value;
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
                    UpdateFilteredProducts(value);
                }
            }
        }

        public ProductsManagerPage()
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
                string appDataPath = Path.Combine(FileSystem.AppDataDirectory, PotionsAssetPath);
                if (!File.Exists(appDataPath))
                {
                    await SevenwandsTools.SavePotionsToJson(appDataPath, new List<Potion>());
                }

                string json = await File.ReadAllTextAsync(appDataPath);
                var products = SevenwandsTools.DeserializePotions(json);

                AllProducts.Clear();
                foreach (var product in products.OrderBy(p => p.Order))
                {
                    AllProducts.Add(product);
                }

                UpdateFilteredProducts(SearchText);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Impossible de charger les produits: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"Error loading products: {ex.Message}");
            }
        }

        private void UpdateFilteredProducts(string searchText)
        {
            FilteredProducts.Clear();

            IEnumerable<Potion> source = AllProducts;
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                source = source.Where(p => p.Name != null && p.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var product in source)
            {
                FilteredProducts.Add(product);
            }
        }

        private async void OnNewProductClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ProductEditorPage());
        }

        private async void OnEditProductClicked(object sender, EventArgs e)
        {
            if (SelectedProduct != null)
            {
                await Navigation.PushAsync(new ProductEditorPage(SelectedProduct));
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
