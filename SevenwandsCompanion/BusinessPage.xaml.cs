using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.Maui.Graphics;
using SevenwandsConsoleTool;

namespace SevenwandsCompanion
{
    public partial class BusinessPage : ContentPage
    {
        private const string IngredientsAssetPath = "Ingredients.json";
        private const string PotionsAssetPath = "Potions.json";
        private const string BusinessAssetPath = "Business.json";

        // ID réservé représentant "pas de catégorie assignée" (jamais utilisé pour une vraie catégorie)
        private const int UncategorizedCategoryId = -1;

        private Dictionary<int, Ingredient> _ingredientsDict = new();
        private List<Potion> _potions = new();

        private readonly BusinessCategory _uncategorizedOption = new(UncategorizedCategoryId, "— Non catégorisé —");

        // Cagnotte globale alimentée par les commandes terminées, et commandes elles-mêmes.
        // BusinessPage ne les édite pas directement (c'est le rôle d'OrdersManagerPage/
        // OrderEditorPage), mais doit les reporter telles quelles dans BuildBusinessData() —
        // sinon son autosave (déclenché par le stock/les produits) écraserait Orders/Treasury
        // avec des valeurs vides à chaque modification de ressource ou de produit.
        private List<Order> _orders = new();
        private float _treasury;
        public string TreasuryDisplay => $"🪙 Cagnotte: {GallyonsFormat.Format0(_treasury)} Gallyons";

        private int _pendingOrdersCount;
        public string OrdersButtonText => $"📋 Commandes ({_pendingOrdersCount})";

        // Catégories réellement persistées (créées/supprimées par l'utilisateur),
        // partagées entre les ressources et les produits finis.
        public ObservableCollection<BusinessCategory> Categories { get; set; } = new();

        // Options du picker "Filtrer par catégorie" ("Toutes" + Categories), côté produits
        public ObservableCollection<CategoryFilterOption> FilterOptions { get; set; } = new();

        // Options des pickers de catégorie par ligne (Non catégorisé + Categories)
        public ObservableCollection<BusinessCategory> CategoryPickerOptions { get; set; } = new();

        private CategoryFilterOption? _selectedFilter;
        public CategoryFilterOption? SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                if (_selectedFilter != value)
                {
                    _selectedFilter = value;
                    OnPropertyChanged();
                    ApplyFilters();
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
                    ApplyFilters();
                }
            }
        }

        public ObservableCollection<SortOption<StockItemViewModel>> StockSortOptions { get; } = new();

        private SortOption<StockItemViewModel>? _selectedStockSort;
        public SortOption<StockItemViewModel>? SelectedStockSort
        {
            get => _selectedStockSort;
            set
            {
                if (_selectedStockSort != value)
                {
                    _selectedStockSort = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        public ObservableCollection<SortOption<PotionProfitViewModel>> ProductSortOptions { get; } = new();

        private SortOption<PotionProfitViewModel>? _selectedProductSort;
        public SortOption<PotionProfitViewModel>? SelectedProductSort
        {
            get => _selectedProductSort;
            set
            {
                if (_selectedProductSort != value)
                {
                    _selectedProductSort = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        public ObservableCollection<StockItemViewModel> StockItems { get; set; } = new();
        public ObservableCollection<PotionProfitViewModel> PotionProfits { get; set; } = new();

        // Ressources/produits regroupés par catégorie (accordéon), "Non catégorisé" en dernier.
        // L'état plié/déplié est conservé entre deux reconstructions (par nom de catégorie).
        public ObservableCollection<StockCategoryGroupViewModel> StockGroups { get; set; } = new();
        public ObservableCollection<ProductCategoryGroupViewModel> ProductGroups { get; set; } = new();

        private const string UncategorizedGroupName = "— Non catégorisé —";
        private readonly Dictionary<string, bool> _stockGroupExpanded = new();
        private readonly Dictionary<string, bool> _productGroupExpanded = new();

        public BusinessPage()
        {
            InitializeComponent();

            StockSortOptions.Add(new SortOption<StockItemViewModel>("Nom (A→Z)", items => items.OrderBy(i => i.Name)));
            StockSortOptions.Add(new SortOption<StockItemViewModel>("Quantité possédée (croissant)", items => items.OrderBy(i => i.QuantityOwned)));
            StockSortOptions.Add(new SortOption<StockItemViewModel>("Quantité possédée (décroissant)", items => items.OrderByDescending(i => i.QuantityOwned)));
            StockSortOptions.Add(new SortOption<StockItemViewModel>("Prix unitaire (croissant)", items => items.OrderBy(i => i.UnitPrice ?? 0)));
            StockSortOptions.Add(new SortOption<StockItemViewModel>("Prix unitaire (décroissant)", items => items.OrderByDescending(i => i.UnitPrice ?? 0)));
            StockSortOptions.Add(new SortOption<StockItemViewModel>("Catégorie", items => items.OrderBy(i => i.CategoriesDisplay)));
            _selectedStockSort = StockSortOptions[0];

            ProductSortOptions.Add(new SortOption<PotionProfitViewModel>("Nom (A→Z)", items => items.OrderBy(i => i.Name)));
            ProductSortOptions.Add(new SortOption<PotionProfitViewModel>("Quantité possédée (croissant)", items => items.OrderBy(i => i.QuantityOwned)));
            ProductSortOptions.Add(new SortOption<PotionProfitViewModel>("Quantité possédée (décroissant)", items => items.OrderByDescending(i => i.QuantityOwned)));
            ProductSortOptions.Add(new SortOption<PotionProfitViewModel>("Marge / unité (croissant)", items => items.OrderBy(i => i.MarginPerUnit)));
            ProductSortOptions.Add(new SortOption<PotionProfitViewModel>("Marge / unité (décroissant)", items => items.OrderByDescending(i => i.MarginPerUnit)));
            ProductSortOptions.Add(new SortOption<PotionProfitViewModel>("Coût (croissant)", items => items.OrderBy(i => i.UnitCost)));
            ProductSortOptions.Add(new SortOption<PotionProfitViewModel>("Coût (décroissant)", items => items.OrderByDescending(i => i.UnitCost)));
            ProductSortOptions.Add(new SortOption<PotionProfitViewModel>("Fabricables (croissant)", items => items.OrderBy(i => i.MaxCraftable)));
            ProductSortOptions.Add(new SortOption<PotionProfitViewModel>("Fabricables (décroissant)", items => items.OrderByDescending(i => i.MaxCraftable)));
            ProductSortOptions.Add(new SortOption<PotionProfitViewModel>("Catégorie", items => items.OrderBy(i => i.SelectedCategory?.Name ?? "")));
            _selectedProductSort = ProductSortOptions.First(o => o.Label == "Quantité possédée (décroissant)");

            BindingContext = this;
        }

        // Seul point de chargement : OnAppearing se déclenche déjà à la toute première
        // apparition de la page, donc pas besoin d'un second appel dans le constructeur.
        // L'avoir en plus provoquait deux InitializeDataAsync() concurrents qui vidaient/
        // reconstruisaient les mêmes ObservableCollection en parallèle, ce qui pouvait laisser
        // les CollectionView (StockGroups/ProductGroups) visuellement vides sur WinUI malgré des
        // données correctement chargées en mémoire.
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                await EnsureDataFilesExist();

                string ingredientsPath = Path.Combine(FileSystem.AppDataDirectory, IngredientsAssetPath);
                string ingredientsJson = await File.ReadAllTextAsync(ingredientsPath);
                _ingredientsDict = SevenwandsTools.DeserializeIngredients(ingredientsJson);

                string potionsPath = Path.Combine(FileSystem.AppDataDirectory, PotionsAssetPath);
                string potionsJson = await File.ReadAllTextAsync(potionsPath);
                _potions = SevenwandsTools.DeserializePotions(potionsJson);

                var businessData = await SevenwandsTools.LoadBusinessDataFromJson(
                    Path.Combine(FileSystem.AppDataDirectory, BusinessAssetPath));

                Categories.Clear();
                foreach (var category in businessData.Categories.OrderBy(c => c.Name))
                {
                    Categories.Add(category);
                }
                RebuildCategoryOptions();
                _selectedFilter = FilterOptions.FirstOrDefault();
                OnPropertyChanged(nameof(SelectedFilter));

                BuildStockItems(businessData);
                RecomputePotionProfits(businessData);
                ApplyFilters();

                _orders = businessData.Orders;
                _treasury = businessData.Treasury;
                OnPropertyChanged(nameof(TreasuryDisplay));
                _pendingOrdersCount = businessData.Orders.Count(o => !o.IsCompleted);
                OnPropertyChanged(nameof(OrdersButtonText));

                System.Diagnostics.Debug.WriteLine($"📦 BusinessPage: {StockItems.Count} ressources, {PotionProfits.Count} produits, {Categories.Count} catégories chargées");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur lors du chargement des données commerciales: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"Error loading business data: {ex.Message}");
            }
        }

        /// <summary>
        /// S'assure que les fichiers JSON existent dans AppDataDirectory (copie initiale si nécessaire).
        /// </summary>
        private async Task EnsureDataFilesExist()
        {
            string ingredientsPath = Path.Combine(FileSystem.AppDataDirectory, IngredientsAssetPath);
            string potionsPath = Path.Combine(FileSystem.AppDataDirectory, PotionsAssetPath);
            string businessPath = Path.Combine(FileSystem.AppDataDirectory, BusinessAssetPath);

            if (!File.Exists(ingredientsPath))
            {
                await SevenwandsTools.SaveIngredientsToJson(ingredientsPath, new Dictionary<int, Ingredient>());
            }

            if (!File.Exists(potionsPath))
            {
                await SevenwandsTools.SavePotionsToJson(potionsPath, new List<Potion>());
            }

            if (!File.Exists(businessPath))
            {
                // Catégories de départ suggérées ; l'utilisateur peut les renommer/supprimer librement.
                var defaultBusinessData = new BusinessData
                {
                    Categories = new List<BusinessCategory>
                    {
                        new(1, "Potion"),
                        new(2, "Auberge"),
                        new(3, "Forge"),
                    }
                };
                await SevenwandsTools.SaveBusinessDataToJson(businessPath, defaultBusinessData);
            }
        }

        /// <summary>
        /// Reconstruit les listes d'options de catégories (filtre produits + pickers de ligne)
        /// à partir de la liste Categories actuelle.
        /// </summary>
        private void RebuildCategoryOptions()
        {
            FilterOptions.Clear();
            FilterOptions.Add(new CategoryFilterOption { CategoryId = null, Name = "Toutes", IsSystem = true });
            foreach (var category in Categories.OrderBy(c => c.Name))
            {
                FilterOptions.Add(new CategoryFilterOption { CategoryId = category.Id, Name = category.Name, IsSystem = false });
            }

            CategoryPickerOptions.Clear();
            CategoryPickerOptions.Add(_uncategorizedOption);
            foreach (var category in Categories.OrderBy(c => c.Name))
            {
                CategoryPickerOptions.Add(category);
            }
        }

        private BusinessCategory ResolveCategory(int? categoryId)
        {
            if (categoryId == null) return _uncategorizedOption;
            return Categories.FirstOrDefault(c => c.Id == categoryId) ?? _uncategorizedOption;
        }

        private static int? PersistedCategoryId(BusinessCategory? category)
            => (category == null || category.Id == UncategorizedCategoryId) ? null : category.Id;

        private void BuildStockItems(BusinessData businessData)
        {
            foreach (var item in StockItems)
            {
                item.QuantityChanged -= OnStockItemChanged;
            }
            StockItems.Clear();

            var stockByIngredientId = businessData.IngredientStocks.ToDictionary(s => s.IngredientId);

            foreach (var ingredient in _ingredientsDict.Values
                .Where(i => i.Type.IsStockable())
                .OrderBy(i => i.Name))
            {
                stockByIngredientId.TryGetValue(ingredient.Id, out var existingStock);

                var assignedCategoryIds = EffectiveCategoryIds(existingStock);
                var vm = new StockItemViewModel
                {
                    IngredientId = ingredient.Id,
                    Name = ingredient.Name ?? "",
                    UnitPrice = ingredient.Price,
                    QuantityOwned = existingStock?.QuantityOwned ?? 0,
                    AssignedCategories = assignedCategoryIds
                        .Select(id => Categories.FirstOrDefault(c => c.Id == id))
                        .Where(c => c != null)
                        .Select(c => c!)
                        .ToList()
                };
                vm.QuantityChanged += OnStockItemChanged;
                StockItems.Add(vm);
            }
        }

        // Migration douce : avant le multi-catégorie, une ressource ne stockait qu'un seul
        // category_id. On le reprend comme unique catégorie initiale si category_ids est vide.
        private static IEnumerable<int> EffectiveCategoryIds(IngredientStock? stock)
        {
            if (stock == null) return Enumerable.Empty<int>();
            if (stock.CategoryIds.Any()) return stock.CategoryIds;
            return stock.LegacyCategoryId.HasValue ? new[] { stock.LegacyCategoryId.Value } : Enumerable.Empty<int>();
        }

        // Un changement sur une ressource de stock affecte le nombre de produits fabricables :
        // on recalcule ces compteurs immédiatement (peu coûteux, met juste à jour des propriétés
        // déjà liées), puis on planifie le retri/regroupement + la sauvegarde.
        private void OnStockItemChanged(object? sender, EventArgs e)
        {
            RefreshCraftableCounts();
            ScheduleApplyFiltersAndSave();
        }

        // Un changement sur un produit (prix de revente, quantité possédée, catégorie) n'affecte
        // pas le stock de ressources : pas besoin de recalculer les compteurs fabricables.
        private void OnProductItemChanged(object? sender, EventArgs e)
        {
            ScheduleApplyFiltersAndSave();
        }

        // Reconstruire les accordéons (StockGroups/ProductGroups) reconstruit entièrement les
        // CollectionView imbriquées : coûteux sur WinUI, perceptible comme un délai entre le clic
        // (+/-, saisie d'un prix) et son retour visuel si on le fait à chaque changement. Et
        // lancer une sauvegarde indépendante à chaque frappe/clic peut faire chevaucher plusieurs
        // écritures concurrentes sur Business.json (risque de fichier corrompu). On regroupe donc
        // retri/regroupement et sauvegarde dans un seul enchaînement différé : les clics rapprochés
        // annulent le précédent, une seule écriture séquentielle a lieu une fois le calme revenu.
        // Les propriétés affichées (quantité, marge, fabricables...) restent mises à jour
        // instantanément via le binding, indépendamment de ce délai.
        private CancellationTokenSource? _pendingUpdateCts;
        private const int PendingUpdateDebounceMs = 300;

        private void ScheduleApplyFiltersAndSave()
        {
            _pendingUpdateCts?.Cancel();
            var cts = new CancellationTokenSource();
            _pendingUpdateCts = cts;
            _ = RunDeferredUpdateAsync(cts.Token);
        }

        private async Task RunDeferredUpdateAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(PendingUpdateDebounceMs, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested) return;

            ApplyFilters();

            try
            {
                await PersistBusinessDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Autosave error: {ex.Message}");
            }
        }

        // Liste des ressources physiques (stockables) qu'une recette consomme, avec la quantité
        // déjà possédée en stock pour chacune — sert à afficher le détail par ressource (à la
        // place d'un simple nombre "Fabricables") et à calculer ce nombre pour le tri.
        private static List<ResourceRequirement> BuildResourceRequirements(Potion potion, Dictionary<int, Ingredient> ingredientsDict, Dictionary<int, int> stockByIngredientId)
        {
            return potion.Recipe
                .Where(r => r.Quantity > 0
                    && ingredientsDict.TryGetValue(r.IngredientId, out var ing)
                    && ing.Type.IsStockable())
                .Select(r => new ResourceRequirement
                {
                    Name = ingredientsDict[r.IngredientId].Name ?? "?",
                    Owned = stockByIngredientId.TryGetValue(r.IngredientId, out var owned) ? owned : 0,
                    NeededPerUnit = r.Quantity
                })
                .ToList();
        }

        private static int ComputeMaxCraftable(List<ResourceRequirement> requirements)
        {
            // Aucune ressource physique requise : fabrication illimitée. Sinon, limité par la
            // ressource la plus rare (stock possédé / quantité nécessaire par unité).
            return requirements.Count == 0
                ? -1
                : requirements.Select(r => r.Owned / r.NeededPerUnit).Min();
        }

        // Recalcule le détail par ressource et le nombre de produits fabricables (dépendent du
        // stock de ressources) sans reconstruire la collection PotionProfits : conserve les
        // instances existantes (et donc le focus/l'état d'édition en cours).
        private void RefreshCraftableCounts()
        {
            var stockByIngredientId = StockItems.ToDictionary(s => s.IngredientId, s => s.QuantityOwned);

            foreach (var profit in PotionProfits)
            {
                var potion = _potions.FirstOrDefault(p => p.Id == profit.PotionId);
                if (potion == null) continue;

                var requirements = BuildResourceRequirements(potion, _ingredientsDict, stockByIngredientId);
                profit.MaxCraftable = ComputeMaxCraftable(requirements);
                profit.ResourceRequirements = requirements;
            }
        }

        /// <summary>
        /// Recalcule la liste des produits (potions, et autres types de produits à terme) avec
        /// coût, quantité fabricable, marge et catégorie. Si businessData est fourni, les prix de
        /// revente et catégories sont chargés depuis ce fichier (premier chargement). Sinon, les
        /// valeurs déjà saisies dans PotionProfits sont conservées.
        /// </summary>
        private void RecomputePotionProfits(BusinessData? businessData)
        {
            Dictionary<int, PotionResalePrice> resaleDataByPotionId;
            if (businessData != null)
            {
                resaleDataByPotionId = businessData.PotionResalePrices.ToDictionary(p => p.PotionId);
            }
            else
            {
                resaleDataByPotionId = PotionProfits.ToDictionary(
                    p => p.PotionId,
                    p => new PotionResalePrice(p.PotionId, p.ResalePrice, PersistedCategoryId(p.SelectedCategory), p.QuantityOwned));
            }

            foreach (var item in PotionProfits)
            {
                item.CategoryChanged -= OnProductItemChanged;
                item.QuantityChanged -= OnProductItemChanged;
                item.ResalePriceChanged -= OnProductItemChanged;
            }

            var stockByIngredientId = StockItems.ToDictionary(s => s.IngredientId, s => s.QuantityOwned);

            // Catégorie "Potion" par défaut si elle existe encore (aide au premier remplissage,
            // puisqu'à ce jour tous les produits gérés sont des potions)
            int? defaultProductCategoryId = Categories.FirstOrDefault(c =>
                string.Equals(c.Name, "Potion", StringComparison.OrdinalIgnoreCase))?.Id;

            var newProfits = new List<PotionProfitViewModel>();

            foreach (var potion in _potions)
            {
                float unitCost = SevenwandsTools.CalculatePotionUnitCost(potion, _ingredientsDict);

                var requirements = BuildResourceRequirements(potion, _ingredientsDict, stockByIngredientId);
                int maxCraftable = ComputeMaxCraftable(requirements);

                bool hasExistingEntry = resaleDataByPotionId.TryGetValue(potion.Id, out var existingResale);

                var vm = new PotionProfitViewModel
                {
                    PotionId = potion.Id,
                    Name = potion.Name ?? "",
                    UnitCost = unitCost,
                    MaxCraftable = maxCraftable,
                    ResourceRequirements = requirements,
                    AvailableCategories = CategoryPickerOptions,
                    ResalePrice = hasExistingEntry ? existingResale!.ResalePrice : 0,
                    SelectedCategory = ResolveCategory(hasExistingEntry ? existingResale!.CategoryId : defaultProductCategoryId),
                    QuantityOwned = hasExistingEntry ? existingResale!.QuantityOwned : 0
                };
                vm.CategoryChanged += OnProductItemChanged;
                vm.QuantityChanged += OnProductItemChanged;
                vm.ResalePriceChanged += OnProductItemChanged;

                newProfits.Add(vm);
            }

            // La meilleure marge en tête de liste (indépendant du tri d'affichage choisi)
            var byMargin = newProfits.OrderByDescending(p => p.MarginPerUnit).ToList();
            if (byMargin.Count > 0 && byMargin[0].MarginPerUnit > 0)
            {
                byMargin[0].IsBestMargin = true;
            }

            PotionProfits.Clear();
            foreach (var p in newProfits) PotionProfits.Add(p);
        }

        /// <summary>
        /// Applique le filtre de catégorie (produits), la recherche texte (ressources + produits)
        /// et le tri choisi par l'utilisateur, puis reconstruit les accordéons groupés par
        /// catégorie ("Non catégorisé" en dernier), en conservant l'état plié/déplié.
        /// </summary>
        private void ApplyFilters()
        {
            int? filterId = SelectedFilter?.CategoryId;
            string search = SearchText?.Trim() ?? "";

            IEnumerable<StockItemViewModel> stock = StockItems
                .Where(i => string.IsNullOrEmpty(search) || i.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
            if (SelectedStockSort != null) stock = SelectedStockSort.Apply(stock);
            var stockList = stock.ToList();

            StockGroups.Clear();
            foreach (var category in Categories.OrderBy(c => c.Name))
            {
                var itemsInCategory = stockList.Where(i => i.SelectedCategoryIds.Contains(category.Id)).ToList();
                if (itemsInCategory.Any())
                {
                    StockGroups.Add(new StockCategoryGroupViewModel(
                        category.Name, itemsInCategory, GetExpanded(_stockGroupExpanded, category.Name)));
                }
            }
            var uncategorizedStock = stockList.Where(i => !i.SelectedCategoryIds.Any()).ToList();
            if (uncategorizedStock.Any())
            {
                StockGroups.Add(new StockCategoryGroupViewModel(
                    UncategorizedGroupName, uncategorizedStock, GetExpanded(_stockGroupExpanded, UncategorizedGroupName)));
            }

            IEnumerable<PotionProfitViewModel> products = PotionProfits
                .Where(p => filterId == null || p.SelectedCategory?.Id == filterId)
                .Where(p => string.IsNullOrEmpty(search) || p.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
            if (SelectedProductSort != null) products = SelectedProductSort.Apply(products);
            var productsList = products.ToList();

            ProductGroups.Clear();
            foreach (var category in Categories.OrderBy(c => c.Name))
            {
                var itemsInCategory = productsList.Where(p => p.SelectedCategory?.Id == category.Id).ToList();
                if (itemsInCategory.Any())
                {
                    ProductGroups.Add(new ProductCategoryGroupViewModel(
                        category.Name, itemsInCategory, GetExpanded(_productGroupExpanded, category.Name)));
                }
            }
            var uncategorizedProducts = productsList
                .Where(p => p.SelectedCategory == null || p.SelectedCategory.Id == UncategorizedCategoryId)
                .ToList();
            if (uncategorizedProducts.Any())
            {
                ProductGroups.Add(new ProductCategoryGroupViewModel(
                    UncategorizedGroupName, uncategorizedProducts, GetExpanded(_productGroupExpanded, UncategorizedGroupName)));
            }
        }

        private static bool GetExpanded(Dictionary<string, bool> state, string groupName)
            => !state.TryGetValue(groupName, out var expanded) || expanded; // déplié par défaut

        private void OnIncrementStockClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is StockItemViewModel item)
            {
                item.QuantityOwned++;
            }
        }

        private void OnDecrementStockClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is StockItemViewModel item && item.QuantityOwned > 0)
            {
                item.QuantityOwned--;
            }
        }

        private void OnIncrementProductClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is PotionProfitViewModel item)
            {
                item.QuantityOwned++;
            }
        }

        private void OnDecrementProductClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is PotionProfitViewModel item && item.QuantityOwned > 0)
            {
                item.QuantityOwned--;
            }
        }

        // Le toggle des en-têtes de groupe utilise un TapGestureRecognizer sur un Label/Grid
        // plutôt qu'un Button : un Button comme racine (ou enfant direct) d'un item template
        // de CollectionView ne se rend pas du tout sur WinUI (constaté empiriquement — un Label
        // au même endroit s'affichait normalement, un Button le remplaçant faisait disparaître
        // toute la liste). Le TapGestureRecognizer hérite du BindingContext de son parent, donc
        // pas besoin de CommandParameter : on lit directement (sender as Element).BindingContext.
        private void OnToggleStockGroupClicked(object? sender, TappedEventArgs e)
        {
            if (sender is BindableObject bindable && bindable.BindingContext is StockCategoryGroupViewModel group)
            {
                group.IsExpanded = !group.IsExpanded;
                _stockGroupExpanded[group.CategoryName] = group.IsExpanded;
            }
        }

        private void OnToggleProductGroupClicked(object? sender, TappedEventArgs e)
        {
            if (sender is BindableObject bindable && bindable.BindingContext is ProductCategoryGroupViewModel group)
            {
                group.IsExpanded = !group.IsExpanded;
                _productGroupExpanded[group.CategoryName] = group.IsExpanded;
            }
        }

        private async void OnAddCategoryClicked(object sender, EventArgs e)
        {
            string? name = await DisplayPromptAsync(
                "Nouvelle catégorie",
                "Nom de la catégorie (ex: Potion, Auberge, Forge...)");

            if (string.IsNullOrWhiteSpace(name)) return;

            int newId = Categories.Any() ? Categories.Max(c => c.Id) + 1 : 1;
            Categories.Add(new BusinessCategory(newId, name.Trim()));
            RebuildCategoryOptions();

            // Action ponctuelle (pas un clic répété) : annule toute sauvegarde différée en
            // attente pour éviter deux écritures concurrentes, puis sauvegarde immédiatement.
            _pendingUpdateCts?.Cancel();
            AutosaveBusinessData();
        }

        private async void OnDeleteCategoryClicked(object sender, EventArgs e)
        {
            if (!Categories.Any())
            {
                await DisplayAlert("Information", "Aucune catégorie à supprimer.", "OK");
                return;
            }

            string[] names = Categories.Select(c => c.Name).ToArray();
            string choice = await DisplayActionSheet("Supprimer quelle catégorie ?", "Annuler", null, names);

            if (string.IsNullOrEmpty(choice) || choice == "Annuler") return;

            var category = Categories.FirstOrDefault(c => c.Name == choice);
            if (category == null) return;

            bool confirm = await DisplayAlert(
                "Confirmation",
                $"Supprimer la catégorie '{category.Name}' ? Les éléments assignés deviendront non catégorisés.",
                "Supprimer",
                "Annuler");

            if (!confirm) return;

            Categories.Remove(category);

            foreach (var item in PotionProfits.Where(i => i.SelectedCategory?.Id == category.Id))
            {
                item.SelectedCategory = _uncategorizedOption;
            }

            foreach (var item in StockItems)
            {
                item.RemoveCategory(category.Id);
            }

            int? previousFilterId = SelectedFilter?.CategoryId;
            RebuildCategoryOptions();
            SelectedFilter = FilterOptions.FirstOrDefault(f => f.CategoryId == previousFilterId) ?? FilterOptions.First();

            // Annule toute sauvegarde différée en attente (déclenchée par les changements de
            // catégorie ci-dessus) pour éviter deux écritures concurrentes, puis sauvegarde
            // immédiatement avec l'état à jour.
            _pendingUpdateCts?.Cancel();
            AutosaveBusinessData();
        }

        private async void OnManageResourcesClicked(object sender, EventArgs e)
        {
            // Écran dédié à l'ajout/modification/suppression, distinct de l'éditeur
            // d'ingrédients de recette de potion.
            await Shell.Current.GoToAsync("ResourcesManager");
        }

        private async void OnManageProductsClicked(object sender, EventArgs e)
        {
            // Écran dédié à l'ajout/modification/suppression, distinct du Créateur de Potions.
            await Shell.Current.GoToAsync("ProductsManager");
        }

        private async void OnManageOrdersClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("OrdersManager");
        }

        private async void OnResetTreasuryClicked(object sender, EventArgs e)
        {
            int completedCount = _orders.Count(o => o.IsCompleted);
            string extra = completedCount > 0
                ? $"\n\n{completedCount} commande(s) terminée(s) seront aussi supprimée(s) (les commandes en cours restent)."
                : "";

            bool confirm = await DisplayAlert(
                "Confirmation",
                $"Remettre la cagnotte à 0 ? Elle contient actuellement {GallyonsFormat.Format0(_treasury)} Gallyons.{extra}",
                "Réinitialiser",
                "Annuler");

            if (!confirm) return;

            _treasury = 0;
            OnPropertyChanged(nameof(TreasuryDisplay));

            // Le reset de la cagnotte "clôture" aussi l'historique des commandes livrées :
            // seules les commandes déjà terminées sont supprimées, les commandes en cours
            // restent intactes.
            _orders.RemoveAll(o => o.IsCompleted);
            _pendingOrdersCount = _orders.Count(o => !o.IsCompleted);
            OnPropertyChanged(nameof(OrdersButtonText));

            _pendingUpdateCts?.Cancel();
            AutosaveBusinessData();
        }

        private BusinessData BuildBusinessData()
        {
            return new BusinessData
            {
                Categories = Categories.ToList(),
                IngredientStocks = StockItems
                    .Select(s => new IngredientStock(s.IngredientId, s.QuantityOwned, s.SelectedCategoryIds))
                    .ToList(),
                PotionResalePrices = PotionProfits
                    .Select(p => new PotionResalePrice(p.PotionId, p.ResalePrice, PersistedCategoryId(p.SelectedCategory), p.QuantityOwned))
                    .ToList(),
                Orders = _orders,
                Treasury = _treasury
            };
        }

        private async Task PersistBusinessDataAsync()
        {
            string businessPath = Path.Combine(FileSystem.AppDataDirectory, BusinessAssetPath);
            await SevenwandsTools.SaveBusinessDataToJson(businessPath, BuildBusinessData());
            System.Diagnostics.Debug.WriteLine($"💾 Business auto-sauvegardé: {businessPath}");
        }

        // Sauvegarde silencieuse déclenchée à chaque modification (stock, prix de revente,
        // quantité possédée, catégories...) afin de ne plus dépendre du bouton "Enregistrer"
        // et d'éviter de perdre la saisie si la fenêtre se ferme.
        private async void AutosaveBusinessData()
        {
            try
            {
                await PersistBusinessDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Autosave error: {ex.Message}");
            }
        }

    }

    // Option affichée dans le picker de filtre en haut de la colonne produits ("Toutes" + catégories)
    public class CategoryFilterOption
    {
        public int? CategoryId { get; set; }
        public string Name { get; set; } = "";
        public bool IsSystem { get; set; }
    }

    // Option de tri générique réutilisable pour n'importe quelle liste de la page
    public class SortOption<T>
    {
        public string Label { get; }
        public Func<IEnumerable<T>, IEnumerable<T>> Apply { get; }

        public SortOption(string label, Func<IEnumerable<T>, IEnumerable<T>> apply)
        {
            Label = label;
            Apply = apply;
        }

        public override string ToString() => Label;
    }

    // Section pliable/dépliable regroupant les ressources d'une catégorie (accordéon).
    // Remarque : deux classes non génériques distinctes (plutôt qu'une seule
    // CategoryGroupViewModel<T>) car .NET MAUI CollectionView ne parvient pas à rendre les
    // éléments d'une ObservableCollection dont le type est une classe générique (constaté :
    // aucun item ne s'affiche, même un Label statique, alors que les données sont bien
    // présentes) — un ObservableCollection<string> de test rendait correctement dans le même
    // emplacement, isolant le problème au type générique lui-même.
    public class StockCategoryGroupViewModel : BindableObject
    {
        public string CategoryName { get; }
        public ObservableCollection<StockItemViewModel> Items { get; } = new();

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HeaderText));
                }
            }
        }

        public string HeaderText => $"{(IsExpanded ? "▼" : "▶")} {CategoryName} ({Items.Count})";

        public StockCategoryGroupViewModel(string categoryName, IEnumerable<StockItemViewModel> items, bool isExpanded)
        {
            CategoryName = categoryName;
            foreach (var item in items) Items.Add(item);
            _isExpanded = isExpanded;
        }
    }

    public class ProductCategoryGroupViewModel : BindableObject
    {
        public string CategoryName { get; }
        public ObservableCollection<PotionProfitViewModel> Items { get; } = new();

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HeaderText));
                }
            }
        }

        public string HeaderText => $"{(IsExpanded ? "▼" : "▶")} {CategoryName} ({Items.Count})";

        public ProductCategoryGroupViewModel(string categoryName, IEnumerable<PotionProfitViewModel> items, bool isExpanded)
        {
            CategoryName = categoryName;
            foreach (var item in items) Items.Add(item);
            _isExpanded = isExpanded;
        }
    }

    // Une catégorie assignable à cocher (utilisée par les écrans d'édition dédiés, ex:
    // ResourceEditorPage, pour choisir les catégories d'une ressource).
    public class CategoryToggleOption : BindableObject
    {
        public BusinessCategory Category { get; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public CategoryToggleOption(BusinessCategory category, bool isSelected)
        {
            Category = category;
            _isSelected = isSelected;
        }
    }

    // ViewModel pour une ligne de stock d'ingrédient physique. L'assignation des catégories
    // se fait dans l'écran d'ajout/modification de la ressource (ResourceEditorPage) ; ici
    // on ne fait qu'afficher le résultat et suivre la quantité possédée.
    public class StockItemViewModel : BindableObject
    {
        public int IngredientId { get; set; }
        public string Name { get; set; } = "";
        public float? UnitPrice { get; set; }

        public string UnitPriceDisplay => UnitPrice.HasValue
            ? $"{GallyonsFormat.Format0(UnitPrice.Value)} Gallyons / unité"
            : "Prix non défini";

        private List<BusinessCategory> _assignedCategories = new();
        public List<BusinessCategory> AssignedCategories
        {
            get => _assignedCategories;
            set
            {
                _assignedCategories = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CategoriesDisplay));
                OnPropertyChanged(nameof(SelectedCategoryIds));
            }
        }

        public List<int> SelectedCategoryIds => AssignedCategories.Select(c => c.Id).ToList();

        public string CategoriesDisplay => AssignedCategories.Any()
            ? string.Join(", ", AssignedCategories.Select(c => c.Name))
            : "— Non catégorisé —";

        public void RemoveCategory(int categoryId)
        {
            if (AssignedCategories.RemoveAll(c => c.Id == categoryId) > 0)
            {
                OnPropertyChanged(nameof(AssignedCategories));
                OnPropertyChanged(nameof(CategoriesDisplay));
                OnPropertyChanged(nameof(SelectedCategoryIds));
            }
        }

        private int _quantityOwned;
        public int QuantityOwned
        {
            get => _quantityOwned;
            set
            {
                if (_quantityOwned != value)
                {
                    _quantityOwned = value;
                    OnPropertyChanged();
                    QuantityChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler? QuantityChanged;
    }

    // ViewModel pour une ligne de rentabilité de produit fini (potion, ou autre type de produit à terme)
    public class PotionProfitViewModel : BindableObject
    {
        public int PotionId { get; set; }
        public string Name { get; set; } = "";
        public float UnitCost { get; set; }

        // -1 = illimité (aucune ressource physique consommée par la recette). Conservé pour le
        // tri "Fabricables" ; l'affichage du détail par ressource remplace le nombre unique.
        public int MaxCraftable { get; set; }

        public string CraftableSummary => $"Coût: {GallyonsFormat.Format0(UnitCost)} Gallyons";

        // Détail des ressources nécessaires à la fabrication, avec la quantité déjà possédée
        // pour chacune (ex: "Cacao: 40 | Lait: 5"), colorée en rouge si cette ressource précise
        // manque. Remplace l'ancien nombre unique "Fabricables" jugé pas assez clair.
        private List<ResourceRequirement> _resourceRequirements = new();
        public List<ResourceRequirement> ResourceRequirements
        {
            get => _resourceRequirements;
            set
            {
                _resourceRequirements = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ResourcesSummary));
            }
        }

        public FormattedString ResourcesSummary
        {
            get
            {
                var formatted = new FormattedString();
                if (ResourceRequirements.Count == 0)
                {
                    formatted.Spans.Add(new Span { Text = "Aucune ressource requise", TextColor = Colors.Gray });
                    return formatted;
                }

                for (int i = 0; i < ResourceRequirements.Count; i++)
                {
                    var r = ResourceRequirements[i];
                    formatted.Spans.Add(new Span
                    {
                        Text = $"{r.Name}: {r.Owned}",
                        TextColor = r.Owned >= r.NeededPerUnit ? Colors.LightGreen : Colors.IndianRed,
                        FontAttributes = FontAttributes.Bold
                    });
                    if (i < ResourceRequirements.Count - 1)
                    {
                        formatted.Spans.Add(new Span { Text = " | ", TextColor = Colors.Gray });
                    }
                }

                return formatted;
            }
        }

        public ObservableCollection<BusinessCategory> AvailableCategories { get; set; } = new();

        private BusinessCategory? _selectedCategory;
        public BusinessCategory? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (!Equals(_selectedCategory, value))
                {
                    _selectedCategory = value;
                    OnPropertyChanged();
                    CategoryChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private float _resalePrice;
        public float ResalePrice
        {
            get => _resalePrice;
            set
            {
                if (_resalePrice != value)
                {
                    _resalePrice = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(MarginPerUnit));
                    OnPropertyChanged(nameof(TotalPotentialMargin));
                    OnPropertyChanged(nameof(MarginSummary));
                    OnPropertyChanged(nameof(MarginColor));
                    ResalePriceChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler? ResalePriceChanged;

        public float MarginPerUnit => ResalePrice - UnitCost;

        // Total = marge unitaire appliquée à la quantité DÉJÀ EN STOCK (Possédé), pas au nombre
        // fabricable depuis les ressources restantes — ce dernier dépendait d'un nombre annexe
        // jugé confus ; le Total reflète maintenant directement le bénéfice réalisable en
        // revendant le stock de produits finis existant.
        public float TotalPotentialMargin => MarginPerUnit * QuantityOwned;

        public string MarginSummary => $"Marge/u: {GallyonsFormat.FormatSigned(MarginPerUnit)} | Total: {GallyonsFormat.FormatSigned(TotalPotentialMargin)}";

        public Color MarginColor => MarginPerUnit >= 0 ? Colors.LightGreen : Colors.IndianRed;

        public bool IsBestMargin { get; set; }

        // Quantité de produits finis déjà fabriqués, possédés en stock (indépendante du
        // nombre "Fabricables" calculé à partir des ressources restantes).
        private int _quantityOwned;
        public int QuantityOwned
        {
            get => _quantityOwned;
            set
            {
                if (_quantityOwned != value)
                {
                    _quantityOwned = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalPotentialMargin));
                    OnPropertyChanged(nameof(MarginSummary));
                    QuantityChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler? CategoryChanged;
        public event EventHandler? QuantityChanged;
    }

    // Une ressource physique nécessaire à la fabrication d'un produit : quantité déjà possédée
    // en stock face à la quantité nécessaire par unité fabriquée.
    public class ResourceRequirement
    {
        public string Name { get; set; } = "";
        public int Owned { get; set; }
        public int NeededPerUnit { get; set; }
    }

    // Formatage commun des montants en Gallyons avec séparateur de milliers (espace), utilisé
    // par BusinessPage et OrdersManagerPage (ex: 50000 -> "50 000").
    public static class GallyonsFormat
    {
        private static readonly NumberFormatInfo Format = new()
        {
            NumberGroupSeparator = " ",
            NumberGroupSizes = new[] { 3 },
            NumberDecimalDigits = 0
        };

        public static string Format0(float value) => value.ToString("N0", Format);

        public static string FormatSigned(float value)
        {
            string sign = value > 0 ? "+" : value < 0 ? "-" : "";
            return sign + Format0(Math.Abs(value));
        }
    }
}
