using SevenwandsConsoleTool;

namespace SevenwandsCompanion
{
    public partial class AppShell : Shell
    {
        private const string TokenTrackingAssetPath = "TokenTracking.json";

        public AppShell()
        {
            InitializeComponent();

            // Enregistrer les routes d'édition (n'apparaissent pas dans le menu)
            Routing.RegisterRoute("PotionEditor", typeof(PotionEditorPage));
            Routing.RegisterRoute("IngredientEditor", typeof(IngredientEditorPage));
            Routing.RegisterRoute("ProductsManager", typeof(ProductsManagerPage));
            Routing.RegisterRoute("ResourcesManager", typeof(ResourcesManagerPage));
            Routing.RegisterRoute("OrdersManager", typeof(OrdersManagerPage));

            // S'abonner à l'événement Navigated pour cacher le Flyout sur HouseSelection
            this.Navigated += OnShellNavigated;

            // Si la scolarité (7ème année) est déjà terminée, "Suivi des Jetons" n'a plus
            // besoin d'être en tête de menu : on le déplace tout en bas, juste avant Paramètres.
            _ = ReorderMenuIfGraduatedAsync();
        }

        private async Task ReorderMenuIfGraduatedAsync()
        {
            if (await IsScolariteFinishedAsync())
            {
                MoveTokenTrackingItemBeforeSettings();
            }
        }

        private static async Task<bool> IsScolariteFinishedAsync()
        {
            try
            {
                string path = Path.Combine(FileSystem.AppDataDirectory, TokenTrackingAssetPath);
                if (!File.Exists(path)) return false;

                string json = await File.ReadAllTextAsync(path);
                var years = SevenwandsTools.DeserializeTokenTracking(json);
                return years.Any(y => y.Year == TokenTrackingPage.FinalYearNumber && y.IsYearCompleted);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IsScolariteFinishedAsync error: {ex.Message}");
                return false;
            }
        }

        private void MoveTokenTrackingItemBeforeSettings()
        {
            if (!Items.Contains(TokenTrackingFlyoutItem) || !Items.Contains(ParametresFlyoutItem))
                return;

            Items.Remove(TokenTrackingFlyoutItem);
            int settingsIndex = Items.IndexOf(ParametresFlyoutItem);
            Items.Insert(settingsIndex, TokenTrackingFlyoutItem);
        }

        private void OnShellNavigated(object sender, ShellNavigatedEventArgs e)
        {
            // Cacher le menu Flyout sur la page de sélection de maison
            if (e.Current?.Location?.OriginalString?.Contains("HouseSelection") == true)
            {
                FlyoutBehavior = FlyoutBehavior.Disabled;
            }
            else
            {
                FlyoutBehavior = FlyoutBehavior.Flyout;
            }
        }
    }
}
