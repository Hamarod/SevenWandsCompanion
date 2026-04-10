using SevenwandsCompanion.Services;

namespace SevenwandsCompanion
{
    public partial class HouseSelectionPage : ContentPage
    {
        private string _selectedHouse;

        public HouseSelectionPage()
        {
            InitializeComponent();
            LoadHouses();
        }

        private void LoadHouses()
        {
            var houses = ThemeService.Instance.GetAvailableHouses();

            foreach (var house in houses)
            {
                var houseCard = CreateHouseCard(house);
                HousesContainer.Children.Add(houseCard);
            }
        }

        private Border CreateHouseCard(HouseInfo house)
        {
            var border = new Border
            {
                Style = Application.Current.Resources["CardStyle"] as Style,
                Padding = 20
            };

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => OnHouseSelected(house.Name, border);
            border.GestureRecognizers.Add(tapGesture);

            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 15
            };

            // Icône
            var icon = new Label
            {
                Text = house.Icon,
                FontSize = 40,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(icon, 0);

            // Informations
            var infoStack = new VerticalStackLayout
            {
                Spacing = 5,
                VerticalOptions = LayoutOptions.Center
            };

            var nameLabel = new Label
            {
                Text = house.Name,
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White
            };

            var symbolLabel = new Label
            {
                Text = $"Symbole : {house.Symbol}",
                FontSize = 14,
                TextColor = Application.Current.Resources["TextSecondary"] as Color
            };

            var descLabel = new Label
            {
                Text = house.Description,
                FontSize = 12,
                TextColor = Application.Current.Resources["TextSecondary"] as Color
            };

            infoStack.Children.Add(nameLabel);
            infoStack.Children.Add(symbolLabel);
            infoStack.Children.Add(descLabel);
            Grid.SetColumn(infoStack, 1);

            // Indicateur de sélection
            var checkIcon = new Label
            {
                Text = "✓",
                FontSize = 30,
                TextColor = Application.Current.Resources["AccentGold"] as Color,
                IsVisible = false,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(checkIcon, 2);

            grid.Children.Add(icon);
            grid.Children.Add(infoStack);
            grid.Children.Add(checkIcon);

            border.Content = grid;

            return border;
        }

        private void OnHouseSelected(string houseName, Border selectedCard)
        {
            _selectedHouse = houseName;

            // Désélectionner toutes les maisons
            foreach (var child in HousesContainer.Children)
            {
                if (child is Border border)
                {
                    var grid = border.Content as Grid;
                    var checkIcon = grid?.Children.OfType<Label>().FirstOrDefault(l => l.Text == "✓");
                    if (checkIcon != null)
                    {
                        checkIcon.IsVisible = false;
                    }
                    
                    border.Stroke = Application.Current.Resources["BorderColor"] as Color;
                    border.StrokeThickness = 1;
                }
            }

            // Sélectionner la maison choisie
            var selectedGrid = selectedCard.Content as Grid;
            var selectedCheck = selectedGrid?.Children.OfType<Label>().FirstOrDefault(l => l.Text == "✓");
            if (selectedCheck != null)
            {
                selectedCheck.IsVisible = true;
            }
            
            selectedCard.Stroke = Application.Current.Resources["AccentGold"] as Color;
            selectedCard.StrokeThickness = 3;

            // Activer le bouton continuer
            ContinueButton.IsEnabled = true;

            System.Diagnostics.Debug.WriteLine($"House selected: {houseName}");
        }

        private async void OnContinueClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedHouse))
                return;

            // Sauvegarder le choix
            await ThemeService.Instance.SaveHouseAsync(_selectedHouse);

            // Appliquer le thème
            ThemeService.Instance.ApplyTheme(_selectedHouse);

            // Naviguer vers la page principale
            await Shell.Current.GoToAsync("//TokenTracking");
        }
    }
}
