using System.Text.Json;
using SevenwandsConsoleTool;

namespace SevenwandsCompanion.Services
{
    /// <summary>
    /// Service pour charger et gérer les données de suivi des jetons
    /// </summary>
    public class TokenService
    {
        private const string TokenTrackingFileName = "TokenTracking.json";
        private static TokenService _instance;

        public static TokenService Instance => _instance ??= new TokenService();

        private List<YearData> _allYears;

        private TokenService()
        {
            _allYears = new List<YearData>();
        }

        /// <summary>
        /// Charge les données de suivi des jetons depuis le fichier sauvegardé
        /// </summary>
        public async Task<List<YearData>> LoadTokenDataAsync()
        {
            try
            {
                var settingsPath = Path.Combine(FileSystem.AppDataDirectory, TokenTrackingFileName);

                if (File.Exists(settingsPath))
                {
                    var json = await File.ReadAllTextAsync(settingsPath);

                    // Vérifier que le JSON n'est pas vide
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        _allYears = JsonSerializer.Deserialize<List<YearData>>(json);

                        if (_allYears != null && _allYears.Count > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"✅ {_allYears.Count} années chargées depuis le fichier utilisateur");
                            return _allYears;
                        }
                    }
                }

                // Si pas de fichier ou fichier vide/invalide, charger depuis les assets
                System.Diagnostics.Debug.WriteLine("📝 Chargement des données de jetons par défaut depuis les assets");
                return await LoadDefaultTokenDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur chargement jetons: {ex.Message}");
                // En cas d'erreur, tenter de charger les données par défaut
                try
                {
                    return await LoadDefaultTokenDataAsync();
                }
                catch
                {
                    return new List<YearData>();
                }
            }
        }

        /// <summary>
        /// Charge les données par défaut depuis les assets
        /// </summary>
        private async Task<List<YearData>> LoadDefaultTokenDataAsync()
        {
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("TokenTracking.json");
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();

                _allYears = JsonSerializer.Deserialize<List<YearData>>(json);

                if (_allYears != null && _allYears.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ {_allYears.Count} années chargées depuis les assets");
                    return _allYears;
                }

                System.Diagnostics.Debug.WriteLine("⚠️ Assets vides ou invalides");
                return new List<YearData>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur chargement assets jetons: {ex.Message}");
                return new List<YearData>();
            }
        }

        /// <summary>
        /// Obtient les statistiques de jetons pour une année donnée
        /// </summary>
        public async Task<TokenStats> GetTokenStatsForYearAsync(int year)
        {
            if (_allYears == null || _allYears.Count == 0)
            {
                await LoadTokenDataAsync();
            }

            var yearData = _allYears?.FirstOrDefault(y => y.Year == year);

            if (yearData != null)
            {
                return new TokenStats
                {
                    CurrentPoints = yearData.TotalCurrentPoints,
                    RequiredPoints = yearData.TotalRequiredPoints,
                    CompletedCourses = yearData.CompletedCoursesCount,
                    TotalCourses = yearData.TotalCoursesCount
                };
            }

            return new TokenStats();
        }

        /// <summary>
        /// Calcule l'année actuelle de l'étudiant en fonction des années complétées
        /// Règle : Une année est considérée complétée quand tous ses cours sont terminés
        /// </summary>
        public async Task<int> GetCurrentYearAsync()
        {
            if (_allYears == null || _allYears.Count == 0)
            {
                await LoadTokenDataAsync();
            }

            if (_allYears == null || _allYears.Count == 0)
            {
                return 1; // Année 1 par défaut
            }

            // Trouver la dernière année complétée
            int lastCompletedYear = 0;
            foreach (var year in _allYears.OrderBy(y => y.Year))
            {
                if (year.IsYearCompleted)
                {
                    lastCompletedYear = year.Year;
                }
                else
                {
                    // Dès qu'on trouve une année non complétée, on s'arrête
                    break;
                }
            }

            // L'année actuelle est celle après la dernière complétée
            // Si aucune année complétée, on est en année 1
            // Si année 7 complétée, on reste en année 7
            int currentYear = Math.Min(lastCompletedYear + 1, 7);

            System.Diagnostics.Debug.WriteLine($"📚 Année actuelle calculée: {currentYear} (dernière complétée: {lastCompletedYear})");

            return currentYear;
        }
    }

    /// <summary>
    /// Statistiques de jetons pour affichage
    /// </summary>
    public class TokenStats
    {
        public int CurrentPoints { get; set; }
        public int RequiredPoints { get; set; }
        public int CompletedCourses { get; set; }
        public int TotalCourses { get; set; }

        public string FormattedText => $"Jetons {CurrentPoints}/{RequiredPoints} ({CompletedCourses}/{TotalCourses})";
    }
}
