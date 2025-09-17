using DWB.APIModel;
using DWB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient.DataClassification;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Reflection.Emit;
using System.Reflection.Metadata;
//for pdf packages
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using DWB.Report;

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
            return View(vm);
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
                    VchRemarks = string.IsNullOrEmpty(model.DoctorAssessment.VchRemarks)
                        ? null : model.DoctorAssessment.VchRemarks,
                    BitAsstCompleted = true,
                    DtStartTime = model.DoctorAssessment.DtStartTime,
                    DtEndTime = DateTime.Now,
                    VchCreatedBy = User.Identity.Name,
                    DtCreated = DateTime.Now,
                    IntCode= Convert.ToInt32(User.FindFirst("UnitId")?.Value),
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
                                VchDosage = med.VchDosage,
                                VchFrequency = med.VchFrequency,
                                VchDuration = med.VchDuration,
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
                                DtCreated = DateTime.Now
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
        //public async Task<IActionResult> DocAssmntEdit(int id)
        //{
        //    var assessment = await _context.TblDoctorAssessment
        //        .FirstOrDefaultAsync(x => x.IntId == id);

        //    if (assessment == null)
        //        return NotFound();

        //    var model = new DoctorAssessmentVM
        //    {
        //        DoctorAssessment = new TblDoctorAssessment
        //        {
        //            IntId = assessment.IntId,
        //            FkUhid = assessment.FkUhid,
        //            FkAssessmentId = assessment.FkAssessmentId,
        //            VchChiefcomplaints = assessment.VchChiefcomplaints,
        //            VchDiagnosis = assessment.VchDiagnosis,
        //            VchMedicalHistory = assessment.VchMedicalHistory,
        //            VchSystemicexam = assessment.VchSystemicexam,
        //            VchRemarks = assessment.VchRemarks
        //        },
        //        Medicines = await _context.TblDoctorAssmntMedicine
        //            .Where(m => m.FkDocAssmntId == id).ToListAsync(),
        //        Labs = await _context.TblDoctorAssmntLab
        //            .Where(l => l.FkDocAssmntId == id).ToListAsync(),
        //        Radiology = await _context.TblDoctorAssmntRadiology
        //            .Where(r => r.FkDocAssmntId == id).ToListAsync(),
        //        Procedures = await _context.TblDoctorAssmntProcedure
        //            .Where(p => p.FkDocAsstId == id).ToListAsync(),
        //        Documents = await _context.TblDoctorAssessmentDoc
        //            .Where(d => d.IntFkDoctorAssId == id).ToListAsync()
        //    };

        //    return View(model);
        //}

        //// POST: Doctor Assessment Edit
        //[HttpPost]
        //public async Task<IActionResult> DocAssmntEdit(DoctorAssessmentVM model, IFormFile[] doctorDocs)
        //{
        //    if (!ModelState.IsValid)
        //        return View(model);

        //    try
        //    {
        //        // update assessment
        //        var assessment = await _context.TblDoctorAssessment
        //            .FirstOrDefaultAsync(x => x.IntId == model.DoctorAssessment.IntId);

        //        if (assessment == null) return NotFound();

        //        assessment.VchChiefcomplaints = model.DoctorAssessment.VchChiefcomplaints;
        //        assessment.VchDiagnosis = model.DoctorAssessment.VchDiagnosis;
        //        assessment.VchMedicalHistory = model.DoctorAssessment.VchMedicalHistory;
        //        assessment.VchSystemicexam = model.DoctorAssessment.VchSystemicexam;
        //        assessment.VchRemarks = model.DoctorAssessment.VchRemarks;

        //        _context.TblDoctorAssessment.Update(assessment);
        //        await _context.SaveChangesAsync();

        //        // 🔹 Medicines/Labs/Radiology/Procedures update strategy:
        //        // simplest: delete old and re-insert new
        //        var oldMeds = _context.TblDoctorAssmntMedicine.Where(m => m.FkDocAssmntId == assessment.IntId);
        //        _context.TblDoctorAssmntMedicine.RemoveRange(oldMeds);
        //        await _context.SaveChangesAsync();

        //        foreach (var med in model.Medicines)
        //        {
        //            med.FkDocAssmntId = assessment.IntId;
        //            med.DtCreated = DateTime.Now;
        //            med.VchCreatedBy = User.Identity.Name;
        //            _context.TblDoctorAssmntMedicine.Add(med);
        //        }
        //        await _context.SaveChangesAsync();

        //        // repeat same remove+add logic for Labs, Radiology, Procedures...

        //        // 🔹 Handle file uploads
        //        if (doctorDocs != null && doctorDocs.Length > 0)
        //        {
        //            string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "DoctorDocs");
        //            if (!Directory.Exists(uploadPath))
        //                Directory.CreateDirectory(uploadPath);

        //            foreach (var file in doctorDocs)
        //            {
        //                if (file.Length > 0)
        //                {
        //                    string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        //                    string filePath = Path.Combine(uploadPath, fileName);

        //                    using (var stream = new FileStream(filePath, FileMode.Create))
        //                    {
        //                        await file.CopyToAsync(stream);
        //                    }

        //                    _context.TblDoctorAssessmentDoc.Add(new TblDoctorAssessmentDoc
        //                    {
        //                        IntFkDoctorAssId = assessment.IntId,
        //                        VchFileName = fileName,
        //                        DtCreated = DateTime.Now
        //                    });
        //                }
        //            }
        //            await _context.SaveChangesAsync();
        //        }

        //        TempData["Success"] = "✅ Doctor Assessment updated successfully!";
        //        return RedirectToAction("Index");
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["Error"] = "❌ Error updating assessment: " + ex.Message;
        //        return View(model);
        //    }
        //}

        //Print assessment
        [HttpGet]
        public IActionResult ViewDocAssessment(string uhid,string visit, string date)
        {
            // fetch all data from DB
            var nursing = _context.TblNsassessment.FirstOrDefault(x => x.VchUhidNo == uhid && x.IntIhmsvisit==Convert.ToInt32(visit));
            var doctor = _context.TblDoctorAssessment.FirstOrDefault(x => x.FkAssessmentId == nursing.IntAssessmentId);
            var medicines = _context.TblDoctorAssmntMedicine.Where(x => x.FkDocAssmntId == doctor.IntId).ToList();
            var labs = _context.TblDoctorAssmntLab.Where(x => x.FkDocAssmntId == doctor.IntId).ToList();
            var radiologies = _context.TblDoctorAssmntRadiology.Where(x => x.FkDocAssmntId == doctor.IntId).ToList();
            var procedures = _context.TblDoctorAssmntProcedure.Where(x => x.FkDocAsstId == doctor.IntId).ToList();
            var userData=_context.TblUsers.FirstOrDefault(x => x.IntUserId == doctor.FkUserId);

            var report = new DoctorAssessmentReport(nursing, doctor, medicines, labs, radiologies, procedures,userData);

            var pdf = report.GeneratePdf();

            return File(pdf, "application/pdf"); // <--- Important
        }


        #endregion

    }
}
