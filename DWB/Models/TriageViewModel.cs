using System.ComponentModel.DataAnnotations;

namespace DWB.Models
{
    public class TriageViewModel
    {
        // IDs
        public long AssessmentId { get; set; }
        public long VisitId { get; set; }
        public string? PatientId { get; set; }
        public bool IsEditMode { get; set; } // To toggle Create/Edit UI

        // --- SECTION A: Patient Demographics (Read-Only from API) ---
        public string PatientName { get; set; }
        public string MRN_Number { get; set; }
        public string Age { get; set; }
        public string Sex { get; set; }
        public string Address { get; set; }

        // --- SECTION B: Logistics & Vitals ---
        [Display(Name = "Arrival Date & Time")]
        public DateTime ArrivalDateTime { get; set; }

        [Display(Name = "Time Seen By Provider")]
        public DateTime TimeSeenByProvider { get; set;} // NABH Requirement [cite: 27]

        [Display(Name = "Mode of Arrival")]
        public string TransportationMode { get; set;} // Private/Ambulance [cite: 9]

       
        public string Pulse { get; set; }
        // 1. Fix BP Systolic
        [Required(ErrorMessage = "Systolic BP is required")]
        public int? BPSystolic { get; set; } // Use 'int?' not 'int'

        // 2. Fix BP Diastolic
        [Required(ErrorMessage = "Diastolic BP is required")]
        public int? BPDiastolic { get; set; } // Use 'int?' not 'int'

        // 3. Fix Temperature
        [Required(ErrorMessage = "Temperature is required")]
        [Range(90, 110, ErrorMessage = "Invalid Temp")] // Optional: Adds sanity check
        public decimal? Temperature { get; set; } // Use 'decimal?' or 'double?'
        public int? RespRate { get; set; }      
        public decimal? Weight { get; set; }

        // --- SECTION C: Clinical Assessment (SOAP) ---
        [Display(Name = "Chief Complaint")]
        [Required(ErrorMessage = "Chief Complaint is required")]
        public string ChiefComplaint { get; set; } // [cite: 28]

        [Display(Name = "Objective Data (Exam/Tests)")]
        public string ObjectiveNotes { get; set;} // [cite: 36]

        [Display(Name = "Diagnosis")]
        [Required(ErrorMessage = "Diagnosis is required")]
        public string Diagnosis { get; set; } // [cite: 41]

        [Display(Name = "Plan / Treatment")]
        public string Plan { get; set; } // [cite: 49]

        // --- SECTION D: Triage & Outcome ---
        [Required(ErrorMessage = "Triage Category is mandatory")]
        public string TriageCategory { get; set; } // Emergent/Urgent/Non-Urgent [cite: 40]

        public bool IsAdmissionAdvised { get; set; } // Logic trigger

        [Display(Name = "Condition Upon Release")]
        public string ConditionUponRelease { get; set;} // Improved/Unchanged/Deteriorated [cite: 51]
    }
}