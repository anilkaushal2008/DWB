using DWB.APIModel;
using DWB.Models; // Your EF Models
using DWB.ViewModels;
using FluentAssertions.Equivalency;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Data;

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
                    p.CompCode = code;
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

            //for transferring to admission if advised
            //if (model.IsAdmissionAdvised)
            //{
            //    return RedirectToAction("AdmitPatient", "Admission", new { visitId = model.VisitId });
            //}

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
                IsAdmissionAdvised=model.IsAdmissionAdvised,

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

            // Pass the ID of the record you just saved
            ViewBag.AssessmentId = entity.AssessmentId;

            // Return the intermediate view that handles the popup
            return View("PrintSuccess");
        }

        // 3. GET: Edit Existing Assessment
        [HttpGet]
        [Authorize(Roles = "Admin, EMO")]
        public async Task<IActionResult> Edit(string uhid, int visit)
        {
            // 1. FIND THE RECORD using the specific columns
            // We use FirstOrDefaultAsync because we are searching by non-primary keys
            var entity = await _context.EmergencyTriageAssessment
                .FirstOrDefaultAsync(e => e.PatientId == uhid &&
                                          e.VisitId == visit ); // Check exact capitalization of IntCode in your model

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
                Temperature = entity.Temperature,
                BPDiastolic = entity.Bpdiastolic,
                ChiefComplaint = entity.ChiefComplaint ?? "",
                ObjectiveNotes = entity.ObjectiveNotes ?? "",
                Diagnosis = entity.ProvisionalDiagnosis ?? "",     // Maps to Diagnosis
                Plan = entity.TreatmentPlan ?? "",                 // Maps to TreatmentPlan
                TriageCategory = entity.TriageCategory ?? "",
                ConditionUponRelease = entity.ConditionUponRelease ?? "",
                IsAdmissionAdvised=entity.IsAdmissionAdvised??false,
               

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

            entity.TransportationMode = model.TransportationMode;
            entity.Pulse = model.Pulse;
            entity.Bpsystolic = model.BPSystolic;
            entity.Bpdiastolic = model.BPDiastolic;
            entity.RespRate = model.RespRate;
            entity.Temperature = model.Temperature;
            entity.Weight = model.Weight;

            entity.TriageCategory = model.TriageCategory;
            entity.ConditionUponRelease = model.ConditionUponRelease;

            entity.DtUpdated = DateTime.Now;
            entity.VchUpdatedBy = User.Identity?.Name ?? "Unknown";
            entity.IsAdmissionAdvised = model.IsAdmissionAdvised;
            //entity.in

            await _context.SaveChangesAsync();
            //return RedirectToAction("Print", new { uhid=entity.PatientId, visit=entity.VisitId});
            // Pass the ID of the record you just saved
            ViewBag.AssessmentId = entity.AssessmentId;

            // Return the intermediate view that handles the popup
            return View("PrintSuccess");
        }

        //5. print  
        [HttpGet]
        [Authorize(Roles = "Admin, EMO")]
        public async Task<IActionResult> Print(int id)
        {
            if (id ==0) return BadRequest("Invalid detail");

            var obj = _context.EmergencyTriageAssessment
                    .FirstOrDefault(e => e.AssessmentId==id); // Fixed comparison
            // FIX: If ID is wrong or record deleted, show 404 instead of crashing
            if (obj == null)
            {
                return NotFound($"Assessment with ID {id} not found.");
            }

            return View(obj);
        }
        //5. print  
        [HttpGet]
        [Authorize(Roles = "Admin, EMO")]
        public async Task<IActionResult> APIPrint(string uhid, int visit)
        {
            if (uhid ==null && visit == 0) return BadRequest("Invalid detail");

            var obj = _context.EmergencyTriageAssessment
                    .FirstOrDefault(e => e.PatientId==uhid && e.VisitId==visit); // Fixed comparison
            // FIX: If ID is wrong or record deleted, show 404 instead of crashing
            if (obj == null)
            {
                return NotFound($"Assessment with ID {uhid} not found.");
            }
            ////Use the CreatedByDoctorId to find the specific user who saved this form
            //var creatorSignature = await _context.TblUsers // <--- Adjust table name if needed (e.g. TblUser)
            //    .Where(u => u.VchUsername == obj.VchCreatedBy) // Match the ID
            //    .Select(u => u.VchSignFileName) // Select only the filename column
            //    .FirstOrDefaultAsync();
            // Pass it to the view securely
            //ViewBag.ProviderSignature = creatorSignature;
            return View(obj);
        }

        [Authorize(Roles = "Admin, EMO")]
        public async Task<IActionResult> AllTriageReport(string dateRange)
        {
            // 1. DEFAULT DATES (Today)
            DateTime Sdate = DateTime.Today;
            DateTime Edate = DateTime.Today;

            if (!string.IsNullOrEmpty(dateRange))
            {
                var dates = dateRange.Split(" - ");
                if (dates.Length == 2)
                {
                    DateTime.TryParseExact(dates[0], "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out Sdate);
                    DateTime.TryParseExact(dates[1], "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out Edate);
                }
            }

            // 2. FETCH DATA (All records for the date range)
            var intUnitcode = Convert.ToInt32(User.FindFirst("UnitId")?.Value ?? "0");

            var allRecords = await _context.EmergencyTriageAssessment
                .Where(x => x.Intcode == intUnitcode
                         && x.ArrivalDateTime.Date >= Sdate
                         && x.ArrivalDateTime.Date <= Edate
                         && x.BitIsCompleted == true) // Only completed assessments
                .OrderByDescending(x => x.ArrivalDateTime)
                .ToListAsync();

            // 3. PASS DATES FOR UI
            ViewBag.DateRange = $"{Sdate:dd/MM/yyyy} - {Edate:dd/MM/yyyy}";

            // 4. CALCULATE SUMMARY STATS (Optional but useful for "All Usage")
            ViewBag.TotalPatients = allRecords.Count;
            ViewBag.TotalAdmissions = allRecords.Count(x => x.IsAdmissionAdvised == true);
            ViewBag.TotalEmergent = allRecords.Count(x => x.TriageCategory == "Emergent");

            return View(allRecords);
        }
    }
}