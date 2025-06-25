namespace DWB.Models
{
    public class PermissionEntry
    {
        public string Module { get; set; }
        public string SubModule { get; set; }
        public bool View { get; set; }
        public bool Add { get; set; }
        public bool Edit { get; set; }
        public bool Delete { get; set; }
    }
}
