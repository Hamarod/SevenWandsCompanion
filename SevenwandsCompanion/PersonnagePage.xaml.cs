using SevenwandsCompanion.Services;

namespace SevenwandsCompanion
{
    public partial class PersonnagePage : ContentPage
    {
        private readonly CompetenceService _competenceService;

        public PersonnagePage()
        {
            InitializeComponent();
            _competenceService = CompetenceService.Instance;
            BindingContext = _competenceService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _competenceService.ChargerCompetencesAsync();
        }

        private async void OnAugmenterClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is CompetenceViewModel competence)
            {
                competence.Valeur++;
                await _competenceService.SauvegarderCompetencesAsync();
            }
        }

        private async void OnAugmenter10Clicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is CompetenceViewModel competence)
            {
                competence.Valeur += 10;
                await _competenceService.SauvegarderCompetencesAsync();
            }
        }

        private async void OnDiminuerClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is CompetenceViewModel competence)
            {
                if (competence.Valeur > 0)
                {
                    competence.Valeur--;
                    await _competenceService.SauvegarderCompetencesAsync();
                }
            }
        }

        private async void OnDiminuer10Clicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is CompetenceViewModel competence)
            {
                competence.Valeur = Math.Max(competence.Valeur - 10, 0);
                await _competenceService.SauvegarderCompetencesAsync();
            }
        }

        private async void OnIdentiteChanged(object sender, TextChangedEventArgs e)
        {
            await _competenceService.SauvegarderCompetencesAsync();
        }
    }
}
