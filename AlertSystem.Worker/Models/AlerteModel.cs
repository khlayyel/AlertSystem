using System;

namespace AlertSystem.Worker.Models
{
    public class AlerteModel
    {
        public int AlerteId { get; set; }
        public int? AlertTypeId { get; set; }
        public int? DestinataireId { get; set; }
        public int? PlateformeEnvoieId { get; set; }
        public string TitreAlerte { get; set; } = string.Empty;
        public string DescriptionAlerte { get; set; } = string.Empty;
        public DateTime DateCreationAlerte { get; set; }
        public bool ProcessedByWorker { get; set; }
    }
}
