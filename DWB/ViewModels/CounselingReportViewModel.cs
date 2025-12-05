// File: ViewModels/CounselingReportViewModel.cs

using System.ComponentModel.DataAnnotations;

namespace DWB.ViewModels
{
    public class CounselingReportViewModel
    {
        // Inputs for filtering
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Report Month")]
        public DateTime ReportMonth { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        // Results for display
        public List<CounselorSummaryDto> ReportData { get; set; } = new List<CounselorSummaryDto>();
    }

    // DTO for the aggregated report results
    public class CounselorSummaryDto
    {
        public string CounselorName { get; set; } = string.Empty;
        public int TotalSessions { get; set; }
        public decimal TotalEstimatedValue { get; set; }
        public decimal TotalAdvanceCollected { get; set; }
        public decimal AverageEstimatePerSession => TotalSessions > 0 ? TotalEstimatedValue / TotalSessions : 0;
    }
}