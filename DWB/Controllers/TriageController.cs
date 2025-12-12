using DWB.APIModel;
using DWB.Models; // Your EF Models
using DWB.ViewModels;
using FluentAssertions.Equivalency;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Data;

namespace DoctorWorkBench.Controllers
{
    public class TriageController : Controller
    {
        private readonly DWBEntity _context;
        private readonly ILogger<TriageController> _logger;
        private static readonly DateTime SqlMinDate = new DateTime(1753, 1, 1);
        public TriageController(DWBEntity context, ILogger<TriageController> logger)
        {
            _context = context;
            _logger = logger;
        }

        //Get All EMergency patient     
        [Authorize(Roles = "Admin, EMO")]
        public async Task<IActionResult> EmergencyPatient(string dateRange)
        {
            //Get role
            var isAdmin = User.IsInRole("Admin");
            //get doctor code
            var doccode = User.FindFirst("DoctorCode")?.Value;
            if(doccode == "" && isAdmin==false)
            {
                TempData["Error"] = "Doctor code not found, contact to administrator!";
                return View();
            }
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
            string finalURL = $"{BaseAPI}opd?sdate={finalSdate}&edate={finalEdate}&code={code}&uhidno=null&doctorcode={doccode}&opdType=Emergency";

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
                .Where(a => a.IntCode == intUnitcode
                         && a.IntIhmscode == intHMScode
                         && a.BitIsCompleted == true  // <--- Ensure this column exists                        
                         )   // <--- Ensure this column exists
                .Select(x => new { x.VchUhidNo, x.BigintVisitId, x.BitIsCompleted }) // Select only what we need for speed
                .ToListAsync();

            // 4. MAP STATUS TO THE LIST
            if (assessedList.Any() && AllEmergency.Any())
            {
                foreach (var p in AllEmergency)
                {
                    // Check if this specific patient/visit exists in the assessed list
                    // Note: I used p.uhid. If your API uses p.opdno, switch it back.
                    p.bitTempCounselingComplete = assessedList.Any(x => x.BitIsCompleted==true && x.VchUhidNo==p.opdno && x.BigintVisitId==p.visit);
                    p.CompCode = code;
                }
            }

            return View(AllEmergency);
        }


        // GET: Create (prefill basic patient info)
        [HttpGet]
        public IActionResult Create(long visitId, string uhid, string name, int age = 0, string gender = "", string doccode="", string catcode="", string docname="", string catname="")
        {
            var model = new TriageViewModel
            {
                VisitId = visitId,
                PatientId = uhid,
                ArrivalDateTime = DateTime.Now,
                TimeSeenByProvider = DateTime.Now,
                PatientName = name ?? string.Empty,
                UhidNo = uhid ?? string.Empty,
                Age = age > 0 ? age.ToString() : string.Empty,
                Sex = gender ?? string.Empty,
                IsEditMode = false,
                doccode = doccode,
                CategoryCode = catcode,
                docname=docname,
                Category=catname               
            };
            model.ConsultantList = _context.TblUsers
            .Where(u => u.FkRole.VchRole == "Consultant" && u.BitIsDeActivated == false)
            .Select(u => new SelectListItem
            {
              Value = u.IntUserId.ToString(),
             Text = $"{u.VchFullName}"
             })
        .ToList();
            // ⭐ ADD OTHER OPTION AT END
            model.ConsultantList.Add(new SelectListItem
            {
                Value = "0",
                Text = "Other (Specify Doctor)"
            });

            return View("Form", model);
        }

        //POST: Save new assessment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(TriageViewModel model)
        {
            try
            {
                if (model == null) return BadRequest("Invalid form submission.");

                if (!ModelState.IsValid) return View("Form", model);

                if (model.ArrivalDateTime < SqlMinDate) model.ArrivalDateTime = DateTime.Now;
                if (model.TimeSeenByProvider < SqlMinDate) model.TimeSeenByProvider = DateTime.Now;
                if (model.TimeOfRelease.HasValue && model.TimeOfRelease < SqlMinDate)
                    model.TimeOfRelease = DateTime.Now;

                var intHMScode = Convert.ToInt32(User.FindFirst("HMScode")?.Value ?? "0");
                var intUnitcode = Convert.ToInt32(User.FindFirst("UnitId")?.Value ?? "0");
                var userId = Convert.ToInt32(User.FindFirst("UserId")?.Value ?? "1");
                string ReferedDOctor = string.Empty;
                if(model.ConsultantDoctorId!=0 && model.ConsultantDoctorId != null)
                {
                    var getDocUser = (from e in _context.TblUsers where e.IntUserId == model.ConsultantDoctorId select e).FirstOrDefault();
                    if (getDocUser != null)
                    {
                        ReferedDOctor=getDocUser.VchFullName.ToString();
                    }
                }

                var entity = new EmergencyTriageAssessment
                {
                    BigintVisitId = model.VisitId,
                    VchPatientId = model.PatientId ?? "",
                    VchPatientName = model.PatientName ?? "",
                    VchUhidNo = model.UhidNo ?? model.UhidNo ?? "",
                    VchAge = model.Age ?? "",
                    VchSex = model.Sex ?? "",
                    VchPhoneNumber = model.PhoneNo ?? "",
                    VchAddress = model.PatientAddress ?? "",                   

                    // Arrival & provider
                    DtArrivalDateTime = model.ArrivalDateTime,
                    DtTimeSeenByProvider = model.TimeSeenByProvider,

                    // Transportation
                    VchTransportationMode = model.TransportationMode ?? "",
                    VchTransportationOther = model.TransportationOther ?? "",
                    VchHistoryObtainedFrom = model.HistoryObtainedFrom ?? "",
                    
                    // Doctor & Category Info
                    VchDoctorCode = model.doccode ?? "",
                    VchCatCode = model.CategoryCode ?? "",
                    Vchdocname = model.docname ?? "",
                    VchCategory = model.Category ?? "",

                    // Vitals
                    DtVitalsTime = model.VitalsTime,
                    VchPulse = model.Pulse ?? "",
                    IntBpsystolic = model.BPSystolic,
                    IntBpdiastolic = model.BPDiastolic,
                    IntRespRate = model.RespRate,
                    DecTemperature = model.Temperature,
                    DecWeight = model.Weight,

                    // Clinical
                    VchChiefComplaint = model.ChiefComplaint ?? "",
                    VchCurrentMedication = model.CurrentMedication ?? "",
                    VchAllergies = model.Allergies ?? "",
                    VchSubjectiveNotes = model.SubjectiveNotes ?? "",
                    VchObjectiveNotes = model.ObjectiveNotes ?? "",
                    VchInvestigationsOrdered = model.InvestigationsOrdered ?? "",
                    VchProvisionalDiagnosis = model.Diagnosis ?? "",
                    VchTreatmentPlan = model.Plan ?? "",

                    // Outcome
                    VchTriageCategory = model.TriageCategory ?? "",
                    VchConditionUponRelease = model.ConditionUponRelease ?? "",
                    DtDischargeDateTime = model.TimeOfRelease,

                    //room and bed no
                    VchRoomWardNumber=model.RoomWardNumber ?? "",
                    VchBedNumber=model.BedNumber ?? "",

                    //indoor advice
                    BitIsAdmissionAdvised = model.IsAdmissionAdvised,
                    VchAdmissionRefusalReason = model.AdmissionRefusalReason ?? "",                    
                    BitIsCrossConsultRequired = model.IsCrossConsultRequired,
                    VchSpecialistName = model.SpecialistName ?? "",
                    VchReferredTo = model.ReferredTo ?? "",

                    // refered doctor
                    ConsultantDoctorId = model.ConsultantDoctorId,                    
                    OtherDoctorName = ReferedDOctor,

                    DtFollowUpDate = model.FollowUpDate,
                    DtFollowUpTime = model.FollowUpTime,
                    VchPatientInstructions = model.PatientInstructions ?? "",
                    VchRemarks = model.Remarks ?? "",

                    // System fields
                    BitIsCompleted = true,
                    IntCode = intUnitcode,
                    IntIhmscode = intHMScode,

                    DtCreatedDate = DateTime.Now,
                    DtCreated = DateTime.Now,
                    BigintCreatedByDoctorId = userId,
                    VchCreatedByDoctorName = User.Identity?.Name ?? "",
                    VchCreatedBy = User.Identity?.Name ?? "",
                };
                _context.EmergencyTriageAssessment.Add(entity);
                await _context.SaveChangesAsync();

                ViewBag.AssessmentId = entity.BigintAssessmentId;
                _logger.LogInformation("Created triage assessment {Id} for Visit {VisitId}", entity.BigintAssessmentId, model.VisitId);

                // redirect to Print action (Post-redirect-get)
                //return JavaScriptRedirect(entity.BigintAssessmentId);
                TempData["PrintId"] = entity.BigintAssessmentId.ToString();
                return RedirectToAction("EmergencyPatient");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving triage assessment for visit {VisitId}", model?.VisitId);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while saving the triage assessment.");
                return View("Form", model);
            }
        }

        [Authorize(Roles = "Admin, EMO")]
        public async Task<IActionResult> AssessedEmg()
        {
            // Admin → View ALL records
            if (User.IsInRole("Admin"))
            {
                var allEmg = await _context.EmergencyTriageAssessment
                    .Where(e => e.BitIsCompleted == true)
                    .OrderByDescending(e => e.DtCreated)
                    .ToListAsync();

                return View(allEmg);
            }

            // EMO (Doctor) → Only their own cases
            var doctorCode = User.FindFirst("DoctorCode")?.Value;

            if (string.IsNullOrEmpty(doctorCode))
                return BadRequest("Doctor code not found in user claims.");

            var doctorEmg = await _context.EmergencyTriageAssessment
                .Where(e => e.BitIsCompleted == true &&
                            e.VchDoctorCode == doctorCode)
                .OrderByDescending(e => e.DtCreated)
                .ToListAsync();
            return View(doctorEmg);
        }


        //GET: Edit existing assessment
        [HttpGet]
        public async Task<IActionResult> Edit(int assmentID)
        {
            
            var entity = await _context.EmergencyTriageAssessment
                    .FirstOrDefaultAsync(e => e.BigintAssessmentId == assmentID);            
            if (entity == null) return NotFound("No triage assessment found for this patient & visit.");
           

            var model = new TriageViewModel
            {
                AssessmentId = entity.BigintAssessmentId,
                VisitId = entity.BigintVisitId,
                PatientId = entity.VchPatientId,
                PatientName = entity.VchPatientName,
                UhidNo = entity.VchUhidNo,
                Age = entity.VchAge,
                Sex = entity.VchSex,
                PhoneNo = entity.VchPhoneNumber,
                PatientAddress = entity.VchAddress,
                RoomWardNumber = entity.VchRoomWardNumber,
                BedNumber = entity.VchBedNumber,
                //enable room/bed
                EnableRoomBed = !(string.IsNullOrEmpty(entity.VchRoomWardNumber) &&
                          string.IsNullOrEmpty(entity.VchBedNumber)),
                ArrivalDateTime = entity.DtArrivalDateTime,
                TimeSeenByProvider = entity.DtTimeSeenByProvider,

                TransportationMode = entity.VchTransportationMode,
                TransportationOther = entity.VchTransportationOther,
                HistoryObtainedFrom = entity.VchHistoryObtainedFrom,

                VitalsTime = entity.DtVitalsTime,
                Pulse = entity.VchPulse,
                BPSystolic = entity.IntBpsystolic,
                BPDiastolic = entity.IntBpdiastolic,
                RespRate = entity.IntRespRate,
                Temperature = entity.DecTemperature,
                Weight = entity.DecWeight,

                ChiefComplaint = entity.VchChiefComplaint,
                CurrentMedication = entity.VchCurrentMedication,
                Allergies = entity.VchAllergies,
                SubjectiveNotes = entity.VchSubjectiveNotes,
                ObjectiveNotes = entity.VchObjectiveNotes,
                InvestigationsOrdered = entity.VchInvestigationsOrdered,
                Diagnosis = entity.VchProvisionalDiagnosis,
                Plan = entity.VchTreatmentPlan,                

                TriageCategory = entity.VchTriageCategory,
                ConditionUponRelease = entity.VchConditionUponRelease,
                TimeOfRelease = entity.DtDischargeDateTime,               

                IsAdmissionAdvised = entity.BitIsAdmissionAdvised ?? false,
                AdmissionRefusalReason = entity.VchAdmissionRefusalReason,
                IsCrossConsultRequired = entity.BitIsCrossConsultRequired,
                SpecialistName = entity.VchSpecialistName,
                ReferredTo = entity.VchReferredTo,

                FollowUpDate = entity.DtFollowUpDate,
                FollowUpTime = entity.DtFollowUpTime,
                PatientInstructions = entity.VchPatientInstructions,
                Remarks = entity.VchRemarks,

                BitIsCompleted = entity.BitIsCompleted,
                IsEditMode = true
            };
            model.ConsultantList = _context.TblUsers
    .Where(u => u.FkRole.VchRole == "Consultant" && u.BitIsDeActivated == false)
    .Select(u => new SelectListItem
    {
        Value = u.IntUserId.ToString(),
        Text = $"{u.VchFullName}",

        // 👇 FIX 2: Compare Current User (u) with Entity Value
        Selected = u.IntUserId == entity.ConsultantDoctorId
    })
    .ToList();
            // ⭐ ADD OTHER OPTION AT END
            model.ConsultantList.Add(new SelectListItem
            {
                Value = "0",
                Text = "Other (Specify Doctor)"
            });
            
            return View("Form", model);
        }

        // POST: Update existing assessment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(TriageViewModel model)
        {
            try
            {
                if (model == null) return BadRequest("Invalid submission.");
                //if (!ModelState.IsValid) return View("Form", model);

                var entity = await _context.EmergencyTriageAssessment.FindAsync(model.AssessmentId);
                if (entity == null) return NotFound($"Assessment with ID {model.AssessmentId} not found.");

                // Map editable fields
                entity.VchPatientName = model.PatientName ?? entity.VchPatientName;
                entity.VchUhidNo = model.UhidNo ?? entity.VchUhidNo;
                entity.VchAge = model.Age ?? entity.VchAge;
                entity.VchSex = model.Sex ?? entity.VchSex;
                entity.VchPhoneNumber = model.PhoneNo ?? entity.VchPhoneNumber;
                entity.VchAddress = model.PatientAddress ?? entity.VchAddress;
                entity.VchRoomWardNumber = model.RoomWardNumber ?? entity.VchRoomWardNumber;
                entity.VchBedNumber = model.BedNumber ?? entity.VchBedNumber;

                entity.DtArrivalDateTime = model.ArrivalDateTime;
                entity.DtTimeSeenByProvider = model.TimeSeenByProvider;

                entity.VchTransportationMode = model.TransportationMode ?? entity.VchTransportationMode;
                entity.VchTransportationOther = model.TransportationOther ?? entity.VchTransportationOther;
                entity.VchHistoryObtainedFrom = model.HistoryObtainedFrom ?? entity.VchHistoryObtainedFrom;

                entity.DtVitalsTime = model.VitalsTime ?? entity.DtVitalsTime;
                entity.VchPulse = model.Pulse ?? entity.VchPulse;
                entity.IntBpsystolic = model.BPSystolic;
                entity.IntBpdiastolic = model.BPDiastolic;
                entity.IntRespRate = model.RespRate;
                entity.DecTemperature = model.Temperature;
                entity.DecWeight = model.Weight;

                //refer doctor
                entity.ConsultantDoctorId = model.ConsultantDoctorId??entity.ConsultantDoctorId;
                entity.OtherDoctorName = model.OtherDoctorName??entity.OtherDoctorName;

                entity.VchChiefComplaint = model.ChiefComplaint ?? entity.VchChiefComplaint;
                entity.VchCurrentMedication = model.CurrentMedication ?? entity.VchCurrentMedication;
                entity.VchAllergies = model.Allergies ?? entity.VchAllergies;
                entity.VchSubjectiveNotes = model.SubjectiveNotes ?? entity.VchSubjectiveNotes;
                entity.VchObjectiveNotes = model.ObjectiveNotes ?? entity.VchObjectiveNotes;
                entity.VchInvestigationsOrdered = model.InvestigationsOrdered ?? entity.VchInvestigationsOrdered;
                entity.VchProvisionalDiagnosis = model.Diagnosis ?? entity.VchProvisionalDiagnosis;
                entity.VchTreatmentPlan = model.Plan ?? entity.VchTreatmentPlan;

                entity.VchTriageCategory = model.TriageCategory ?? entity.VchTriageCategory;
                entity.VchConditionUponRelease = model.ConditionUponRelease ?? entity.VchConditionUponRelease;
                entity.DtDischargeDateTime = model.TimeOfRelease ?? entity.DtDischargeDateTime;

                entity.BitIsAdmissionAdvised = model.IsAdmissionAdvised;
                entity.VchAdmissionRefusalReason = model.AdmissionRefusalReason ?? entity.VchAdmissionRefusalReason;
                entity.BitIsCrossConsultRequired = model.IsCrossConsultRequired;
                entity.VchSpecialistName = model.SpecialistName ?? entity.VchSpecialistName;
                entity.VchReferredTo = model.ReferredTo ?? entity.VchReferredTo;

                entity.DtFollowUpDate = model.FollowUpDate ?? entity.DtFollowUpDate;
                entity.DtFollowUpTime = model.FollowUpTime ?? entity.DtFollowUpTime;
                entity.VchPatientInstructions = model.PatientInstructions ?? entity.VchPatientInstructions;
                entity.VchRemarks = model.Remarks ?? entity.VchRemarks;

                entity.BitIsCompleted = model.BitIsCompleted;
                entity.IntIhmscode = Convert.ToInt32(User.FindFirst("HMScode")?.Value ?? "0");
                entity.IntCode = Convert.ToInt32(User.FindFirst("UnitId")?.Value ?? "0");

                entity.DtUpdated = DateTime.Now;
                entity.VchUpdatedBy = User.Identity?.Name ?? "Unknown";

                await _context.SaveChangesAsync();

                TempData["PrintId"] = entity.BigintAssessmentId.ToString();
                return RedirectToAction("AssessedEmg");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating triage assessment {Id}", model?.AssessmentId);
                ModelState.AddModelError(string.Empty, "An error occurred while updating the triage assessment.");
                return View("Form", model);
            }
        }

        // GET: Print
        [Authorize(Roles = "Admin, EMO")]
        [HttpGet]
        public async Task<IActionResult> Print(long id)
        {
            if (id <= 0) return BadRequest("Invalid assessment id.");

            var obj = await _context.EmergencyTriageAssessment.FirstOrDefaultAsync(e => e.BigintAssessmentId == id);
            if (obj == null) return NotFound($"Assessment with ID {id} not found.");

            // Get creator signature/name if present
            var creator = await _context.TblUsers.FirstOrDefaultAsync(u => u.VchUsername == obj.VchCreatedBy);
            ViewBag.ProviderSignature = creator?.VchSignFileName ?? string.Empty;
            ViewBag.ProviderFullName = creator?.VchFullName ?? obj.VchCreatedByDoctorName;

            return View("Print", obj);
        }

        // GET: Print
        [Authorize(Roles = "Admin, EMO")]
        [HttpGet]
        public async Task<IActionResult> APIPrint(string uhid, int visiNo)
        {
            if (uhid == null && visiNo!=0) return BadRequest("Invalid assessment id.");

            var obj = await _context.EmergencyTriageAssessment.FirstOrDefaultAsync(e => e.VchUhidNo == uhid && e.BigintVisitId==visiNo);
            if (obj == null) return NotFound($"Assessment with ID {uhid} not found.");

            // Get creator signature/name if present
            var creator = await _context.TblUsers.FirstOrDefaultAsync(u => u.VchUsername == obj.VchCreatedBy);
            ViewBag.ProviderSignature = creator?.VchSignFileName ?? string.Empty;
            ViewBag.ProviderFullName = creator?.VchFullName ?? obj.VchCreatedByDoctorName;

            return View("Print", obj);
        }

    }
}