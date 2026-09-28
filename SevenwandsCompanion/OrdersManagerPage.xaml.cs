using System.Collections.ObjectModel;
using Microsoft.Maui.Graphics;
using SevenwandsConsoleTool;

namespace SevenwandsCompanion
{
    // Écran dédié à la gestion des commandes des autres joueurs, distinct des écrans de
    // gestion des ressources/produits. Une commande "terminée" retire les produits fournis
    // du stock de produits finis et ajoute la somme négociée à la cagnotte globale.
    public partial class OrdersManagerPage : ContentPage
    {
        private const string IngredientsAssetPath = "Ingredients.json";
        private const string PotionsAssetPath = "Potions.json";
        private const string BusinessAssetPath = "Business.json";

        private BusinessData _businessData = new();
        private Dictionary<int, Ingredient> _ingredientsById = new();
        private Dictionary<int, Potion> _potionsById = new();

        public ObservableCollection<OrderViewModel> Orders { get; set; } = new();

        public ObservableCollection<SortOption<OrderViewModel>> SortOptions { get; } = new();

        private SortOption<OrderViewModel>? _selectedSort;
        public SortOption<OrderViewModel>? SelectedSort
        {
            get => _selectedSort;
            set
            {
                if (_selectedSort != value)
                {
                    _selectedSort = value;
                    OnPropertyChanged();
                    ApplySort();
                }
            }
        }

        private OrderViewModel? _selectedOrder;
        public OrderViewModel? SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                if (_selectedOrder != value)
                {
                    _selectedOrder = value;
                    OnPropertyChanged();
                }
            }
        }

        private List<OrderViewModel> _allOrders = new();

        public OrdersManagerPage()
        {
            InitializeComponent();

            SortOptions.Add(new SortOption<OrderViewModel>("Date de livraison (croissant)", items => items.OrderBy(o => o.Deadline)));
            SortOptions.Add(new SortOption<OrderViewModel>("Date de livraison (décroissant)", items => items.OrderByDescending(o => o.Deadline)));
            SortOptions.Add(new SortOption<OrderViewModel>("Quantité de ressources (croissant)", items => items.OrderBy(o => o.ResourceQuantity)));
            SortOptions.Add(new SortOption<OrderViewModel>("Quantité de ressources (décroissant)", items => items.OrderByDescending(o => o.ResourceQuantity)));
            SortOptions.Add(new SortOption<OrderViewModel>("Nom du commanditaire (A→Z)", items => items.OrderBy(o => o.CustomerName)));
            _selectedSort = SortOptions[0];

            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                string ingredientsPath = Path.Combine(FileSystem.AppDataDirectory, IngredientsAssetPath);
                if (File.Exists(ingredientsPath))
                {
                    _ingredientsById = SevenwandsTools.DeserializeIngredients(await File.ReadAllTextAsync(ingredientsPath));
                }

                string potionsPath = Path.Combine(FileSystem.AppDataDirectory, PotionsAssetPath);
                if (File.Exists(potionsPath))
                {
                    var potions = SevenwandsTools.DeserializePotions(await File.ReadAllTextAsync(potionsPath));
                    _potionsById = potions.ToDictionary(p => p.Id);
                }

                string businessPath = Path.Combine(FileSystem.AppDataDirectory, BusinessAssetPath);
                _businessData = File.Exists(businessPath)
                    ? await SevenwandsTools.LoadBusinessDataFromJson(businessPath)
                    : new BusinessData();

                _allOrders = _businessData.Orders.Select(BuildOrderViewModel).ToList();
                ApplySort();

                SelectedOrder = null;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur lors du chargement des commandes: {ex.Message}", "OK");
            }
        }

        private void ApplySort()
        {
            IEnumerable<OrderViewModel> ordered = _allOrders;
            if (SelectedSort != null) ordered = SelectedSort.Apply(ordered);

            Orders.Clear();
            foreach (var order in ordered) Orders.Add(order);
        }

        private OrderViewModel BuildOrderViewModel(Order order)
        {
            var productStocks = order.Items.Select(i =>
            {
                string name = _potionsById.TryGetValue(i.PotionId, out var p) ? p.Name ?? "?" : "?";
                int owned = _businessData.PotionResalePrices.FirstOrDefault(r => r.PotionId == i.PotionId)?.QuantityOwned ?? 0;
                return new OrderProductStockViewModel { Name = name, Owned = owned, Needed = i.Quantity };
            }).ToList();

            // Ressources brutes demandées directement par la commande (en plus des produits
            // finis) : même affichage "possédé/besoin", même vérification avant de terminer.
            productStocks.AddRange(order.ResourceItems.Select(i =>
            {
                string name = _ingredientsById.TryGetValue(i.IngredientId, out var ing) ? ing.Name ?? "?" : "?";
                int owned = _businessData.IngredientStocks.FirstOrDefault(s => s.IngredientId == i.IngredientId)?.QuantityOwned ?? 0;
                return new OrderProductStockViewModel { Name = name, Owned = owned, Needed = i.Quantity };
            }));

            int resourceQuantity = order.Items.Sum(i =>
            {
                if (!_potionsById.TryGetValue(i.PotionId, out var potion)) return 0;
                return potion.Recipe
                    .Where(r => r.Quantity > 0
                        && _ingredientsById.TryGetValue(r.IngredientId, out var ing)
                        && ing.Type.IsStockable())
                    .Sum(r => r.Quantity * i.Quantity);
            });

            return new OrderViewModel
            {
                Id = order.Id,
                CustomerName = order.CustomerName,
                Items = order.Items,
                NegotiatedPrice = order.NegotiatedPrice,
                IsCompleted = order.IsCompleted,
                Deadline = order.Deadline,
                ProductStocks = productStocks,
                ResourceQuantity = resourceQuantity
            };
        }

        private async void OnNewOrderClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new OrderEditorPage());
        }

        // Le bouton "Commande terminée" est sur la ligne de la commande (Label + tap) plutôt
        // qu'en haut de la liste : un Button comme enfant d'un item template de CollectionView
        // ne se rend pas du tout sur WinUI (bug constaté ailleurs dans l'app).
        private async void OnCompleteOrderTapped(object? sender, TappedEventArgs e)
        {
            if (sender is not BindableObject bindable || bindable.BindingContext is not OrderViewModel orderVm || orderVm.IsCompleted)
                return;

            if (!orderVm.CanBeCompleted)
            {
                await DisplayAlert(
                    "Stock insuffisant",
                    "Le stock de produits finis ne couvre pas encore tout ce que demande cette commande. Fabriquez ou approvisionnez les produits manquants avant de la marquer comme terminée.",
                    "OK");
                return;
            }

            bool confirm = await DisplayAlert(
                "Confirmation",
                $"Marquer la commande de '{orderVm.CustomerName}' comme terminée ?\n\nLes produits fournis seront retirés du stock et {GallyonsFormat.Format0(orderVm.NegotiatedPrice)} Gallyons seront ajoutés à la cagnotte.",
                "Terminer",
                "Annuler");

            if (!confirm) return;

            try
            {
                var order = _businessData.Orders.FirstOrDefault(o => o.Id == orderVm.Id);
                if (order == null) return;

                foreach (var item in order.Items)
                {
                    var resale = _businessData.PotionResalePrices.FirstOrDefault(p => p.PotionId == item.PotionId);
                    if (resale != null)
                    {
                        resale.QuantityOwned = Math.Max(0, resale.QuantityOwned - item.Quantity);
                    }
                }

                foreach (var resourceItem in order.ResourceItems)
                {
                    var stock = _businessData.IngredientStocks.FirstOrDefault(s => s.IngredientId == resourceItem.IngredientId);
                    if (stock != null)
                    {
                        stock.QuantityOwned = Math.Max(0, stock.QuantityOwned - resourceItem.Quantity);
                    }
                }

                order.IsCompleted = true;
                _businessData.Treasury += order.NegotiatedPrice;

                string businessPath = Path.Combine(FileSystem.AppDataDirectory, BusinessAssetPath);
                await SevenwandsTools.SaveBusinessDataToJson(businessPath, _businessData);

                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur: {ex.Message}", "OK");
            }
        }

        private async void OnDeleteOrderClicked(object sender, EventArgs e)
        {
            if (SelectedOrder == null) return;

            bool confirm = await DisplayAlert(
                "Confirmation",
                $"Supprimer la commande de '{SelectedOrder.CustomerName}' ?",
                "Supprimer",
                "Annuler");

            if (!confirm) return;

            try
            {
                _businessData.Orders.RemoveAll(o => o.Id == SelectedOrder.Id);

                string businessPath = Path.Combine(FileSystem.AppDataDirectory, BusinessAssetPath);
                await SevenwandsTools.SaveBusinessDataToJson(businessPath, _businessData);

                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur: {ex.Message}", "OK");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    // ViewModel d'affichage pour une commande (liste + tri), avec le détail par produit
    // (possédé/besoin) et la quantité totale de ressources physiques que sa fabrication
    // consommera.
    public class OrderViewModel
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = "";
        public List<OrderItem> Items { get; set; } = new();
        public float NegotiatedPrice { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime Deadline { get; set; }
        public List<OrderProductStockViewModel> ProductStocks { get; set; } = new();
        public int ResourceQuantity { get; set; }

        public string StatusDisplay => IsCompleted ? "✅ Terminée" : "⏳ En cours";
        public Color StatusColor => IsCompleted ? Colors.LightGreen : Colors.Orange;
        public string DeadlineDisplay => $"Livraison prévue: {Deadline:dd/MM/yyyy} | Ressources: {ResourceQuantity}";
        public string NegotiatedPriceDisplay => $"{GallyonsFormat.Format0(NegotiatedPrice)} Gallyons";
        public bool IsPending => !IsCompleted;

        // Impossible de terminer une commande tant que le stock de produits finis ne couvre pas
        // tout ce qui est demandé (chaque ligne de ProductStocks doit avoir Owned >= Needed).
        public bool CanBeCompleted => ProductStocks.All(p => p.Owned >= p.Needed);
        public string ActionBarText => CanBeCompleted ? "✅ MARQUER COMME TERMINÉE" : "🔒 STOCK INSUFFISANT";
        public Color ActionBarColor => CanBeCompleted ? Color.FromArgb("#2E7D32") : Color.FromArgb("#555555");
    }

    // Une ligne "produit fini" d'une commande : quantité déjà possédée en stock face à la
    // quantité nécessaire pour honorer la commande (format "possédé / besoin"), colorée en vert
    // si le stock suffit déjà, en rouge sinon.
    public class OrderProductStockViewModel
    {
        public string Name { get; set; } = "";
        public int Owned { get; set; }
        public int Needed { get; set; }

        public string Display => $"{Name}: {Owned}/{Needed}";
        public Color StatusColor => Owned >= Needed ? Colors.LightGreen : Colors.IndianRed;
    }
}
