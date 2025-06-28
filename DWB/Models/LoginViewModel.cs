using System.ComponentModel.DataAnnotations;

namespace DWB.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Select company")]
        [Range(1, int.MaxValue, ErrorMessage = "Select a valid company")]
        public int fk_intPK { get; set; }
        
        [Required(ErrorMessage ="Enter username")]
        public string? Username { get; set; }

        [Required(ErrorMessage ="Enter password")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }
    }
}
