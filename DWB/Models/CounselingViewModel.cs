// File: ViewModels/CounselingViewModel.cs

using System.ComponentModel.DataAnnotations;

namespace DWB.ViewModels
{
    public class CounselingViewModel
    {
        public long EstimateRecordId { get; set; }
        // --- 1. Main Estimate Record Fields ---
        // This maps to UHID in the DB model
        [Required(ErrorMessage = "Patient ID is required.")]
        [Display(Name = "Registration ID / UHID")]
        public string PatientRegistrationID { get; set; } = string.Empty;

        [Required(ErrorMessage = "Patient Name is required.")]
        [Display(Name = "Patient Name")]
        public string PatientName { get; set; } = string.Empty;

        // Counselor is usually filled from the logged-in user context
        public string CounselorName { get; set; } = "Current User Name";

        public DateTime EstimateDate { get; set; } = DateTime.Today;

        // Calculated on the server
        public decimal TotalEstimatedCost { get; set; }
        public decimal TotalAmountReceived { get; set; }
        public string CLientHost { get; set; } = string.Empty;
        public string ClientIp { get; set; } = string.Empty;

        // -- 2. Acknowledgement Fields ---
        [Required(ErrorMessage = "Relative Name is required.")]
        [Display(Name = "Relative/Attendant Name")]
        public string RelativeName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Relative Relation is required.")]
        [Display(Name = "Relation")]
        public string RelativeRelation { get; set; } = string.Empty;

        //Binds to the mandatory acknowledgment checkbox
        //[Range(typeof(bool), "true", "true", ErrorMessage = "Acknowledgment is mandatory.")]
        [Required(ErrorMessage = "You must confirm acceptance...")]
        public bool IsAcknowledged { get; set; } = false;

        public string? RelativeSignatureData { get; set; }


        // --- 3. Line Item Collection ---
        public List<EstimateLineItemViewModel> LineItems { get; set; } = new List<EstimateLineItemViewModel>();
        public List<EstimateLineItemViewModel> AvailableTariffs { get; set; } = new List<EstimateLineItemViewModel>();

        // --- 4. Payment Collection ---
        public List<PaymentTransactionViewModel> Payments { get; set; } = new List<PaymentTransactionViewModel>();
        public int VisitNo { get; set; }        
    }

    //Auxiliary ViewModel for Line Item Collection
    public class EstimateLineItemViewModel
    {
        public string ServiceName { get; set; } = string.Empty;
        public string UnitType { get; set; } = string.Empty;
        public decimal TariffRate { get; set; }

        [Display(Name = "Est. Quantity")]
        public decimal EstimatedQuantity { get; set; }
        public decimal CalculatedAmount { get; set; }
        public Boolean bitISdeafult { get; set; } = false;
    }
}