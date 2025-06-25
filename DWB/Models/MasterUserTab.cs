namespace DWB.Models
{
    public class MasterUserTab
    {
        public IEnumerable<TblUsers> AllUsers { get; set; }
        public IEnumerable<TblUserCompany> UsersCompany{ get; set; }      
    }
}
