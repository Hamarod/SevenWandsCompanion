using SevenwandsCompanion.Services;

namespace SevenwandsCompanion
{
    public partial class SettingsPage : ContentPage
    {

        private readonly CarteService _carteService;

        public SettingsPage()
        {
            InitializeComponent();
            _carteService = new CarteService(ThemeService.Instance);
            LoadCurrentHouse();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadCurrentHouse();
        }

        private void LoadCurrentHouse()
        {
            // La maison actuelle est gérée par ThemeService
            // Plus besoin d'afficher un label ici
        }

        private async void OnChangeHouseClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//HouseSelection");
        }

        private async void OnGererCompetencesClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//Personnage");
        }

        private async void OnGenererCarteClicked(object sender, EventArgs e)
        {
            try
            {
                // Génération avec les données du personnage sauvegardées
                byte[] imageBytes = await _carteService.GenererCarteEtudiantAsync();

                // Sauvegarde et Partage
                await SauvegarderEtPartager(imageBytes);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Impossible de générer la carte : {ex.Message}", "OK");
            }
        }

        private async Task SauvegarderEtPartager(byte[] imageBytes)
        {
            // Chemin temporaire dans le cache de l'application
            string fileName = "MaCarteSevenWands.png";
            string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

            // Écriture du fichier sur le disque
            await File.WriteAllBytesAsync(filePath, imageBytes);

            // Ouverture de la fenêtre de partage native (Discord, Mail, Copier, etc.)
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Partager ma carte d'étudiant",
                File = new ShareFile(filePath)
            });
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
