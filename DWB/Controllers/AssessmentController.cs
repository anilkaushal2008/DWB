using DWB.APIModel;
using DWB.Models;
using DWB.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient.DataClassification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
//for pdf packages
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SixLabors.ImageSharp.Processing.Processors.Convolution;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static QuestPDF.Helpers.Colors;
using DWB.Services;

using OpenAI;
using OpenAI.Audio;
using System.IO;
using System.Threading.Tasks;

using System.Net.Http.Headers;
using System.Text.Json;

namespace DWB.Controllers
{
    public class AssessmentController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly DWBEntity _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly OpenAIClient _openAI;
        private readonly IDoctorScheduleService _services;
        public AssessmentController(IConfiguration configuration, DWBEntity dWBEntity, IWebHostEnvironment webHostEnvironment, IDoctorScheduleService services)
        {
            _configuration = configuration;
            _context = dWBEntity;
            _webHostEnvironment = webHostEnvironment;
            _openAI = new OpenAIClient(configuration["OpenAI:ApiKey"]);
            _services = services;
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
                IntAge=Convert.ToInt32(age),
                VchCreatedBy = User.Identity.Name,
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
                model.FkUnitName=User.FindFirst("UnitName")?.Value;
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

        [HttpGet]
        [Authorize(Roles = "Admin, Consultant")]
        public ActionResult DoctorAssmntCreate(string uhid, string pname, string visit)
        {
            var nursing = _context.TblNsassessment
            .Include(x => x.TblNassessmentDoc) //load documents
            .FirstOrDefault(x => x.VchUhidNo == uhid && x.IntIhmsvisit == Convert.ToInt32(visit));

            if (nursing == null)
            {
                TempData["Error"] = "Nursing assessment not found, complete nursing assessment first!";
                return RedirectToAction("DoctorAssessment");
            }

            var doctor = new TblDoctorAssessment
            {
                FkUhid = uhid,
                FkAssessmentId = nursing.IntAssessmentId,
                DtStartTime = DateTime.Now,
                FkVisitNo = nursing.IntIhmsvisit,
                DtHmsentry = nursing.VchHmsdtEntry,
            };

            var vm = new DoctorAssessmentVM
            {
                NursingAssessment = nursing,
                DoctorAssessment = doctor,
                Medicines = new List<TblDoctorAssmntMedicine>(),
                Labs = new List<TblDoctorAssmntLab>(),
                Radiology = new List<TblDoctorAssmntRadiology>(),
                Procedures = new List<TblDoctorAssmntProcedure>()
            };
            // Fetch the list of templates
            //var templates = _context.TblDocTemplateAssessment
            //                        .Select(t => new
            //                        {
            //                            TempId = t.Intid,
            //                            TempName = t.VchTempleteName
            //                        })
            //                        .ToList();

            //// Store the list directly in the dynamic ViewBag object
            //// You are storing a List<anonymous object> here.
            //ViewBag.TemplateList = templates;
            return View(vm);
        }

        //for masters entry from assessment
        [HttpGet]
        public IActionResult SearchMaster(string term, string type)
        {
            // Fix: If term is null, use empty string to fetch default Top 10
            string searchTerm = string.IsNullOrEmpty(term) ? "" : term.ToLower().Trim();

            try
            {
                if (type == "ChiefComplaint") // Ensure you map the correct table
                {
                    var query = _context.TblCheifComplaintMas.AsQueryable();

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        query = query.Where(x => x.VchCheifComplaint != null && x.VchCheifComplaint.ToLower().Contains(searchTerm));
                    }

                    // If searchTerm is empty, this takes the first 10 alphabetical records
                    var data = query
                        .OrderBy(x => x.VchCheifComplaint)
                        .Select(x => new { label = x.VchCheifComplaint, value = x.VchCheifComplaint })
                        .Take(10)
                        .ToList();

                    return Json(data);
                }
                // ... (Repeat identical logic for Diagnosis and Systemic blocks)
                else if (type == "Diagnosis") // Make sure to use TblDiagnoseMas here
                {
                    var query = _context.TblDiagnoseMas.AsQueryable(); // <--- Check this table!

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        query = query.Where(x => x.VchDiagnose != null && x.VchDiagnose.ToLower().Contains(searchTerm));
                    }

                    var data = query
                        .OrderBy(x => x.VchDiagnose)
                        .Select(x => new { label = x.VchDiagnose, value = x.VchDiagnose })
                        .Take(10)
                        .ToList();

                    return Json(data);
                }
                // Inside SearchMaster method...

                else if (type == "Systemic")  // <--- Make sure this spelling matches JS
                {
                    var query = _context.TblSystemicExamMas.AsQueryable();

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        // Ensure you are checking 'VchSystemicExamination' and NOT 'VchDiagnose'
                        query = query.Where(x => x.VchSystemicExamination != null && x.VchSystemicExamination.ToLower().Contains(searchTerm));
                    }

                    var data = query
                        .OrderBy(x => x.VchSystemicExamination)
                        .Select(x => new { label = x.VchSystemicExamination, value = x.VchSystemicExamination })
                        .Take(10)
                        .ToList();

                    return Json(data);
                }

            }
            catch (Exception ex)
            {
                return Json(new List<object>());
            }
            return Json(new List<object>());
        }

        [HttpPost]
        public IActionResult AddMasterItem(string name, string type)
        {
            if (string.IsNullOrEmpty(name)) return Json(new { success = false, message = "Name is required" });

            try
            {
                int userId = 1; // REPLACE WITH: User.Identity.GetUserId() or similar
                DateTime now = DateTime.Now;

                if (type == "ChiefComplaint")
                {
                    // Check duplicate
                    if (_context.TblCheifComplaintMas.Any(x => x.VchCheifComplaint.ToLower() == name.ToLower()))
                        return Json(new { success = true, message = "Exists" }); // Return success so it adds to UI

                    var item = new TblCheifComplaintMas
                    {
                        VchCheifComplaint = name,
                        DtCreated = now,
                        FkUserid = userId,
                        VchCreatedBy = "Sys"
                    };
                    _context.TblCheifComplaintMas.Add(item);
                }
                else if (type == "Diagnosis")
                {
                    if (_context.TblDiagnoseMas.Any(x => x.VchDiagnose.ToLower() == name.ToLower()))
                        return Json(new { success = true, message = "Exists" });

                    var item = new TblDiagnoseMas
                    {
                        VchDiagnose = name,
                        DtCreated = now,
                        FkUserid = userId,
                        VchCreatedBy = "Sys"
                    };
                    _context.TblDiagnoseMas.Add(item);
                }
                else if (type == "Systemic")
                {
                    if (_context.TblSystemicExamMas.Any(x => x.VchSystemicExamination.ToLower() == name.ToLower()))
                        return Json(new { success = true, message = "Exists" });

                    var item = new TblSystemicExamMas
                    {
                        VchSystemicExamination = name,
                        DtCreated = now,
                        FkUserid = userId,
                        VchCreatedBy = "Sys"
                    };
                    _context.TblSystemicExamMas.Add(item);
                }

                _context.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        //Change the method signature of SearchMedicine to async Task<JsonResult>
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
        [HttpGet]
        public async Task<JsonResult> SearchRadiology(string term, string type)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<string>());
            //string search = "Radio";
            List<spGetRadioProcedureResult> getProcedure = new List<spGetRadioProcedureResult>();
            var searchResults = await _context.Procedures.spGetRadioProcedureAsync(type, term);
            getProcedure = searchResults
                .Select(r => new spGetRadioProcedureResult { service = r.service, scode = r.scode })
                .ToList();
            return Json(searchResults);
        }
        [HttpGet]
        public async Task<JsonResult> SearchLab(string term)
        {
            // If the term is empty, just return empty results
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<spGetLabTestResult>());

            var searchResults = await _context.Procedures.spGetLabTestAsync(term);

            var getLabTest = searchResults
                .Select(r => new spGetLabTestResult
                {
                    descript = r.descript,
                    tcode = r.tcode
                })
                .ToList();

            return Json(getLabTest);
        }

        //get cheif complaint for doctor assessment
        [HttpGet]
        public JsonResult GetChiefComplaints(string term)
        {
            var list = _context.TblCheifComplaintMas
                          .Where(c => c.VchCheifComplaint.Contains(term))
                          .OrderBy(c => c.VchCheifComplaint)
                          .Take(20)
                          .Select(c => c.VchCheifComplaint)
                          .ToList();
            return Json(list);
        }

        private void RehydrateDoctorAssmntCreateVM(DoctorAssessmentVM model)
        {
            // Ensure we have keys to reload data
            var uhid = model.DoctorAssessment?.FkUhid ?? model.NursingAssessment?.VchUhidNo;
            var visit = model.DoctorAssessment?.FkVisitNo ?? model.NursingAssessment?.IntIhmsvisit ?? 0;

            // 🔁 Re-load nursing + related data needed by the view
            var nsg = _context.TblNsassessment
            .Include(x => x.TblNassessmentDoc) // 🔑 load documents
            .FirstOrDefault(x => x.VchUhidNo == uhid && x.IntIhmsvisit == Convert.ToInt32(visit));

            // 🧰 Rebuild dropdowns / lookups used by the view
            ViewBag.TemplateList = _context.TblDocTemplateAssessment
                .Select(t => new { t.Intid, t.VchTempleteName })
                .ToList();

            // Guard null collections so Razor loops don't break
            model.NursingAssessment = nsg;
            model.Medicines ??= new List<TblDoctorAssmntMedicine>();
            model.Labs ??= new List<TblDoctorAssmntLab>();
            model.Radiology ??= new List<TblDoctorAssmntRadiology>();
            model.Procedures ??= new List<TblDoctorAssmntProcedure>();
        }

        [HttpPost]
        public IActionResult AddMedicineMaster(string name)
        {
            if (string.IsNullOrEmpty(name))
                return Json(new { success = false, message = "Medicine Name is required" });

            try
            {
                name = name.Trim();

                // 1. Check if already exists to prevent duplicates
                var existing = _context.Imaster
                    .FirstOrDefault(x => x.Descript.ToLower() == name.ToLower() && x.ItemGroup == "Medicine");

                if (existing != null)
                {
                    return Json(new { success = true, code = existing.Icode, name = existing.Descript });
                }

                // 2. Generate a Unique Icode (Key)
                // Logic: "DOC" + Current Timestamp to ensure uniqueness. 
                // You can change this logic if your system requires a specific number format (e.g. "000501")
                string newCode = "DWB-" + DateTime.Now.ToString("yyMMddHHmm");

                // 3. Create the new Medicine Object
                var newMed = new Imaster
                {
                    Icode = newCode,
                    Descript = name,
                    ItemGroup = "Medicine", // Important: Categorize it correctly
                    ItemType = "FROM-DWB",      // Default type
                    Uom = "FROM-DWB",            // Default Unit of Measurement
                    DtAdded = DateTime.Now,
                    UserEntry = "Doctor",   // Track who added it

                    // Set defaults for other fields to avoid null errors if your DB requires them
                    PurRate = 0,
                    SaleRate = 0,
                    MinQty = 0,
                    MaxQty = 0,
                    IntQty = 0
                };

                _context.Imaster.Add(newMed);
                _context.SaveChanges();

                // 4. Return the new Code and Name
                return Json(new { success = true, code = newMed.Icode, name = newMed.Descript });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error saving medicine: " + ex.Message });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoctorAssmntCreate(DoctorAssessmentVM model, IFormFile[] doctorDocs)
        {
            if (!ModelState.IsValid)
            {
                var allErrors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
                ViewBag.Error = "Validation failed, model error generated, contact to admin!";
                RehydrateDoctorAssmntCreateVM(model);   // ⬅️ critical
                return View(model);
            }
            try
            {
                //get nursing first for update status
                var nsgAssessment=(from e in _context.TblNsassessment
                                   where e.IntAssessmentId == model.NursingAssessment.IntAssessmentId
                                   select e).FirstOrDefault();
                //Save Doctor Assessment details
                var doctorAssessment = new TblDoctorAssessment
                {
                    FkUhid = model.DoctorAssessment.FkUhid,
                    FkAssessmentId = model.NursingAssessment.IntAssessmentId,
                    VchChiefcomplaints = string.IsNullOrEmpty(model.DoctorAssessment.VchChiefcomplaints) ?
                    null : model.DoctorAssessment.VchChiefcomplaints,
                    VchDiagnosis = string.IsNullOrEmpty(model.DoctorAssessment.VchDiagnosis)
                        ? null : model.DoctorAssessment.VchDiagnosis,
                    VchMedicalHistory = string.IsNullOrEmpty(model.DoctorAssessment.VchMedicalHistory)
                        ? null : model.DoctorAssessment.VchMedicalHistory,
                    VchSystemicexam = string.IsNullOrEmpty(model.DoctorAssessment.VchSystemicexam)
                        ? null : model.DoctorAssessment.VchSystemicexam,
                    VchRemarks = string.IsNullOrEmpty(model.DoctorAssessment.VchRemarks)? null : model.DoctorAssessment.VchRemarks,
                    DtFollowUpDate = model.DoctorAssessment.DtFollowUpDate,
                    FollowUpTime = model.DoctorAssessment.FollowUpTime,
                    BitAsstCompleted = true,
                    DtStartTime = model.DoctorAssessment.DtStartTime,
                    DtEndTime = DateTime.Now,
                    VchCreatedBy = User.Identity.Name,
                    DtCreated = DateTime.Now,
                    FkVisitNo=model.DoctorAssessment.FkVisitNo,
                    IntCode = Convert.ToInt32(User.FindFirst("UnitId")?.Value),
                    FkUserId=Convert.ToInt32(User.FindFirst("UserId")?.Value),
                    FkUnitName= User.FindFirst("CompName")?.Value,
                    DtHmsentry =model.DoctorAssessment.DtHmsentry                    
                };
                if (nsgAssessment != null)
                {
                    nsgAssessment.BitIsDoctorCompleted = true; //mark nursing assessment doctor completed
                }
                _context.TblDoctorAssessment.Add(doctorAssessment);                
                _context.SaveChanges();
                //Save medicines
                if (model.Medicines.Count() != 0)
                {
                    //add all medicine if prescribed
                    foreach (var med in model.Medicines)
                    {
                        if (med.VchMedicineName != null && med.VchMedicineCode!=null)
                        {
                            TblDoctorAssmntMedicine objMedicine = new TblDoctorAssmntMedicine
                            {
                                FkDocAssmntId = doctorAssessment.IntId, // use saved assessment PK
                                VchMedicineName = med.VchMedicineName,
                                VchMedicineCode = med.VchMedicineCode, // hidden code
                                IntQuantity = med.IntQuantity,
                                VchFrequency = med.VchFrequency,
                                VchDuration = med.VchDuration,
                                BitBbf = med.BreakFastTiming == "BBF",
                                BitAbf = med.BreakFastTiming == "ABF",
                                BitBl = med.LunchTiming == "BL",
                                BitAl = med.LunchTiming == "AL",
                                BitBd = med.DinnerTiming == "BD",
                                BitAd = med.DinnerTiming == "AD",
                                DtCreated = DateTime.Now,
                                VchCreatedBy = User.Identity.Name
                            };
                            _context.TblDoctorAssmntMedicine.Add(objMedicine);
                            doctorAssessment.BitPrescribeMedicine = true; //mark medicine prescribed
                            _context.SaveChanges();
                        }
                    }
                }
                //add lab if prescribed
                if (model.Labs.Count() != 0)
                {
                    //add all lab if prescribed
                    foreach (var lab in model.Labs)
                    {
                        if (lab.VchTestName != null && lab.VchTestCode!=null)
                        {
                            TblDoctorAssmntLab objLab = new TblDoctorAssmntLab
                            {
                                FkDocAssmntId = doctorAssessment.IntId, // use saved assessment PK
                                VchTestName = lab.VchTestName,
                                VchTestCode = lab.VchTestCode, // hidden code
                                VchPriority = lab.VchPriority,
                                //VchRemarks = lab.VchRemarks,
                                DtCreated = DateTime.Now,
                                VchCreatedBy = User.Identity.Name
                            };
                            _context.TblDoctorAssmntLab.Add(objLab);
                            doctorAssessment.BitPrescribeLabTest = true; //mark lab test prescribed
                            _context.SaveChanges();
                        }
                    }
                }
                //add radiology if prescribed
                if (model.Radiology.Count() != 0)
                {
                    //add all radiology if prescribed
                    foreach (var radio in model.Radiology)
                    {
                        if (radio.VchRadiologyName != null && radio.VchRadiologyCode!=null)
                        {
                            TblDoctorAssmntRadiology objRadio = new TblDoctorAssmntRadiology
                            {
                                FkDocAssmntId = doctorAssessment.IntId, // use saved assessment PK
                                VchRadiologyName = radio.VchRadiologyName,
                                VchRadiologyCode = radio.VchRadiologyCode, // hidden code
                                VchPriority = radio.VchPriority,
                                //VchRemarks = radio.VchRemarks,
                                DtCreated = DateTime.Now,
                                VchCreatedBy = User.Identity.Name
                            };
                            _context.TblDoctorAssmntRadiology.Add(objRadio);
                            doctorAssessment.BitRadioInvestigation = true; //mark radiology test prescribed
                            _context.SaveChanges();
                        }
                    }
                }
                //add procedures if any
                if (model.Procedures.Count() != 0)
                {
                    //add all procedures if prescribed
                    foreach (var proc in model.Procedures)
                    {
                        if (proc.VchProcedureName != null && proc.VchProcedureCode!=null)
                        {
                            TblDoctorAssmntProcedure objProc = new TblDoctorAssmntProcedure
                            {
                                FkDocAsstId = doctorAssessment.IntId, // use saved assessment PK
                                VchProcedureName = proc.VchProcedureName,
                                VchProcedureCode = proc.VchProcedureCode, // hidden code
                                VchPriority = proc.VchPriority,                                
                                DtCreated = DateTime.Now,
                                VchCreatedBy = User.Identity.Name
                            };
                            _context.TblDoctorAssmntProcedure.Add(objProc);
                            doctorAssessment.BitPrescribeProcedure = true; //mark procedure prescribed
                            _context.SaveChanges();
                        }
                    }
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
                                VchFilePath = "/uploads/DoctorDocs/" + fileName,
                                DtCreated = DateTime.Now,
                                VchCreatedBy = User.Identity?.Name ?? "System",
                                VchCreatedHost = HttpContext.Session.GetString("ClientHost"),
                                VchCreatedIp = HttpContext.Session.GetString("ClientIP")
                            });
                            doctorAssessment.BitIsSupportDoc = true; //mark document uploaded
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                TempData["Success"] = "✅ Doctor Assessment saved successfully!";
                return RedirectToAction("DoctorAssessment"); //or wherever you want
            }
            catch (Exception ex)
            {
                //Add error to model state
                ModelState.AddModelError("", "❌ Error saving assessment: " + ex.Message);
                //Ensure child collections are not null
                model.Medicines = model.Medicines ?? new List<TblDoctorAssmntMedicine>();
                model.Labs = model.Labs ?? new List<TblDoctorAssmntLab>();
                model.Radiology = model.Radiology ?? new List<TblDoctorAssmntRadiology>();
                model.Procedures = model.Procedures ?? new List<TblDoctorAssmntProcedure>();
                //Return same view with model and errors
                return View("DoctorAssmntCreate",model);
            }
        }

        // GET: Doctor Assessment Edit
        public async Task<IActionResult> DocAssmntEdit(string uhid, int visit, int templateID) //passing template id for load existing template in the current assessment
        {
            var assessment = await _context.TblDoctorAssessment
                .FirstOrDefaultAsync(x => x.FkUhid == uhid && x.FkVisitNo == visit);

            if (assessment == null)
                return NotFound();

            var model = new DoctorAssessmentVM
            {
                //DoctorAssessment = new TblDoctorAssessment
                //{
                //    IntId = assessment.IntId,
                //    FkUhid = assessment.FkUhid,
                //    FkAssessmentId = assessment.FkAssessmentId,
                //    VchChiefcomplaints = assessment.VchChiefcomplaints,
                //    VchDiagnosis = assessment.VchDiagnosis,
                //    VchMedicalHistory = assessment.VchMedicalHistory,
                //    VchSystemicexam = assessment.VchSystemicexam,
                //    VchRemarks = assessment.VchRemarks,                    
                //},
                DoctorAssessment = assessment,
                NursingAssessment = (from e in _context.TblNsassessment where e.IntAssessmentId == assessment.FkAssessmentId select e).FirstOrDefault(),

                Medicines = await _context.TblDoctorAssmntMedicine
                    .Where(m => m.FkDocAssmntId == assessment.IntId).ToListAsync(),
                Labs = await _context.TblDoctorAssmntLab
                    .Where(l => l.FkDocAssmntId == assessment.IntId).ToListAsync(),
                Radiology = await _context.TblDoctorAssmntRadiology
                    .Where(r => r.FkDocAssmntId == assessment.IntId).ToListAsync(),
                Procedures = await _context.TblDoctorAssmntProcedure
                    .Where(p => p.FkDocAsstId == assessment.IntId).ToListAsync(),
                Documents = await _context.TblDoctorAssessmentDoc
                    .Where(d => d.IntFkDoctorAssId == assessment.IntId).ToListAsync()
            };

            return View("DoctorAssmntCreate", model);
        }

        [HttpPost]
        public async Task<IActionResult> DocAssmntEdit(DoctorAssessmentVM model, IFormFile[] doctorDocs)
        {
            if (!ModelState.IsValid)
            {
                // Return same view with validation messages
                var nursing = _context.TblNsassessment
           .Include(x => x.TblNassessmentDoc) // 🔑 load documents
           .FirstOrDefault(x => x.VchUhidNo == model.DoctorAssessment.FkUhid && x.IntIhmsvisit == Convert.ToInt32(model.DoctorAssessment.FkVisitNo));
                return View(model);
            }
            try
            {
                // Get existing doctor assessment
                var doctorAssessment = _context.TblDoctorAssessment
                    .FirstOrDefault(d => d.IntId == model.DoctorAssessment.IntId);

                if (doctorAssessment == null)
                {
                    ModelState.AddModelError("", "Doctor Assessment not found.");
                    return View(model);
                }

                // Update main assessment fields
                doctorAssessment.VchChiefcomplaints = string.IsNullOrEmpty(model.DoctorAssessment.VchChiefcomplaints)
                    ? null : model.DoctorAssessment.VchChiefcomplaints;
                doctorAssessment.VchDiagnosis = string.IsNullOrEmpty(model.DoctorAssessment.VchDiagnosis)
                    ? null : model.DoctorAssessment.VchDiagnosis;
                doctorAssessment.VchMedicalHistory = string.IsNullOrEmpty(model.DoctorAssessment.VchMedicalHistory)
                    ? null : model.DoctorAssessment.VchMedicalHistory;
                doctorAssessment.VchSystemicexam = string.IsNullOrEmpty(model.DoctorAssessment.VchSystemicexam)
                    ? null : model.DoctorAssessment.VchSystemicexam;
                doctorAssessment.VchRemarks = string.IsNullOrEmpty(model.DoctorAssessment.VchRemarks)
                    ? null : model.DoctorAssessment.VchRemarks;
                doctorAssessment.DtFollowUpDate = model.DoctorAssessment.DtFollowUpDate;
                doctorAssessment.FollowUpTime = model.DoctorAssessment.FollowUpTime;
                doctorAssessment.DtEndTime = DateTime.Now;
                doctorAssessment.VchUpdatedBy = User.Identity.Name;
                doctorAssessment.DtUpdated = DateTime.Now;
                _context.TblDoctorAssessment.Update(doctorAssessment);
                _context.SaveChanges();

                // ======== MEDICINES ========
                var existingMeds = _context.TblDoctorAssmntMedicine
                   .Where(m => m.FkDocAssmntId == doctorAssessment.IntId).ToList();
                if (existingMeds.Any())
                {
                    _context.TblDoctorAssmntMedicine.RemoveRange(existingMeds);
                    _context.SaveChanges();
                }
                if (model.Medicines != null && model.Medicines.Count > 0)
                {                   
                    foreach (var med in model.Medicines)
                    {
                        if (!string.IsNullOrEmpty(med.VchMedicineName) && !string.IsNullOrEmpty(med.VchMedicineCode))
                        {
                            _context.TblDoctorAssmntMedicine.Add(new TblDoctorAssmntMedicine
                            {
                                FkDocAssmntId = doctorAssessment.IntId,
                                VchMedicineName = med.VchMedicineName,
                                VchMedicineCode = med.VchMedicineCode,
                                IntQuantity = med.IntQuantity,
                                VchFrequency = med.VchFrequency,
                                VchDuration = med.VchDuration,
                                BitBbf = med.BreakFastTiming == "BBF",
                                BitAbf = med.BreakFastTiming == "ABF",
                                BitBl = med.LunchTiming == "BL",
                                BitAl = med.LunchTiming == "AL",
                                BitBd = med.DinnerTiming == "BD",
                                BitAd = med.DinnerTiming == "AD",
                                DtCreated = DateTime.Now,
                                VchCreatedBy = User.Identity.Name
                            });
                        }
                    }
                    //_context.TblDoctorAssmntMedicine.Add(model);
                    doctorAssessment.BitPrescribeMedicine = true; //mark medicine prescribed
                    await _context.SaveChangesAsync();

                }

                // ======== LABS ========
                var existingLabs = _context.TblDoctorAssmntLab
                .Where(l => l.FkDocAssmntId == doctorAssessment.IntId).ToList();
                if (existingLabs.Any())
                {
                    _context.TblDoctorAssmntLab.RemoveRange(existingLabs);
                    _context.SaveChanges();
                }
                if (model.Labs != null && model.Labs.Count > 0)
                {                 
                    foreach (var lab in model.Labs)
                    {
                        if (!string.IsNullOrEmpty(lab.VchTestName) && !string.IsNullOrEmpty(lab.VchTestCode))
                        {
                            _context.TblDoctorAssmntLab.Add(new TblDoctorAssmntLab
                            {
                                FkDocAssmntId = doctorAssessment.IntId,
                                VchTestName = lab.VchTestName,
                                VchTestCode = lab.VchTestCode,
                                VchPriority = lab.VchPriority,
                                DtCreated = DateTime.Now,
                                VchCreatedBy = User.Identity.Name
                            });
                        }

                    }
                    doctorAssessment.BitPrescribeLabTest = model.Labs.Any();
                    await _context.SaveChangesAsync();
                }

                // ======== RADIOLOGY ========
                var existingRadios = _context.TblDoctorAssmntRadiology
                .Where(r => r.FkDocAssmntId == doctorAssessment.IntId).ToList();
                if (existingRadios.Any())
                {
                    _context.TblDoctorAssmntRadiology.RemoveRange(existingRadios);
                    _context.SaveChanges();
                }
                if (model.Radiology != null && model.Radiology.Count > 0)
                {
                    foreach (var radio in model.Radiology)
                    {
                        if (!string.IsNullOrEmpty(radio.VchRadiologyName) && !string.IsNullOrEmpty(radio.VchRadiologyCode))
                        {
                            _context.TblDoctorAssmntRadiology.Add(new TblDoctorAssmntRadiology
                            {
                                FkDocAssmntId = doctorAssessment.IntId,
                                VchRadiologyName = radio.VchRadiologyName,
                                VchRadiologyCode = radio.VchRadiologyCode,
                                VchPriority = radio.VchPriority,
                                DtCreated = DateTime.Now,
                                VchCreatedBy = User.Identity.Name,                                
                            });
                        }
                    }
                    doctorAssessment.BitRadioInvestigation = model.Radiology.Any();
                    await _context.SaveChangesAsync();
                }

                // ======== PROCEDURES ========
                var existingProcs = _context.TblDoctorAssmntProcedure
                  .Where(p => p.FkDocAsstId == doctorAssessment.IntId).ToList();
                if (existingProcs.Any())
                {
                    _context.TblDoctorAssmntProcedure.RemoveRange(existingProcs);
                    _context.SaveChanges();
                }                
               
                if (model.Procedures != null && model.Procedures.Count > 0)
                {                   
                    foreach (var proc in model.Procedures)
                    {
                        if (!string.IsNullOrEmpty(proc.VchProcedureName) && !string.IsNullOrEmpty(proc.VchProcedureCode))
                        {
                            _context.TblDoctorAssmntProcedure.Add(new TblDoctorAssmntProcedure
                            {
                                FkDocAsstId = doctorAssessment.IntId,
                                VchProcedureName = proc.VchProcedureName,
                                VchProcedureCode = proc.VchProcedureCode,
                                VchPriority = proc.VchPriority,
                                DtCreated = DateTime.Now,
                                VchCreatedBy = User.Identity.Name
                            });
                        }
                    }
                    doctorAssessment.BitPrescribeProcedure = model.Procedures.Any();
                    await _context.SaveChangesAsync();
                }

                // ======== FILES ========
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
                                VchFilePath="/uploads/DoctorDocs/"+ fileName,
                                DtCreated = DateTime.Now,
                                VchCreatedBy = User.Identity?.Name ?? "System",
                                VchCreatedHost= HttpContext.Session.GetString("ClientHost"),
                                VchCreatedIp= HttpContext.Session.GetString("ClientIp")
                            });
                            doctorAssessment.BitIsSupportDoc = true; //mark document uploaded
                            await _context.SaveChangesAsync();
                        }
                    }                    
                }
                TempData["Success"] = "✅ Doctor Assessment updated successfully!";
                return RedirectToAction("DoctorAssessment");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "❌ Error updating assessment: " + ex.Message);
                model.Medicines = model.Medicines ?? new List<TblDoctorAssmntMedicine>();
                model.Labs = model.Labs ?? new List<TblDoctorAssmntLab>();
                model.Radiology = model.Radiology ?? new List<TblDoctorAssmntRadiology>();
                model.Procedures = model.Procedures ?? new List<TblDoctorAssmntProcedure>();
                return View("DoctorAssmntEdit", model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ViewDocAssessment(string uhid,string visit, string date)
        {
            //get company id
            var intUnitcode = Convert.ToInt32(User.FindFirst("UnitId")?.Value);
            //get user if
            var ConsultantID = Convert.ToInt32(User.FindFirst("UserId")?.Value);
            string GetTiming = String.Empty;
            if(intUnitcode!=0 && ConsultantID != 0 )
            {
                GetTiming = await _services.GetScheduleStringAsync(ConsultantID, intUnitcode);
            }
            // fetch all data from DB
            var nursing = _context.TblNsassessment.FirstOrDefault(x => x.VchUhidNo == uhid && x.IntIhmsvisit==Convert.ToInt32(visit));
            var doctor = _context.TblDoctorAssessment.FirstOrDefault(x => x.FkAssessmentId == nursing.IntAssessmentId);
            var medicines = _context.TblDoctorAssmntMedicine.Where(x => x.FkDocAssmntId == doctor.IntId).ToList();
            var labs = _context.TblDoctorAssmntLab.Where(x => x.FkDocAssmntId == doctor.IntId).ToList();
            var radiologies = _context.TblDoctorAssmntRadiology.Where(x => x.FkDocAssmntId == doctor.IntId).ToList();
            var procedures = _context.TblDoctorAssmntProcedure.Where(x => x.FkDocAsstId == doctor.IntId).ToList();
            var userData=_context.TblUsers.FirstOrDefault(x => x.IntUserId == doctor.FkUserId);
            var company = _context.IndusCompanies.FirstOrDefault(x => x.IntPk == intUnitcode);
            var Timing = string.Empty;
            if (GetTiming != null)
            {
                Timing = GetTiming;
            }
            var report = new DoctorAssessmentReport(nursing, doctor, medicines, labs, radiologies, procedures,userData,company,Timing);

            var pdf = report.GeneratePdf();

            return File(pdf, "application/pdf"); // <--- Important
        }

        //for template save 
        [HttpPost]
        public IActionResult SaveAsTemplate([FromBody] DoctorAssessmentTemplateVM model, DoctorAssessmentVM objdoc)
        {
            if (model == null || string.IsNullOrEmpty(model.TemplateName))
                return BadRequest("Invalid template data");
            //check Name of template duplication
            bool isDuplicate = _context.TblDocTemplateAssessment
            .Any(t => t.VchTempleteName.ToLower() == model.TemplateName.ToLower());
            if (isDuplicate)
            {
                return BadRequest($"Template name '{model.TemplateName}' already exists.");
            }

            var template = new TblDocTemplateAssessment
            {
                VchTempleteName = model.TemplateName,
                DataJson = JsonConvert.SerializeObject(model.Assessment),
                DtCreated = DateTime.Now,
                VchCreatedBy = User.Identity?.Name ?? "NA",
                IntFkuserid = int.TryParse(User.FindFirst("UserId")?.Value, out var uid) ? uid : 0
            };

            _context.TblDocTemplateAssessment.Add(template);
            _context.SaveChanges();

            //deserliased data to save in other medications
            Dictionary<string, string> fullData = JsonConvert.DeserializeObject<Dictionary<string, string>>(model.Assessment.ToString());
           
            // 1. Save Medicines
            var medicineGroups = ExtractGroups(fullData, "Medicines");
            if (medicineGroups.Any())
            {
                foreach (var medicineData in medicineGroups)
                {
                    medicineData.TryGetValue("VchMedicineName", out var name);
                    medicineData.TryGetValue("VchMedicineCode", out var code);
                    medicineData.TryGetValue("IntQuantity", out var quantityStr);
                    medicineData.TryGetValue("VchFrequency", out var frequency);
                    medicineData.TryGetValue("VchDuration", out var duration);
                    medicineData.TryGetValue("BreakFastTiming", out var bfTiming);
                    medicineData.TryGetValue("LunchTiming", out var lunchTiming);
                    medicineData.TryGetValue("DinnerTiming", out var dinnerTiming);

                    int.TryParse(quantityStr, out int quantity);
                    _context.TblDocTemplateMedicine.Add(new TblDocTemplateMedicine
                    {
                        IntFkTempleteId = template.Intid,
                        VchMedicineName = name ?? "", // Use null-coalescing
                        VchMedicineCode = code ?? "",
                        VchFrequency = frequency ?? "",
                        IntQuantity = quantity,
                        VchDuration = duration ?? "",

                        // Timing flags using simple string comparison
                        BitBbf = bfTiming == "BBF",
                        BitAbf = bfTiming == "AFB",
                        BitAl = lunchTiming == "AL",
                        BitBl = lunchTiming == "BL",
                        BitBd = dinnerTiming == "BD",
                        BitAd = dinnerTiming == "AD",

                        VchCreatedBy = User.Identity?.Name ?? "NA",
                        DtCreated = DateTime.Now
                    });
                }
            }

            // 4. Save Labs
            //check lab from json
            var labGroups = ExtractGroups(fullData, "Labs");
            if(labGroups.Any())
            { 
                foreach (var group in labGroups)
                {
                    var labData = group.ToDictionary(
                        kv => kv.Key.Substring(kv.Key.LastIndexOf('.') + 1), // → property name only
                        kv => kv.Value
                    );

                    labData.TryGetValue("VchTestName", out var testName);
                    labData.TryGetValue("VchTestCode", out var testCode);
                    labData.TryGetValue("VchPriority", out var priority);

                    _context.TblDocTemplateLab.Add(new TblDocTemplateLab
                    {
                        FkTempId = template.Intid,
                        VchTestName = testName ?? "",
                        VchTestCode = testCode ?? "",
                        VchPriority = priority ?? "",
                        VchCreatedBy = User.Identity?.Name ?? "NA",
                        DtCreated = DateTime.Now
                    });
                }
            }


            // 5. Save Procedures using helper method
            var procedureGroups = ExtractGroups(fullData, "Procedures");

            foreach (var proc in procedureGroups)
            {
                proc.TryGetValue("VchProcedureName", out var procedureName);
                proc.TryGetValue("VchProcedureCode", out var procedureCode);
                proc.TryGetValue("VchPriority", out var priority);

                _context.TblDocTemplateProcedure.Add(new TblDocTemplateProcedure
                {
                    FkTempId = template.Intid,
                    VchProcedureName = procedureName ?? "",
                    VchProcedureCode = procedureCode ?? "",
                    VchPriority = priority ?? "",
                    DtCreated = DateTime.Now,
                    VchCreatedBy = User.Identity?.Name ?? "NA",
                });
            }

            // 6. Save Radiology
            var radiologyGroups = ExtractGroups(fullData, "Radiology");
           
            if (radiologyGroups.Any())
            {
                // Iterate through each radiology group
                foreach (var radiologyData in radiologyGroups)
                {                    
                    radiologyData.TryGetValue("VchRadiologyName", out var radiologyName);
                    radiologyData.TryGetValue("VchRadiologyCode", out var radiologyCode);
                    radiologyData.TryGetValue("VchPriority", out var priority);                   
                    _context.TblDocTemplateRadiology.Add(new TblDocTemplateRadiology
                    {
                        FkTempId = template.Intid,
                        VchRadiologyName = radiologyName ?? "",
                        VchRadiologyCode = radiologyCode ?? "",
                        VchPriority = priority ?? "",
                        DtCreated = DateTime.Now,
                        VchCreatedBy = User.Identity?.Name ?? "NA",
                    });
                }
            }
            _context.SaveChanges();
            return Ok(new { success = true, id = template.Intid });
        }

        //This method returns the list of templates for the dropdown        
        public IActionResult GetAllTemplates()
        {
            try
            {
                var templates = _context.TblDocTemplateAssessment
              .Select(t => new SelectListItem
              {
                  Value = t.Intid.ToString(),
                  Text = t.VchTempleteName
              })
              .ToList(); 
               return Json(new { success = true, templates = templates });
            }
            catch (Exception ex)
            {
                // TEMPORARILY change the return to expose the specific error message
                Console.WriteLine("Server-Side Error: " + ex.Message);

                // This is the CRUCIAL line to change:
                return Ok(new { success = false, message = "ERROR: " + ex.Message, templates = new List<object>() });
            }
        }

        //Load selected templates in current assessment       
        [HttpGet]
        public IActionResult LoadTemplateData(string type, int? id, string uhid) //visit number for loading selected history from all history)
        {
            if (type == "template")
            {
                // 🔹 Load Template
                var template = _context.TblDocTemplateAssessment
                    .Include(t => t.TblDocTemplateMedicine)
                    .Include(t => t.TblDocTemplateLab)
                    .Include(t => t.TblDocTemplateRadiology)
                    .Include(t => t.TblDocTemplateProcedure)
                    .FirstOrDefault(t => t.Intid == id);

                if (template == null)
                    return Json(new { success = false, message = "Template not found" });

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        source = "template",
                        tname = template.VchTempleteName,
                        VchChiefcomplaints = template.VchChiefComplaints,
                        VchMedicalHistory = template.VchMedicalHistory,
                        VchSystemicexam = template.VchSystemicExam,
                        VchDiagnosis = template.VchDiagnosis,
                        VchRemarks = template.VchRemarks,
                        medicines = template.TblDocTemplateMedicine.Select(m => new
                        {
                            m.IntId,
                            m.VchMedicineName,
                            m.VchMedicineCode,
                            m.IntQuantity,
                            m.VchFrequency,
                            m.VchDuration,
                            m.BitBbf,
                            m.BitAbf,
                            m.BitBl,
                            m.BitAl,
                            m.BitBd,
                            m.BitAd
                        }),
                        labs = template.TblDocTemplateLab.Select(l => new
                        {
                            l.VchTestCode,
                            l.VchTestName,
                            l.VchPriority
                        }),
                        radiology = template.TblDocTemplateRadiology.Select(r => new
                        {
                            r.VchRadiologyCode,
                            r.VchRadiologyName,
                            r.VchPriority
                        }),
                        procedures = template.TblDocTemplateProcedure.Select(p => new
                        {
                            p.VchProcedureCode,
                            p.VchProcedureName,
                            p.VchPriority
                        })
                    }
                });
            }
            else if (type == "history")
            {
                // 🔹 Load Last Assessment History
                var lastAssessment = _context.TblDoctorAssessment
                    .Include(a => a.TblDoctorAssmntMedicine)
                    .Include(a => a.TblDoctorAssmntLab)
                    .Include(a => a.TblDoctorAssmntRadiology)
                    .Include(a => a.TblDoctorAssmntProcedure)
                    .Where(a => a.FkUhid == uhid)
                    .OrderByDescending(a => a.DtCreated)
                    .FirstOrDefault();

                if (lastAssessment == null)
                    return Json(new { success = false, message = "No previous assessment found" });

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        source = "history",
                        VchChiefcomplaints = lastAssessment.VchChiefcomplaints,
                        VchMedicalHistory = lastAssessment.VchMedicalHistory,
                        VchSystemicexam = lastAssessment.VchSystemicexam,
                        VchDiagnosis = lastAssessment.VchDiagnosis,
                        VchRemarks = lastAssessment.VchRemarks,
                        medicines = lastAssessment.TblDoctorAssmntMedicine.Select(m => new
                        {
                            m.VchMedicineName,
                            m.VchMedicineCode,
                            m.IntQuantity,
                            m.VchFrequency,
                            m.VchDuration,
                            m.BitBbf,
                            m.BitAbf,
                            m.BitBl,
                            m.BitAl,
                            m.BitBd,
                            m.BitAd
                        }),
                        labs = lastAssessment.TblDoctorAssmntLab.Select(l => new
                        {
                            l.VchTestCode,
                            l.VchTestName,
                            l.VchPriority
                        }),
                        radiology = lastAssessment.TblDoctorAssmntRadiology.Select(r => new
                        {
                            r.VchRadiologyCode,
                            r.VchRadiologyName,
                            r.VchPriority
                        }),
                        procedures = lastAssessment.TblDoctorAssmntProcedure.Select(p => new
                        {
                            p.VchProcedureCode,
                            p.VchProcedureName,
                            p.VchPriority
                        })
                    }
                });
            }

            else if (type == "selectedHistory")
            {
                // 🔹 Load Last Assessment History
                var lastAssessment = _context.TblDoctorAssessment
                    .Include(a => a.TblDoctorAssmntMedicine)
                    .Include(a => a.TblDoctorAssmntLab)
                    .Include(a => a.TblDoctorAssmntRadiology)
                    .Include(a => a.TblDoctorAssmntProcedure)
                    .Where(a => a.FkUhid == uhid && a.IntId==id)
                    .OrderByDescending(a => a.DtCreated)
                    .FirstOrDefault();

                if (lastAssessment == null)
                    return Json(new { success = false, message = "No previous assessment found" });

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        source = "history",
                        VchChiefcomplaints = lastAssessment.VchChiefcomplaints,
                        VchMedicalHistory = lastAssessment.VchMedicalHistory,
                        VchSystemicexam = lastAssessment.VchSystemicexam,
                        VchDiagnosis = lastAssessment.VchDiagnosis,
                        VchRemarks = lastAssessment.VchRemarks,
                        medicines = lastAssessment.TblDoctorAssmntMedicine.Select(m => new
                        {
                            m.VchMedicineName,
                            m.VchMedicineCode,
                            m.IntQuantity,
                            m.VchFrequency,
                            m.VchDuration,
                            m.BitBbf,
                            m.BitAbf,
                            m.BitBl,
                            m.BitAl,
                            m.BitBd,
                            m.BitAd
                        }),
                        labs = lastAssessment.TblDoctorAssmntLab.Select(l => new
                        {
                            l.VchTestCode,
                            l.VchTestName,
                            l.VchPriority
                        }),
                        radiology = lastAssessment.TblDoctorAssmntRadiology.Select(r => new
                        {
                            r.VchRadiologyCode,
                            r.VchRadiologyName,
                            r.VchPriority
                        }),
                        procedures = lastAssessment.TblDoctorAssmntProcedure.Select(p => new
                        {
                            p.VchProcedureCode,
                            p.VchProcedureName,
                            p.VchPriority
                        })
                    }
                });
            }

            return Json(new { success = false, message = "Gistory detail not found check it again, and try!" });
        }

        //public IActionResult LoadTemplateData(int id)
        //{
        //    var template = _context.TblDocTemplateAssessment
        //        .Include(t => t.TblDocTemplateMedicine)
        //        .Include(t => t.TblDocTemplateLab)
        //        .Include(t => t.TblDocTemplateRadiology)
        //        .Include(t => t.TblDocTemplateProcedure)
        //        .FirstOrDefault(t => t.Intid == id);

        //    if (template == null)
        //        return Json(new { success = false, message = "Template not found" });

        //    return Json(new
        //    {
        //        success = true,
        //        template = new
        //        {
        //            tname = template.VchTempleteName,
        //            //Clinical fields
        //            VchChiefcomplaints = template.VchChiefComplaints,
        //            VchMedicalHistory = template.VchMedicalHistory,
        //            VchSystemicexam = template.VchSystemicExam,
        //            VchDiagnosis = template.VchDiagnosis,
        //            VchRemarks = template.VchRemarks,
        //            medicines = template.TblDocTemplateMedicine.Select(m => new
        //            {
        //                m.IntId,
        //                m.VchMedicineName,
        //                m.VchMedicineCode,
        //                m.IntQuantity,
        //                m.VchFrequency,
        //                m.VchDuration,
        //                m.BitBbf,
        //                m.BitAbf,
        //                m.BitBl,
        //                m.BitAl,
        //                m.BitBd,
        //                m.BitAd
        //            }),
        //            labs = template.TblDocTemplateLab.Select(l => new
        //            {
        //                l.VchTestCode,
        //                l.VchTestName,
        //                l.VchPriority
        //            }),
        //            radiology = template.TblDocTemplateRadiology.Select(r => new
        //            {
        //                r.VchRadiologyCode,
        //                r.VchRadiologyName,
        //                r.VchPriority
        //            }),
        //            procedures = template.TblDocTemplateProcedure.Select(p => new
        //            {
        //                p.VchProcedureCode,
        //                p.VchProcedureName,
        //                p.VchPriority
        //            })

        //        }
        //    });
        //}

        [HttpGet]
        public IActionResult AllHistory(string uhid)
        {
            var assessments = (from doc in _context.TblDoctorAssessment
                               join nur in _context.TblNsassessment
                                   on doc.FkAssessmentId equals nur.IntAssessmentId
                               where doc.FkUhid == uhid
                               orderby doc.DtCreated descending
                               select new
                               {
                                   doc.IntId,                                   
                                   visitno=doc.FkVisitNo,
                                   uhid=doc.FkUhid,
                                   CreatedDate = doc.DtCreated.HasValue
                                       ? doc.DtCreated.Value.ToString("dd/MM/yyyy")
                                       : "N/A",
                                   FollowUpDate = doc.DtFollowUpDate.HasValue
                                       ? doc.DtFollowUpDate.Value.ToString("dd/MM/yyyy")
                                       : "N/A",
                                   hmsPatientName = nur.VchHmsname, // ✅ from NursingAssessment
                                   doc.VchDiagnosis,
                                   doc.VchRemarks
                               })
                       .ToList();

            return Json(new
            {
                success = true,
                data = assessments
            });
        }

        [HttpGet]
        public IActionResult LoadLastHistory(string uhid)
        {
            var lastAssessment = _context.TblDoctorAssessment
           .Where(a => a.FkUhid == uhid)
           .OrderByDescending(a => a.DtCreated)
           .Select(a => new
        {
         a.IntId,
         a.FkVisitNo,
         CreatedDate = a.DtCreated.HasValue ? a.DtCreated.Value.ToString("dd/MM/yyyy") : "N/A",
         FollowUpDate = a.DtFollowUpDate.HasValue ? a.DtFollowUpDate.Value.ToString("dd/MM/yyyy") : "N/A",
         a.VchChiefcomplaints,
         a.VchDiagnosis,
         a.VchRemarks
     })
     .FirstOrDefault();
            if (lastAssessment == null)
            {
                return Json(new { success = false, message = "No previous assessments found." });
            }
            return Json(new { success = true, assessment = lastAssessment });
        }

        #endregion

        #region helper groups
        //add all medicine, radiology, lab, Procedure using it
        private static List<Dictionary<string, string>> ExtractGroups(
        Dictionary<string, string> fullData,
        string prefix // "Medicines", "LabTests", "Procedures", "Radiology"
            )
        {
            // Step 1: Get groups by prefix
            var groups = fullData
                .Where(kv => kv.Key.StartsWith(prefix + "["))
                .GroupBy(kv => kv.Key.Split(']')[0]) // Groups like Medicines[0], Procedures[1]
                .ToList();

            var result = new List<Dictionary<string, string>>();

            // Step 2: Iterate groups
            foreach (var group in groups)
            {
                // Convert each group into a property dictionary
                var dict = group.ToDictionary(
                    kv =>
                    {
                        var parts = kv.Key.Split('[', ']');
                        return parts.Length > 2 ? parts[2].TrimStart('.') : "";
                    },
                    kv => kv.Value
                );
               result.Add(dict);
            }
            return result;
        }

        #endregion

        #region ai sections
        [HttpPost]
        public async Task<IActionResult> UploadAudio(IFormFile audioFile)
        {
            if (audioFile == null || audioFile.Length == 0)
                return BadRequest(new { error = "No audio file received" });

            // Save temporarily (for next step: Whisper transcription)
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(audioFile.FileName);
            var tempPath = Path.Combine(Path.GetTempPath(), fileName);

            await using (var stream = System.IO.File.Create(tempPath))
            {
                await audioFile.CopyToAsync(stream);
            }

            Console.WriteLine($"🎧 Audio saved to: {tempPath}");

            return Ok(new
            {
                message = "Audio uploaded successfully",
                path = tempPath
            });
        }

        [HttpPost]
        public async Task<IActionResult> TranscribeAudio([FromBody] string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                return BadRequest(new { error = "Invalid or missing audio file." });

            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", "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");

                using var form = new MultipartFormDataContent();
                var fileStream = System.IO.File.OpenRead(filePath);
                var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("audio/webm");
                form.Add(streamContent, "file", Path.GetFileName(filePath));
                form.Add(new StringContent("whisper-1"), "model");

                var response = await httpClient.PostAsync("https://api.openai.com/v1/audio/transcriptions", form);
                var result = await response.Content.ReadAsStringAsync();

                try
                {
                    using var doc = JsonDocument.Parse(result);
                    var text = doc.RootElement.GetProperty("text").GetString();
                    return Json(new { success = true, transcription = text });
                }
                catch
                {
                    return Json(new { success = false, rawResponse = result });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // AssessmentController.cs

        // Make sure to include the using statement for your Scribe models (e.g., using YourAppName.Models.EkaScribe;)

        [HttpPost]
        public IActionResult ProcessScribeNotes(EkaScribeDataModel scribeData)
        {
            if (scribeData == null)
            {
                return Json(new { success = false, message = "No data received from Scribe." });
            }

            // --- 1. Prepare the output model ---
            // Initialize your existing VM structure
            var assessmentVM = new DWB.Models.DoctorAssessmentVM
            {
                DoctorAssessment = new DWB.Models.TblDoctorAssessment(),
                // Note: Medicine and Labs lists can be empty or null as we are skipping them
            };

            // --- 2. Map Notes and Complaints (Structural Documentation) ---

            // A. Map Symptoms (Chief Complaints)
            var chiefComplaints = scribeData.symptoms?
               .Select(s => $"{s.name} (Since {GetSymptomDuration(s)})")
               .ToList() ?? new List<string>();

            assessmentVM.DoctorAssessment.VchChiefcomplaints = string.Join("; ", chiefComplaints);

            // B. Map Examinations (Systemic Exam)
            var systemicExam = scribeData.medicalHistory?.examinations?
               .Select(e => $"{e.name}: {e.notes}")
               .ToList() ?? new List<string>();

            assessmentVM.DoctorAssessment.VchSystemicexam = string.Join(Environment.NewLine, systemicExam);

            // C. Map Past/Medical History (Combine Vitals and Patient History)
            var medicalHistory = new List<string>();

            //// Add Vitals (from Nursing Assessment display in Step 1 of CSHTML)
            //medicalHistory.AddRange(scribeData.medicalHistory?.vitals?
            //   .Select(v => $"{v.name}: {v.value?.} {v.value?.unit}") ?? new List<string>());

            // Add Patient History
            //medicalHistory.Add("Past History:");
            //medicalHistory.AddRange(scribeData.medicalHistory?.patientHistory?.patientMedicalConditions?
            //   .Where(c => c.status != "Absent")
            //   .Select(c => $"- {c.name}: {c.status}") ?? new List<string>());

            assessmentVM.DoctorAssessment.VchMedicalHistory = string.Join(Environment.NewLine, medicalHistory);


            // D. Map Diagnosis
            var diagnosisList = scribeData.diagnosis?
               .Select(d => d.name)
               .ToList() ?? new List<string>();

            assessmentVM.DoctorAssessment.VchDiagnosis = string.Join("; ", diagnosisList);

            // E. Map Follow-up Notes to Remarks
            assessmentVM.DoctorAssessment.VchRemarks = scribeData.followup?.notes;

            // --- 3. Return the fully populated VM to the client ---
            return Json(new { success = true, data = assessmentVM });
        }

        // Helper function to extract structured duration from Symptom object (adjust based on your exact model)
        private string GetSymptomDuration(Symptom s)
        {
            // Requires implementation to parse the complex 'properties' JSON structure 
            // For demonstration, returning a placeholder or simplified text
            return "3 Days";
        }
        #endregion
    }
}



