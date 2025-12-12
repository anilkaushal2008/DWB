using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace DWB.Models
{
    public class TriageViewModel
    {
        // IDs & flags
        public long AssessmentId { get; set; }
        public long VisitId { get; set; }

        [Required]
        public string? PatientId { get; set; }

        public bool IsEditMode { get; set; }

        // Patient basics
        [Required, StringLength(150)]
        [Display(Name = "Patient Name")]
        public string? PatientName { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "UHID/No")]
        public string? UhidNo { get; set; }
        public string Category { get; set; }
        public string CategoryCode { get; set; }
        public string doccode { get; set; }
        public string docname{ get; set; }

        //referal doctor
        public int? ConsultantDoctorId { get; set; }
        public string? OtherDoctorName { get; set; }

        public string? Age { get; set; }
        public string? Sex { get; set; }

        [Phone, StringLength(15)]
        public string? PhoneNo { get; set; }

        [StringLength(250)]
        public string? PatientAddress { get; set; }

        [StringLength(50)]
        public string? RoomWardNumber { get; set; }

        [StringLength(50)]
        public string? BedNumber { get; set; }

        // Arrival & provider
        [Required(ErrorMessage = "Select arrival date & time")]
        [Display(Name = "Arrival Date & Time")]
        public DateTime ArrivalDateTime { get; set; }

        [Required(ErrorMessage = "Select time seen by provider")]
        [Display(Name = "Time Seen By Provider")]
        public DateTime TimeSeenByProvider { get; set; }

        // Transportation & History
        public string? TransportationMode { get; set; }
        public string? TransportationOther { get; set; }

        [Required(ErrorMessage = "Please specify who provided the history.")]
        [StringLength(150)]
        public string? HistoryObtainedFrom { get; set; }

        // Vitals
        // Vitals
        [Required(ErrorMessage = "Vitals time is required.")]
        [Display(Name = "Vitals Time")]
        public DateTime? VitalsTime { get; set; }

        [Required(ErrorMessage = "Pulse is required.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Pulse must be numeric.")]
        public string? Pulse { get; set; }

        [Required(ErrorMessage = "Systolic BP is required.")]
        [Range(40, 250, ErrorMessage = "Invalid systolic BP value.")]
        public int? BPSystolic { get; set; }

        [Required(ErrorMessage = "Diastolic BP is required.")]
        [Range(20, 200, ErrorMessage = "Invalid diastolic BP value.")]
        public int? BPDiastolic { get; set; }

        [Required(ErrorMessage = "Respiratory rate is required.")]
        [Range(5, 60, ErrorMessage = "Respiratory rate must be between 5–60.")]
        public int? RespRate { get; set; }

        [Required(ErrorMessage = "Temperature is required.")]
        [Range(90, 110, ErrorMessage = "Temperature must be between 90°F and 110°F.")]
        public decimal? Temperature { get; set; }

        [Required(ErrorMessage = "Weight is required.")]
        [Range(1, 400, ErrorMessage = "Weight must be between 1–400 kg.")]
        public decimal? Weight { get; set; }


        // Clinical Assessment
        [Required, MinLength(3), StringLength(500)]
        [Display(Name = "Chief Complaint")]
        public string? ChiefComplaint { get; set; }

        [StringLength(500)]
        public string? CurrentMedication { get; set; }

        [StringLength(300)]
        public string? Allergies { get; set; }

        [StringLength(2000)]
        public string? SubjectiveNotes { get; set; }

        [StringLength(2000)]
        public string? ObjectiveNotes { get; set; }

        [StringLength(500)]
        public string? InvestigationsOrdered { get; set; }

        [StringLength(500)]
        [Display(Name = "Provisional Diagnosis")]
        public string? Diagnosis { get; set; }

        [StringLength(2000)]
        public string? Plan { get; set; }

        // Triage
        [Required(ErrorMessage = "Triage category is required")]
        public string? TriageCategory { get; set; }

        // Disposition
        [StringLength(150)]
        public string? ConditionUponRelease { get; set; }

        public DateTime? TimeOfRelease { get; set; }
       
        public bool IsAdmissionAdvised { get; set; }


        [StringLength(250)]
        public string? AdmissionRefusalReason { get; set; }

        // Cross Consultation
        public bool? IsCrossConsultRequired { get; set; }

        [StringLength(150)]
        public string? SpecialistName { get; set; }

        [StringLength(150)]
        public string? ReferredTo { get; set; }

        // Follow Up
        public DateTime? FollowUpDate { get; set; }
        public DateTime? FollowUpTime { get; set; }

        [StringLength(500)]
        public string? PatientInstructions { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        // Status flag
        public bool BitIsCompleted { get; set; } = true;

        public List<SelectListItem>? ConsultantList { get; set; }
    }
}
