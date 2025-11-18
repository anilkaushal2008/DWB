using DWB.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DWB.Services
{
    public class DoctorScheduleService : IDoctorScheduleService
    {
        private readonly DWBEntity _context;

        public DoctorScheduleService(DWBEntity context)
        {
            _context = context;
        }

        public async Task<string> GetScheduleStringAsync(int doctorId, int companyId)
        {
            var doctorSchedule = await _context.TblUserCompany
                .FirstOrDefaultAsync(s => s.FkUseriId == doctorId && s.FkIntCompanyId == companyId);

            string scheduleString = ""; // Default empty

            if (doctorSchedule != null && doctorSchedule.TimeStartTime.HasValue && doctorSchedule.TimeEndTime.HasValue)
            {
                // 1. Collect selected days as DayOfWeek Enums (Integers)
                //    Sunday = 0, Monday = 1, etc.
                var selectedDays = new List<DayOfWeek>();

                if (doctorSchedule.BitIsSunday) selectedDays.Add(DayOfWeek.Sunday);
                if (doctorSchedule.BitIsMonday) selectedDays.Add(DayOfWeek.Monday);
                if (doctorSchedule.BitIsTuesday) selectedDays.Add(DayOfWeek.Tuesday);
                if (doctorSchedule.BitIsWednesday) selectedDays.Add(DayOfWeek.Wednesday);
                if (doctorSchedule.BitIsThursday) selectedDays.Add(DayOfWeek.Thursday);
                if (doctorSchedule.BitIsFriday) selectedDays.Add(DayOfWeek.Friday);
                if (doctorSchedule.BitIsSaturday) selectedDays.Add(DayOfWeek.Saturday);

                if (selectedDays.Any())
                {
                    // 2. Use helper method to format "Mon-Wed", "Mon, Wed, Fri", etc.
                    string dayString = FormatDayRanges(selectedDays);

                    // 3. Format Time
                    string timeString = $"{doctorSchedule.TimeStartTime.Value:hh:mm tt} - {doctorSchedule.TimeEndTime.Value:hh:mm tt}";

                    // 4. Final String
                    scheduleString = $"{dayString} ({timeString})";
                }
            }

            return scheduleString;
        }

        // --- HELPER METHOD FOR GROUPING DAYS ---
        private string FormatDayRanges(List<DayOfWeek> days)
        {
            if (days.Count == 0) return "";

            // Sort days to be safe (Sun, Mon, Tue...)
            days.Sort();

            var groups = new List<string>();
            int start = 0;

            // Loop through the days to find consecutive sequences
            for (int i = 0; i < days.Count; i++)
            {
                // Check if the next day is consecutive (e.g., Mon(1) + 1 == Tue(2))
                // If we are at the last item, or next item is NOT consecutive...
                if (i == days.Count - 1 || days[i + 1] != days[i] + 1)
                {
                    // We found the end of a group (from index 'start' to index 'i')
                    int count = i - start + 1;

                    if (count >= 3)
                    {
                        // Case: 3 or more days (e.g., Mon, Tue, Wed) -> "Mon-Wed"
                        groups.Add($"{days[start].ToString().Substring(0, 3)}-{days[i].ToString().Substring(0, 3)}");
                    }
                    else if (count == 2)
                    {
                        // Case: 2 days (e.g., Mon, Tue) -> "Mon, Tue" (Usually looks better than Mon-Tue)
                        groups.Add($"{days[start].ToString().Substring(0, 3)}, {days[i].ToString().Substring(0, 3)}");
                    }
                    else
                    {
                        // Case: 1 day -> "Mon"
                        groups.Add(days[start].ToString().Substring(0, 3));
                    }

                    // Reset start for the next group
                    start = i + 1;
                }
            }

            return string.Join(", ", groups);
        }
    }
}