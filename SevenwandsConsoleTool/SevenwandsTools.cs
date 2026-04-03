using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SevenwandsConsoleTool
{
    public class SevenwandsTools
    {
        #region Méthodes de traitement des données

        /// <summary>
        /// Calcule le coût et les bénéfices pour chaque potion
        /// </summary>
        //static void CalculateAllPotionsCosts(List<Potion> potions, Dictionary<int, Ingredient> ingredients)
        //{
        //    foreach (var potion in potions)
        //    {
        //        potion.Cost = potion.Recipe
        //            .Where(r => ingredients.ContainsKey(r.IngredientId))
        //            .Sum(r => (ingredients[r.IngredientId].Price ?? 0) * r.Quantity);

        //        potion.Benefits = potion.SellPrice - (potion.Cost ?? 0);
        //    }
        //}

        /// <summary>
        /// Calcule le coût et les bénéfices pour une quantité donnée de potions
        /// </summary>
        public static Potion CalculatePotionCosts(Potion potion, Dictionary<int, Ingredient> ingredients, int quantity)
        {
            // (Code existant conservé)
            if (ingredients == null) throw new ArgumentNullException(nameof(ingredients));

            float totalCost = potion.Recipe
                .Where(r => ingredients.ContainsKey(r.IngredientId) && ingredients[r.IngredientId].Price.HasValue)
                .Sum(r => (ingredients[r.IngredientId].Price ?? 0) * r.Quantity);

            potion.Cost = totalCost;
            potion.Benefits = (potion.SellPrice * quantity) - totalCost;

            return potion;
        }

        public static int CalculatePotionExperience(Potion potion, int quantity)
        {
            return (potion.Experience ?? 0) * quantity;
        }
        // ----------------------------------------------

        /// <summary>
        /// Clacule le nombre de potions nécessaires pour atteindre un bénéfice de 0 par rapport au nombre de potions voulu
        /// et retourne les données d'analyse.
        /// </summary>
        public static async Task<BreakEvenData?> CalculeZeroBenefits(Potion potion, string ingredientsFilePath, int stockEauPurete)
        {
            var ingredients = await LoadIngredientsFromJson(ingredientsFilePath);
            if (!ingredients.Values.Any(i => i.Id == IngredientIds.EauPurete && i.Price.HasValue))
            {
                Console.WriteLine("Le prix de l'eau de pureté n'est pas défini dans les ingrédients.");
                return null;
            }

            float eauPuretePrice = ingredients[IngredientIds.EauPurete].Price.Value;

            // Calculer combien d'eau de pureté nécessaire pour 1 potion
            var eauPuretePerPotion = potion.Recipe
                .FirstOrDefault(r => r.IngredientId == IngredientIds.EauPurete)?.Quantity ?? 0;

            if (eauPuretePerPotion == 0)
            {
                Console.WriteLine($"La potion {potion.Name} ne nécessite pas d'eau de pureté.");
                return null;
            }

            // Combien de potions maximum peut-on fabriquer avec le stock d'eaux ?
            int maxPotionsFabricables = stockEauPurete / eauPuretePerPotion;

            if (maxPotionsFabricables == 0)
            {
                Console.WriteLine($"⚠️ Vous n'avez pas assez d'eau de pureté. Il en faut {eauPuretePerPotion} pour 1 potion.");
                return new BreakEvenData(stockEauPurete, 0, eauPuretePerPotion, 0, 0, potion.SellPrice, 0, 0, 0, stockEauPurete, 0, 0, false, false);
            }

            // Coût total du stock d'eaux de pureté
            float totalInvestissement = stockEauPurete * eauPuretePrice;

            // Calculer le coût total pour 1 potion (avec tous les ingrédients)
            float costPerPotion = potion.Recipe
                .Where(r => ingredients.ContainsKey(r.IngredientId) && ingredients[r.IngredientId].Price.HasValue)
                .Sum(r => ingredients[r.IngredientId].Price.Value * r.Quantity);

            // Calculer le bénéfice par potion
            float benefitPerPotion = potion.SellPrice - costPerPotion;

            if (benefitPerPotion <= 0)
            {
                Console.WriteLine($"❌ La potion {potion.Name} n'est jamais rentable (coût: {costPerPotion:F2}, vente: {potion.SellPrice:F2}).");
                return new BreakEvenData(stockEauPurete, totalInvestissement, eauPuretePerPotion, maxPotionsFabricables, costPerPotion, potion.SellPrice, benefitPerPotion, 0, 0, stockEauPurete, 0, 0, false, false);
            }

            // ✅ CORRECTION : diviser par benefitPerPotion au lieu de potion.SellPrice
            int potionsMinPourBreakEven = (int)Math.Ceiling(totalInvestissement / potion.SellPrice);

            // Eaux de pureté utilisées pour atteindre le break-even
            int eauxUtiliseesBreakEven = potionsMinPourBreakEven * eauPuretePerPotion;

            // Eaux de pureté restantes après break-even
            int eauxRestantes = stockEauPurete - eauxUtiliseesBreakEven;

            // Potions supplémentaires fabricables avec le reste
            int potionsSupplementaires = Math.Max(0, eauxRestantes / eauPuretePerPotion);

            bool isPossible = potionsMinPourBreakEven <= maxPotionsFabricables;

            Console.WriteLine($"\n💰 Analyse de rentabilité avec {stockEauPurete} eaux de pureté en stock:");
            Console.WriteLine($"   • Investissement total en eaux: {totalInvestissement:F2} Galyons");
            Console.WriteLine($"   • Eau de pureté par potion: {eauPuretePerPotion}");
            Console.WriteLine($"   • Potions max fabricables: {maxPotionsFabricables}");
            Console.WriteLine($"   • Coût par potion: {costPerPotion:F2} Galyons");
            Console.WriteLine($"   • Prix de vente: {potion.SellPrice:F2} Galyons");
            Console.WriteLine($"   • Bénéfice par potion: {benefitPerPotion:F2} Galyons");
            Console.WriteLine();
            Console.WriteLine($"   🎯 BREAK-EVEN:");
            Console.WriteLine($"      ✅ Il faut vendre {potionsMinPourBreakEven} potion(s) pour atteindre 0 bénéfice");
            Console.WriteLine($"      📊 Eaux utilisées: {eauxUtiliseesBreakEven} / {stockEauPurete}");

            if (!isPossible)
            {
                Console.WriteLine($"      ⚠️ IMPOSSIBLE ! Vous ne pouvez fabriquer que {maxPotionsFabricables} potions.");
                int eauxManquantes = eauxUtiliseesBreakEven - stockEauPurete;
                Console.WriteLine($"      ⚠️ Il vous manque {eauxManquantes} eaux de pureté pour le break-even.");
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine($"   🎁 APRÈS BREAK-EVEN:");
                Console.WriteLine($"      • Eaux de pureté restantes: {eauxRestantes}");
                Console.WriteLine($"      • Potions supplémentaires fabricables: {potionsSupplementaires}");

                if (potionsSupplementaires > 0)
                {
                    float beneficeSupplementaire = potionsSupplementaires * benefitPerPotion;
                    Console.WriteLine($"      • Bénéfice supplémentaire potentiel: {beneficeSupplementaire:F2} Galyons");
                    Console.WriteLine($"      ✨ Ces {potionsSupplementaires} potions sont 'gratuites' (eaux déjà amorties)");
                }
            }

            return new BreakEvenData(
                StockEauPurete: stockEauPurete,
                TotalInvestissement: totalInvestissement,
                EauPuretePerPotion: eauPuretePerPotion,
                MaxPotionsFabricables: maxPotionsFabricables,
                CostPerPotion: costPerPotion,
                SellPrice: potion.SellPrice,
                BenefitPerPotion: benefitPerPotion,
                PotionsMinPourBreakEven: potionsMinPourBreakEven,
                EauxUtiliseesBreakEven: eauxUtiliseesBreakEven,
                EauxRestantes: eauxRestantes,
                PotionsSupplementaires: potionsSupplementaires,
                BeneficeSupplementaire: potionsSupplementaires > 0 ? potionsSupplementaires * benefitPerPotion : 0,
                IsRentable: true,
                IsPossible: isPossible
            );
        }

        #endregion

        #region Méthodes de sérialisation JSON

        /// <summary>
        /// Crée les options de sérialisation JSON configurées
        /// </summary>
        static JsonSerializerOptions CreateJsonOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
        }

        /// <summary>
        /// Sauvegarde les ingrédients dans un fichier JSON
        /// </summary>
        public static async Task SaveIngredientsToJson(string filePath, Dictionary<int, Ingredient>? ingredients = null)
        {
            ingredients ??= Ingredient.GetDefaultIngredients();

            var options = CreateJsonOptions();
            var ingredientsJson = JsonSerializer.Serialize(
                ingredients.Values.OrderBy(i => i.Id),
                options
            );
            await File.WriteAllTextAsync(filePath, ingredientsJson);
            Console.WriteLine("✅ Ingredients.json généré");
        }

        /// <summary>
        /// Sauvegarde les potions dans un fichier JSON
        /// </summary>
        public static async Task SavePotionsToJson(string filePath, List<Potion>? potions = null)
        {
            potions ??= Potion.GetDefaultPotions();

            var options = CreateJsonOptions();
            var potionsJson = JsonSerializer.Serialize(
                potions.OrderByDescending(p => p.Benefits),
                options
            );
            await File.WriteAllTextAsync(filePath, potionsJson);
            Console.WriteLine("✅ Potions.json généré");
        }

        /// <summary>
        /// Désérialise les ingrédients depuis une chaîne JSON
        /// </summary>
        public static Dictionary<int, Ingredient> DeserializeIngredients(string json)
        {
            var ingredientsList = JsonSerializer.Deserialize<List<Ingredient>>(json);
            return ingredientsList?.ToDictionary(i => i.Id) ?? new Dictionary<int, Ingredient>();
        }

        /// <summary>
        /// Charge les ingrédients depuis un fichier JSON
        /// </summary>
        public static async Task<Dictionary<int, Ingredient>> LoadIngredientsFromJson(string filePath)
        {
            var json = await File.ReadAllTextAsync(filePath);
            return DeserializeIngredients(json);
        }

        /// <summary>
        /// Désérialise les potions depuis une chaîne JSON
        /// </summary>
        public static List<Potion> DeserializePotions(string json)
        {
            return JsonSerializer.Deserialize<List<Potion>>(json) ?? new List<Potion>();
        }

        /// <summary>
        /// Charge les potions depuis un fichier JSON
        /// </summary>
        public static List<Potion> LoadPotionsFromJson(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return DeserializePotions(json);
        }

        /// <summary>
        /// Désérialise les données de suivi des jetons depuis une chaîne JSON
        /// </summary>
        public static List<YearData> DeserializeTokenTracking(string json)
        {
            return JsonSerializer.Deserialize<List<YearData>>(json) ?? new List<YearData>();
        }

        /// <summary>
        /// Charge les données de suivi des jetons depuis un fichier JSON
        /// </summary>
        public static async Task<List<YearData>> LoadTokenTrackingFromJson(string filePath)
        {
            var json = await File.ReadAllTextAsync(filePath);
            return DeserializeTokenTracking(json);
        }

        /// <summary>
        /// Sauvegarde les données de suivi des jetons dans un fichier JSON
        /// </summary>
        public static async Task SaveTokenTrackingToJson(string filePath, List<YearData> years)
        {
            var options = CreateJsonOptions();
            var json = JsonSerializer.Serialize(years, options);
            await File.WriteAllTextAsync(filePath, json);
            Console.WriteLine("✅ TokenTracking.json sauvegardé");
        }

        #endregion

        #region Méthodes d'affichage

        /// <summary>
        /// Affiche un résumé du nombre d'ingrédients et de potions
        /// </summary>
        public static void DisplaySummary(Dictionary<int, Ingredient>? ingredients = null, List<Potion>? potions = null)
        {
            ingredients ??= Ingredient.GetDefaultIngredients();
            potions ??= Potion.GetDefaultPotions();

            Console.WriteLine($"\n📦 {ingredients.Count} ingrédients créés");
            Console.WriteLine($"🧪 {potions.Count} potions créées\n");
        }

        /// <summary>
        /// Affiche les détails de toutes les potions avec leurs recettes
        /// </summary>
        public static void DisplayPotionRecipes(List<Potion>? potions = null, Dictionary<int, Ingredient>? ingredients = null)
        {
            potions ??= Potion.GetDefaultPotions();
            ingredients ??= Ingredient.GetDefaultIngredients();

            foreach (var potion in potions)
            {
                Console.WriteLine($"🧪 {potion.Name} ({potion.Category}) - Niveau {potion.MinimumLevel}");
                Console.WriteLine($"   💰 Prix de vente: {potion.SellPrice} pièces d'or");
                Console.WriteLine($"   📜 Recette:");
                foreach (var recipeItem in potion.Recipe)
                {
                    if (ingredients.TryGetValue(recipeItem.IngredientId, out var ingredient))
                    {
                        Console.WriteLine($"      - {recipeItem.Quantity}x {ingredient.Name}");
                    }
                    else
                    {
                        Console.WriteLine($"      ⚠️ Ingrédient {recipeItem.IngredientId} introuvable!");
                    }
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Affiche les ingrédients nécessaires pour fabriquer un nombre donné de potions
        /// </summary>
        public static async Task<List<Ingredient>> DisplayIngredientNeeds(Potion potion, string ingredientsFilePath, int quantity)
        {
            var ingredients = await LoadIngredientsFromJson(ingredientsFilePath);
            return DisplayIngredientNeeds(potion, ingredients, quantity);
        }

        /// <summary>
        /// Surcharge acceptant directement le dictionnaire d'ingrédients
        /// </summary>
        public static List<Ingredient> DisplayIngredientNeeds(Potion potion, Dictionary<int, Ingredient> ingredients, int quantity)
        {
            Console.WriteLine($"Pour fabriquer {quantity} potions de {potion.Name}, il vous faut:");
            foreach (var recipeItem in potion.Recipe)
            {
                if (ingredients.TryGetValue(recipeItem.IngredientId, out var ingredient))
                {
                    Console.WriteLine($"- {recipeItem.Quantity}x {ingredient.Name}");
                }
            }

            var ingredientsNeed = potion.CalculIngredientNeeded(quantity, ingredients).Recipe
                .Where(r => ingredients.ContainsKey(r.IngredientId))
                .Select(r => new Ingredient(
                    id: r.IngredientId,
                    name: $"{r.Quantity}x {ingredients[r.IngredientId].Name}",
                    type: ingredients[r.IngredientId].Type,
                    description: ingredients[r.IngredientId].Description,
                    price: ingredients[r.IngredientId].Price
                ))
                .ToList();

            return ingredientsNeed;
        }

        public static async Task DisplayPotions(string potionFilePath)
        {
            var potions = LoadPotionsFromJson(potionFilePath);
            foreach (var potion in potions)
            {
                Console.WriteLine($"Potion ID: {potion.Id}, Name: {potion.Name}");
            }
        }

        #endregion


    }
    #region Classes et Enums
    /// <summary>
    /// Contient les données calculées pour l'analyse de rentabilité
    /// </summary>
    public record BreakEvenData(
        int StockEauPurete,
        float TotalInvestissement,
        int EauPuretePerPotion,
        int MaxPotionsFabricables,
        float CostPerPotion,
        float SellPrice,
        float BenefitPerPotion,
        int PotionsMinPourBreakEven,
        int EauxUtiliseesBreakEven,
        int EauxRestantes,
        int PotionsSupplementaires,
        float BeneficeSupplementaire,
        bool IsRentable,
        bool IsPossible
    );
    public class Ingredient
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("type")]
        public IngredientType Type { get; set; }

        [JsonPropertyName("price")]
        public float? Price { get; set; }

        public Ingredient(int id, string name, IngredientType type, string description = "", float? price = null)
        {
            Id = id;
            Name = name;
            Description = description;
            Type = type;
            Price = price;
        }

        // Implémentation de Equals et GetHashCode pour permettre la comparaison par ID
        // Nécessaire pour le binding SelectedItem du Picker en MAUI
        public override bool Equals(object? obj)
        {
            if (obj is Ingredient other)
            {
                return Id == other.Id;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Retourne la liste par défaut des ingrédients
        /// </summary>
        public static Dictionary<int, Ingredient> GetDefaultIngredients()
        {
            return new Dictionary<int, Ingredient>
            {
                [IngredientIds.FeuEteint] = new(IngredientIds.FeuEteint, "Feu éteint", IngredientType.Fire, "Cendre froide"),
                [IngredientIds.Feufaible] = new(IngredientIds.Feufaible, "Feu faible", IngredientType.Fire, "Cendre tiède"),
                [IngredientIds.FeuMoyen] = new(IngredientIds.FeuMoyen, "Feu moyen", IngredientType.Fire, "Cendre chaude"),
                [IngredientIds.FeuFort] = new(IngredientIds.FeuFort, "Feu fort", IngredientType.Fire, "Cendre brûlante"),
                [IngredientIds.FeuTresFort] = new(IngredientIds.FeuTresFort, "Feu très fort", IngredientType.Fire, "Cendre incandescente"),
                [IngredientIds.sensHoraire] = new(IngredientIds.sensHoraire, "Sens horaire", IngredientType.rotate, "Tourner le mortier dans le sens des aiguilles d'une montre"),
                [IngredientIds.sensAntiHoraire] = new(IngredientIds.sensAntiHoraire, "Sens antihoraire", IngredientType.rotate, "Tourner le mortier dans le sens inverse des aiguilles d'une montre"),
                [IngredientIds.EauTristesse] = new(IngredientIds.EauTristesse, "Eau de tristesse", IngredientType.ingredient, "Larmes cristallisées"),
                [IngredientIds.EauPurete] = new(IngredientIds.EauPurete, "Eau de pureté", IngredientType.ingredient, "réculté directement dans les puits", 250.0f),
                [IngredientIds.BaieMandragore] = new(IngredientIds.BaieMandragore, "Baie de mandragore", IngredientType.ingredient, "Fruit rare aux propriétés magiques"),
                [IngredientIds.FleurSilmera] = new(IngredientIds.FleurSilmera, "Fleur de Silmera", IngredientType.ingredient, "Fleur délicate des montagnes"),
                [IngredientIds.Rosimence] = new(IngredientIds.Rosimence, "Rosimence", IngredientType.ingredient, "Plante aquatique purifiante"),
                [IngredientIds.Melorine] = new(IngredientIds.Melorine, "Mélorine", IngredientType.ingredient, "Herbe ignifuge aux pétales rouges", 15.0f),
                [IngredientIds.PoudreCornlongue] = new(IngredientIds.PoudreCornlongue, "Poudre de Cornlongue", IngredientType.ingredient, "Poudre volatile extraite de corne", 15.0f),
                [IngredientIds.RacineHerbecorce] = new(IngredientIds.RacineHerbecorce, "Racine d'Herbécorce", IngredientType.ingredient, "Racine ligneuse aux vertus fortifiantes"),
                [IngredientIds.SouffreErochevice] = new(IngredientIds.SouffreErochevice, "Souffre d'érochevisse", IngredientType.ingredient, "Cristaux soufrés de créature marine"),
                [IngredientIds.FilamentLumineux] = new(IngredientIds.FilamentLumineux, "Filament lumineux", IngredientType.ingredient, "Fibre phosphorescente rare"),
                [IngredientIds.GraineSoliviane] = new(IngredientIds.GraineSoliviane, "Graine de Soliviane", IngredientType.ingredient, "Graine énergétique du désert"),
                [IngredientIds.RocheLueurLune] = new(IngredientIds.RocheLueurLune, "Roche de lueur de lune", IngredientType.ingredient, "Pierre imprégnée de magie lunaire"),
                [IngredientIds.Larmelite] = new(IngredientIds.Larmelite, "Larmélite", IngredientType.ingredient, "Cristal qu'on trouve au fond du lac de bourg le saoul"),
                [IngredientIds.CristalMagique] = new(IngredientIds.CristalMagique, "Cristal magique", IngredientType.ingredient, ""),
                [IngredientIds.FleurVoile] = new(IngredientIds.FleurVoile, "Fleur du voile", IngredientType.ingredient, ""),
                [IngredientIds.NectarVoile] = new(IngredientIds.NectarVoile, "Nectar du voile", IngredientType.ingredient, ""),
                [IngredientIds.PlumeMiragel] = new(IngredientIds.PlumeMiragel, "Plume de miragel", IngredientType.ingredient, ""),
                [IngredientIds.EncreMiragel] = new(IngredientIds.EncreMiragel, "Encre de Miragel", IngredientType.ingredient, ""),
                [IngredientIds.ElixiremSanationis] = new(IngredientIds.ElixiremSanationis, "Elixirem Sanationis", IngredientType.spell, ""),
                [IngredientIds.MensClara] = new(IngredientIds.MensClara, "Mens Clara", IngredientType.spell, ""),
                [IngredientIds.AfflatusMaritis] = new(IngredientIds.AfflatusMaritis, "Afflatus maritis", IngredientType.spell, ""),
                [IngredientIds.AmissioCapillorum] = new(IngredientIds.AmissioCapillorum, "Amissio capillorum", IngredientType.spell, ""),
                [IngredientIds.FolliculiRenascuntur] = new(IngredientIds.FolliculiRenascuntur, "Folliculi renascuntur", IngredientType.spell, ""),
                [IngredientIds.AscensusFulguris] = new(IngredientIds.AscensusFulguris, "Ascensus fulguris", IngredientType.spell, ""),
                [IngredientIds.ScintillaMagica] = new(IngredientIds.ScintillaMagica, "Scintilla magica", IngredientType.spell, ""),
                [IngredientIds.FulmenAetheris] = new(IngredientIds.FulmenAetheris, "Fulmen aetheris", IngredientType.spell, ""),
                [IngredientIds.CorpusOccultum] = new(IngredientIds.CorpusOccultum, "Corpus occultum", IngredientType.spell, ""),
            };
        }
    }

    public enum IngredientType
    {
        Fire,
        ingredient,
        rotate,
        spell
    }

    // Classe pour les ingrédients dans une recette
    public class RecipeIngredient
    {
        [JsonPropertyName("ingredient_id")]
        public int IngredientId { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        public RecipeIngredient(int ingredientId, int quantity)
        {
            IngredientId = ingredientId;
            Quantity = quantity;
        }

        public RecipeIngredient() { }
    }

    // Classe Potion
    public class Potion
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("minimum_level")]
        public int MinimumLevel { get; set; }

        [JsonPropertyName("cost")]
        public float? Cost { get; set; }

        [JsonPropertyName("experience")]
        public int? Experience { get; set; }

        [JsonPropertyName("sell_price")]
        public float SellPrice { get; set; }

        [JsonPropertyName("benefits")]
        public float Benefits { get; set; }

        [JsonPropertyName("order")]
        public int Order { get; set; }

        [JsonPropertyName("recipe")]
        public List<RecipeIngredient> Recipe { get; set; }

        public Potion()
        {
            Recipe = new List<RecipeIngredient>();
        }

        public Potion CalculIngredientNeeded(int numberOfPotions, Dictionary<int, Ingredient> ingredients)
        {
            var totalIngredients = new Potion
            {
                Id = this.Id,
                Name = this.Name,
                Description = this.Description,
                Category = this.Category,
                MinimumLevel = this.MinimumLevel,
                SellPrice = this.SellPrice,
                Experience = this.Experience, // --- AJOUT : Copie de l'expérience de base ---
                Order = this.Order,
                Recipe = this.Recipe
                    .Select(r =>
                    {
                        if (ingredients.TryGetValue(r.IngredientId, out var ingredient) &&
                            ingredient.Type == IngredientType.ingredient)
                        {
                            return new RecipeIngredient(r.IngredientId, r.Quantity * numberOfPotions);
                        }
                        return new RecipeIngredient(r.IngredientId, r.Quantity);
                    })
                    .ToList()
            };
            return totalIngredients;
        }

        /// <summary>
        /// Retourne la liste par défaut des potions
        /// </summary>
        public static List<Potion> GetDefaultPotions()
        {
            return new List<Potion>
        {
            new()
            {
                Id = 1,
                Name = "Nanis jouvencine",
                Description = "Potion curative faible pour débutant",
                Category = "Curative",
                MinimumLevel = 1,
                SellPrice = 0.0f,
                Experience = 10,
                Order = 10,
                Recipe = new List<RecipeIngredient>
                {
                    new(IngredientIds.Feufaible, 1),
                    new(IngredientIds.EauTristesse, 1),
                    new(IngredientIds.sensHoraire, 5),
                    new(IngredientIds.BaieMandragore, 1),
                    new(IngredientIds.sensAntiHoraire, 3),
                    new(IngredientIds.FleurSilmera, 1),
                    new(IngredientIds.sensAntiHoraire, 3),
                    new(IngredientIds.FeuFort, 1),
                    new(IngredientIds.ElixiremSanationis, 1),
                }
            },
            new()
            {
                Id = 2,
                Name = "Jouvencine",
                Description = "Potion curative neutre pour débutant",
                Category = "Curative",
                MinimumLevel = 18,
                SellPrice = 1533.0f,
                Experience = 39,
                Order = 20,
                Recipe = new List<RecipeIngredient>
                {
                    new(IngredientIds.Feufaible, 1),
                    new(IngredientIds.EauPurete, 5),
                    new(IngredientIds.sensHoraire, 5),
                    new(IngredientIds.BaieMandragore, 5),
                    new(IngredientIds.sensAntiHoraire, 3),
                    new(IngredientIds.FleurSilmera, 5),
                    new(IngredientIds.sensAntiHoraire, 3),
                    new(IngredientIds.FeuFort, 1),
                    new(IngredientIds.ElixiremSanationis, 1),
                }
            },
            new()
            {
                Id = 3,
                Name = "Magna Jouvencine",
                Description = "Potion curative élevé pour niveau intermédiaire",
                Category = "Curative",
                MinimumLevel = 28,
                SellPrice = 3033.0f,
                Experience = 73,
                Order = 30,
                Recipe = new List<RecipeIngredient>
                {
                    new(IngredientIds.Feufaible, 1),
                    new(IngredientIds.EauPurete, 10),
                    new(IngredientIds.sensHoraire, 5),
                    new(IngredientIds.BaieMandragore, 10),
                    new(IngredientIds.sensAntiHoraire, 3),
                    new(IngredientIds.FleurSilmera, 10),
                    new(IngredientIds.sensAntiHoraire, 3),
                    new(IngredientIds.FeuFort, 1),
                    new(IngredientIds.ElixiremSanationis, 1),
                }
            },
            new()
            {
                Id = 4,
                Name = "Nanis Esprit clair",
                Description = "éclaircie l'esprit",
                Category = "Curative",
                MinimumLevel = 1,
                SellPrice = 0.0f,
                Order = 40,
                Recipe = new List<RecipeIngredient>
                {
                    new(IngredientIds.FeuMoyen, 1),
                    new(IngredientIds.EauTristesse, 1),
                    new(IngredientIds.sensHoraire, 2),
                    new(IngredientIds.FleurSilmera, 1),
                    new(IngredientIds.sensHoraire, 3),
                    new(IngredientIds.Rosimence, 1),
                    new(IngredientIds.sensHoraire, 5),
                    new(IngredientIds.Feufaible, 1),
                    new(IngredientIds.MensClara, 1),
                }
            },
            new()
            {
                Id = 5,
                Name = "Esprit clair",
                Description = "éclaircie l'esprit",
                Category = "Curative",
                MinimumLevel = 18,
                SellPrice = 1269.0f,
                Experience = 37,
                Order = 40,
                Recipe = new List<RecipeIngredient>
                {
                    new(IngredientIds.FeuMoyen, 1),
                    new(IngredientIds.EauPurete, 4),
                    new(IngredientIds.sensHoraire, 2),
                    new(IngredientIds.FleurSilmera, 5),
                    new(IngredientIds.sensHoraire, 3),
                    new(IngredientIds.Rosimence, 5),
                    new(IngredientIds.sensHoraire, 5),
                    new(IngredientIds.Feufaible, 1),
                    new(IngredientIds.MensClara, 1),
                }
            },
            new()
            {
                Id = 6,
                Name = "Magna Esprit clair",
                Description = "éclaircie l'esprit",
                Category = "Curative",
                MinimumLevel = 28,
                SellPrice = 2504.0f,
                Experience = 69,
                Order = 40,
                Recipe = new List<RecipeIngredient>
                {
                    new(IngredientIds.FeuMoyen, 1),
                    new(IngredientIds.EauPurete, 8),
                    new(IngredientIds.sensHoraire, 2),
                    new(IngredientIds.FleurSilmera, 10),
                    new(IngredientIds.sensHoraire, 3),
                    new(IngredientIds.Rosimence, 10),
                    new(IngredientIds.sensHoraire, 5),
                    new(IngredientIds.Feufaible, 1),
                    new(IngredientIds.MensClara, 1),
                }
            },
            new()
            {
                Id = 7,
                Name = "Nanis Humécume",
                Description = "Respirer sous l'eau",
                Category = "Alteration",
                MinimumLevel = 1,
                SellPrice = 0.0f,
                Experience = 15,
                Order = 80,
                Recipe = new List<RecipeIngredient>
                {
                    new(IngredientIds.FeuFort, 1),
                    new(IngredientIds.EauTristesse, 1),
                    new(IngredientIds.sensAntiHoraire, 5),
                    new(IngredientIds.RacineHerbecorce, 1),
                    new(IngredientIds.sensHoraire, 3),
                    new(IngredientIds.Feufaible, 1),
                    new(IngredientIds.Melorine, 1),
                    new(IngredientIds.sensAntiHoraire, 5),
                    new(IngredientIds.FeuFort, 1),
                    new(IngredientIds.AfflatusMaritis, 1),
                }
            },
            new()
            {
                Id = 8,
                Name = "Humécume",
                Description = "Respirer sous l'eau",
                Category = "Alteration",
                MinimumLevel = 35,
                SellPrice = 2742.0f,
                Experience = 30,
                Order = 90,
                Recipe = new List<RecipeIngredient>
                {
                    new(IngredientIds.FeuFort, 1),
                    new(IngredientIds.EauPurete, 10),
                    new(IngredientIds.sensAntiHoraire, 5),
                    new(IngredientIds.RacineHerbecorce, 1),
                    new(IngredientIds.sensHoraire, 3),
                    new(IngredientIds.Feufaible, 1),
                    new(IngredientIds.Melorine, 1),
                    new(IngredientIds.sensAntiHoraire, 5),
                    new(IngredientIds.FeuFort, 1),
                    new(IngredientIds.AfflatusMaritis, 1),
                }
            },
            new()
            {
                Id = 9,
                Name = "Magna Humécume",
                Description = "Respirer sous l'eau",
                Category = "Alteration",
                MinimumLevel = 0,
                SellPrice = 0.0f,
                Order = 100,
                Recipe = new List<RecipeIngredient>
                {
                    new(IngredientIds.FeuFort, 1),
                    new(IngredientIds.EauPurete, 20),
                    new(IngredientIds.sensAntiHoraire, 5),
                    new(IngredientIds.RacineHerbecorce, 2),
                    new(IngredientIds.sensHoraire, 3),
                    new(IngredientIds.Feufaible, 1),
                    new(IngredientIds.Melorine, 2),
                    new(IngredientIds.sensAntiHoraire, 5),
                    new(IngredientIds.FeuFort, 1),
                    new(IngredientIds.AfflatusMaritis, 1),
                }
            },
            new()
            {
                Id = 10,
                Name = "Chauvus Maximus",
                Description = "Rend chauve",
                Category = "Alteration",
                MinimumLevel = 0,
                SellPrice = 198.0f,
                Experience = 21,
                Order = 110,
                Recipe = new List<RecipeIngredient>
                {
                    new(IngredientIds.BaieMandragore, 5),
                    new(IngredientIds.FeuMoyen, 1),
                    new(IngredientIds.sensHoraire, 1),
                    new(IngredientIds.sensAntiHoraire, 2),
                    new(IngredientIds.FleurSilmera, 2),
                    new(IngredientIds.sensHoraire, 3),
                    new(IngredientIds.PoudreCornlongue, 2),
                    new(IngredientIds.sensAntiHoraire, 4),
                    new(IngredientIds.FeuFort, 1),
                    new(IngredientIds.AmissioCapillorum, 1),
                }
            },
            new()
            {
                Id = 11,
                Name = "Magna Chauvus Maximus",
                Description = "Rend chauve",
                Category = "Alteration",
                MinimumLevel = 0,
                SellPrice = 363.0f,
                Experience = 37,
                Order = 120,
                Recipe = new List<RecipeIngredient>
                {
                    new(IngredientIds.BaieMandragore, 10),
                    new(IngredientIds.FeuMoyen, 1),
                    new(IngredientIds.sensHoraire, 1),
                    new(IngredientIds.sensAntiHoraire, 2),
                    new(IngredientIds.FleurSilmera, 4),
                    new(IngredientIds.sensHoraire, 3),
                    new(IngredientIds.PoudreCornlongue, 4),
                    new(IngredientIds.sensAntiHoraire, 4),
                    new(IngredientIds.FeuFort, 1),
                    new(IngredientIds.AmissioCapillorum, 1),
                }
            },
            new()
            {
                Id = 12,
                Name = "Calvas Finitas",
                Description = "Rend les cheveux",
                Category = "Alteration",
                MinimumLevel = 0,
                SellPrice = 242.0f,
                Order = 130,
                Recipe = new List<RecipeIngredient>
                {
                    new(IngredientIds.FeuFort, 1),
                    new(IngredientIds.BaieMandragore, 5),
                    new(IngredientIds.sensHoraire, 2),
                    new(IngredientIds.BaieMandragore, 5),
                    new(IngredientIds.sensAntiHoraire, 5),
                    new(IngredientIds.FeuMoyen, 1),
                    new(IngredientIds.FleurSilmera, 1),
                    new(IngredientIds.Larmelite, 1),
                    new(IngredientIds.sensHoraire, 2),
                    new(IngredientIds.sensAntiHoraire, 2),
                    new(IngredientIds.FolliculiRenascuntur, 1),
                }
            },
            new()
            {
                Id = 13,
                Name = "Magna Calvas Finitas",
                Description = "Rend les cheveux",
                Category = "Alteration",
                MinimumLevel = 0,
                SellPrice = 447.0f,
                Experience = 61,
                Order = 140,
                Recipe = new List<RecipeIngredient>
                {
                    new(IngredientIds.FeuFort, 1),
                    new(IngredientIds.BaieMandragore, 10),
                    new(IngredientIds.sensHoraire, 2),
                    new(IngredientIds.BaieMandragore, 10),
                    new(IngredientIds.sensAntiHoraire, 5),
                    new(IngredientIds.FeuMoyen, 1),
                    new(IngredientIds.FleurSilmera, 2),
                    new(IngredientIds.Larmelite, 2),
                    new(IngredientIds.sensHoraire, 2),
                    new(IngredientIds.sensAntiHoraire, 2),
                    new(IngredientIds.FolliculiRenascuntur, 1)
                }
            },
            new()
            {
                Id = 14,
                Name = "Choku",
                Description = "Fait des bonds",
                Category = "Alteration",
                MinimumLevel = 0,
                SellPrice = 1073.0f,
                Experience = 44,
                Order = 150,
                Recipe = new List<RecipeIngredient>
                {
                    new(IngredientIds.FeuFort, 1),
                    new(IngredientIds.EauPurete, 3),
                    new(IngredientIds.RacineHerbecorce, 1),
                    new(IngredientIds.sensAntiHoraire, 3),
                    new(IngredientIds.FeuEteint, 1),
                    new(IngredientIds.SouffreErochevice, 2),
                    new(IngredientIds.BaieMandragore, 5),
                    new(IngredientIds.sensHoraire, 3),
                    new(IngredientIds.FeuMoyen, 1),
                    new(IngredientIds.BaieMandragore, 5),
                    new(IngredientIds.sensHoraire, 3),
                    new(IngredientIds.AscensusFulguris, 1)
                }
            },
            new()
            {
                Id = 15,
                Name = "Magna Choku",
                Description = "Fait des bonds",
                Category = "Alteration",
                MinimumLevel = 0,
                SellPrice = 2103.0f,
                Experience = 83,
                Order = 160,
                Recipe = new List<RecipeIngredient>
                {
                    new(IngredientIds.FeuFort, 1),
                    new(IngredientIds.EauPurete, 6),
                    new(IngredientIds.RacineHerbecorce, 2),
                    new(IngredientIds.sensAntiHoraire, 3),
                    new(IngredientIds.FeuEteint, 1),
                    new(IngredientIds.SouffreErochevice, 4),
                    new(IngredientIds.BaieMandragore, 10),
                    new(IngredientIds.sensHoraire, 3),
                    new(IngredientIds.FeuMoyen, 1),
                    new(IngredientIds.BaieMandragore, 10),
                    new(IngredientIds.sensHoraire, 3),
                    new(IngredientIds.AscensusFulguris, 1)
                }
            },
            new()
            {
                Id = 16,
                Name = "Nanis Stimulis",
                Description = "Puise l'energie magique",
                Category = "Aetheric",
                MinimumLevel = 0,
                SellPrice = 0.0f,
                Order = 170,
                Recipe = new List<RecipeIngredient>
                {
                    new(IngredientIds.FeuFort, 1),
                    new(IngredientIds.EauTristesse, 1),
                    new(IngredientIds.Feufaible, 1),
                    new(IngredientIds.FleurSilmera, 1),
                    new(IngredientIds.sensAntiHoraire, 5),
                    new(IngredientIds.CristalMagique, 1),
                    new(IngredientIds.sensAntiHoraire, 2),
                    new(IngredientIds.FeuMoyen, 1),
                    new(IngredientIds.ScintillaMagica, 1)
                }
            },
            new()
            {
                Id = 17,
                Name = "Stimulis",
                Description = "Puise l'energie magique",
                Category = "Aetheric",
                MinimumLevel = 0,
                SellPrice = 1222.0f,
                Experience = 30,
                Order = 180,
                Recipe = new List<RecipeIngredient>
                {
                    new(IngredientIds.FeuFort, 1),
                    new(IngredientIds.EauPurete, 4),
                    new(IngredientIds.Feufaible, 1),
                    new(IngredientIds.FleurSilmera, 6),
                    new(IngredientIds.sensAntiHoraire, 5),
                    new(IngredientIds.CristalMagique, 1),
                    new(IngredientIds.sensAntiHoraire, 2),
                    new(IngredientIds.FeuMoyen, 1),
                    new(IngredientIds.ScintillaMagica, 1)
                }
            },
            new()
            {
                Id = 18,
                Name = "Magna Stimulis",
                Description = "Puise l'energie magique",
                Category = "Aetheric",
                MinimumLevel = 0,
                SellPrice = 2410.0f,
                Experience = 56,
                Order = 190,
                Recipe = new List<RecipeIngredient>
                {
                    new(IngredientIds.FeuFort, 1),
                    new(IngredientIds.EauPurete, 8),
                    new(IngredientIds.Feufaible, 1),
                    new(IngredientIds.FleurSilmera, 12),
                    new(IngredientIds.sensAntiHoraire, 5),
                    new(IngredientIds.CristalMagique, 2),
                    new(IngredientIds.sensAntiHoraire, 2),
                    new(IngredientIds.FeuMoyen, 1),
                    new(IngredientIds.ScintillaMagica, 1)
                }
            },
            new()
            {
                Id = 19,
                Name = "Elixir de vifsoleil",
                Description = "Eclair le lieu ou est bu la potion",
                Category = "Aetheric",
                MinimumLevel = 0,
                SellPrice = 1780.0f,
                Experience = 68,
                Order = 200,
                Recipe = new List<RecipeIngredient>
                {
                    new(IngredientIds.Feufaible, 1),
                    new(IngredientIds.RocheLueurLune, 1),
                    new(IngredientIds.sensHoraire, 4),
                    new(IngredientIds.FeuMoyen, 1),
                    new(IngredientIds.FilamentLumineux, 6),
                    new(IngredientIds.GraineSoliviane, 5),
                    new(IngredientIds.sensAntiHoraire, 8),
                    new(IngredientIds.FeuFort, 1),
                    new(IngredientIds.EauPurete, 4),
                    new(IngredientIds.sensHoraire, 3),
                    new(IngredientIds.FulmenAetheris, 1)
                }
            },
            new()
            {
                Id = 20,
                Name = "Magna Elixir de vifsoleil",
                Description = "Eclair le lieu ou est bu la potion",
                Category = "Aetheric",
                MinimumLevel = 0,
                SellPrice = 3504.0f,
                Experience = 128,
                Order = 210,
                Recipe = new List<RecipeIngredient>
                {
                    new(IngredientIds.Feufaible, 1),
                    new(IngredientIds.RocheLueurLune, 2),
                    new(IngredientIds.sensHoraire, 4),
                    new(IngredientIds.FeuMoyen, 1),
                    new(IngredientIds.FilamentLumineux, 12),
                    new(IngredientIds.GraineSoliviane, 10),
                    new(IngredientIds.sensAntiHoraire, 8),
                    new(IngredientIds.FeuFort, 1),
                    new(IngredientIds.EauPurete, 8),
                    new(IngredientIds.sensHoraire, 3),
                    new(IngredientIds.FulmenAetheris, 1)
                }
            },
            new()
            {
                Id = 21,
                Name = "Elixir du voile",
                Description = "Rend transparent pendant quelques secondes",
                Category = "Aetheric",
                MinimumLevel = 0,
                SellPrice = 1272.0f,
                Experience = 91,
                Order = 220,
                Recipe = new List<RecipeIngredient>
                {
                    new(IngredientIds.FeuMoyen, 1),
                    new(IngredientIds.FleurVoile, 8),
                    new(IngredientIds.EncreMiragel, 5),
                    new(IngredientIds.sensHoraire, 6),
                    new(IngredientIds.NectarVoile, 8),
                    new(IngredientIds.FeuFort, 1),
                    new(IngredientIds.sensAntiHoraire, 3),
                    new(IngredientIds.CorpusOccultum, 1)
                }
            },
            new()
            {
                Id = 22,
                Name = "Magna Elixir du voile",
                Description = "Rend transparent pendant quelques secondes",
                Category = "Aetheric",
                MinimumLevel = 0,
                SellPrice = 2489.0f,
                Order = 230,
                Recipe = new List<RecipeIngredient>
                {
                    new(IngredientIds.FeuMoyen, 1),
                    new(IngredientIds.FleurVoile, 16),
                    new(IngredientIds.EncreMiragel, 10),
                    new(IngredientIds.sensHoraire, 6),
                    new(IngredientIds.NectarVoile, 16),
                    new(IngredientIds.FeuFort, 1),
                    new(IngredientIds.sensAntiHoraire, 3),
                    new(IngredientIds.CorpusOccultum, 1)
                }
            },
        };
        }
    }

    // IDs des ingrédients (espacés de 10 pour insertions futures)
    static class IngredientIds
    {
        public const int FeuEteint = 10;
        public const int Feufaible = 20;
        public const int FeuMoyen = 30;
        public const int FeuFort = 40;
        public const int FeuTresFort = 50;
        public const int sensHoraire = 60;
        public const int sensAntiHoraire = 70;
        public const int EauTristesse = 80;
        public const int EauPurete = 90;
        public const int BaieMandragore = 100;
        public const int FleurSilmera = 110;
        public const int Rosimence = 120;
        public const int Melorine = 130;
        public const int PoudreCornlongue = 140;
        public const int RacineHerbecorce = 150;
        public const int SouffreErochevice = 160;
        public const int FilamentLumineux = 170;
        public const int GraineSoliviane = 180;
        public const int RocheLueurLune = 190;
        public const int Larmelite = 200;
        public const int CristalMagique = 210;
        public const int FleurVoile = 220;
        public const int NectarVoile = 230;
        public const int PlumeMiragel = 240;
        public const int EncreMiragel = 250;
        public const int ElixiremSanationis = 260;
        public const int MensClara = 270;
        public const int AfflatusMaritis = 280;
        public const int AmissioCapillorum = 290;
        public const int FolliculiRenascuntur = 300;
        public const int AscensusFulguris = 310;
        public const int ScintillaMagica = 320;
        public const int FulmenAetheris = 330;
        public const int CorpusOccultum = 340;
    }

    #endregion
}
