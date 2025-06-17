namespace DWB.Models
{
    public class MasterTabView
    {       
            public IEnumerable<TblDietMaster> Diets { get; set; }
            public IEnumerable<TblFloorMaster> Floors { get; set; }
            public IEnumerable<TblRoomMaster> Rooms { get; set; }
            public IEnumerable<TblRoleMas> RoleMas { get; set; }
            // Add other tab data as needed
    }
}
