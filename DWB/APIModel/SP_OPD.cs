using System.ComponentModel.DataAnnotations;

namespace DWB.APIModel
{
    public class SP_OPD
    {
        public decimal opd_rec { get; set; }
        public string pname { get; set; } = string.Empty;
        public string opdno { get; set; } = string.Empty;

        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateOnly jdate { get; set; }
        public string descript { get; set; } = string.Empty;
        public string doccode { get; set; } = string.Empty;
        public string timein { get; set; } = string.Empty;
        public string age { get; set; } = string.Empty;
        public string? sex { get; set; }
        public int visit { get; set; }
        public string Pcategory { get; set; } = string.Empty;
        public int CompCode { get; set; }
        public DateTime dtEntry { get; set; }
        public Boolean bitTempNSAssComplete { get; set; } = false;
        public Boolean bitTempDOcAssComplete { get; set; } = false;

    }
}
