using DWB.APIModel;
using DWB.Models; // Your EF Models
using DWB.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;

namespace DoctorWorkBench.Controllers
{
    public class TriageController : Controller
    {
        private readonly DWBEntity _context;

        public TriageController(DWBEntity context)
        {
            _context = context;
        }

        //Get All EMergency patient     
        [Authorize(Roles = "Admin, EMO")]
        public async Task<IActionResult> EmergencyPatient(string dateRange)
        {
            List<SP_OPD> AllEmergency = new List<SP_OPD>();

            // 1. SETUP DATES
            int code = Convert.ToInt32(User.FindFirst("HMScode")?.Value);
            string BaseAPI = (User.FindFirst("BaseAPI")?.Value ?? string.Empty).Replace("\n", "").Replace("\r", "").Trim();

            DateTime Sdate = DateTime.Today;
            DateTime Edate = DateTime.Today;

            if (!string.IsNullOrEmpty(dateRange))
            {
                var dates = dateRange.Split(" - ");
                DateTime.TryParseExact(dates[0], "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out Sdate);
                DateTime.TryParseExact(dates[1], "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out Edate);
            }

            string finalSdate = Sdate.ToString("dd-MM-yyyy");
            string finalEdate = Edate.ToString("dd-MM-yyyy");

            // 2. CALL API (Get Only Emergency Patients)
            // We append &opdType=Emergency so the API filters the list for us.
            string finalURL = $"{BaseAPI}opd?sdate={finalSdate}&edate={finalEdate}&code={code}&uhidno=null&doctorcode=null&opdType=Emergency";

            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync(finalURL);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(jsonString))
                        {
                            AllEmergency = JsonConvert.DeserializeObject<List<SP_OPD>>(jsonString) ?? new List<SP_OPD>();
                        }
                    }
                }
                catch (Exception)
                {
                    // Ideally log the error here
                }
            }

            // 3. CHECK COMPLETION STATUS (Using EmergencyTriageAssessment)
            var intHMScode = Convert.ToInt32(User.FindFirst("HMScode")?.Value);
            var intUnitcode = Convert.ToInt32(User.FindFirst("UnitId")?.Value);

            // We convert the Date to string to match your database format
            string dbSdate = Sdate.ToString("dd/MM/yyyy");

            // Query the correct table
            var assessedList = await _context.EmergencyTriageAssessment
                .Where(a => a.Intcode == intUnitcode
                         && a.IntIhmscode == intHMScode
                         && a.BitIsCompleted == true  // <--- Ensure this column exists                        
                         )   // <--- Ensure this column exists
                .Select(x => new { x.PatientId, x.VisitId, x.BitIsCompleted }) // Select only what we need for speed
                .ToListAsync();

            // 4. MAP STATUS TO THE LIST
            if (assessedList.Any() && AllEmergency.Any())
            {
                foreach (var p in AllEmergency)
                {
                    // Check if this specific patient/visit exists in the assessed list
                    // Note: I used p.uhid. If your API uses p.opdno, switch it back.
                    p.bitTempCounselingComplete = assessedList.Any(x => x.BitIsCompleted==true && x.PatientId==p.opdno && x.VisitId==p.visit);
                }
            }

            return View(AllEmergency);
        }
      

        [HttpGet]
        [Authorize(Roles = "Admin, EMO")]
        public async Task<IActionResult> Create(int visitId, string uhid, string name, int age, string gender)
        {
            // TODO: Call your HMS API here to get Patient Details
            var model = new TriageViewModel
            {
                VisitId = visitId,
                PatientId = uhid,
                ArrivalDateTime = DateTime.Now,
                TimeSeenByProvider = DateTime.Now,

                // Pre-fill from API data
                PatientName = name, // patientData.Name
                MRN_Number = uhid.ToString(),         // patientData.MRN
                Age = age.ToString(),
                Sex = gender,
                IsEditMode = false
            };

            return View("Form", model); // Reusing 'Form.cshtml' for both Create/Edit
        }

        // 2. POST: Save Data
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, EMO")]
        public async Task<IActionResult> Save(TriageViewModel model)
        {
            if (!ModelState.IsValid) return View("Form", model);

            if (model.IsAdmissionAdvised)
            {
                return RedirectToAction("AdmitPatient", "Admission", new { visitId = model.VisitId });
            }

            // --- STEP 1: FIX THE SQL DATE OVERFLOW ERROR ---
            // If the date is not selected (Year 0001), set it to NOW.
            if (model.ArrivalDateTime < DateTime.Parse("01/01/1753"))
            {
                model.ArrivalDateTime = DateTime.Now;
            }

            if (model.TimeSeenByProvider < DateTime.Parse("01/01/1753"))
            {
                model.TimeSeenByProvider = DateTime.Now;
            }

            // --- STEP 2: GET CODES ---
            var intHMScode = Convert.ToInt32(User.FindFirst("HMScode")?.Value ?? "0");
            var intUnitcode = Convert.ToInt32(User.FindFirst("UnitId")?.Value ?? "0");
            var userId = Convert.ToInt32(User.FindFirst("UserId")?.Value ?? "1");

            // --- STEP 3: MAP ENTITY ---
            EmergencyTriageAssessment entity = new EmergencyTriageAssessment
            {
                VisitId = model.VisitId,
                PatientId = model.PatientId,
                PatientName = model.PatientName,
                MrnNumber = model.MRN_Number.ToString(),
                Age = model.Age,
                Sex = model.Sex,

                // These are now safe because of Step 1
                ArrivalDateTime = model.ArrivalDateTime,
                TimeSeenByProvider = model.TimeSeenByProvider,
                TransportationMode = model.TransportationMode,

                Pulse = model.Pulse,
                Bpsystolic = model.BPSystolic,
                Bpdiastolic = model.BPDiastolic,
                RespRate = model.RespRate,
                Temperature = model.Temperature,
                Weight = model.Weight,

                ChiefComplaint = model.ChiefComplaint,
                ObjectiveNotes = model.ObjectiveNotes,
                ProvisionalDiagnosis = model.Diagnosis,
                TreatmentPlan = model.Plan,

                TriageCategory = model.TriageCategory,
                ConditionUponRelease = model.ConditionUponRelease,

                CreatedByDoctorId = userId,
                Intcode = intUnitcode,
                IntIhmscode = intHMScode,
                BitIsCompleted = true,

                // This Date is safe
                DtCreated = DateTime.Now,
                VchCreatedBy= User.Identity?.Name ?? "Unknown"

                // --- IMPORTANT FOR DASHBOARD ---
                // You MUST save this string date, or your Dashboard count will be 0.
                // Assuming your column name is EntryDate or VchDate (check your table)
                //CreatedDate = DateTime.Now.ToString("dd/MM/yyyy")
            };

            _context.EmergencyTriageAssessment.Add(entity);
            await _context.SaveChangesAsync();

            return RedirectToAction("Print", new { id = entity.AssessmentId });
        }

        // 3. GET: Edit Existing Assessment
        [HttpGet]
        [Authorize(Roles = "Admin, EMO")]
        public async Task<IActionResult> Edit(long id) //uhid,visitno to get
        {
            var entity = await _context.EmergencyTriageAssessment.FindAsync(id);
            if (entity == null) return NotFound();

            // Map Entity back to ViewModel
            var triageViewModel = new TriageViewModel
            {
                AssessmentId = entity.AssessmentId,
                VisitId = entity.VisitId,
                PatientId = entity.PatientId.ToString(),
                PatientName = entity.PatientName,
                MRN_Number = entity.MrnNumber,
                Age = entity.Age,
                Sex = entity.Sex,

                ChiefComplaint = entity.ChiefComplaint,
                Diagnosis = entity.ProvisionalDiagnosis,
                Plan = entity.TreatmentPlan,
                TriageCategory = entity.TriageCategory,
                ConditionUponRelease = entity.ConditionUponRelease,

                // Map Vitals...
                Pulse = entity.Pulse,
                BPSystolic = entity.Bpsystolic,

                IsEditMode = true
            };

            return View("Form", triageViewModel);
        }

        // 4. POST: Update Existing
        [HttpPost]
        [Authorize(Roles = "Admin, EMO")]
        public async Task<IActionResult> Update(TriageViewModel model)
        {
            var entity = await _context.EmergencyTriageAssessment.FindAsync(model.AssessmentId);
            if (entity == null) return NotFound();

            // Update fields
            entity.ChiefComplaint = model.ChiefComplaint;
            entity.ProvisionalDiagnosis = model.Diagnosis;
            entity.TreatmentPlan = model.Plan;
            entity.TriageCategory = model.TriageCategory;
            entity.ConditionUponRelease = model.ConditionUponRelease;
            entity.Pulse = model.Pulse;
            // ... update other fields as needed

            await _context.SaveChangesAsync();
            return RedirectToAction("Print", new { id = entity.AssessmentId });
        }

        // 5. GET: Print View
        [Authorize(Roles = "Admin, EMO")]
        public async Task<IActionResult> Print(long id)
        {
            var entity = await _context.EmergencyTriageAssessment.FindAsync(id);
            return View(entity);
        }
    }
}