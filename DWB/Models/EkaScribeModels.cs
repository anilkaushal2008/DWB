namespace DWB.Models
{
    // EkaScribeModels.cs

    // --- Root Model ---
    public class EkaScribeDataModel
    {
        // The main list of symptoms reported
        public List<Symptom> symptoms { get; set; }

        // Contains Vitals, Past History, and Examinations
        public MedicalHistory medicalHistory { get; set; }

        // List of Diagnoses
        public List<Diagnosis> diagnosis { get; set; }

        // Follow up instructions/notes
        public FollowUp followup { get; set; }

        // Note: Other fields (medications, labTests) are omitted as per your request
    }

    // --- Sub-Models for Notes Mapping ---

    public class Symptom
    {
        public string name { get; set; }
        // Add properties structure if you want to extract duration/severity, 
        // otherwise name alone is sufficient for simple notes.
    }

    public class Diagnosis
    {
        public string name { get; set; }
        // Add properties structure if you want to extract status (Suspected/Confirmed)
    }

    public class MedicalHistory
    {
        public PatientHistory patientHistory { get; set; }
        public List<Examination> examinations { get; set; }
        public List<Vital> vitals { get; set; }
    }

    public class PatientHistory
    {
        // Simplified model to extract key history points
        public List<TblDoctorAssmntMedicine> patientMedicalConditions { get; set; }
        //public List<> lifestyleHabits { get; set; }
    }

    public class Examination
    {
        public string name { get; set; }
        public string notes { get; set; } // Detailed free text notes from exam
    }

    public class Vital
    {
        public string name { get; set; }
        public ValueType value { get; set; }
    }

    public class FollowUp
    {
        public string notes { get; set; }
    }

    // Define other necessary micro-classes (ValueType, MedicalCondition, Habit, etc.) 
    // to match the exact structure of your EkaScribe JSON data
}
