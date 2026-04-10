namespace SevenwandsCompanion
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Enregistrer les routes d'édition (n'apparaissent pas dans le menu)
            Routing.RegisterRoute("PotionEditor", typeof(PotionEditorPage));
            Routing.RegisterRoute("IngredientEditor", typeof(IngredientEditorPage));

            // S'abonner à l'événement Navigated pour cacher le Flyout sur HouseSelection
            this.Navigated += OnShellNavigated;
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
