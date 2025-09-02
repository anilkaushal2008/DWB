using System.ComponentModel.DataAnnotations;

namespace DWB.APIModel
{
    public class spMedicine
    {
        [StringLength(255)]
        public string descript { get; set; }

        [StringLength(255)]
        public string icode { get; set; }
    }
}
