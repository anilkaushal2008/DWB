namespace DWB.Models
{
    public class MasterUserTab
    {
        public IEnumerable<TblUsers> AllUsers { get; set; } = new List<TblUsers>();
        public IEnumerable<TblUserCompany> UsersCompany{ get; set; }  = new List<TblUserCompany>();
    }
}
