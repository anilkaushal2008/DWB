using System.ComponentModel.DataAnnotations;

namespace DWB.APIModel
{
    public class SP_OPD
    {
        public decimal opd_rec { get; set; }
        public string pname { get; set; }
        public string opdno { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateOnly jdate { get; set; }
        public string descript { get; set; }
        public int CompCode { get; set; }
    }
}
