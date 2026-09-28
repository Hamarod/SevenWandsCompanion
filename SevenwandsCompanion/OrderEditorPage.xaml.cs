using System.Collections.ObjectModel;
using SevenwandsConsoleTool;

namespace SevenwandsCompanion
{
    // Écran de création d'une commande : commanditaire, deadline de livraison, somme négociée,
    // et sélection (via recherche) des produits finis et/ou ressources brutes à fournir. Les
    // produits et ressources partagent une seule liste (un badge distingue les deux) car un nom
    // de ressource peut coïncider avec un nom de produit fini.
    public partial class OrderEditorPage : ContentPage
    {
        private const string IngredientsAssetPath = "Ingredients.json";
        private const string PotionsAssetPath = "Potions.json";
        private const string BusinessAssetPath = "Business.json";

        private BusinessData _businessData = new();

        // Liste complète (non filtrée) conservant les quantités saisies même quand un élément
        // est temporairement masqué par la recherche.
        private List<OrderEntryViewModel> _allEntries = new();

        private string _customerName = "";
        public string CustomerName
        {
            get => _customerName;
            set { _customerName = value; OnPropertyChanged(); }
        }

        private DateTime _deadline = DateTime.Today.AddDays(7);
        public DateTime Deadline
        {
            get => _deadline;
            set { _deadline = value; OnPropertyChanged(); }
        }

        private float _negotiatedPrice;
        public float NegotiatedPrice
        {
            get => _negotiatedPrice;
            set { _negotiatedPrice = value; OnPropertyChanged(); }
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
                    ApplySearchFilter();
                }
            }
        }

        // Collection affichée (filtrée par SearchText), liée au BindableLayout du XAML.
        public ObservableCollection<OrderEntryViewModel> Entries { get; set; } = new();

        public OrderEditorPage()
        {
            InitializeComponent();
            BindingContext = this;
            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                string potionsPath = Path.Combine(FileSystem.AppDataDirectory, PotionsAssetPath);
                var potions = File.Exists(potionsPath)
                    ? SevenwandsTools.DeserializePotions(await File.ReadAllTextAsync(potionsPath))
                    : new List<Potion>();

                string ingredientsPath = Path.Combine(FileSystem.AppDataDirectory, IngredientsAssetPath);
                var ingredients = File.Exists(ingredientsPath)
                    ? SevenwandsTools.DeserializeIngredients(await File.ReadAllTextAsync(ingredientsPath))
                    : new Dictionary<int, Ingredient>();

                string businessPath = Path.Combine(FileSystem.AppDataDirectory, BusinessAssetPath);
                _businessData = File.Exists(businessPath)
                    ? await SevenwandsTools.LoadBusinessDataFromJson(businessPath)
                    : new BusinessData();

                var products = potions.Select(p => new OrderEntryViewModel
                {
                    Kind = OrderEntryKind.Product,
                    PotionId = p.Id,
                    Name = p.Name ?? ""
                });

                var resources = ingredients.Values
                    .Where(i => i.Type.IsStockable())
                    .Select(i => new OrderEntryViewModel
                    {
                        Kind = OrderEntryKind.Resource,
                        IngredientId = i.Id,
                        Name = i.Name ?? ""
                    });

                // Trié par nom d'abord : un produit et une ressource homonymes se retrouvent
                // l'un à côté de l'autre, le badge (🧪/📦) permettant de les distinguer.
                _allEntries = products.Concat(resources)
                    .OrderBy(e => e.Name)
                    .ThenBy(e => e.Kind)
                    .ToList();

                ApplySearchFilter();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur lors du chargement: {ex.Message}", "OK");
            }
        }

        private void ApplySearchFilter()
        {
            string search = SearchText?.Trim() ?? "";

            Entries.Clear();
            foreach (var entry in _allEntries.Where(e => string.IsNullOrEmpty(search) || e.Name.Contains(search, StringComparison.OrdinalIgnoreCase)))
            {
                Entries.Add(entry);
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CustomerName))
            {
                await DisplayAlert("Erreur", "Le nom du commanditaire est obligatoire.", "OK");
                return;
            }

            // On regarde la liste complète (pas la collection filtrée affichée) pour ne perdre
            // aucune quantité saisie avant une recherche.
            var items = _allEntries
                .Where(entry => entry.Kind == OrderEntryKind.Product && entry.Quantity > 0)
                .Select(entry => new OrderItem(entry.PotionId!.Value, entry.Quantity))
                .ToList();

            var resourceItems = _allEntries
                .Where(entry => entry.Kind == OrderEntryKind.Resource && entry.Quantity > 0)
                .Select(entry => new OrderResourceItem(entry.IngredientId!.Value, entry.Quantity))
                .ToList();

            if (items.Count == 0 && resourceItems.Count == 0)
            {
                await DisplayAlert("Erreur", "Ajoutez au moins un produit ou une ressource à fournir.", "OK");
                return;
            }

            try
            {
                int newId = _businessData.Orders.Any() ? _businessData.Orders.Max(o => o.Id) + 1 : 1;
                _businessData.Orders.Add(new Order(newId, CustomerName.Trim(), items, NegotiatedPrice, Deadline, resourceItems));

                string businessPath = Path.Combine(FileSystem.AppDataDirectory, BusinessAssetPath);
                await SevenwandsTools.SaveBusinessDataToJson(businessPath, _businessData);

                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur lors de la sauvegarde: {ex.Message}", "OK");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    public enum OrderEntryKind
    {
        Product,
        Resource
    }

    // Une ligne de la liste combinée produits/ressources proposée dans une commande, avec la
    // quantité demandée (0 = non incluse dans la commande). PotionId est renseigné pour un
    // produit, IngredientId pour une ressource (jamais les deux).
    public class OrderEntryViewModel : BindableObject
    {
        public OrderEntryKind Kind { get; set; }
        public int? PotionId { get; set; }
        public int? IngredientId { get; set; }
        public string Name { get; set; } = "";

        public string KindLabel => Kind == OrderEntryKind.Product ? "🧪 Produit" : "📦 Ressource";

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
