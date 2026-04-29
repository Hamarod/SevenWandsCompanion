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
                    BuildCoursesGrid(); // Reconstruire la grille quand l'année change
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

                    // Migration automatique : ajouter la 5ème année si elle manque
                    bool needsMigration = years.Count < 5;
                    if (needsMigration)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Migration needed: only {years.Count} years found, adding missing years...");
                        years = await MigrateToLatestVersionAsync(years);

                        // Sauvegarder immédiatement après la migration
                        await SevenwandsTools.SaveTokenTrackingToJson(userDataPath, years);
                        System.Diagnostics.Debug.WriteLine($"✅ Migration complete, saved {years.Count} years");
                    }

                    foreach (var year in years)
                    {
                        // S'assurer que les événements de changement sont écoutés
                        year.SubscribeToCourseChanges();
                        Years.Add(year);
                    }

                    // Sélectionner automatiquement la première année non complétée
                    var currentYear = Years.FirstOrDefault(y => !y.IsYearCompleted);
                    if (currentYear != null)
                    {
                        SelectedYear = currentYear;
                        System.Diagnostics.Debug.WriteLine($"✅ Selected current year: {currentYear.YearTitle} (not completed)");
                    }
                    else
                    {
                        // Toutes les années sont complétées, sélectionner la dernière
                        SelectedYear = Years.LastOrDefault();
                        System.Diagnostics.Debug.WriteLine($"✅ All years completed, selected last year: {SelectedYear?.YearTitle}");
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

        /// <summary>
        /// Construit dynamiquement la grille des cours pour qu'elle soit responsive
        /// Affiche les cours sur 2 lignes pour élargir chaque carte
        /// </summary>
        private void BuildCoursesGrid()
        {
            if (CoursesGrid == null || SelectedYear?.Courses == null)
                return;

            CoursesGrid.Children.Clear();
            CoursesGrid.ColumnDefinitions.Clear();
            CoursesGrid.RowDefinitions.Clear();

            var courses = SelectedYear.Courses;
            int courseCount = courses.Count;

            // Calculer le nombre de colonnes (moitié des cours, arrondi vers le haut)
            int columnsCount = (int)Math.Ceiling(courseCount / 2.0);

            // Créer 2 lignes
            CoursesGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            CoursesGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Créer les colonnes (répartition égale)
            for (int i = 0; i < columnsCount; i++)
            {
                CoursesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            // Placer les cours sur la grille
            for (int i = 0; i < courseCount; i++)
            {
                var course = courses[i];
                var card = CreateCourseCard(course);

                // Première ligne: indices 0 à columnsCount-1
                // Deuxième ligne: indices columnsCount à courseCount-1
                int row = i < columnsCount ? 0 : 1;
                int col = i < columnsCount ? i : i - columnsCount;

                Grid.SetRow(card, row);
                Grid.SetColumn(card, col);
                CoursesGrid.Children.Add(card);
            }

            System.Diagnostics.Debug.WriteLine($"✅ Grille créée avec {columnsCount} colonnes × 2 lignes (total: {courseCount} cours)");
        }

        /// <summary>
        /// Crée une carte de cours compacte
        /// </summary>
        private Border CreateCourseCard(Course course)
        {
            var border = new Border
            {
                Style = (Style)Application.Current.Resources["CardStyle"],
                VerticalOptions = LayoutOptions.Fill,
                HorizontalOptions = LayoutOptions.Fill
            };

            var mainGrid = new Grid
            {
                Padding = 8,
                RowSpacing = 5,
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto }
                }
            };

            // Icône
            var icon = new Label
            {
                Text = course.Icon,
                FontSize = 34, // +2 unités
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 0, 0, 3)
            };
            Grid.SetRow(icon, 0);
            mainGrid.Children.Add(icon);

            // Nom du cours
            var name = new Label
            {
                Text = course.Name,
                FontSize = 12, // +2 unités
                FontAttributes = FontAttributes.Bold,
                TextColor = (Color)Application.Current.Resources["TextPrimary"],
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.WordWrap,
                MaxLines = 2,
                HeightRequest = 32 // Ajusté pour la plus grande police
            };
            name.SetBinding(Label.TextProperty, new Binding(nameof(course.Name), source: course));
            Grid.SetRow(name, 1);
            mainGrid.Children.Add(name);

            // Barre de progression
            var progressBar = new ProgressBar
            {
                ProgressColor = (Color)Application.Current.Resources["AccentGold"],
                BackgroundColor = (Color)Application.Current.Resources["BackgroundCard"],
                HeightRequest = 8,
                Margin = new Thickness(0, 2)
            };
            progressBar.SetBinding(ProgressBar.ProgressProperty, new Binding(nameof(course.Progress), source: course));
            Grid.SetRow(progressBar, 2);
            mainGrid.Children.Add(progressBar);

            // Points avec points restants et valeur/2
            var pointsStack = new HorizontalStackLayout
            {
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 3
            };

            var currentPoints = new Label
            {
                FontSize = 22, // +2 unités
                FontAttributes = FontAttributes.Bold,
                TextColor = (Color)Application.Current.Resources["AccentGold"]
            };
            currentPoints.SetBinding(Label.TextProperty, new Binding(nameof(course.CurrentPoints), source: course));
            pointsStack.Children.Add(currentPoints);

            pointsStack.Children.Add(new Label
            {
                Text = "/",
                FontSize = 13, // +2 unités
                TextColor = (Color)Application.Current.Resources["TextSecondary"],
                VerticalOptions = LayoutOptions.Center
            });

            var requiredPoints = new Label
            {
                FontSize = 13, // +2 unités
                TextColor = (Color)Application.Current.Resources["TextSecondary"],
                VerticalOptions = LayoutOptions.End,
                Margin = new Thickness(0, 0, 0, 3)
            };
            requiredPoints.SetBinding(Label.TextProperty, new Binding(nameof(course.RequiredPoints), source: course));
            pointsStack.Children.Add(requiredPoints);

            // Restant (cours_restant/tokens_nécessaires)
            var remainingLabel = new Label
            {
                FontSize = 11, // +2 unités
                TextColor = (Color)Application.Current.Resources["TextSecondary"],
                VerticalOptions = LayoutOptions.End,
                Margin = new Thickness(0, 0, 0, 3)
            };
            // Bind directement sur la propriété calculée du modèle
            remainingLabel.SetBinding(Label.TextProperty, new Binding(nameof(course.RemainingTokensText), source: course));
            pointsStack.Children.Add(remainingLabel);

            Grid.SetRow(pointsStack, 3);
            mainGrid.Children.Add(pointsStack);

            // Boutons
            var buttonGrid = new Grid
            {
                RowSpacing = 3,
                ColumnSpacing = 3,
                Margin = new Thickness(0, 3, 0, 0),
                RowDefinitions =
                {
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
                },
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                }
            };

            buttonGrid.Children.Add(CreateButton("−", course, OnDecrementClicked, "AccentRed", 0, 0));
            buttonGrid.Children.Add(CreateButton("+", course, OnIncrementClicked, "AccentGreen", 0, 1));
            buttonGrid.Children.Add(CreateButton("+2", course, OnIncrement2Clicked, "AccentOrange", 1, 0));
            buttonGrid.Children.Add(CreateButton("+5", course, OnIncrement5Clicked, "AccentBlue", 1, 1));

            Grid.SetRow(buttonGrid, 4);
            mainGrid.Children.Add(buttonGrid);

            border.Content = mainGrid;
            return border;
        }

        /// <summary>
        /// Crée un bouton d'action
        /// </summary>
        private Button CreateButton(string text, Course course, EventHandler handler, string colorKey, int row, int col)
        {
            var button = new Button
            {
                Text = text,
                FontSize = text.Length == 1 ? 15 : 12, // +2 unités
                HeightRequest = 30, // +2 pour accommoder la plus grande police
                Padding = 0,
                BackgroundColor = (Color)Application.Current.Resources[colorKey],
                TextColor = Colors.White,
                CornerRadius = 4,
                CommandParameter = course
            };
            button.Clicked += handler;
            Grid.SetRow(button, row);
            Grid.SetColumn(button, col);
            return button;
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

                // Après la sauvegarde, rafraîchir l'année du personnage
                await Services.CompetenceService.Instance.RefreshAnneeFromTokensAsync();

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
        /// Migre les données vers la dernière version en ajoutant les années manquantes
        /// </summary>
        private async Task<List<YearData>> MigrateToLatestVersionAsync(List<YearData> existingYears)
        {
            try
            {
                // Charger le fichier par défaut pour obtenir le template des années manquantes
                var defaultJson = await LoadAssetAsStringAsync(TokenTrackingAssetPath);
                var defaultYears = SevenwandsTools.DeserializeTokenTracking(defaultJson);

                // Ajouter les années manquantes
                for (int yearNumber = existingYears.Count + 1; yearNumber <= defaultYears.Count; yearNumber++)
                {
                    var templateYear = defaultYears.FirstOrDefault(y => y.Year == yearNumber);
                    if (templateYear != null)
                    {
                        // Créer une nouvelle année avec tous les points à 0
                        var newYear = new YearData
                        {
                            Year = templateYear.Year,
                            Courses = templateYear.Courses.Select(c => new Course
                            {
                                Id = c.Id,
                                Name = c.Name,
                                CurrentPoints = 0,
                                RequiredPoints = c.RequiredPoints,
                                Icon = c.Icon
                            }).ToList()
                        };

                        existingYears.Add(newYear);
                        System.Diagnostics.Debug.WriteLine($"✅ Added {newYear.YearTitle} with {newYear.Courses.Count} courses");
                    }
                }

                return existingYears;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Migration error: {ex.Message}");
                return existingYears;
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

