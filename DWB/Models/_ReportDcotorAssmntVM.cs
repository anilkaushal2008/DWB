namespace DWB.Models
{
    public class _ReportDcotorAssmntVM
    {
        public string? PatientName { get; set; }
        public int? Age { get; set; }
        public string? Gender { get; set; }
        public string? DoctorName { get; set; }
        public string? Diagnosis { get; set; }
        public string? Category { get; set; }
        public string? DoctorTiming { get; set; } = "10am-4pm";
        public string? SignaturePath { get; set; }
        public List<TblDoctorAssmntMedicine> Medicines { get; set; }= new List<TblDoctorAssmntMedicine>();
        public List<TblDoctorAssmntLab> Labs { get; set; } = new List<TblDoctorAssmntLab>();
        public List<TblDoctorAssmntRadiology> Radiology { get; set; } = new List<TblDoctorAssmntRadiology>();
        public List<TblDoctorAssmntProcedure> Procedure { get; set; } = new List<TblDoctorAssmntProcedure>();   
    }
}
