namespace DWB.Models
{
    public class RoleModulePermissionItem
    {
        public int ModuleId { get; set; }
        public string MasterModule { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string SubModule { get; set; } = string.Empty;
        public bool CanAdd { get; set; }
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool Status { get; set; }
    }
}
