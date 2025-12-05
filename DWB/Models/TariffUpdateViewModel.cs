using DWB.Models;
namespace DWB.Models
{
    public class TariffUpdateViewModel
    {
        // This array name MUST match the JavaScript naming: TariffItems
        public List<TariffMaster> TariffItems { get; set; } = new List<TariffMaster>();
        public string ClientIP { get; set; } = "N/A";
        public string ClientHostName { get; set; } = "N/A";
        //public int IHMSCODE { get; set; } = 0;
        //public int INTCODE { get; set; } = 0;
    }
}
