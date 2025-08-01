using System.ComponentModel.DataAnnotations;

namespace DWB.Models
{
    [MetadataType(typeof(TblNsassessmentMetadata))]
    public partial class TblNsassessment
    {
        //calling class
    }
    public class TblNsassessmentMetadata
    {
        [Required(ErrorMessage = "UHID is required")]
        public string? VchUhidNo { get; set; }

        [Required(ErrorMessage = "Blood Pressure is required")]
        public string? VchBloodPressure { get; set; }

        [Required(ErrorMessage = "Pulse is required")]
        public string? VchPulse { get; set; }

        [Required(ErrorMessage = "Temperature is required")]
        public string? DecTemperature { get; set; }

        [Required(ErrorMessage = "Enter SpO2")]
        [Range(50, 100, ErrorMessage = "SpO2 must be between 50 and 100")]
        public decimal DecSpO2 { get; set; }

        [Required(ErrorMessage = "Enter weight in kg")]
        [Range(1, 300, ErrorMessage = "Weight must be between 1 and 300")]
        public decimal DecWeight { get; set; }

        [Required(ErrorMessage = "Enter height ")]
        [Range(30, 250, ErrorMessage = "Height must be between 30 and 250")]
        public decimal? DecHeight { get; set; }

        [Range(5, 50, ErrorMessage = "Respiratory Rate must be between 5 and 50")]
        public decimal DecRespiratoryRate { get; set; }

        [Range(0, 20, ErrorMessage = "O₂ Flow Rate must be between 0 and 20")]
        public decimal DecOxygenFlowRate { get; set; }
    }
}

