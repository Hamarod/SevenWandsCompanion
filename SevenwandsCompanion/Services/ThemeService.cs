using System.Text.Json;

namespace SevenwandsCompanion.Services
{
    public class ThemeService
    {
        private const string SettingsFileName = "UserSettings.json";
        private static ThemeService _instance;
        private string _currentHouse;

        public static ThemeService Instance => _instance ??= new ThemeService();

        public string CurrentHouse
        {
            get => _currentHouse;
            private set
            {
                if (_currentHouse != value)
                {
                    _currentHouse = value;
                    OnThemeChanged?.Invoke(value);
                }
            }
        }

        public event Action<string> OnThemeChanged;

        private ThemeService()
        {
            _currentHouse = string.Empty;
            InitializeDefaultColors();
        }

        /// <summary>
        /// Initialise les couleurs par défaut de l'application
        /// </summary>
        private void InitializeDefaultColors()
        {
            var app = Application.Current;
            if (app == null)
                return;

            // Couleurs par défaut (style neutre)
            var defaultColors = new Dictionary<string, Color>
            {
                ["BackgroundDark"] = Color.FromArgb("#1A1D29"),
                ["BackgroundCard"] = Color.FromArgb("#252836"),
                ["BorderColor"] = Color.FromArgb("#2D3142"),
                ["TextPrimary"] = Colors.White,
                ["TextSecondary"] = Color.FromArgb("#8B8B8B"),
                ["TextAccent"] = Color.FromArgb("#D4AF37"),
                ["AccentGold"] = Color.FromArgb("#D4AF37"),
                ["AccentOrange"] = Color.FromArgb("#FFA500"),
                ["AccentGreen"] = Color.FromArgb("#4CAF50"),
                ["AccentBlue"] = Color.FromArgb("#2196F3"),
                ["AccentRed"] = Color.FromArgb("#F44336"),
                ["AccentSecondary"] = Color.FromArgb("#D4AF37"),
                ["ButtonBackground"] = Color.FromArgb("#2D3142"),
                ["ButtonBackgroundHover"] = Color.FromArgb("#3D4152"),
                ["ButtonText"] = Colors.White
            };

            foreach (var kvp in defaultColors)
            {
                if (!app.Resources.ContainsKey(kvp.Key))
                {
                    app.Resources.Add(kvp.Key, kvp.Value);
                }
            }
        }

        /// <summary>
        /// Charge la maison sauvegardée depuis les préférences
        /// </summary>
        public async Task<string> LoadSavedHouseAsync()
        {
            try
            {
                var settingsPath = Path.Combine(FileSystem.AppDataDirectory, SettingsFileName);
                System.Diagnostics.Debug.WriteLine($"📁 Loading house from: {settingsPath}");

                if (File.Exists(settingsPath))
                {
                    var json = await File.ReadAllTextAsync(settingsPath);
                    System.Diagnostics.Debug.WriteLine($"📄 JSON content: {json}");

                    var settings = JsonSerializer.Deserialize<UserSettings>(json);
                    CurrentHouse = settings?.SelectedHouse ?? string.Empty;

                    System.Diagnostics.Debug.WriteLine($"✅ House loaded: '{CurrentHouse}'");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Settings file does not exist at: {settingsPath}");
                    CurrentHouse = string.Empty;
                }

                return CurrentHouse;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading house: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                CurrentHouse = string.Empty;
                return string.Empty;
            }
        }

        /// <summary>
        /// Sauvegarde le choix de la maison
        /// </summary>
        public async Task SaveHouseAsync(string house)
        {
            try
            {
                var settingsPath = Path.Combine(FileSystem.AppDataDirectory, SettingsFileName);
                System.Diagnostics.Debug.WriteLine($"💾 Saving house '{house}' to: {settingsPath}");

                var settings = new UserSettings { SelectedHouse = house };
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });

                System.Diagnostics.Debug.WriteLine($"📄 JSON to save: {json}");

                await File.WriteAllTextAsync(settingsPath, json);
                CurrentHouse = house;

                // Vérifier que le fichier a bien été créé
                if (File.Exists(settingsPath))
                {
                    System.Diagnostics.Debug.WriteLine($"✅ House saved successfully: {house}");
                    var savedContent = await File.ReadAllTextAsync(settingsPath);
                    System.Diagnostics.Debug.WriteLine($"✅ Verified saved content: {savedContent}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ File was not created!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error saving house: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Applique le thème correspondant à la maison
        /// </summary>
        public void ApplyTheme(string house)
        {
            if (string.IsNullOrEmpty(house))
                return;

            var app = Application.Current;
            if (app == null)
                return;

            try
            {
                // Obtenir les couleurs de la maison
                var houseColors = GetHouseColors(house);
                if (houseColors == null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Unknown house: {house}");
                    return;
                }

                // Appliquer directement les couleurs dans les ressources de l'application
                foreach (var kvp in houseColors)
                {
                    if (app.Resources.ContainsKey(kvp.Key))
                    {
                        app.Resources[kvp.Key] = kvp.Value;
                    }
                    else
                    {
                        app.Resources.Add(kvp.Key, kvp.Value);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"✅ Theme applied: {house}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading theme: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtient les couleurs d'une maison
        /// </summary>
        private Dictionary<string, Color> GetHouseColors(string house)
        {
            return house switch
            {
                "Lombrasier" => new Dictionary<string, Color>
                {
                    ["BackgroundDark"] = Color.FromArgb("#0D0808"),
                    ["BackgroundCard"] = Color.FromArgb("#2D1414"),
                    ["BorderColor"] = Color.FromArgb("#8B0000"),
                    ["TextPrimary"] = Colors.White,
                    ["TextSecondary"] = Color.FromArgb("#C0C0C0"),
                    ["TextAccent"] = Color.FromArgb("#D4AF37"),
                    ["AccentGold"] = Color.FromArgb("#D4AF37"),
                    ["AccentOrange"] = Color.FromArgb("#DC2626"),
                    ["AccentGreen"] = Color.FromArgb("#DC2626"),
                    ["AccentBlue"] = Color.FromArgb("#DC2626"),
                    ["AccentRed"] = Color.FromArgb("#8B0000"),
                    ["AccentSecondary"] = Color.FromArgb("#D4AF37"),
                    ["ButtonBackground"] = Color.FromArgb("#8B0000"),
                    ["ButtonBackgroundHover"] = Color.FromArgb("#A62020"),
                    ["ButtonText"] = Colors.White
                },
                "Vervenin" => new Dictionary<string, Color>
                {
                    ["BackgroundDark"] = Color.FromArgb("#0A0F0A"),
                    ["BackgroundCard"] = Color.FromArgb("#1A2318"),
                    ["BorderColor"] = Color.FromArgb("#2F5233"),
                    ["TextPrimary"] = Colors.White,
                    ["TextSecondary"] = Color.FromArgb("#C0C0C0"),
                    ["TextAccent"] = Color.FromArgb("#C0C0C0"),
                    ["AccentGold"] = Color.FromArgb("#C0C0C0"),
                    ["AccentOrange"] = Color.FromArgb("#10B981"),
                    ["AccentGreen"] = Color.FromArgb("#10B981"),
                    ["AccentBlue"] = Color.FromArgb("#10B981"),
                    ["AccentRed"] = Color.FromArgb("#059669"),
                    ["AccentSecondary"] = Color.FromArgb("#9CA3AF"),
                    ["ButtonBackground"] = Color.FromArgb("#2F5233"),
                    ["ButtonBackgroundHover"] = Color.FromArgb("#3F6943"),
                    ["ButtonText"] = Colors.White
                },
                "Briselune" => new Dictionary<string, Color>
                {
                    ["BackgroundDark"] = Color.FromArgb("#0A0E1A"),
                    ["BackgroundCard"] = Color.FromArgb("#1E2A3A"),
                    ["BorderColor"] = Color.FromArgb("#1E3A8A"),
                    ["TextPrimary"] = Colors.White,
                    ["TextSecondary"] = Color.FromArgb("#C0C0C0"),
                    ["TextAccent"] = Color.FromArgb("#C0C0C0"),
                    ["AccentGold"] = Color.FromArgb("#C0C0C0"),
                    ["AccentOrange"] = Color.FromArgb("#3B82F6"),
                    ["AccentGreen"] = Color.FromArgb("#3B82F6"),
                    ["AccentBlue"] = Color.FromArgb("#3B82F6"),
                    ["AccentRed"] = Color.FromArgb("#1E40AF"),
                    ["AccentSecondary"] = Color.FromArgb("#94A3B8"),
                    ["ButtonBackground"] = Color.FromArgb("#1E3A8A"),
                    ["ButtonBackgroundHover"] = Color.FromArgb("#2E4A9A"),
                    ["ButtonText"] = Colors.White
                },
                "Rongebois" => new Dictionary<string, Color>
                {
                    ["BackgroundDark"] = Color.FromArgb("#1A1410"),
                    ["BackgroundCard"] = Color.FromArgb("#2A1F18"),
                    ["BorderColor"] = Color.FromArgb("#92400E"),
                    ["TextPrimary"] = Colors.White,
                    ["TextSecondary"] = Color.FromArgb("#D4D4D4"),
                    ["TextAccent"] = Color.FromArgb("#FCD34D"),
                    ["AccentGold"] = Color.FromArgb("#FCD34D"),
                    ["AccentOrange"] = Color.FromArgb("#F59E0B"),
                    ["AccentGreen"] = Color.FromArgb("#F59E0B"),
                    ["AccentBlue"] = Color.FromArgb("#F59E0B"),
                    ["AccentRed"] = Color.FromArgb("#92400E"),
                    ["AccentSecondary"] = Color.FromArgb("#FDE68A"),
                    ["ButtonBackground"] = Color.FromArgb("#92400E"),
                    ["ButtonBackgroundHover"] = Color.FromArgb("#A65020"),
                    ["ButtonText"] = Colors.White
                },
                _ => null
            };
        }

        public List<HouseInfo> GetAvailableHouses()
        {
            return new List<HouseInfo>
            {
                new HouseInfo 
                { 
                    Name = "Lombrasier", 
                    Icon = "🔥", 
                    Symbol = "Phoenix",
                    Description = "Rouge et noir avec or",
                    PrimaryColor = "#8B0000",
                    SecondaryColor = "#D4AF37"
                },
                new HouseInfo 
                { 
                    Name = "Vervenin", 
                    Icon = "🐍", 
                    Symbol = "Serpent",
                    Description = "Vert et noir avec argent",
                    PrimaryColor = "#2F5233",
                    SecondaryColor = "#C0C0C0"
                },
                new HouseInfo 
                { 
                    Name = "Briselune", 
                    Icon = "🦉", 
                    Symbol = "Hibou",
                    Description = "Bleu et argent",
                    PrimaryColor = "#1E3A8A",
                    SecondaryColor = "#C0C0C0"
                },
                new HouseInfo 
                { 
                    Name = "Rongebois", 
                    Icon = "🦫", 
                    Symbol = "Castor",
                    Description = "Jaune et marron",
                    PrimaryColor = "#92400E",
                    SecondaryColor = "#FCD34D"
                }
            };
        }
    }

    public class UserSettings
    {
        public string SelectedHouse { get; set; } = string.Empty;
    }

    public class HouseInfo
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Symbol { get; set; }
        public string Description { get; set; }
        public string PrimaryColor { get; set; }
        public string SecondaryColor { get; set; }
        public string DisplayName => $"{Icon} {Name}";
    }
}
