using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace SevenwandsConsoleTool
{
    public class Course : INotifyPropertyChanged
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        private int _currentPoints;
        [JsonPropertyName("currentPoints")]
        public int CurrentPoints
        {
            get => _currentPoints;
            set
            {
                if (_currentPoints != value)
                {
                    _currentPoints = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Progress));
                    OnPropertyChanged(nameof(RemainingPoints));
                    OnPropertyChanged(nameof(ProgressText));
                    OnPropertyChanged(nameof(IsCompleted));
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        [JsonPropertyName("requiredPoints")]
        public int RequiredPoints { get; set; }

        [JsonPropertyName("icon")]
        public string Icon { get; set; }

        // Propriétés calculées pour le binding
        [JsonIgnore]
        public double Progress => RequiredPoints > 0 ? (double)CurrentPoints / RequiredPoints : 0;

        [JsonIgnore]
        public int RemainingPoints => Math.Max(0, RequiredPoints - CurrentPoints);

        [JsonIgnore]
        public string ProgressText => $"{CurrentPoints} / {RequiredPoints}";

        [JsonIgnore]
        public string RemainingText => $"Il te reste {RemainingPoints} points à obtenir";

        [JsonIgnore]
        public bool IsCompleted => CurrentPoints >= RequiredPoints;

        [JsonIgnore]
        public string StatusText
        {
            get
            {
                if (IsCompleted) return "Terminé ✓";
                if (Progress >= 0.8) return "Presque terminé";
                if (Progress >= 0.5) return "En cours";
                if (Progress >= 0.2) return "En progression";
                return "À commencer";
            }
        }

        [JsonIgnore]
        public bool IsHighPriority => !IsCompleted && Progress < 0.5;

        [JsonIgnore]
        public string PriorityText => "Priorité haute";

        [JsonIgnore]
        public string TotalPointsText => RequiredPoints.ToString();

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class YearData : INotifyPropertyChanged
    {
        [JsonPropertyName("year")]
        public int Year { get; set; }

        private List<Course> _courses = new List<Course>();
        [JsonPropertyName("courses")]
        public List<Course> Courses
        {
            get => _courses;
            set
            {
                // Désabonner les anciens cours
                if (_courses != null)
                {
                    foreach (var course in _courses)
                    {
                        course.PropertyChanged -= OnCoursePropertyChanged;
                    }
                }

                _courses = value;

                // S'abonner aux nouveaux cours pour écouter leurs changements
                if (_courses != null)
                {
                    foreach (var course in _courses)
                    {
                        course.PropertyChanged += OnCoursePropertyChanged;
                    }
                }

                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gestionnaire d'événements pour les changements de propriétés des cours
        /// Permet de rafraîchir automatiquement les statistiques de l'année
        /// </summary>
        private void OnCoursePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Lorsqu'un cours change, rafraîchir les calculs de l'année
            if (e.PropertyName == nameof(Course.CurrentPoints) || 
                e.PropertyName == nameof(Course.IsCompleted))
            {
                RefreshCalculations();
            }
        }

        // Propriétés calculées
        [JsonIgnore]
        public int TotalCurrentPoints => Courses?.Sum(c => c.CurrentPoints) ?? 0;

        [JsonIgnore]
        public int TotalRequiredPoints => Courses?.Sum(c => c.RequiredPoints) ?? 0;

        [JsonIgnore]
        public double TotalProgress => TotalRequiredPoints > 0 ? (double)TotalCurrentPoints / TotalRequiredPoints : 0;

        [JsonIgnore]
        public int CompletedCoursesCount => Courses?.Count(c => c.IsCompleted) ?? 0;

        [JsonIgnore]
        public int TotalCoursesCount => Courses?.Count ?? 0;

        [JsonIgnore]
        public string ProgressPercentage => $"{(TotalProgress * 100):F0}%";

        [JsonIgnore]
        public string TotalProgressText => $"{TotalCurrentPoints} / {TotalRequiredPoints}";

        [JsonIgnore]
        public string CompletedCoursesText => $"{CompletedCoursesCount}/{TotalCoursesCount} cours complétés";

        [JsonIgnore]
        public string YearTitle
        {
            get
            {
                return Year switch
                {
                    1 => "PREMIÈRE ANNÉE",
                    2 => "DEUXIÈME ANNÉE",
                    3 => "TROISIÈME ANNÉE",
                    4 => "QUATRIÈME ANNÉE",
                    5 => "CINQUIÈME ANNÉE",
                    6 => "SIXIÈME ANNÉE",
                    7 => "SEPTIÈME ANNÉE",
                    _ => $"ANNÉE {Year}"
                };
            }
        }

        [JsonIgnore]
        public int HighPriorityCount => Courses?.Count(c => !c.IsCompleted && c.Progress < 0.5) ?? 0;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void RefreshCalculations()
        {
            OnPropertyChanged(nameof(TotalCurrentPoints));
            OnPropertyChanged(nameof(TotalRequiredPoints));
            OnPropertyChanged(nameof(TotalProgress));
            OnPropertyChanged(nameof(CompletedCoursesCount));
            OnPropertyChanged(nameof(ProgressPercentage));
            OnPropertyChanged(nameof(TotalProgressText));
            OnPropertyChanged(nameof(CompletedCoursesText));
            OnPropertyChanged(nameof(HighPriorityCount));
        }

        /// <summary>
        /// S'assure que tous les cours sont écoutés pour leurs changements
        /// À appeler après la désérialisation
        /// </summary>
        public void SubscribeToCourseChanges()
        {
            if (_courses != null)
            {
                foreach (var course in _courses)
                {
                    // Éviter les doubles abonnements
                    course.PropertyChanged -= OnCoursePropertyChanged;
                    course.PropertyChanged += OnCoursePropertyChanged;
                }
            }
        }
    }

    public class TokenTrackingData
    {
        public List<YearData> Years { get; set; } = new List<YearData>();
    }
}
