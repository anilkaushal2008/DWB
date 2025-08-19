namespace DWB.Models
{
    public class CompanySelectionViewModel
    {
        public int CompanyId { get; set; }
        public string? CompanyName { get; set; }

        public bool IsSelected { get; set; }

        // Optional doctor code
        public string? VchDoctorCode { get; set; }
    }
}
