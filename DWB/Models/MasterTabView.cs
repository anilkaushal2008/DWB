using System.Collections.Generic;
namespace DWB.Models
{
    public class MasterTabView
    {
        public IEnumerable<TblDietMaster> Diets { get; set; } = new List<TblDietMaster>();
        public IEnumerable<TblFloorMaster> Floors { get; set; } = new List<TblFloorMaster>();
        public IEnumerable<TblRoomMaster> Rooms { get; set; } = new List<TblRoomMaster>();
        public IEnumerable<TblRoleMas> RoleMas { get; set; } = new List<TblRoleMas>();
        public IEnumerable<TblModules> ModuleMas { get; set; } = new List<TblModules>();
    }
}
