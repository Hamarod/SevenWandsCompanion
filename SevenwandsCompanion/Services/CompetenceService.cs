using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using SevenwandsCompanion.Models;

namespace SevenwandsCompanion.Services
{
    /// <summary>
    /// Service pour gérer les compétences du personnage
    /// </summary>
    public class CompetenceService : INotifyPropertyChanged
    {
        private const string SettingsFileName = "PersonnageSettings.json";
        private static CompetenceService _instance;

        public static CompetenceService Instance => _instance ??= new CompetenceService();

        public ObservableCollection<CompetenceViewModel> Competences { get; private set; }

        private string _prenom = string.Empty;
        public string Prenom
        {
            get => _prenom;
            set
            {
                if (_prenom != value)
                {
                    _prenom = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _nom = string.Empty;
        public string Nom
        {
            get => _nom;
            set
            {
                if (_nom != value)
                {
                    _nom = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _annee = 1;
        public int Annee
        {
            get => _annee;
            private set // PRIVATE : ne peut être modifié que par le service lui-même
            {
                if (_annee != value)
                {
                    _annee = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private CompetenceService()
        {
            Competences = new ObservableCollection<CompetenceViewModel>();
            InitialiserCompetencesParDefaut();
        }

        /// <summary>
        /// Initialise les compétences avec les valeurs par défaut
        /// </summary>
        private void InitialiserCompetencesParDefaut()
        {
            Competences.Clear();
            Competences.Add(new CompetenceViewModel
            {
                Nom = "Intelligence",
                Icone = "🧠",
                Valeur = 0,
                Couleur = "#FFD700" // Jaune or
            });
            Competences.Add(new CompetenceViewModel
            {
                Nom = "Énergie",
                Icone = "⚡",
                Valeur = 0,
                Couleur = "#00BFFF" // Bleu clair
            });
            Competences.Add(new CompetenceViewModel
            {
                Nom = "Santé",
                Icone = "❤️",
                Valeur = 0,
                Couleur = "#FF1493" // Rose magenta
            });
            Competences.Add(new CompetenceViewModel
            {
                Nom = "Force",
                Icone = "💪",
                Valeur = 0,
                Couleur = "#DC143C" // Rouge
            });
            Competences.Add(new CompetenceViewModel
            {
                Nom = "Agilité",
                Icone = "🏃",
                Valeur = 0,
                Couleur = "#32CD32" // Vert
            });
        }

        /// <summary>
        /// Charge les compétences sauvegardées
        /// </summary>
        public async Task ChargerCompetencesAsync()
        {
            try
            {
                var settingsPath = Path.Combine(FileSystem.AppDataDirectory, SettingsFileName);

                if (File.Exists(settingsPath))
                {
                    var json = await File.ReadAllTextAsync(settingsPath);

                    // Vérifier que le JSON n'est pas vide
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var data = JsonSerializer.Deserialize<PersonnageData>(json);

                        if (data != null)
                        {
                            Prenom = data.Prenom ?? string.Empty;
                            Nom = data.Nom ?? string.Empty;
                            // NE PAS charger l'année depuis le JSON - elle sera calculée depuis les jetons

                            if (data.Competences != null && data.Competences.Count > 0)
                            {
                                Competences.Clear();
                                foreach (var comp in data.Competences)
                                {
                                    Competences.Add(new CompetenceViewModel
                                    {
                                        Nom = comp.Nom,
                                        Icone = comp.Icone,
                                        Valeur = comp.Valeur,
                                        Couleur = comp.Couleur
                                    });
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Nouveau utilisateur : créer un fichier par défaut
                    System.Diagnostics.Debug.WriteLine("📝 Nouveau utilisateur détecté, création du fichier personnage par défaut");
                    await SauvegarderCompetencesAsync();
                }

                // TOUJOURS calculer l'année depuis les jetons après le chargement
                await RefreshAnneeFromTokensAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur chargement compétences: {ex.Message}");
                // En cas d'erreur, garder les valeurs par défaut déjà initialisées
                await RefreshAnneeFromTokensAsync();
            }
        }

        /// <summary>
        /// Met à jour l'année en fonction des jetons accumulés
        /// </summary>
        public async Task RefreshAnneeFromTokensAsync()
        {
            try
            {
                var currentYear = await TokenService.Instance.GetCurrentYearAsync();
                Annee = currentYear;
                System.Diagnostics.Debug.WriteLine($"✅ Année mise à jour depuis les jetons: {currentYear}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur calcul année: {ex.Message}");
            }
        }

        /// <summary>
        /// Sauvegarde les compétences (l'année n'est PAS sauvegardée, elle est calculée depuis les jetons)
        /// </summary>
        public async Task SauvegarderCompetencesAsync()
        {
            try
            {
                var settingsPath = Path.Combine(FileSystem.AppDataDirectory, SettingsFileName);

                var data = new PersonnageData
                {
                    Prenom = Prenom ?? string.Empty,
                    Nom = Nom ?? string.Empty,
                    // Annee n'est PAS sauvegardée - elle est calculée depuis les jetons
                    Competences = Competences?.Select(c => new CompetenceData
                    {
                        Nom = c.Nom ?? string.Empty,
                        Icone = c.Icone ?? string.Empty,
                        Valeur = c.Valeur,
                        Couleur = c.Couleur ?? "#FFFFFF"
                    }).ToList() ?? new List<CompetenceData>()
                };

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(settingsPath, json);

                System.Diagnostics.Debug.WriteLine($"✅ Personnage sauvegardé: {Prenom} {Nom}, {Competences.Count} compétences (année {Annee} calculée)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur sauvegarde personnage: {ex.Message}");
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Vérifie l'état du fichier de personnage pour le débogage
        /// </summary>
        public async Task<string> VerifierEtatFichierAsync()
        {
            var settingsPath = Path.Combine(FileSystem.AppDataDirectory, SettingsFileName);

            if (!File.Exists(settingsPath))
            {
                return $"❌ Fichier non trouvé: {settingsPath}";
            }

            try
            {
                var json = await File.ReadAllTextAsync(settingsPath);
                var fileInfo = new FileInfo(settingsPath);

                return $"✅ Fichier OK: {fileInfo.Length} octets\nChemin: {settingsPath}\nContenu: {(string.IsNullOrWhiteSpace(json) ? "VIDE" : "Valide")}";
            }
            catch (Exception ex)
            {
                return $"❌ Erreur lecture: {ex.Message}";
            }
        }
    }

    /// <summary>
    /// Classe pour la sérialisation JSON du personnage
    /// </summary>
    public class PersonnageData
    {
        public string Prenom { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public int Annee { get; set; } = 1;
        public List<CompetenceData> Competences { get; set; } = new List<CompetenceData>();
    }

    /// <summary>
    /// Classe pour la sérialisation JSON
    /// </summary>
    public class CompetenceData
    {
        public string Nom { get; set; } = string.Empty;
        public string Icone { get; set; } = string.Empty;
        public int Valeur { get; set; } = 0;
        public string Couleur { get; set; } = "#FFFFFF";
    }

    /// <summary>
    /// ViewModel pour une compétence avec binding
    /// </summary>
    public class CompetenceViewModel : INotifyPropertyChanged
    {
        private string _nom;
        private string _icone;
        private int _valeur;
        private string _couleur;

        public string Nom
        {
            get => _nom;
            set
            {
                _nom = value;
                OnPropertyChanged();
            }
        }

        public string Icone
        {
            get => _icone;
            set
            {
                _icone = value;
                OnPropertyChanged();
            }
        }

        public int Valeur
        {
            get => _valeur;
            set
            {
                _valeur = value;
                OnPropertyChanged();
            }
        }

        public string Couleur
        {
            get => _couleur;
            set
            {
                _couleur = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
