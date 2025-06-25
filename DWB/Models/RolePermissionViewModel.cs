using Microsoft.AspNetCore.Mvc.Rendering;

namespace DWB.Models
{
    public class RolePermissionViewModel
    {
        public int RoleId { get; set; }

        public string RoleName { get; set; }

        public List<SelectListItem> Roles { get; set; }

        public List<PermissionItem> Permissions { get; set; } = new List<PermissionItem>();
    }
}
