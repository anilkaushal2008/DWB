using DWB.GroupModels;
using DWB.Models;
using DWB.ViewModels; // Assume you place your ViewModels here
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DWB.APIModel;

namespace DWB.Controllers
{
    // Ensure this controller is protected, as financial counseling is not public access
    [Authorize]
    public class CounselingController : Controller
    {
        private readonly ILogger<CounselingController> _logger;
        private readonly DWBEntity _context; // Main DWB data context (will save our new tables)
        private readonly GroupEntity _groupcontext; // Group/Company data context

        // Constructor: Inject the same dependencies as HomeController
        public CounselingController(ILogger<CounselingController> logger, DWBEntity dWBEntity, GroupEntity groupcontext)
        {
            _logger = logger;
            _context = dWBEntity;
            _groupcontext = groupcontext;
        }
        //get patient from HMS API
        [Authorize(Roles = "Admin, Counsellor ")]
        public async Task<IActionResult> GetCounseling(string dateRange)
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
                        AllCounseling = JsonConvert.DeserializeObject<List<SP_OPD>>(jsonString) ?? new List<SP_OPD>(); // Handle possible null value
                        //filter counseling
                       // AllCounseling = AllCounseling
                       //.Where(p => p. != null && p.VisitType.Equals("Counseling", StringComparison.OrdinalIgnoreCase))
                       //.ToList();
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

        //File: Services/TariffService.cs (Modified Method)
        public async Task<List<EstimateLineItemViewModel>> GetActiveTariffItemsAsync()
        {
            var masterItems = await _context.TariffMaster
                                            .Where(t => t.IsActive)
                                            .ToListAsync();

            return masterItems.Select(t => new EstimateLineItemViewModel
            {
                ServiceName = t.ServiceName,
                UnitType = t.UnitType,
                TariffRate = t.CurrentRate,
                EstimatedQuantity = 0,
                CalculatedAmount = 0,
                // --- ADD THIS NEW FIELD TO THE MAPPING ---
                bitISdeafult = t.BitIsDeafult // Assuming your model uses this property name
                                           // ----------------------------------------
            }).ToList();
        }

        //New Estimate
        [HttpGet]
        public async Task<IActionResult> CreateEstimate(string uhid, string patientName, int VisitNo)
        {
            var counselorName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown Counselor";

            // 1. Fetch ALL active tariffs from the master list
            var allTariffs = await GetActiveTariffItemsAsync();

            var model = new CounselingViewModel
            {
                PatientRegistrationID = uhid ?? string.Empty,
                PatientName = patientName ?? string.Empty,
                CounselorName = counselorName,
                EstimateDate = DateTime.Today,
                VisitNo = VisitNo
            };

            // 2. Divide tariffs into two lists:

            // A. Items added to the form initially (bitIsDefault = true)
            model.LineItems.AddRange(allTariffs.Where(t => t.bitISdeafult).ToList());

            // B. Items available for selection in the dropdown (bitIsDefault = false)
            model.AvailableTariffs.AddRange(allTariffs.Where(t => !t.bitISdeafult).ToList());

            // ADD THIS: Pass the full list of tariff/services for the dropdown
            ViewBag.ServiceList = _context.TariffMaster                         
                          .Select(t => new {
                              ServiceName = t.ServiceName,
                              CurrentRate =t.CurrentRate,
                              UnitType = t.UnitType
                          })
                          .ToList();

            model.Payments.Add(new PaymentTransactionViewModel());
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Good practice to add this
        public async Task<IActionResult> CreateEstimate(CounselingViewModel model)
        {
            // 1. Recalculate Totals (Trust Server Math)
            RecalculateViewModelTotals(model);
            // 1. Check Acknowledgement
            if (!model.IsAcknowledged)
            {
                ModelState.AddModelError("IsAcknowledged", "You must confirm that you have read and agreed to the terms.");
            }

            // 2. Check Total Cost
            if (model.TotalEstimatedCost <= 0)
            {
                ModelState.AddModelError("", "Total Estimated Cost cannot be zero.");
            }
            // 2. CHECK VALIDATION
            if (!ModelState.IsValid)
            {
                // --- CRITICAL FIX START: RELOAD SEARCH DATA ---
                // If we don't reload this, the Search Dropdown will break on the error page
                ViewBag.ServiceList = _context.TariffMaster
                                        .Where(t => t.IsActive == true)
                                        .Select(t => new {
                                            ServiceName = t.ServiceName,
                                            CurrentRate = t.CurrentRate,
                                            UnitType = t.UnitType
                                        })
                                        .ToList();
                // --- CRITICAL FIX END ---

                // Re-initialize hardcoded items only if list is totally empty
                if (model.LineItems == null || !model.LineItems.Any())
                {
                    model.LineItems = new List<EstimateLineItemViewModel>(); // Ensure not null
                    //model.LineItems.AddRange(GetHardcodedTariffItems());
                }

                // Retain counselor name
                model.CounselorName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown Counselor";

                return View(model);
            }

            // 3. Map ViewModel to Database Entity
            var record = MapViewModelToRecord(model);

            // 4. Set Audit Fields (Cleaned up logic)
            record.VisitNumber = model.VisitNo;
            record.CreatedBy = User.Identity?.Name ?? "Unknown";
            record.VchCompletedUser = User.Identity?.Name ?? "Unknown";
            record.CreatedAt = DateTime.Now;
            record.BitIsCompleted = true;
            record.VchHostName = model.CLientHost ?? "Unknown Host";

            // Safe Conversion of Claims (Prevents crash if Claim is null)
            if (int.TryParse(User.FindFirst("UnitId")?.Value, out int unitId))
            {
                record.IntCode = unitId;
            }

            if (int.TryParse(User.FindFirst("HMScode")?.Value, out int hmsCode))
            {
                record.IntIhmscode = hmsCode;
            }

            // 5. Save to Database
            _context.PatientEstimateRecord.Add(record);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Counseling Record saved successfully.";

            // 6. Redirect
            return RedirectToAction("ViewEstimate", new { uhid = record.Uhid, visit=record.VisitNumber });
        }


        // --- 2. COUNSELING AUDIT REPORT ACTION ---
        // Handles monthly audit reports for accreditation.
        [HttpGet]
        [HttpPost] // Allows the month filter form to post back to the same action
        [Authorize(Roles = "Admin, Billing, Audit")] // Example roles for accessing financial reports
        public async Task<IActionResult> AuditReport(CounselingReportViewModel? viewModel = null)
        {
            if (viewModel == null || viewModel.ReportMonth == default)
            {
                viewModel = new CounselingReportViewModel
                {
                    ReportMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1) // Default to current month
                };
            }

            DateTime startOfMonth = new DateTime(viewModel.ReportMonth.Year, viewModel.ReportMonth.Month, 1);
            DateTime endOfMonth = startOfMonth.AddMonths(1);

            // LINQ query to group data by CounselorName and aggregate metrics
            var reportQuery = await _context.PatientEstimateRecord // Your DbSet name
                .Where(r => r.EstimateDate >= startOfMonth && r.EstimateDate < endOfMonth)
                .GroupBy(r => r.CounselorName)
                .Select(g => new CounselorSummaryDto
                {
                    CounselorName = g.Key,
                    TotalSessions = g.Count(),
                    TotalEstimatedValue = g.Sum(r => r.TotalEstimatedCost),
                    TotalAdvanceCollected = g.Sum(r => r.TotalAmountReceived)
                })
                .OrderByDescending(r => r.TotalSessions)
                .ToListAsync();

            viewModel.ReportData = reportQuery;

            return View(viewModel);
        }


        [HttpGet]
        public async Task<IActionResult> ViewEstimate(string uhid, int visit)
        {
            // 1. Fetch the record using Entity Framework
            // Adjust '_context.PatientEstimateRecord' to match your exact DbSet name
            var record = await _context.PatientEstimateRecord
                .Include(x => x.EstimateLineItem) // Load Services
                .Include(x => x.PaymentTransaction)  // Load Payments
                .FirstOrDefaultAsync(x => x.Uhid == uhid && x.VisitNumber==visit);

            if (record == null)
            {
                return NotFound();
            }

            // 2. Return the Entity directly (or map to a ViewModel if you prefer)
            return View(record);
        }

        [HttpGet]
        public async Task<IActionResult> EditEstimate(string uhid, int visit)
        {
            // 1. Fetch existing record with all children
            var record = await _context.PatientEstimateRecord
                .Include(x => x.EstimateLineItem)
                .Include(x => x.PaymentTransaction)
                .FirstOrDefaultAsync(x => x.Uhid == uhid && x.VisitNumber == visit);

            if (record == null)
            {
                TempData["Error"] = "Estimate not found for editing.";
                return RedirectToAction("GetCounseling"); // Or your list page action
            }

            // 2. Map Entity -> ViewModel (Reverse of Create)
            var model = new CounselingViewModel
            {
                EstimateRecordId = record.EstimateRecordId,
                PatientRegistrationID = record.Uhid,
                PatientName = record.PatientName,
                VisitNo = record.VisitNumber,
                CounselorName = record.CounselorName,
                EstimateDate = record.EstimateDate,

                // Map Patient/Attendant Details
                RelativeName = record.RelativeName,
                RelativeRelation = record.RelativeRelation,
                RelativeSignatureData = record.RelativeSignatureData, // Signature text
                IsAcknowledged = true, // Usually true if editing an existing record

                // Map Line Items
                LineItems = record.EstimateLineItem.Select(x => new EstimateLineItemViewModel
                {
                    ServiceName = x.ServiceName,
                    UnitType = x.UnitType,
                    TariffRate = x.TariffRate,
                    EstimatedQuantity = x.EstimatedQuantity,
                    CalculatedAmount = x.CalculatedAmount,
                    bitISdeafult = false // Loaded items are no longer default
                }).ToList(),

                // MAP PAYMENTS
                Payments = (record.PaymentTransaction ?? new List<PaymentTransaction>())
            .Select(x => new PaymentTransactionViewModel
            {
                // Fix for DateOnly to DateTime conversion
                PaymentDate = x.PaymentDate.HasValue
                    ? x.PaymentDate.Value.ToDateTime(TimeOnly.MinValue)
                    : DateTime.Today,

                // Fix for Nullable Decimal
                AmountPaid = x.AmountPaid ?? 0,

                PayerName = x.PayerName,
                PayerRelation = x.PayerRelation
            }).ToList()
            };
            // ▼▼▼ THE FIX: IF NO PAYMENTS EXIST, ADD A BLANK ROW ▼▼▼
            if (!model.Payments.Any())
            {
                model.Payments.Add(new PaymentTransactionViewModel
                {
                    PaymentDate = DateTime.Today,
                    AmountPaid = 0,
                    PayerName = ""
                });
            }


            // 3. Load the Dropdown Data (Crucial for the view to work)
            ViewBag.ServiceList = _context.TariffMaster
                                  .Where(t => t.IsActive == true)
                                  .Select(t => new {
                                      ServiceName = t.ServiceName,
                                      CurrentRate = t.CurrentRate,
                                      UnitType = t.UnitType
                                  })
                                  .ToList();

            // 4. Return the SAME View used for Create
            return View("CreateEstimate", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEstimate(CounselingViewModel model)
        {
            // ---------------------------------------------------------
            // 1. PRE-VALIDATION CLEANUP
            // ---------------------------------------------------------
            // Filter out empty payment rows (Amount 0 or No Name)
            if (model.Payments != null)
            {
                model.Payments = model.Payments
                    .Where(p => p.AmountPaid > 0 || !string.IsNullOrWhiteSpace(p.PayerName))
                    .ToList();
            }

            // Remove validation errors for the empty rows we just deleted
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Payments")))
            {
                ModelState.Remove(key);
            }

            // Recalculate Totals
            RecalculateViewModelTotals(model);

            // ---------------------------------------------------------
            // 2. CHECK VALIDATION
            // ---------------------------------------------------------
            if (!ModelState.IsValid)
            {
                // Reload Dropdown Data
                ViewBag.ServiceList = _context.TariffMaster.Where(t => t.IsActive).Select(t => new { Text = t.ServiceName, Rate = t.CurrentRate, Unit = t.UnitType }).ToList();
                return View("CreateEstimate", model);
            }
            try
            {
                // ---------------------------------------------------------
                // 3. FETCH EXISTING RECORD (Include Children!)
                // ---------------------------------------------------------
                // Note: Check if your entity property is named 'Payments' or 'PaymentTransaction'
                var record = await _context.PatientEstimateRecord
                    .Include(x => x.EstimateLineItem)
                    .Include(x => x.PaymentTransaction) // <--- CHECK THIS NAME matches your DB Context
                    .FirstOrDefaultAsync(x => x.EstimateRecordId == model.EstimateRecordId);

                if (record == null) return NotFound();

                // ---------------------------------------------------------
                // 4. UPDATE PARENT FIELDS
                // ---------------------------------------------------------
                record.RelativeName = model.RelativeName;
                record.RelativeRelation = model.RelativeRelation;
                record.RelativeSignatureData = model.RelativeSignatureData;
                record.EstimateDate = model.EstimateDate;
                //Audit: Update the "Last Modified By" user
                record.VchCompletedUser = User.Identity?.Name ?? "Unknown";

                // ---------------------------------------------------------
                // 5. UPDATE LINE ITEMS (Strategy: Replace All)
                // ---------------------------------------------------------
                // A. Remove existing lines
                if (record.EstimateLineItem != null)
                {
                    _context.EstimateLineItem.RemoveRange(record.EstimateLineItem);
                }

                // B. Add new lines from Model
                // Ensure property names match your EstimateLineItem entity
                record.EstimateLineItem = model.LineItems.Select(x => new EstimateLineItem
                {
                    ServiceName = x.ServiceName,
                    UnitType = x.UnitType,
                    TariffRate = x.TariffRate,
                    EstimatedQuantity = x.EstimatedQuantity,
                    CalculatedAmount = x.TariffRate * (decimal)x.EstimatedQuantity
                }).ToList();

                // ---------------------------------------------------------
                // 6. UPDATE PAYMENTS (Strategy: Replace All)
                // ---------------------------------------------------------
                // A. Remove existing payments
                if (record.PaymentTransaction != null)
                {
                    _context.PaymentTransaction.RemoveRange(record.PaymentTransaction);
                }

                //B. Add new payments from Model
                if (model.Payments != null && model.Payments.Any())
                {
                    record.PaymentTransaction = model.Payments.Select(p => new PaymentTransaction
                    {
                        // 1. Handle Decimal (Nullable to Non-Nullable)
                        AmountPaid = (decimal?)p.AmountPaid ?? 0,
                        PayerName = p.PayerName,
                        PayerRelation = p.PayerRelation,
                        PaymentDate = DateOnly.FromDateTime(p.PaymentDate)
                    }).ToList();
                }              
                else
                {
                    // Ensure the list isn't null if empty
                    // FIX: Changed from List<PatientEstimateRecord> to List<PaymentTransaction>
                    record.PaymentTransaction = new List<PaymentTransaction>();
                }

                // ---------------------------------------------------------
                // 7. RECALCULATE DB TOTALS
                // ---------------------------------------------------------
                // Summing from the new lists we just attached ensures accuracy
                // FIX: Changed from record.LineItems/record.Payments to record.EstimateLineItem/record.PaymentTransaction
                record.TotalEstimatedCost = record.EstimateLineItem.Sum(x => x.CalculatedAmount);
                record.TotalAmountReceived = record.PaymentTransaction.Sum(x => x.AmountPaid ?? 0);

                // ---------------------------------------------------------
                // 8. SAVE
                // ---------------------------------------------------------
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Estimate updated successfully.";

                // Redirect to View/Print page
                return RedirectToAction("ViewEstimate", new { uhid = record.Uhid, visit=record.VisitNumber });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Update Failed: " + ex.Message);
                // Reload ViewBag on error
                ViewBag.ServiceList = _context.TariffMaster.Where(t => t.IsActive).Select(t => new { Text = t.ServiceName, Rate = t.CurrentRate, Unit = t.UnitType }).ToList();
                return View("CreateEstimate", model);
            }
        }

        // Helper to recalculate totals safely on the server
        private void RecalculateViewModelTotals(CounselingViewModel vm)
        {
            decimal totalCost = 0;
            // Recalculate Line Item totals
            foreach (var item in vm.LineItems)
            {
                // Ensure logic matches client-side: Rate * Quantity
                item.CalculatedAmount = item.TariffRate * item.EstimatedQuantity;
                totalCost += item.CalculatedAmount;
            }

            // Recalculate Payment totals
            vm.TotalAmountReceived = vm.Payments.Where(p => p.AmountPaid > 0).Sum(p => p.AmountPaid);

            // Update the main total field on the record
            vm.TotalEstimatedCost = totalCost;
        }

        // Helper to map ViewModel data to the EF Core Models for saving
        private PatientEstimateRecord MapViewModelToRecord(CounselingViewModel vm)
        {
            // NOTE: Ensure property names match your scaffolded models exactly!
            var record = new PatientEstimateRecord
            {
                Uhid = vm.PatientRegistrationID, // Assuming UHID maps to PatientRegistrationID
                PatientName = vm.PatientName,
                CounselorName = vm.CounselorName,
                EstimateDate = vm.EstimateDate,
                IsAcknowledged = vm.IsAcknowledged,
                RelativeName = vm.RelativeName,
                RelativeRelation = vm.RelativeRelation,
                RelativeSignatureData = vm.RelativeSignatureData,
                TotalEstimatedCost = vm.TotalEstimatedCost, // Use the calculated total
                TotalAmountReceived = vm.TotalAmountReceived,
            };

            // Map Line Items
            record.EstimateLineItem = vm.LineItems.Select(li => new EstimateLineItem // Assuming LineItems is the navigation property name
            {
                ServiceName = li.ServiceName,
                UnitType = li.UnitType,
                TariffRate = li.TariffRate,
                EstimatedQuantity = li.EstimatedQuantity,
                CalculatedAmount = li.CalculatedAmount,
                // EstimateRecordID is set automatically by EF Core when adding to the navigation collection
            }).ToList();

            // Map Payments
            record.PaymentTransaction = vm.Payments.Where(p => p.AmountPaid > 0).Select(p => new PaymentTransaction // Assuming Payments is the navigation property name
            {
                PaymentDate = DateOnly.FromDateTime(p.PaymentDate),
                AmountPaid = p.AmountPaid,
                PayerName = p.PayerName,
                PayerRelation = p.PayerRelation
                // EstimateRecordID is set automatically by EF Core
            }).ToList();

            return record;
        }


        // Mock Data: This must be in your Controller class
        //private List<EstimateLineItemViewModel> GetHardcodedTariffItems()
        //{
        //    return new List<EstimateLineItemViewModel>
        //    {
        //        new EstimateLineItemViewModel { ServiceName = "Private Room Rent", TariffRate = 5500.00m, UnitType = "PER DAY", EstimatedQuantity = 0 },
        //        new EstimateLineItemViewModel { ServiceName = "General Ward Rent", TariffRate = 2500.00m, UnitType = "PER DAY", EstimatedQuantity = 0 },
        //        new EstimateLineItemViewModel { ServiceName = "ICU/CCU/MICU Rent", TariffRate = 8000.00m, UnitType = "PER DAY", EstimatedQuantity = 0 },
        //        new EstimateLineItemViewModel { ServiceName = "Medicine Charges", TariffRate = 1.00m, UnitType = "As per actual", EstimatedQuantity = 1 },
        //        new EstimateLineItemViewModel { ServiceName = "Consumable Charges", TariffRate = 1.00m, UnitType = "As per actual", EstimatedQuantity = 1 },
        //        new EstimateLineItemViewModel { ServiceName = "Lab Charges", TariffRate = 1.00m, UnitType = "As per actual", EstimatedQuantity = 1 },
        //        new EstimateLineItemViewModel { ServiceName = "OT Charges", TariffRate = 0.35m, UnitType = "35% of Procedure Cost", EstimatedQuantity = 0 },
        //        new EstimateLineItemViewModel { ServiceName = "Anesthesia Charges", TariffRate = 0.30m, UnitType = "30% of Procedure Cost", EstimatedQuantity = 0 },
        //        new EstimateLineItemViewModel { ServiceName = "Asst. Surgeon Charges", TariffRate = 0.30m, UnitType = "30% of Procedure Cost", EstimatedQuantity = 0 },
        //        new EstimateLineItemViewModel { ServiceName = "Ventilator Charges", TariffRate = 3500.00m, UnitType = "PER DAY", EstimatedQuantity = 0 },
        //        new EstimateLineItemViewModel { ServiceName = "Intubation", TariffRate = 1200.00m, UnitType = "Procedure Cost", EstimatedQuantity = 0 },
        //        new EstimateLineItemViewModel { ServiceName = "Center Line", TariffRate = 4000.00m, UnitType = "Procedure Cost", EstimatedQuantity = 0 },
        //        new EstimateLineItemViewModel { ServiceName = "Oxygen/Hour", TariffRate = 150.00m, UnitType = "PER HOUR", EstimatedQuantity = 0 },
        //        new EstimateLineItemViewModel { ServiceName = "Physiotherapy", TariffRate = 500.00m, UnitType = "PER Visit", EstimatedQuantity = 0 },
        //        new EstimateLineItemViewModel { ServiceName = "Dietitian Consultation", TariffRate = 500.00m, UnitType = "PER Visit", EstimatedQuantity = 0 },
        //        new EstimateLineItemViewModel { ServiceName = "Nursing", TariffRate = 1.00m, UnitType = "As per actual", EstimatedQuantity = 1 },
        //        new EstimateLineItemViewModel { ServiceName = "R.M.O.", TariffRate = 1.00m, UnitType = "As per actual", EstimatedQuantity = 1 }
        //    };
        //}

        // File: Controllers/CounselingController.cs

        [HttpPost]
        [Authorize(Roles = "Admin, Counsellor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTariffMaster(TariffUpdateViewModel model)
        {
            // Retrieve audit codes once:
            long unitCode = Convert.ToInt64(User.FindFirst("UnitId")?.Value ?? "0");
            long hmsCode = Convert.ToInt64(User.FindFirst("HMScode")?.Value ?? "0");
            string userId = User.FindFirst("UserId")?.Value??"0";

            // We do NOT check model.PatientRegistrationID here anymore.
            if (!ModelState.IsValid || model.TariffItems == null || !model.TariffItems.Any())
            {
                // On error, we rely on the client-side AJAX handler to show the message and refresh the page.
                // We only return JSON now.
                return Json(new { success = false, message = "Validation failed on one or more items." });
            }

            foreach (var item in model.TariffItems)
            {
                // 1. Clean up input and set all audit info
                item.ServiceName = item.ServiceName?.Trim() ?? string.Empty;
                //Assign Audit Data:
                item.CreatedBy = userId;
                item.CreatedAt = DateTime.Now;
                item.ClientIp = model.ClientIP;
                item.ClientHostName = model.ClientHostName;
                item.IntCode = unitCode.ToString();
                item.IntHmscode = hmsCode.ToString();

                // 2. Database Logic (Update/Add)
                var existingItem = await _context.TariffMaster
                                                 .AsNoTracking()
                                                 .FirstOrDefaultAsync(t => t.ServiceName == item.ServiceName);
                if (existingItem != null)
                {
                    item.TariffMasterId = existingItem.TariffMasterId;
                    _context.TariffMaster.Update(item);
                }
                else
                {
                    item.IsActive = true;
                    _context.TariffMaster.Add(item);
                }
                if(item.BitIsDeafult==true)
                {
                    item.BitIsDeafult = true;
                }
            }
            await _context.SaveChangesAsync();
            // SUCCESS RETURN: Return JSON, client-side JS handles the message display and full page reload.
            return Json(new { success = true, message = "Tariff Master updated successfully. Rates will apply after refresh." });
        }


    }
}