using DWB.APIModel;
using DWB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Reflection.Emit;
using System.Reflection.Metadata;

namespace DWB.Controllers
{
    public class AssessmentController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly DWBEntity _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public AssessmentController(IConfiguration configuration, DWBEntity dWBEntity, IWebHostEnvironment webHostEnvironment)
        {
            _configuration = configuration;
            _context = dWBEntity;
            _webHostEnvironment = webHostEnvironment;
        }

        #region Nursing Assessment Actions ADD,EDIT,VIEW
        [Authorize(Roles = "Admin, Nursing")]
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
               .Where(a => a.IntCode == intUnitcode && a.IntHmscode == intHMScode && a.BitIsCompleted == true)
               .ToListAsync();
            if (tbllist.Count() != 0)
            {
                foreach (var p in patients)
                {
                    var t = tbllist.FirstOrDefault(x => x.VchUhidNo == p.opdno && x.IntIhmsvisit == p.visit);
                    if (t != null)
                    {
                        p.bitTempNSAssComplete = t != null ? t.BitIsCompleted : false;
                        p.bitTempDOcAssComplete = t != null ? t.BitIsDoctorCompleted : false; // Adjust based on your field name
                    }
                }
            }
            return View(patients);
        }
        [Authorize(Roles = "Admin, Nursing")]
        [HttpGet]
        public IActionResult NAssessmentCreate(string uhid, string pname, string visit, string gender, string age, string jdate, string timein, string consultant, string category)
        {
            var model = new TblNsassessment
            {
                VchUhidNo = uhid,
                VchHmsname = pname,
                VchHmsage = age,
                VchGender = gender,
                VchHmsdtEntry = DateTime.ParseExact(jdate, "MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"),
                VchIhmintime = timein,
                VchHmsconsultant = consultant,
                VchHmscategory = category,
                DtStartTime = DateTime.Now,
                IntIhmsvisit = Convert.ToInt32(visit),
                VchCreatedBy = User.Identity.Name.ToString(),
                VchIpUsed = HttpContext.Connection.RemoteIpAddress.ToString()
            };
            return View(model);
        }

        [Authorize(Roles = "Admin, Nursing")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NAssessmentCreate(TblNsassessment model, List<IFormFile>? supportDocsI)
        {
            if (ModelState.IsValid)
            {
                // Save logic here, for example:
                model.DtEndTime = DateTime.Now; //Set end time to now
                model.VchTat = null; //Set TAT to null initially
                model.VchCreatedBy = User.Identity.Name; //Set created by user
                model.VchIpUsed = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"; // Get IP address
                model.BitIsCompleted = true;
                model.IntHmscode = Convert.ToInt32(User.FindFirst("HMScode")?.Value);
                model.IntCode = Convert.ToInt32(User.FindFirst("UnitId")?.Value);
                //model.IntYr= get year from OPD api (pending in api)
                _context.TblNsassessment.Add(model);
                _context.SaveChanges();
                //save uploaded files if any
                if (supportDocsI != null && supportDocsI.Any())
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".pdf" };
                    var uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "NAssessment");
                    //Path.Combine("wwwroot/uploads/NAssessment");
                    if (!Directory.Exists(uploadFolder))
                        Directory.CreateDirectory(uploadFolder);
                    foreach (var file in supportDocsI)
                    {
                        var ext = Path.GetExtension(file.FileName).ToLower();
                        if (!allowedExtensions.Contains(ext))
                            continue;
                        var originalName = Path.GetFileNameWithoutExtension(file.FileName);
                        var cleanedName = originalName.Replace(" ", "_");
                        cleanedName = cleanedName + model.IntAssessmentId.ToString();
                        var savedName = $"{cleanedName}{ext}"; //$"{Guid.NewGuid()}{ext}";
                        var filePath = Path.Combine(uploadFolder, savedName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        var assessmentFile = new TblNassessmentDoc
                        {
                            IntFkAssId = model.IntAssessmentId,
                            VchFileName = savedName,
                            VchFilePath = uploadFolder + "/" + savedName,
                            VchCreatedBy = User.Identity.Name,
                            VchCreatedHost = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                            VchCreatedIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                            BitIsForNassessment = true
                        };
                        _context.TblNassessmentDoc.Add(assessmentFile);
                        await _context.SaveChangesAsync();
                    }
                }
                TempData["Success"] = "Nursing assessment saved successfully!";
                return RedirectToAction("NursingAssessment"); // or redirect to listing or details
            }
            //Model is invalid; re-display the form with errors
            return View(model);
        }

        [Authorize(Roles = "Admin, Nursing")]
        [HttpGet]
        public IActionResult ViewAssessment(string uhid, int visit, string date)
        {
            var data = _context.TblNsassessment
                .Include(a => a.TblNassessmentDoc)
                .FirstOrDefault(x => x.VchUhidNo == uhid && x.IntIhmsvisit == visit && x.BitIsCompleted == true && x.VchHmsdtEntry == date);
            if (data == null)
            {
                return Content("<div class='alert alert-warning'>No data found.</div>");
            }
            else
            {
                return PartialView("_NsAssessmentView", data);
            }
        }

        [Authorize(Roles = "Admin, Nursing")]
        public IActionResult NAssessmentEdit(string uhid, int visit, string date)
        {
            //check param reach here
            var getAssessment = _context.TblNsassessment.Include(a => a.TblNassessmentDoc)
                        .FirstOrDefault(x => x.VchUhidNo == uhid && x.IntIhmsvisit == visit && x.VchHmsdtEntry == date);
            if (getAssessment != null)
            {
                return View(getAssessment);
            }
            else
            {
                TempData["Error"] = "Assessment detail not found, contact to admin!";
                return RedirectToAction("NursingAssessment");
            }
        }

        [Authorize(Roles = "Admin, Nursing")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NAssessmentEdit(TblNsassessment updatedModel, List<IFormFile>? supportDocsI, string? deletedFileIds)
        {
            if (updatedModel.IntAssessmentId == 0)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                // --- 1. Update the main assessment data ---
                var existingAssessmentWithDoc = await _context.TblNsassessment.Include(a => a.TblNassessmentDoc)
                       .FirstOrDefaultAsync(a => a.IntAssessmentId == updatedModel.IntAssessmentId);
                //check existing documents
                var GetDocuments = await _context.TblNassessmentDoc
                    .Where(d => d.IntFkAssId == updatedModel.IntAssessmentId)
                    .ToListAsync();
                if (existingAssessmentWithDoc != null)
                {
                    var filesToDeleteFromDb = await _context.TblNassessmentDoc.Where(d => d.IntFkAssId == updatedModel.IntAssessmentId).ToListAsync();
                    //If there are files to delete, remove them from the database
                    if (filesToDeleteFromDb.Count() != 0 && supportDocsI != null)
                    {
                        foreach (var doc in filesToDeleteFromDb)
                        {
                            // delete file from disk
                            var filePath = Path.Combine("wwwroot/uploads/NAssessment", doc.VchFileName);
                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }
                            // remove from database
                            _context.TblNassessmentDoc.Remove(doc);
                            await _context.SaveChangesAsync();
                        }
                    }
                    // Update the existing assessment with the new values
                    existingAssessmentWithDoc.VchBloodPressure = updatedModel.VchBloodPressure;
                    existingAssessmentWithDoc.VchPulse = updatedModel.VchPulse;
                    existingAssessmentWithDoc.DecTemperature = updatedModel.DecTemperature;
                    existingAssessmentWithDoc.DecSpO2 = updatedModel.DecSpO2;
                    existingAssessmentWithDoc.DecWeight = updatedModel.DecWeight;
                    existingAssessmentWithDoc.DecHeight = updatedModel.DecHeight;
                    existingAssessmentWithDoc.DecRespiratoryRate = updatedModel.DecRespiratoryRate;
                    existingAssessmentWithDoc.DecOxygenFlowRate = updatedModel.DecOxygenFlowRate;

                    existingAssessmentWithDoc.BitIsAllergical = updatedModel.BitIsAllergical;
                    existingAssessmentWithDoc.VchAllergicalDrugs = updatedModel.VchAllergicalDrugs;
                    existingAssessmentWithDoc.BitIsAlcoholic = updatedModel.BitIsAlcoholic;
                    existingAssessmentWithDoc.BitIsSmoking = updatedModel.BitIsSmoking;
                    existingAssessmentWithDoc.VchLmpForFemale = updatedModel.VchLmpForFemale;

                    existingAssessmentWithDoc.BitDiabetes = updatedModel.BitDiabetes;
                    existingAssessmentWithDoc.BitHeartDisease = updatedModel.BitHeartDisease;
                    existingAssessmentWithDoc.BitHypertension = updatedModel.BitHypertension;
                    existingAssessmentWithDoc.BitAsthma = updatedModel.BitAsthma;
                    existingAssessmentWithDoc.BitCholesterol = updatedModel.BitCholesterol;
                    existingAssessmentWithDoc.BitTuberculosis = updatedModel.BitTuberculosis;
                    existingAssessmentWithDoc.BitSurgery = updatedModel.BitSurgery;
                    existingAssessmentWithDoc.BitHospitalization = updatedModel.BitHospitalization;
                    existingAssessmentWithDoc.VchOtherHistory = updatedModel.VchOtherHistory;

                    existingAssessmentWithDoc.VchPsychologicalStatus = updatedModel.VchPsychologicalStatus;
                    existingAssessmentWithDoc.VchOccupation = updatedModel.VchOccupation;
                    existingAssessmentWithDoc.VchSocialEconomicStatus = updatedModel.VchSocialEconomicStatus;
                    existingAssessmentWithDoc.VchFamilySupport = updatedModel.VchFamilySupport;

                    existingAssessmentWithDoc.IntPainScore = updatedModel.IntPainScore;
                    existingAssessmentWithDoc.BitFallRisk = updatedModel.BitFallRisk;
                    existingAssessmentWithDoc.VchFallRiskRemarks = updatedModel.VchFallRiskRemarks;

                    _context.Update(existingAssessmentWithDoc);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    TempData["Error"] = "Nursing assessment detail not found contact to admin!";
                    return RedirectToAction("NursingAssessment");
                }
                // --- 3. Handle NEW files to be uploaded ---
                if (supportDocsI != null && supportDocsI.Any())
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".pdf" };
                    var uploadFolderPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "NAssessment");
                    if (!Directory.Exists(uploadFolderPath))
                    {
                        Directory.CreateDirectory(uploadFolderPath);
                    }
                    foreach (var file in supportDocsI)
                    {
                        var ext = Path.GetExtension(file.FileName).ToLower();
                        if (allowedExtensions.Contains(ext))
                        {
                            var uniqueFileName = $"{Guid.NewGuid()}{ext}";
                            var filePath = Path.Combine(uploadFolderPath, uniqueFileName);
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }
                            var assessmentFile = new TblNassessmentDoc
                            {
                                IntFkAssId = updatedModel.IntAssessmentId, // Link to the existing assessment
                                VchFileName = file.FileName,
                                VchFilePath = $"/uploads/NAssessment/{uniqueFileName}",
                                VchCreatedBy = User.Identity.Name,
                                VchCreatedHost = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                                VchCreatedIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                                DtCreated = DateTime.Now
                            };
                            _context.TblNassessmentDoc.Add(assessmentFile);
                        }
                    }
                    // Save all changes (updates, deletions, and new additions) in one go
                    await _context.SaveChangesAsync();
                }
                TempData["Success"] = "Nursing assessment updated successfully!";
                return RedirectToAction("NursingAssessment");
            }
            else
            {
                TempData["ErrorMessage"] = "Model error generated contact to administrator!";
                return View(updatedModel); // Return the view with validation errors
            }
        }

        [Authorize(Roles = "Admin, Nursing")]
        private bool TblNsassessmentExists(int id)
        {
            return _context.TblNsassessment.Any(e => e.IntAssessmentId == id);
        }

        #endregion

        #region Doctor Assessment Actions ADD,EDIT,VIEW

        [HttpGet]
        [Authorize(Roles = "Admin, Consultant")]
        public async Task<IActionResult> DoctorAssessment(string dateRange)
        {
            //if user consultant get consultant code from claim
            var doccode = User.FindFirst("DoctorCode")?.Value;
            List<SP_OPD> patients = new List<SP_OPD>();
            //Get branch ihms code
            int code = Convert.ToInt32(User.FindFirst("HMScode")?.Value);
            //Get Deafult OPD API
            string BaseAPI = (User.FindFirst("BaseAPI")?.Value ?? string.Empty).Replace("\n", "").Replace("\r", "").Trim();
            string finalURL = string.Empty;
            if (dateRange != null && doccode != null)
            {
                var dates = dateRange.Split(" - ");
                DateTime Sdate = DateTime.ParseExact(dates[0], "dd/MM/yyyy", null);
                DateTime Edate = DateTime.ParseExact(dates[1], "dd/MM/yyyy", null);
                string finalSdate = Convert.ToDateTime(Sdate).ToString("dd-MM-yyyy");
                string finalEdate = Convert.ToDateTime(Edate).ToString("dd-MM-yyyy");
                finalURL = BaseAPI + "opd?&sdate=" + finalSdate + "&edate=" + finalEdate + "&code=" + code + "&uhidno=null" + "&doccode=" + doccode;
            }
            else if (doccode == null)
            {
                //if doctor code is null
                TempData["Error"] = "Your doctor code for current branch is missing so contact to administrator!";
                return View();

            }
            else if (dateRange == null && doccode != null)
            {
                //set today date patient view               
                string today = DateTime.Now.ToString("dd-MM-yyyy");
                //format = opd?sdate=12-07-2025&edate=12-07-2025&code=1&uhidno=uhid
                finalURL = BaseAPI + "opd?&sdate=" + today + "&edate=" + today + "&code=" + code + "&uhidno=null" + "&doccode=" + doccode;
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
               .Where(a => a.IntCode == intUnitcode && a.IntHmscode == intHMScode && a.BitIsCompleted == true)
               .ToListAsync();
            if (tbllist.Count() != 0)
            {
                foreach (var p in patients)
                {
                    var t = tbllist.FirstOrDefault(x => x.VchUhidNo == p.opdno && x.IntIhmsvisit == p.visit);
                    if (t != null)
                    {
                        p.bitTempNSAssComplete = t != null ? t.BitIsCompleted : false;
                        p.bitTempDOcAssComplete = t != null ? t.BitIsDoctorCompleted : false; // Adjust based on your field name
                    }
                }
            }
            return View(patients);
        }

        [Authorize(Roles = "Admin, Consultant")]
        public ActionResult DoctorAssmntCreate(string uhid, string pname, string visit)
        {
            var nursing = _context.TblNsassessment
            .Include(x => x.TblNassessmentDoc) // 🔑 load documents
            .FirstOrDefault(x => x.VchUhidNo == uhid && x.IntIhmsvisit == Convert.ToInt32(visit));

            if (nursing == null)
            {
                TempData["Error"] = "Nursing assessment not found, complete nursing assessment first!";
                return RedirectToAction("DoctorAssessment");
            }

            var doctor = new TblDoctorAssessment
            {
                FkUhid = uhid,
                DtStartTime = DateTime.Now
            };

            var vm = new DoctorAssessmentVM
            {
                NursingAssessment = nursing,
                DoctorAssessment = doctor,
                Medicines = new List<TblDoctorAssmntMedicine>(),
                Labs = new List<TblDoctorAssmntLab>(),
                Procedures = new List<TblDoctorAssmntRadiology>()
            };
            return View(vm);
        }

        // Change the method signature of SearchMedicine to async Task<JsonResult>
        [HttpGet]
        public async Task<JsonResult> SearchMedicine(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<string>());

            term = term.Trim().ToLower();         
            List<spMedicine> medicines = new List<spMedicine>();
            //With the following code to map the result to List<spMedicine>:
            var searchResults = await _context.Procedures.spSearchMedicinesAsync(term);
            medicines = searchResults
                .Select(r => new spMedicine { descript = r.descript, icode = r.icode })
                .ToList();
            return Json(medicines);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DocAssmntCreate(DoctorAssessmentVM model, IFormFile[] doctorDocs)
        {
            if (!ModelState.IsValid)
            {
                //return the same view with validation messages
                return View(model);
            }
            try
            {
                //1. Save Doctor Assessment details
                var doctorAssessment = new TblDoctorAssessment
                {
                    FkUhid = model.DoctorAssessment.FkUhid,
                    FkAssessmentId = model.DoctorAssessment.FkAssessmentId,
                    VchChiefcomplaints = model.DoctorAssessment.VchChiefcomplaints,
                    VchDiagnosis = model.DoctorAssessment.VchDiagnosis,
                    VchMedicalHistory = model.DoctorAssessment.VchMedicalHistory,
                    VchSystemicexam = model.DoctorAssessment.VchSystemicexam,
                    VchRemarks = model.DoctorAssessment.VchRemarks
                };
                _context.TblDoctorAssessment.Add(doctorAssessment);
                _context.SaveChanges();
                if(model.Medicines.Count() != 0)
                {
                    //add all medicine if prescribed
                    foreach(var med in model.Medicines)
                    {
                        TblDoctorAssmntMedicine objMedicine = new TblDoctorAssmntMedicine
                        {
                            FkDocAssmntId = doctorAssessment.IntId, // use saved assessment PK
                            VchMedicineName = med.VchMedicineName,
                            VchMedicineCode = med.VchMedicineCode, // hidden code
                            VchDosage = med.VchDosage,
                            VchFrequency = med.VchFrequency,
                            VchDuration = med.VchDuration,
                            DtCreated = DateTime.Now,
                            VchCreatedBy = User.Identity.Name
                        };
                        _context.TblDoctorAssmntMedicine.Add(objMedicine);
                        _context.SaveChanges();
                    }
                }
                //add lab if prescribed
                if (model.Labs.Count() != 0)
                {

                }
                //add procedure if prescribed
                if (model.Procedures.Count() != 0)
                {

                }
                //uploaded supporting documents
                if (doctorDocs != null && doctorDocs.Length > 0)
                {
                    string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "DoctorDocs");
                    if (!Directory.Exists(uploadPath))
                        Directory.CreateDirectory(uploadPath);
                    foreach (var file in doctorDocs)
                    {
                        if (file.Length > 0)
                        {
                            string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                            string filePath = Path.Combine(uploadPath, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }
                            //Save file info to database
                            _context.TblDoctorAssessmentDoc.Add(new TblDoctorAssessmentDoc
                            {
                                IntFkDoctorAssId = model.DoctorAssessment.IntId,
                                VchFileName = fileName,
                                DtCreated = DateTime.Now
                            });
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                TempData["Success"] = "✅ Doctor Assessment saved successfully!";
                return RedirectToAction("Index"); // or wherever you want
            }
            catch (Exception ex)
            {
                TempData["Error"] = "❌ Error saving assessment: " + ex.Message;
                return View(model);
            }
            #endregion
        }
    }
}
