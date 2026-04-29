using System.Globalization;
using SevenwandsCompanion.Services;

namespace SevenwandsCompanion
{
    public partial class App : Application
    {
        private string _savedHouse = string.Empty;

        public App()
        {
            InitializeComponent();
            _ = InitializeAppAsync();
        }

        private async Task InitializeAppAsync()
        {
            // Charger la maison sauvegardée
            _savedHouse = await ThemeService.Instance.LoadSavedHouseAsync();

            if (!string.IsNullOrEmpty(_savedHouse))
            {
                // Appliquer le thème de la maison
                ThemeService.Instance.ApplyTheme(_savedHouse);
                System.Diagnostics.Debug.WriteLine($"✅ Theme loaded and applied: {_savedHouse}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ No house selected, will redirect to selection");
            }

            // Charger les compétences du personnage au démarrage de l'app
            await CompetenceService.Instance.ChargerCompetencesAsync();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = new AppShell();

            // Vérifier si une maison est sélectionnée pour déterminer la page de démarrage
            _ = Task.Run(async () =>
            {
                // Attendre que l'initialisation soit complète
                var startTime = DateTime.Now;
                while (string.IsNullOrEmpty(_savedHouse) && string.IsNullOrEmpty(ThemeService.Instance.CurrentHouse))
                {
                    await Task.Delay(50);
                    // Timeout après 2 secondes
                    if ((DateTime.Now - startTime).TotalSeconds > 2)
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ Timeout waiting for house to load");
                        break;
                    }
                }

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var currentHouse = ThemeService.Instance.CurrentHouse;

                    if (string.IsNullOrEmpty(currentHouse))
                    {
                        // Pas de maison sélectionnée, aller à la sélection
                        System.Diagnostics.Debug.WriteLine("🏰 Navigating to HouseSelection");
                        await shell.GoToAsync("//HouseSelection");
                    }
                    else
                    {
                        // Maison déjà sélectionnée, aller au suivi des jetons
                        System.Diagnostics.Debug.WriteLine($"📊 Navigating to TokenTracking with house: {currentHouse}");
                        await shell.GoToAsync("//TokenTracking");
                    }
                });
            });

            return new Window(shell);
        }
    }

    // Converter pour vérifier si un objet est non-null
    public class IsNotNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}