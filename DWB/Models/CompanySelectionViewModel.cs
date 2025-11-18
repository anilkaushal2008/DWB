using System.ComponentModel.DataAnnotations;

namespace DWB.Models
{
    public class CompanySelectionViewModel
    {
        public int CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public bool IsSelected { get; set; }

        // Optional doctor code
        public string? VchDoctorCode { get; set; }

       
        [Display(Name = "Sun")]
        public bool IsSunday { get; set; }

        [Display(Name = "Mon")]
        public bool IsMonday { get; set; }

        [Display(Name = "Tue")]
        public bool IsTuesday { get; set; }

        [Display(Name = "Wed")]
        public bool IsWednesday { get; set; }

        [Display(Name = "Thu")]
        public bool IsThursday { get; set; }

        [Display(Name = "Fri")]
        public bool IsFriday { get; set; }

        [Display(Name = "Sat")]
        public bool IsSaturday { get; set; }

        [DataType(DataType.Time)]
        public TimeOnly? StartTime { get; set; }

        [DataType(DataType.Time)]
        public TimeOnly? EndTime { get; set; }
    }
}
