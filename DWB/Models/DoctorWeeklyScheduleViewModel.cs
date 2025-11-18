using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

public class DoctorWeeklyScheduleViewModel
{
    public int? ScheduleId { get; set; }

    [Required]
    [Display(Name = "Doctor")]
    public int DoctorId { get; set; }

    [Required]
    [Display(Name = "Unit")]
    public int UnitId { get; set; }

    [Required]
    [Display(Name = "Day")]
    public byte DayOfWeek { get; set; }

    [Required]
    [Display(Name = "Start Time")]
    public TimeSpan StartTime { get; set; }

    [Required]
    [Display(Name = "End Time")]
    public TimeSpan EndTime { get; set; }

    [Display(Name = "Shift")]
    public int? ShiftId { get; set; }

    [Display(Name = "Max Patients")]
    public int? MaxPatients { get; set; }

    public bool IsActive { get; set; } = true;

    // Dropdowns
    public IEnumerable<SelectListItem> DoctorList { get; set; }
    public IEnumerable<SelectListItem> UnitList { get; set; }
    public IEnumerable<SelectListItem> ShiftList { get; set; }
    public IEnumerable<SelectListItem> DayList { get; set; }
}
