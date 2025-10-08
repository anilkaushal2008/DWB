using System.ComponentModel.DataAnnotations.Schema;

namespace DWB.Models
{
    public class DoctorAssessmentVM
    {
        public TblNsassessment NursingAssessment { get; set; }
        public TblDoctorAssessment DoctorAssessment { get; set; }       
        [NotMapped]
        //public List<IFormFile> UploadedFiles { get; set; }
        // file upload
        //public IFormFile[] UploadedFiles { get; set; }
        public List<TblDoctorAssmntMedicine> Medicines { get; set; } = new List<TblDoctorAssmntMedicine>();
        public List<TblDoctorAssmntLab> Labs { get; set; } = new List<TblDoctorAssmntLab>();
        public List<TblDoctorAssmntRadiology> Radiology { get; set; } = new List<TblDoctorAssmntRadiology>();
        public List<TblDoctorAssmntProcedure> Procedures { get; set; } = new List<TblDoctorAssmntProcedure>();
        //for get upload document
        public List<TblDoctorAssessmentDoc> Documents { get; set; } = new List<TblDoctorAssessmentDoc>();

        //for templates
       // public List<TblDocTemplateAssessment> Templates { get; set; }

    }
}

