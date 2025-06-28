namespace DWB.Models
{
    public class PermissionItem
    {
        public int fkIntRoleId { get; set; }
        public int PermissionId { get; set; }
        public string? Module { get; set; }
        public string? SubModule { get; set; }
        public bool BitView { get; set; }
        public bool BitAdd { get; set; }
        public bool BitEdit { get; set; }
        public bool BitDelete { get; set; }
        public bool BitStatus { get; set; }
    }
}
