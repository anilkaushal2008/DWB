// File: ViewModels/PaymentTransactionViewModel.cs

using System.ComponentModel.DataAnnotations;

namespace DWB.ViewModels
{
    public class PaymentTransactionViewModel
    {
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime PaymentDate { get; set; } = DateTime.Today;

        [Display(Name = "Amount Received")]
        [Range(0, 999999999999.99, ErrorMessage = "Amount must be a positive value.")]
        public decimal AmountPaid { get; set; }
        
        [Display(Name = "Payer Name")]
        public string? PayerName { get; set; } = string.Empty;

        public string? PayerRelation { get; set; }
    }
}