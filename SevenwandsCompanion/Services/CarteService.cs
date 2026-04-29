using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Skia;

namespace SevenwandsCompanion.Services;

public class CarteService
{
    private readonly ThemeService _themeService;

    public CarteService(ThemeService themeService)
    {
        _themeService = themeService;
    }

    public async Task<byte[]> GenererCarteEtudiantAsync()
    {
        // Charger les données du personnage
        var competenceService = CompetenceService.Instance;
        await competenceService.ChargerCompetencesAsync();

        // Utiliser des valeurs par défaut si vides
        string prenom = string.IsNullOrWhiteSpace(competenceService.Prenom) ? "Étudiant" : competenceService.Prenom;
        string nom = string.IsNullOrWhiteSpace(competenceService.Nom) ? "Anonyme" : competenceService.Nom;
        int annee = competenceService.Annee > 0 && competenceService.Annee <= 7 ? competenceService.Annee : 1;
        string maisonNom = _themeService.CurrentHouse ?? "Lombrasier";

        System.Diagnostics.Debug.WriteLine($"🎨 Génération carte pour: {prenom} {nom}, Année {annee}, Maison {maisonNom}");

        // 1. Définition de la taille de la carte (Format standard 800x500)
        SkiaBitmapExportContext context = new SkiaBitmapExportContext(800, 500, 1.0f);
        ICanvas canvas = context.Canvas;

        // 2. Récupération des couleurs via le ThemeService
        Color couleurFond = _themeService.GetHousePrimaryColor(maisonNom);
        Color couleurAccent = _themeService.GetHouseSecondaryColor(maisonNom);

        // 3. DESSIN DU FOND (Vue de face, à plat)
        canvas.FillColor = couleurFond;
        canvas.FillRoundedRectangle(0, 0, 800, 500, 20); // Coins arrondis

        // 4. EFFET DE BRILLANCE (Glossy frontal)
        // On crée un dégradé radial pour simuler un reflet de lumière central
        RadialGradientPaint brillantEffect = new RadialGradientPaint
        {
            StartColor = Color.FromRgba(255, 255, 255, 0.3),
            EndColor = Colors.Transparent,
            Center = new Point(0.5, 0.2), // Reflet centré en haut
            Radius = 0.7
        };
        canvas.SetFillPaint(brillantEffect, new RectF(0, 0, 800, 500));
        canvas.FillRectangle(0, 0, 800, 500);

        // 5. CHARGEMENT DU LOGO (Depuis Resources/Raw)
        string logoFile = $"{maisonNom.ToLower()}_logo.png";
        try
        {
            //Charge l'image depuis Resources/Raw/{maisonNom.ToLower()}_logo.png
            //Note: FileSystem.OpenAppPackageFileAsync cherche dans Resources/Raw par défaut
            using (var stream = await FileSystem.OpenAppPackageFileAsync(logoFile))
            {
                var image = SkiaImage.FromStream(stream);
                canvas.DrawImage(image, 50, 50, 150, 150);
            }
        }
        catch (FileNotFoundException)
        {
            // Si le logo n'existe pas, on dessine un placeholder (première lettre de la maison)
            canvas.FillColor = couleurAccent.WithAlpha(0.3f);
            canvas.FillRoundedRectangle(50, 50, 150, 150, 10);

            canvas.FontColor = Colors.White;
            canvas.FontSize = 80;
            canvas.DrawString(maisonNom.Substring(0, 1).ToUpper(), 125, 140, HorizontalAlignment.Center);
        }

        // 6. TEXTES (Identité à l'horizontal)
        canvas.FontColor = Colors.White;
        canvas.FontSize = 50;
        canvas.DrawString($"{prenom.ToUpper()} {nom.ToUpper()}", 230, 130, HorizontalAlignment.Left);

        // Ligne de séparation élégante (Utilise ta couleur secondaire)
        canvas.StrokeColor = couleurAccent;
        canvas.StrokeSize = 4;
        canvas.DrawLine(50, 220, 750, 220);

        // Année scolaire
        canvas.FontSize = 35;
        canvas.DrawString($"{annee}EME ANNÉE", 50, 430, HorizontalAlignment.Left);

        // Afficher les jetons à droite de l'année
        await DessinerJetons(canvas, annee, couleurAccent);

        // 7. AFFICHAGE DES COMPÉTENCES
        await DessinerCompetences(canvas, couleurAccent);

        // 8. EXPORTATION
        using (MemoryStream ms = new MemoryStream())
        {
            context.WriteToStream(ms);
            return ms.ToArray();
        }
    }

    private async Task DessinerJetons(ICanvas canvas, int annee, Color couleurAccent)
    {
        try
        {
            // Charger les statistiques de jetons pour l'année
            var tokenService = TokenService.Instance;
            var stats = await tokenService.GetTokenStatsForYearAsync(annee);

            if (stats != null)
            {
                // Position à droite de l'année (après "4EME ANNÉE")
                float x = 320;
                float y = 430;

                // Dessiner le texte des jetons
                canvas.FontSize = 24;
                canvas.FontColor = couleurAccent;
                canvas.Font = Microsoft.Maui.Graphics.Font.Default;

                string jetonsText = $"Jetons {stats.CurrentPoints}/{stats.RequiredPoints} ({stats.CompletedCourses}/{stats.TotalCourses})";
                canvas.DrawString(jetonsText, x, y, HorizontalAlignment.Left);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Erreur affichage jetons sur carte: {ex.Message}");
        }
    }

    private async Task DessinerCompetences(ICanvas canvas, Color couleurAccent)
    {
        // Charger les compétences
        var competenceService = CompetenceService.Instance;
        await competenceService.ChargerCompetencesAsync();

        if (competenceService.Competences.Count == 0)
            return;

        // Position de départ pour les compétences (disposition horizontale)
        float startX = 50;
        float startY = 280;
        float spacingX = 140; // Espacement horizontal entre chaque compétence

        for (int i = 0; i < competenceService.Competences.Count; i++)
        {
            var competence = competenceService.Competences[i];
            float x = startX + (i * spacingX);

            // Dessiner la valeur en haut (grande et colorée)
            canvas.FontSize = 32;
            canvas.FontColor = Color.FromArgb(competence.Couleur);
            canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
            canvas.DrawString(competence.Valeur.ToString(), x + 35, startY, HorizontalAlignment.Center);

            // Dessiner le nom en dessous (blanc, plus petit)
            canvas.FontSize = 16;
            canvas.FontColor = Colors.White;
            canvas.Font = Microsoft.Maui.Graphics.Font.Default;

            // Tronquer le nom si trop long
            string nomAffiche = competence.Nom.Length > 9 ? competence.Nom.Substring(0, 9) : competence.Nom;
            canvas.DrawString(nomAffiche, x + 35, startY + 35, HorizontalAlignment.Center);
        }
    }
}