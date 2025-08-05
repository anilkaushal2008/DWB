using DWB.APIModel;
using DWB.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Reflection.Metadata;

namespace DWB.Controllers
{
    public class AssessmentController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly DWBEntity _context;
        public AssessmentController(IConfiguration configuration, DWBEntity dWBEntity)
        {
            _configuration = configuration;
            _context = dWBEntity;
        }
        //GET:AssessmentController
        public async Task<IActionResult> NursingAssessment(string dateRange)
        {
            List<SP_OPD> patients = new List<SP_OPD>();
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
                finalURL = BaseAPI + "opd?&sdate=" + finalSdate + "&edate=" + finalEdate + "&code=" + code + "&uhidno=null";
            }
            else
            {
                //set today date patient view               
                string today = DateTime.Now.ToString("dd-MM-yyyy");
                //format = opd?sdate=12-07-2025&edate=12-07-2025&code=1&uhidno=null
                finalURL = BaseAPI + "opd?&sdate=" + today + "&edate=" + today + "&code=" + code + "&uhidno=null";
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
                        patients = JsonConvert.DeserializeObject<List<SP_OPD>>(jsonString) ?? new List<SP_OPD>(); // Handle possible null value
                    }
                }
            }
            //get assessed patients
            var intHMScode = Convert.ToInt32(User.FindFirst("HMScode")?.Value);
            var intUnitcode = Convert.ToInt32(User.FindFirst("UnitId")?.Value);
            //check and update completed status of nursing and doctor too
            List<TblNsassessment> tbllist = new List<TblNsassessment>();
            tbllist = await _context.TblNsassessment
               .Where(a => a.IntCode == intUnitcode && a.IntHmscode==intHMScode && a.BitIsCompleted == true)
               .ToListAsync();
            if (tbllist.Count() != 0)
            {
                foreach (var p in patients)
                {
                    var t = tbllist.FirstOrDefault(x => x.VchUhidNo == p.opdno && x.IntIhmsvisit==p.visit);
                    if (t!=null)
                    {
                        p.bitTempNSAssComplete = t != null ? t.BitIsCompleted : false;
                        p.bitTempDOcAssComplete = t != null ? t.BitIsDoctorCompleted : false; // Adjust based on your field name
                    }                   
                }               
            }
            return View(patients);
        }

        [HttpGet]
        public IActionResult NAssessmentCreate(string uhid, string pname,string visit,string gender, string age,string jdate, string timein, string consultant, string category)
        {
            var model = new TblNsassessment
            {
                VchUhidNo = uhid,
                VchHmsname = pname,
                VchHmsage = age,
                VchHmsdtEntry = DateTime.ParseExact(jdate, "MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"),
                VchIhmintime = timein,
                VchHmsconsultant = consultant,
                VchHmscategory = category,
                DtStartTime = DateTime.Now,  
                IntIhmsvisit=Convert.ToInt32(visit),
                VchCreatedBy = User.Identity.Name.ToString(),
                VchIpUsed = HttpContext.Connection.RemoteIpAddress.ToString()                
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NAssessmentCreate(TblNsassessment model, List<IFormFile>? UploadFiles)
        {
            if (ModelState.IsValid)
            {
                // Save logic here, for example:
                model.DtEndTime = DateTime.Now; // Set end time to now
                model.VchTat = null; // Set TAT to null initially
                model.VchCreatedBy = User.Identity.Name; // Set created by user
                model.VchIpUsed = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"; // Get IP address
                model.BitIsCompleted = true;
                model.IntHmscode = Convert.ToInt32(User.FindFirst("HMScode")?.Value);
                model.IntCode = Convert.ToInt32(User.FindFirst("UnitId")?.Value);
                //model.IntYr= get year from OPD api (pending in api)
                _context.TblNsassessment.Add(model);
                _context.SaveChanges();
                //save uploaded files if any
                if (UploadFiles != null && UploadFiles.Any())
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".pdf" };
                    var uploadFolder = Path.Combine("wwwroot/uploads/NAssessment");

                    if (!Directory.Exists(uploadFolder))
                        Directory.CreateDirectory(uploadFolder);

                    foreach (var file in UploadFiles)
                    {
                        var ext = Path.GetExtension(file.FileName).ToLower();

                        if (!allowedExtensions.Contains(ext))
                            continue;
                        var originalName = Path.GetFileNameWithoutExtension(file.FileName);
                        var cleanedName = originalName.Replace(" ", "_");
                        var savedName = $"{cleanedName}{ext}"; //$"{Guid.NewGuid()}{ext}";
                        var filePath = Path.Combine(uploadFolder, savedName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        var assessmentFile = new TblNassessmentDoc
                        {
                            IntFkAssId = model.IntAssessmentId,
                            VchFileName = file.FileName,
                            VchFilePath = "/uploads/assessments/" + savedName,
                            VchCreatedBy = User.Identity.Name,
                            VchCreatedHost = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                            VchCreatedIp= HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                            BitIsForNassessment=true
                        };
                        _context.TblNassessmentDoc.Add(assessmentFile);
                        await _context.SaveChangesAsync();
                    }
                }
                TempData["Success"] = "Nursing assessment saved successfully!";
                return RedirectToAction("NursingAssessment"); // or redirect to listing or details
            }
            // Model is invalid; re-display the form with errors
            return View(model);
        }       

        [HttpGet]
        public IActionResult ViewAssessment(string uhid, int visit, string date)
        {
            var data = _context.TblNsassessment.FirstOrDefault(x => x.VchUhidNo == uhid && x.IntIhmsvisit==visit && x.BitIsCompleted == true && x.VchHmsdtEntry==date);
            if (data == null)
                return Content("<div class='alert alert-warning'>No data found.</div>");

            return PartialView("_NsAssessmentView", data);
        }
        public IActionResult Edit(string uhid, int visit, string date)
        {
            //check param reach here
            var model = _context.TblNsassessment
                        .FirstOrDefault(x => x.VchUhidNo == uhid && x.IntIhmsvisit == visit && x.VchHmsdtEntry == date);
            return PartialView("_EditAssessmentPartial", model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(TblNsassessment model)
        {
            if (!ModelState.IsValid)
            {
                //Return the partial view with validation errors
                return PartialView("_EditAssessmentPartial", model);
            }
            var existingRecord = await _context.TblNsassessment.FindAsync(model.IntAssessmentId);
            if (existingRecord == null)
            {
                return NotFound();
            }
            // Update fields
            existingRecord.VchBloodPressure = model.VchBloodPressure;
            existingRecord.VchPulse = model.VchPulse;
            existingRecord.DecTemperature = model.DecTemperature;
            existingRecord.DecSpO2 = model.DecSpO2;
            existingRecord.DecWeight = model.DecWeight;
            existingRecord.DecHeight = model.DecHeight;
            existingRecord.DecRespiratoryRate = model.DecRespiratoryRate;
            existingRecord.DecOxygenFlowRate = model.DecOxygenFlowRate;

            existingRecord.BitIsAllergical = model.BitIsAllergical;
            existingRecord.VchAllergicalDrugs = model.VchAllergicalDrugs;
            existingRecord.BitIsAlcoholic = model.BitIsAlcoholic;
            existingRecord.BitIsSmoking = model.BitIsSmoking;
            existingRecord.VchLmpForFemale = model.VchLmpForFemale;

            existingRecord.BitDiabetes = model.BitDiabetes;
            existingRecord.BitHeartDisease = model.BitHeartDisease;
            existingRecord.BitHypertension = model.BitHypertension;
            existingRecord.BitAsthma = model.BitAsthma;
            existingRecord.BitCholesterol = model.BitCholesterol;
            existingRecord.BitTuberculosis = model.BitTuberculosis;
            existingRecord.BitSurgery = model.BitSurgery;
            existingRecord.BitHospitalization = model.BitHospitalization;
            existingRecord.VchOtherHistory = model.VchOtherHistory;

            existingRecord.VchPsychologicalStatus = model.VchPsychologicalStatus;
            existingRecord.VchOccupation = model.VchOccupation;
            existingRecord.VchSocialEconomicStatus = model.VchSocialEconomicStatus;
            existingRecord.VchFamilySupport = model.VchFamilySupport;

            existingRecord.IntPainScore = model.IntPainScore;
            existingRecord.BitFallRisk = model.BitFallRisk;
            existingRecord.VchFallRiskRemarks = model.VchFallRiskRemarks;

            _context.Update(existingRecord);
            await _context.SaveChangesAsync();

            // Return success response to close modal via JS or AJAX
            return Json(new { success = true, message = "Assessment updated successfully." });
            //return View("NursingAssessment");
        }
    }
}
