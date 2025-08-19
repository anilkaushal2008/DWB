using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Collections;
using DWB.Attributes;

namespace DWB.Models
{
    public class CreateUserViewModel
    {
        public int IntUserId { get; set; }

        [Display(Name ="User name")]
        [Required(ErrorMessage = "Username is required")]
        [MaxLength(50, ErrorMessage = "Username cannot exceed 50 characters")]
        [RegularExpression(@"^\S+$", ErrorMessage = "Username cannot contain spaces")]
        public string? VchUsername { get; set; }

        [Display(Name ="Password")]
        [Required(ErrorMessage = "Password is required")]
        public string? HpasswordHash { get; set; }

        [Display(Name ="Full name")]
        [Required(ErrorMessage ="Full name required")]
        public string? VchFullName { get; set; }

        [Required(ErrorMessage ="Email address required")]
        [Display(Name ="Email")]
        //[RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",ErrorMessage = "Email format is not valid")]
        public string? VchEmail { get; set; }

        [Required(ErrorMessage ="Mobile number required")]
        [Display(Name ="Mobile number")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid mobile number")]
        public string? VchMobile { get; set; }

        [Required(ErrorMessage ="Select role")]
        public int FkRoleId { get; set; }  

        // ✅ File Uploads
        [Display(Name = "Profile Picture")]
        public IFormFile? ProfileFile { get; set; }

        [Display(Name = "Signature")]
        public IFormFile? SignatureFile { get; set; }

        // To show uploaded images (optional)
        public string? ProfilePath { get; set; }
        public string? SignaturePath { get; set; }

        public List<CompanySelectionViewModel> UserCompanies { get; set; } = new List<CompanySelectionViewModel>();
    }
   
}
