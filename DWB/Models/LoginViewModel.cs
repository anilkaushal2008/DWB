using System.ComponentModel.DataAnnotations;

namespace DWB.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Select company")]
        public int fk_intPK { get; set; }
        [Required]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
