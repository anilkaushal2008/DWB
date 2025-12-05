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
        [Authorize(Roles = "Admin, Consultant ")]
        public async Task<IActionResult> EmergencyPatient(string dateRange)
        {
            List<SP_OPD> AllCounseling = new List<SP_OPD>();
            //Get branch ihms code
            int code = Convert.ToInt32(User.FindFirst("HMScode")?.Value);
            //Get Deafult OPD API
            string BaseAPI = (User.FindFirst("BaseAPI")?.Value ?? string.Empty).Replace("\n", "").Replace("\r", "").Trim();
            string finalURL = string.Empty;
            if (dateRange != null)
            {
                var dates = dateRange.Split(" - ");
                DateTime Sdate = DateTime.ParseExact(dates[0], "dd/MM/yyyy", null);
                DateTime Edate = DateTime.ParseExact(dates[1], "dd/MM/yyyy", null);
                string finalSdate = Convert.ToDateTime(Sdate).ToString("dd-MM-yyyy");
                string finalEdate = Convert.ToDateTime(Edate).ToString("dd-MM-yyyy");
                //format = opd?sdate=12-07-2025&edate=12-07-2025&code=1&uhidno=uhid
                finalURL = BaseAPI + "opd?&sdate=" + finalSdate + "&edate=" + finalEdate + "&code=" + code + "&uhidno=null" + "&doctorcode=null";
                //finalURL = BaseAPI + "opd?&sdate=" + finalSdate + "&edate=" + finalEdate + "&code=" + code + "&uhidno=null";

            }
            else if (dateRange == null)
            {
                DateTime today = DateTime.Now;
                string finalSdate = Convert.ToDateTime(today).ToString("dd-MM-yyyy");
                //both start and ende date same
                //format = opd?sdate=12-07-2025&edate=12-07-2025&code=1&uhidno=uhid
                finalURL = BaseAPI + "opd?&sdate=" + finalSdate + "&edate=" + finalSdate + "&code=" + code + "&uhidno=null" + "&doctorcode=null";
            }

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(finalURL);
                var response = await client.GetAsync(finalURL);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(jsonString)) // Ensure jsonString is not null or empty
                    {
                        AllCounseling = JsonConvert.DeserializeObject<List<SP_OPD>>(jsonString) ?? new List<SP_OPD>();                        
                    }
                }
            }
            //get assessed patients
            var intHMScode = Convert.ToInt32(User.FindFirst("HMScode")?.Value);
            var intUnitcode = Convert.ToInt32(User.FindFirst("UnitId")?.Value);
            //check and update completed status of nursing and doctor too
            List<PatientEstimateRecord> tbllist = new List<PatientEstimateRecord>();
            tbllist = await _context.PatientEstimateRecord
               .Where(a => a.IntCode == intUnitcode && a.IntIhmscode == intHMScode && a.BitIsCompleted == true)
               .ToListAsync();
            if (tbllist.Count() != 0)
            {
                foreach (var p in AllCounseling)
                {
                    var t = tbllist.FirstOrDefault(x => x.Uhid == p.opdno && x.VisitNumber == p.visit);
                    if (t != null)
                    {
                        p.bitTempCounselingComplete = t != null ? t.BitIsCompleted : false;
                    }
                }
            }
            return View(AllCounseling);
        }

        // 1. GET: Create New Assessment

        [HttpGet]
        [Authorize(Roles = "Admin, Consultant")]
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
        [Authorize(Roles = "Admin, Consultant")]
        public async Task<IActionResult> Save(TriageViewModel model)
        {
            if (!ModelState.IsValid) return View("Form", model);

            // LOGIC: If Admission Advised -> Skip saving form -> Go to Admission Module
            if (model.IsAdmissionAdvised)
            {
                // You might want to log a small note here, but per your requirement:
                return RedirectToAction("AdmitPatient", "Admission", new { visitId = model.VisitId });
            }

            // Map ViewModel to Entity (Table)
            EmergencyTriageAssessment entity = new EmergencyTriageAssessment
            {
                VisitId = model.VisitId,
                PatientId = model.PatientId,
                PatientName = model.PatientName, // Saving snapshot
                MrnNumber = model.MRN_Number.ToString(),
                Age = model.Age,
                Sex = model.Sex,

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

                CreatedDate = DateTime.Now,
                CreatedByDoctorId = 1 // Get from User.Identity
            };

            _context.EmergencyTriageAssessment.Add(entity);
            await _context.SaveChangesAsync();

            // Redirect to Print
            return RedirectToAction("Print", new { id = entity.AssessmentId });
        }

        // 3. GET: Edit Existing Assessment
        [HttpGet]
        [Authorize(Roles = "Admin, Consultant")]
        public async Task<IActionResult> Edit(long id)
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
        [Authorize(Roles = "Admin, Consultant")]
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
        [Authorize(Roles = "Admin, Consultant")]
        public async Task<IActionResult> Print(long id)
        {
            var entity = await _context.EmergencyTriageAssessment.FindAsync(id);
            return View(entity);
        }
    }
}