using System.ComponentModel.DataAnnotations.Schema;

namespace DWB.Models
{
    public class DoctorAssessmentVM
    {
        public TblNsassessment NursingAssessment { get; set; }
        public TblDoctorAssessment DoctorAssessment { get; set; }
       
        [NotMapped]
        public List<IFormFile> UploadedFiles { get; set; }
    }
}
