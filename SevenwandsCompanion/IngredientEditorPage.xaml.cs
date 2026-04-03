using System.Collections.ObjectModel;
using SevenwandsConsoleTool;

namespace SevenwandsCompanion
{
    public partial class IngredientEditorPage : ContentPage
    {
        private const string IngredientsAssetPath = "Ingredients.json";

        public ObservableCollection<IngredientViewModel> AllIngredients { get; set; }
        public ObservableCollection<IngredientViewModel> FilteredIngredients { get; set; }
        public ObservableCollection<IngredientType> IngredientTypes { get; set; }

        private IngredientViewModel _selectedIngredient;
        public IngredientViewModel SelectedIngredient
        {
            get => _selectedIngredient;
            set
            {
                _selectedIngredient = value;
                OnPropertyChanged();
                if (value != null)
                {
                    LoadIngredientForEdit(value);
                }
            }
        }

        private IngredientViewModel _editedIngredient;
        public IngredientViewModel EditedIngredient
        {
            get => _editedIngredient;
            set
            {
                _editedIngredient = value;
                OnPropertyChanged();
            }
        }

        private string _formTitle = "➕ NOUVEL INGRÉDIENT";
        public string FormTitle
        {
            get => _formTitle;
            set
            {
                _formTitle = value;
                OnPropertyChanged();
            }
        }

        private string _idInfo = "";
        public string IdInfo
        {
            get => _idInfo;
            set
            {
                _idInfo = value;
                OnPropertyChanged();
            }
        }

        private bool _isEditMode = false;
        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                _isEditMode = value;
                OnPropertyChanged();
            }
        }

        private Dictionary<int, Ingredient> _ingredientsDict;

        public IngredientEditorPage()
        {
            InitializeComponent();

            AllIngredients = new ObservableCollection<IngredientViewModel>();
            FilteredIngredients = new ObservableCollection<IngredientViewModel>();
            IngredientTypes = new ObservableCollection<IngredientType>
            {
                IngredientType.ingredient,
                IngredientType.Fire,
                IngredientType.rotate,
                IngredientType.spell
            };

            // Nouvel ingrédient par défaut
            EditedIngredient = new IngredientViewModel
            {
                Name = "",
                Description = "",
                Type = IngredientType.ingredient,
                Price = null
            };

            BindingContext = this;
            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                // S'assurer que le fichier existe dans AppDataDirectory (copie initiale si nécessaire)
                await EnsureDataFileExists();

                // Charger les ingrédients UNIQUEMENT depuis AppDataDirectory (source unique de vérité)
                string appDataPath = Path.Combine(FileSystem.AppDataDirectory, IngredientsAssetPath);
                string ingredientsJson = await File.ReadAllTextAsync(appDataPath);

                _ingredientsDict = SevenwandsTools.DeserializeIngredients(ingredientsJson);

                // Remplir la liste
                AllIngredients.Clear();
                foreach (var ingredient in _ingredientsDict.Values.OrderBy(i => i.Name))
                {
                    AllIngredients.Add(new IngredientViewModel(ingredient));
                }

                System.Diagnostics.Debug.WriteLine($"📦 IngredientEditor: {AllIngredients.Count} ingrédients chargés depuis AppDataDirectory");

                // Initialiser la liste filtrée
                UpdateFilteredIngredients(string.Empty);

                UpdateIdInfo();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur lors du chargement des ingrédients: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"Error loading ingredients: {ex.Message}");
            }
        }

        /// <summary>
        /// S'assure que le fichier Ingredients.json existe dans AppDataDirectory.
        /// Crée un fichier VIDE au premier lancement pour permettre à l'utilisateur de découvrir et ajouter progressivement les ingrédients.
        /// </summary>
        private async Task EnsureDataFileExists()
        {
            string ingredientsPath = Path.Combine(FileSystem.AppDataDirectory, IngredientsAssetPath);

            if (!File.Exists(ingredientsPath))
            {
                try
                {
                    // Créer un dictionnaire vide pour les ingrédients
                    var emptyIngredientsDict = new Dictionary<int, Ingredient>();
                    await SevenwandsTools.SaveIngredientsToJson(ingredientsPath, emptyIngredientsDict);
                    System.Diagnostics.Debug.WriteLine($"🌱 IngredientEditor: Création fichier vide (l'utilisateur ajoutera les ingrédients)");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Erreur création fichier vide: {ex.Message}");
                    throw;
                }
            }
        }

        private void LoadIngredientForEdit(IngredientViewModel ingredient)
        {
            IsEditMode = true;
            FormTitle = "✏️ MODIFIER INGRÉDIENT";

            EditedIngredient = new IngredientViewModel
            {
                Id = ingredient.Id,
                Name = ingredient.Name,
                Description = ingredient.Description,
                Type = ingredient.Type,
                Price = ingredient.Price
            };

            IdInfo = $"ID: {ingredient.Id}";
        }

        private void OnNewIngredientClicked(object sender, EventArgs e)
        {
            IsEditMode = false;
            FormTitle = "➕ NOUVEL INGRÉDIENT";
            SelectedIngredient = null;

            // Générer un nouvel ID
            int newId = _ingredientsDict.Any() ? _ingredientsDict.Keys.Max() + 10 : 10;

            EditedIngredient = new IngredientViewModel
            {
                Id = newId,
                Name = "",
                Description = "",
                Type = IngredientType.ingredient,
                Price = null
            };

            UpdateIdInfo();
        }

        private void UpdateIdInfo()
        {
            if (IsEditMode)
            {
                IdInfo = $"ID: {EditedIngredient.Id} (existant)";
            }
            else
            {
                int nextId = _ingredientsDict.Any() ? _ingredientsDict.Keys.Max() + 10 : 10;
                IdInfo = $"Nouvel ID auto: {nextId} (espacés de 10 pour insertions futures)";
                EditedIngredient.Id = nextId;
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(EditedIngredient.Name))
            {
                await DisplayAlert("Erreur", "Le nom de l'ingrédient est obligatoire.", "OK");
                return;
            }

            try
            {
                // Convertir en Ingredient
                var ingredient = new Ingredient(
                    id: EditedIngredient.Id,
                    name: EditedIngredient.Name,
                    type: EditedIngredient.Type,
                    description: EditedIngredient.Description ?? "",
                    price: EditedIngredient.Price
                );

                // Mettre à jour ou ajouter
                if (_ingredientsDict.ContainsKey(ingredient.Id))
                {
                    _ingredientsDict[ingredient.Id] = ingredient;
                }
                else
                {
                    _ingredientsDict.Add(ingredient.Id, ingredient);
                }

                // Sauvegarder dans AppDataDirectory (source unique de vérité)
                string appDataPath = Path.Combine(FileSystem.AppDataDirectory, IngredientsAssetPath);
                await SevenwandsTools.SaveIngredientsToJson(appDataPath, _ingredientsDict);
                System.Diagnostics.Debug.WriteLine($"💾 Ingrédient sauvegardé: {appDataPath}");

                await DisplayAlert("Succès", $"L'ingrédient '{EditedIngredient.Name}' a été {(IsEditMode ? "modifié" : "créé")} avec succès !", "OK");

                // Recharger la liste
                await InitializeDataAsync();

                // Réinitialiser le formulaire
                OnNewIngredientClicked(null, null);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur lors de la sauvegarde: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"Save error: {ex.Message}");
            }
        }

        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            if (!IsEditMode || EditedIngredient == null)
                return;

            bool confirm = await DisplayAlert(
                "Confirmation", 
                $"Voulez-vous vraiment supprimer l'ingrédient '{EditedIngredient.Name}' ?\n\n⚠️ Attention: Les potions utilisant cet ingrédient pourraient devenir invalides.", 
                "Supprimer", 
                "Annuler");

            if (!confirm)
                return;

            try
            {
                _ingredientsDict.Remove(EditedIngredient.Id);

                // Sauvegarder
                string appDataPath = Path.Combine(FileSystem.AppDataDirectory, IngredientsAssetPath);
                await SevenwandsTools.SaveIngredientsToJson(appDataPath, _ingredientsDict);
                System.Diagnostics.Debug.WriteLine($"🗑️ Ingrédient supprimé, sauvegardé: {appDataPath}");

                await DisplayAlert("Succès", $"L'ingrédient '{EditedIngredient.Name}' a été supprimé.", "OK");

                // Recharger
                await InitializeDataAsync();
                OnNewIngredientClicked(null, null);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur lors de la suppression: {ex.Message}", "OK");
            }
        }

        private void OnCancelEditClicked(object sender, EventArgs e)
        {
            if (SelectedIngredient != null)
            {
                LoadIngredientForEdit(SelectedIngredient);
            }
            else
            {
                OnNewIngredientClicked(null, null);
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        // Search functionality
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFilteredIngredients(e.NewTextValue);
        }

        private void UpdateFilteredIngredients(string searchText)
        {
            FilteredIngredients.Clear();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Show all ingredients
                foreach (var ingredient in AllIngredients)
                {
                    FilteredIngredients.Add(ingredient);
                }
            }
            else
            {
                // Filter ingredients by name or type (case-insensitive)
                var filtered = AllIngredients.Where(i =>
                    (i.Name != null && i.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                    i.TypeDisplay.Contains(searchText, StringComparison.OrdinalIgnoreCase));

                foreach (var ingredient in filtered)
                {
                    FilteredIngredients.Add(ingredient);
                }
            }
        }
    }

    // ViewModel pour un ingrédient avec binding XAML
    public class IngredientViewModel : BindableObject
    {
        private int _id;
        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        private string _description;
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        private IngredientType _type;
        public IngredientType Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TypeDisplay));
            }
        }

        private float? _price;
        public float? Price
        {
            get => _price;
            set
            {
                _price = value;
                OnPropertyChanged();
            }
        }

        public string TypeDisplay => Type switch
        {
            IngredientType.Fire => "🔥 Feu",
            IngredientType.ingredient => "🌿 Ingrédient",
            IngredientType.rotate => "🔄 Rotation",
            IngredientType.spell => "✨ Formule",
            _ => Type.ToString()
        };

        public IngredientViewModel() { }

        public IngredientViewModel(Ingredient ingredient)
        {
            Id = ingredient.Id;
            Name = ingredient.Name;
            Description = ingredient.Description;
            Type = ingredient.Type;
            Price = ingredient.Price;
        }
    }
}
