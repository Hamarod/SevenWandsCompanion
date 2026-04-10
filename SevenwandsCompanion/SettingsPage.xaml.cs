using SevenwandsCompanion.Services;

namespace SevenwandsCompanion
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();
            LoadCurrentHouse();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadCurrentHouse();
        }

        private void LoadCurrentHouse()
        {
            var currentHouse = ThemeService.Instance.CurrentHouse;
            
            if (!string.IsNullOrEmpty(currentHouse))
            {
                var houseInfo = ThemeService.Instance.GetAvailableHouses()
                    .FirstOrDefault(h => h.Name == currentHouse);

                if (houseInfo != null)
                {
                    CurrentHouseLabel.Text = $"Maison actuelle : {houseInfo.Icon} {houseInfo.Name}";
                }
            }
            else
            {
                CurrentHouseLabel.Text = "Maison actuelle : Aucune";
            }
        }

        private async void OnChangeHouseClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//HouseSelection");
        }

        private async void OnResetDataClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert(
                "⚠️ Confirmation",
                "Voulez-vous vraiment réinitialiser toutes les données ?\n\nCela supprimera :\n- Votre progression de jetons\n- Votre choix de maison\n- Tous vos paramètres\n\nCette action est irréversible !",
                "Oui, réinitialiser",
                "Annuler");

            if (confirm)
            {
                try
                {
                    // Supprimer le fichier de tracking
                    var trackingPath = Path.Combine(FileSystem.AppDataDirectory, "TokenTracking.json");
                    if (File.Exists(trackingPath))
                    {
                        File.Delete(trackingPath);
                    }

                    // Supprimer les paramètres
                    var settingsPath = Path.Combine(FileSystem.AppDataDirectory, "UserSettings.json");
                    if (File.Exists(settingsPath))
                    {
                        File.Delete(settingsPath);
                    }

                    await DisplayAlert("✅ Succès", "Toutes les données ont été réinitialisées.\n\nL'application va redémarrer.", "OK");

                    // Redémarrer l'application en retournant à la sélection de maison
                    await Shell.Current.GoToAsync("//HouseSelection");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("❌ Erreur", $"Impossible de réinitialiser : {ex.Message}", "OK");
                }
            }
        }
    }
}
