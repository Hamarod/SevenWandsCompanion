using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SevenwandsConsoleTool
{
    // Catégorie métier configurable par l'utilisateur (ex: Potion, Auberge, Forge)
    public class BusinessCategory
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        public BusinessCategory() { }

        public BusinessCategory(int id, string name)
        {
            Id = id;
            Name = name;
        }

        // Comparaison par ID pour permettre le binding SelectedItem du Picker en MAUI
        public override bool Equals(object? obj)
        {
            return obj is BusinessCategory other && Id == other.Id;
        }

        public override int GetHashCode() => Id.GetHashCode();
    }

    // Quantité d'un ingrédient physique possédée en stock
    public class IngredientStock
    {
        [JsonPropertyName("ingredient_id")]
        public int IngredientId { get; set; }

        [JsonPropertyName("quantity_owned")]
        public int QuantityOwned { get; set; }

        // Catégories métier assignées à cette ressource : une ressource peut servir
        // à plusieurs métiers (ex: Forge ET Auberge), contrairement à un produit fini.
        [JsonPropertyName("category_ids")]
        public List<int> CategoryIds { get; set; } = new();

        // Ancien champ (catégorie unique) conservé uniquement pour migrer les données
        // déjà sauvegardées avant l'introduction du multi-catégorie ; jamais ré-écrit.
        [JsonPropertyName("category_id")]
        public int? LegacyCategoryId { get; set; }

        public IngredientStock() { }

        public IngredientStock(int ingredientId, int quantityOwned, List<int>? categoryIds = null)
        {
            IngredientId = ingredientId;
            QuantityOwned = quantityOwned;
            CategoryIds = categoryIds ?? new List<int>();
        }
    }

    // Prix pratiqué pour revendre un produit fini (potion, ou autre plus tard) à un autre joueur
    public class PotionResalePrice
    {
        [JsonPropertyName("potion_id")]
        public int PotionId { get; set; }

        [JsonPropertyName("resale_price")]
        public float ResalePrice { get; set; }

        // Catégorie métier assignée à ce produit (null = non catégorisé)
        [JsonPropertyName("category_id")]
        public int? CategoryId { get; set; }

        // Quantité de produits finis déjà fabriqués et possédés en stock (distinct de
        // "Fabricables", qui est le nombre calculé à partir du stock de ressources restant).
        [JsonPropertyName("quantity_owned")]
        public int QuantityOwned { get; set; }

        public PotionResalePrice() { }

        public PotionResalePrice(int potionId, float resalePrice, int? categoryId = null, int quantityOwned = 0)
        {
            PotionId = potionId;
            ResalePrice = resalePrice;
            CategoryId = categoryId;
            QuantityOwned = quantityOwned;
        }
    }

    // Quantité d'un produit fini demandée par une commande
    public class OrderItem
    {
        [JsonPropertyName("potion_id")]
        public int PotionId { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        public OrderItem() { }

        public OrderItem(int potionId, int quantity)
        {
            PotionId = potionId;
            Quantity = quantity;
        }
    }

    // Quantité d'une ressource brute demandée par une commande (en plus, ou à la place, de
    // produits finis).
    public class OrderResourceItem
    {
        [JsonPropertyName("ingredient_id")]
        public int IngredientId { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        public OrderResourceItem() { }

        public OrderResourceItem(int ingredientId, int quantity)
        {
            IngredientId = ingredientId;
            Quantity = quantity;
        }
    }

    // Commande passée par un autre joueur : des produits finis et/ou des ressources brutes à
    // fournir contre une somme négociée. Une fois livrée ("Commande terminée"), les produits et
    // ressources fournis sont retirés du stock correspondant et la somme rejoint la cagnotte
    // globale.
    public class Order
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("customer_name")]
        public string CustomerName { get; set; } = "";

        [JsonPropertyName("items")]
        public List<OrderItem> Items { get; set; } = new();

        // Ressources brutes demandées, en plus des produits finis (ex: fournir 20 Cacao en plus
        // de 2 Chocolat).
        [JsonPropertyName("resource_items")]
        public List<OrderResourceItem> ResourceItems { get; set; } = new();

        [JsonPropertyName("negotiated_price")]
        public float NegotiatedPrice { get; set; }

        [JsonPropertyName("is_completed")]
        public bool IsCompleted { get; set; }

        // Date de livraison négociée avec le commanditaire, utilisée pour trier les commandes
        // par urgence.
        [JsonPropertyName("deadline")]
        public DateTime Deadline { get; set; }

        public Order() { }

        public Order(int id, string customerName, List<OrderItem> items, float negotiatedPrice, DateTime deadline, List<OrderResourceItem>? resourceItems = null, bool isCompleted = false)
        {
            Id = id;
            CustomerName = customerName;
            Items = items;
            ResourceItems = resourceItems ?? new List<OrderResourceItem>();
            NegotiatedPrice = negotiatedPrice;
            Deadline = deadline;
            IsCompleted = isCompleted;
        }
    }

    // Racine du fichier Business.json : catégories + stock de ressources + prix de revente joueur
    // Séparé de Ingredients.json/Potions.json car ce sont des données d'inventaire
    // qui évoluent en jeu, pas des définitions de recettes/ingrédients.
    public class BusinessData
    {
        [JsonPropertyName("categories")]
        public List<BusinessCategory> Categories { get; set; } = new();

        [JsonPropertyName("ingredient_stocks")]
        public List<IngredientStock> IngredientStocks { get; set; } = new();

        [JsonPropertyName("potion_resale_prices")]
        public List<PotionResalePrice> PotionResalePrices { get; set; } = new();

        [JsonPropertyName("orders")]
        public List<Order> Orders { get; set; } = new();

        // Cagnotte globale alimentée par les commandes terminées.
        [JsonPropertyName("treasury")]
        public float Treasury { get; set; }
    }
}
