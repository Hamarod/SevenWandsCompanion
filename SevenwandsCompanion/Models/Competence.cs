namespace SevenwandsCompanion.Models
{
    /// <summary>
    /// Représente une compétence du personnage
    /// </summary>
    public class Competence
    {
        public string Nom { get; set; }
        public string Icone { get; set; }
        public int Valeur { get; set; }
        public string Couleur { get; set; }

        /// <summary>
        /// Types de compétences disponibles
        /// </summary>
        public enum TypeCompetence
        {
            Intelligence,    // 🧠 Jaune (cerveau)
            Energie,        // ⚡ Bleu clair (éclair)
            Sante,          // ❤️ Rose/Magenta (cœur)
            Force,          // 💪 Rouge (haltère)
            Agilite         // 🏃 Vert (coureur)
        }
    }
}
