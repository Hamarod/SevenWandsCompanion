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
        }
    }
}
