using System.Collections.ObjectModel;
using SevenwandsConsoleTool;

namespace SevenwandsCompanion
{
    public partial class TokenTrackingPage : ContentPage
    {
        private const string TokenTrackingAssetPath = "TokenTracking.json";

        public ObservableCollection<YearData> Years { get; set; } = new ObservableCollection<YearData>();

        private YearData _selectedYear;
        public YearData SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (_selectedYear != value)
                {
                    _selectedYear = value;
                    OnPropertyChanged();
                    UpdateStatistics();
                }
            }
        }

        // Statistiques globales
        private string _progressPercentage = "0%";
        public string ProgressPercentage
        {
            get => _progressPercentage;
            set
            {
                if (_progressPercentage != value)
                {
                    _progressPercentage = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _completedCourses = "0/7";
        public string CompletedCourses
        {
            get => _completedCourses;
            set
            {
                if (_completedCourses != value)
                {
                    _completedCourses = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _totalCourses = 7;
        public int TotalCourses
        {
            get => _totalCourses;
            set
            {
                if (_totalCourses != value)
                {
                    _totalCourses = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _highPriority = 0;
        public int HighPriority
        {
            get => _highPriority;
            set
            {
                if (_highPriority != value)
                {
                    _highPriority = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isSaving = false;
        public bool IsSaving
        {
            get => _isSaving;
            set
            {
                if (_isSaving != value)
                {
                    _isSaving = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _lastSaveTime = "";
        public string LastSaveTime
        {
            get => _lastSaveTime;
            set
            {
                if (_lastSaveTime != value)
                {
                    _lastSaveTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public TokenTrackingPage()
        {
            InitializeComponent();
            BindingContext = this;
            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                // D'abord, essayer de charger depuis AppDataDirectory (données modifiées par l'utilisateur)
                var userDataPath = Path.Combine(FileSystem.AppDataDirectory, TokenTrackingAssetPath);
                string json = null;

                if (File.Exists(userDataPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Loading from user data: {userDataPath}");
                    json = await File.ReadAllTextAsync(userDataPath);
                }
                else
                {
                    // Sinon, charger depuis Resources/Raw (données d'origine)
                    System.Diagnostics.Debug.WriteLine($"Loading from app package: {TokenTrackingAssetPath}");
                    json = await LoadAssetAsStringAsync(TokenTrackingAssetPath);

                    // Copier dans AppDataDirectory pour les futures modifications
                    if (!string.IsNullOrEmpty(json))
                    {
                        await File.WriteAllTextAsync(userDataPath, json);
                        System.Diagnostics.Debug.WriteLine($"Initial data copied to: {userDataPath}");
                    }
                }

                if (!string.IsNullOrEmpty(json))
                {
                    var years = SevenwandsTools.DeserializeTokenTracking(json);
                    Years.Clear();
                    foreach (var year in years)
                    {
                        // S'assurer que les événements de changement sont écoutés
                        year.SubscribeToCourseChanges();
                        Years.Add(year);
                    }

                    // Sélectionner l'année 3 par défaut (index 2)
                    if (Years.Count > 3)
                    {
                        SelectedYear = Years.Last();
                    }
                    else if (Years.Count > 0)
                    {
                        SelectedYear = Years[0];
                    }

                    System.Diagnostics.Debug.WriteLine($"Loaded {Years.Count} years successfully");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading TokenTracking data: {ex.Message}");
                await DisplayAlert("Erreur", "Impossible de charger les données de suivi des jetons.", "OK");
            }
        }

        private async Task<string> LoadAssetAsStringAsync(string assetName)
        {
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync(assetName);
                using var reader = new StreamReader(stream);
                return await reader.ReadToEndAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load asset {assetName}: {ex.Message}");
                return null;
            }
        }

        private void UpdateStatistics()
        {
            if (SelectedYear != null)
            {
                ProgressPercentage = SelectedYear.ProgressPercentage;
                CompletedCourses = $"{SelectedYear.CompletedCoursesCount}/{SelectedYear.TotalCoursesCount}";
                TotalCourses = SelectedYear.TotalCoursesCount;
                HighPriority = SelectedYear.HighPriorityCount;
            }
        }

        // Méthodes pour les boutons +/- (à implémenter via Command Binding ou événements)
        public void OnDecrementClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Course course)
            {
                if (course.CurrentPoints > 0)
                {
                    course.CurrentPoints -= 1;
                    SelectedYear?.RefreshCalculations();
                    UpdateStatistics();
                    _ = SaveDataAsync();
                }
            }
        }

        public void OnIncrementClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Course course)
            {
                course.CurrentPoints += 1;
                SelectedYear?.RefreshCalculations();
                UpdateStatistics();
                _ = SaveDataAsync();
            }
        }

        public void OnIncrement2Clicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Course course)
            {
                course.CurrentPoints += 2;
                SelectedYear?.RefreshCalculations();
                UpdateStatistics();
                _ = SaveDataAsync();
            }
        }

        public void OnIncrement5Clicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Course course)
            {
                course.CurrentPoints += 5;
                SelectedYear?.RefreshCalculations();
                UpdateStatistics();
                _ = SaveDataAsync();
            }
        }

        public void OnResetClicked(object sender, EventArgs e)
        {
            _ = ResetToDefaultDataAsync();
        }

        private async Task SaveDataAsync()
        {
            try
            {
                IsSaving = true;

                // Sauvegarder dans AppDataDirectory (données utilisateur)
                var userDataPath = Path.Combine(FileSystem.AppDataDirectory, TokenTrackingAssetPath);
                await SevenwandsTools.SaveTokenTrackingToJson(userDataPath, Years.ToList());

                LastSaveTime = $"Sauvegardé à {DateTime.Now:HH:mm:ss}";
                System.Diagnostics.Debug.WriteLine($"✅ Token tracking data saved to: {userDataPath}");

                // En mode DEBUG, sauvegarder aussi dans le fichier source pour le développement
#if DEBUG
                try
                {
                    // Chemin vers le fichier source dans le projet
                    var projectPath = Path.GetFullPath(Path.Combine(FileSystem.AppDataDirectory, "..", "..", "..", "..", "..", "..", "..", "Src", TokenTrackingAssetPath));

                    if (Directory.Exists(Path.GetDirectoryName(projectPath)))
                    {
                        await SevenwandsTools.SaveTokenTrackingToJson(projectPath, Years.ToList());
                        System.Diagnostics.Debug.WriteLine($"✅ Token tracking data also saved to source: {projectPath}");
                    }
                }
                catch (Exception debugEx)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Could not save to source file (DEBUG mode): {debugEx.Message}");
                }
#endif

                // Cacher l'indicateur après 2 secondes
                await Task.Delay(2000);
                IsSaving = false;
            }
            catch (Exception ex)
            {
                IsSaving = false;
                System.Diagnostics.Debug.WriteLine($"❌ Error saving TokenTracking data: {ex.Message}");
                await DisplayAlert("Erreur", $"Impossible de sauvegarder les données: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Réinitialise les données avec celles d'origine depuis Resources/Raw
        /// </summary>
        public async Task ResetToDefaultDataAsync()
        {
            bool confirm = await DisplayAlert(
                "Réinitialiser", 
                "Voulez-vous réinitialiser toutes les données aux valeurs d'origine? Toutes vos modifications seront perdues.", 
                "Oui", 
                "Non");

            if (confirm)
            {
                try
                {
                    var userDataPath = Path.Combine(FileSystem.AppDataDirectory, TokenTrackingAssetPath);

                    // Supprimer le fichier utilisateur
                    if (File.Exists(userDataPath))
                    {
                        File.Delete(userDataPath);
                    }

                    // Recharger les données
                    await InitializeDataAsync();

                    await DisplayAlert("Succès", "Les données ont été réinitialisées.", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Erreur", $"Impossible de réinitialiser: {ex.Message}", "OK");
                }
            }
        }
    }
}

