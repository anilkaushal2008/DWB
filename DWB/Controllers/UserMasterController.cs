using DWB.Models;
using DWB.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System.Threading.Tasks;

namespace DWB.Controllers
{
    public class UserMasterController : Controller
    {
        private readonly DWBEntity _context;
        private readonly IWebHostEnvironment _WebHostEnvironment;
        public UserMasterController(DWBEntity dWBEntity, IWebHostEnvironment webHostEnvironment)
        {
            _context = dWBEntity;
            _WebHostEnvironment = webHostEnvironment;
        }
        [Authorize(Roles = "Admin,Nursing")]
        public ActionResult UserMasters()
        {
            if (!TempData.ContainsKey("ActiveTab"))
            {
                TempData["ActiveTab"] = "UserTab"; // Default first tab
            }
            _AllUsers();
            return View();
        }

        #region User Master Tab
        // GET: UserMasterController/AllUsers
        [Authorize(Roles = "Admin")]
        public IActionResult _AllUsers()
        {
            var model = new MasterUserTab
            {
                AllUsers = _context.TblUsers.Include(u => u.TblUserCompany).ThenInclude(uc => uc.FkIntCompany).ToList(),
                UsersCompany = _context.TblUserCompany.ToList()
            };
            if (model.AllUsers.Count() == 0)
            {
                TempData["EmptyUser"] = "No users found in database!";
            }
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult NewUser()
        {
            //for role selection
            var getRole = _context.TblRoleMas.OrderBy(m => m.VchRole).ToList();
            if (getRole.Count != 0)
            {
                ViewBag.RoleList = new SelectList(getRole, "IntId", "VchRole");
            }
            else
            {
                var emptyList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "0", Text = "No roles available" }
                };
                ViewBag.RoleList = emptyList;
            }
            //for Multi Select branches
            var GetBranches = _context.IndusCompanies.OrderBy(m => m.Descript).ToList();
            if (GetBranches.Count != 0)
            {
                ViewBag.BranchList = new SelectList(GetBranches, "IntPk", "Descript");
            }
            else
            {
                var emptyList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "0", Text = "No branches available" }
                };
            }
            return PartialView("_PartialCreateUser", new CreateUserViewModel());
        }        

        public IActionResult UserDeactivate(int id, int code)
        {
            var GetUser = _context.TblUsers.Find(id);
            if (GetUser != null)
            {
                _context.SaveChanges();
                TempData["ActiveTab"] = "UserTab";
                TempData["userSuccess"] = "User Deactivated successfully.";
                return RedirectToAction("UserMasters");
            }
            else
            {
                TempData["ActiveTab"] = "UserTab";
                TempData["userError"] = "User not found or already deactivated.";
                return RedirectToAction("UserMasters");
            }
        }

        public IActionResult UserActivate(int id)
        {
            var GetUser = _context.TblUsers.Find(id);
            if (GetUser != null)
            {
                _context.SaveChanges();
                TempData["ActiveTab"] = "UserTab";
                TempData["userSuccess"] = "User Activated successfully.";
                return RedirectToAction("UserMasters");
            }
            else
            {
                TempData["ActiveTab"] = "UserTab";
                TempData["userError"] = "User not found or already activated.";
                return RedirectToAction("UserMasters");
            }
        }

        #endregion

        #region New user code
        // -- Role HELPERS --
        private void LoadDropdowns()
        {
            ViewBag.RoleList = new SelectList(_context.TblRoleMas.OrderBy(r => r.VchRole), "IntId", "VchRole");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]        
        public IActionResult CreateUser()
        {
            var model = new CreateUserViewModel
            {
                UserCompanies = _context.IndusCompanies
                    .Select(c => new CompanySelectionViewModel
                    {
                        CompanyId = c.IntPk,
                        CompanyName = c.Descript,
                        IsSelected = false
                    })
                    .ToList()
            };

            LoadDropdowns();
            return PartialView("_PartialCreateUser", model);
        }

        [Authorize(Roles = "Admin")]
        //POST: UserMasterController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model, IFormFile ProfileFile, IFormFile SignatureFile)
        {
            
            //check duplicate user name
            if (_context.TblUsers.Any(d => d.VchUsername == model.VchUsername))
            {   //add model error
                //TempData["ActiveTab"] = "UserTab";
                ModelState.AddModelError("VchUsername", "User already existing ");
                LoadDropdowns();
                return PartialView("_PartialCreateUser", model);
            }
            // --- VALIDATION BLOCK ---
            if (HasScheduleConflict(model, out string conflictDay))
            {
                // A conflict was found. Add a model error and return.
                // This error will appear in your validation summary area
                ModelState.AddModelError("", $"Schedule Conflict: This user is assigned to multiple companies on {conflictDay}.");
                LoadDropdowns();
                return PartialView("_PartialCreateUser", model);
            }

            var FinalProfileFile = string.Empty;
            var FinalSignatureFile = string.Empty;
            var ProfileFolder = string.Empty;
            var SignatureFolder = string.Empty;
            if (model.ProfileFile != null)
            {
                ProfileFolder= Path.Combine(_WebHostEnvironment.WebRootPath, "uploads", "Profile");
                var Extension = Path.GetExtension(ProfileFile.FileName);
                FinalProfileFile = $"{Path.GetFileNameWithoutExtension(model.ProfileFile.FileName)}-{DateTime.Now:yyyyMMddHHmmss}{Extension}";
                var filepath = Path.Combine(ProfileFolder, FinalProfileFile);
                using (var stream = model.ProfileFile.OpenReadStream())
                {
                    using (var image = Image.Load(stream))
                    {
                        //Resize the image if needed
                        var maxWidth = 200; // Set your desired max width
                        var maxHeight = 200; // Set your desired max height
                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(maxWidth, maxHeight),
                            Mode = ResizeMode.Crop //crop/fir to aspect ratio
                        })
                        );
                        var encoder = new JpegEncoder
                        {
                            Quality = 95 //Set quality for JPEG
                        };
                        // Save the resized image
                        await image.SaveAsync(filepath, encoder);
                    }
                }
            }
            if (model.SignatureFile != null)
            {
                SignatureFolder = Path.Combine(_WebHostEnvironment.WebRootPath, "uploads", "Signature");
                var Extension = Path.GetExtension(SignatureFile.FileName);
                FinalSignatureFile = $"{Path.GetFileNameWithoutExtension(model.SignatureFile.FileName)}_{DateTime.Now:yyyyMMddHHmmss}{Extension}";
                var filePath = Path.Combine(SignatureFolder, FinalSignatureFile);
                //crop and grayscale the image as desired
                using (var stream = model.SignatureFile.OpenReadStream())
                {
                    using (var image = Image.Load(stream))
                    {
                        // Resize the image if needed
                        var maxWidth = 200; // Set your desired max width
                        var maxHeight = 100; // Set your desired max height
                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(maxWidth, maxHeight),
                            Mode = ResizeMode.Crop // crop/fir to aspect ratio
                        }).Grayscale()
                         );
                        var encoder = new JpegEncoder
                        {
                            Quality = 95 // Set quality for JPEG
                        };
                        // Save the resized image
                        await image.SaveAsync(filePath, encoder);
                    }
                }
            }
            //change password to hashpassword          
            var HasedPassword = PasswordHelper.ConvertHashPassword(model.HpasswordHash ?? string.Empty);
            var user = new TblUsers
            {


                VchUsername = model.VchUsername,
                HpasswordHash = HasedPassword, // model.HpasswordHash, // You should hash it in real app
                VchFullName = model.VchFullName,
                VchEmail = model.VchEmail,
                VchMobile = model.VchMobile,
                FkRoleId = model.FkRoleId,
                VchProfileFileAddress = ProfileFolder + "\\" + FinalProfileFile,
                VchProfileFileName = FinalProfileFile,
                VchSignFileAddress = SignatureFolder + "\\" + FinalSignatureFile,
                VchSignFileName = FinalSignatureFile,
                VchCreatedBy = User.Identity?.Name ?? "system",
                VchIpUsed = HttpContext.Connection.RemoteIpAddress?.ToString()
            };
            _context.TblUsers.Add(user);
            _context.SaveChanges();

            //Save company mappings
            foreach (var company in model.UserCompanies)
            {
                if (company.IsSelected)
                {
                    var userCompany = new TblUserCompany
                    {
                        //add company hms id
                        FkUseriId = user.IntUserId,
                        FkIntCompanyId = company.CompanyId,
                        VchDoctorCode = company.VchDoctorCode,                        
                        DtCreated = DateTime.Now,
                        VchCreatedBy = User.Identity?.Name ?? "system",
                        VchIpUsed = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        BitIsSunday = company.IsSunday,
                        BitIsMonday = company.IsMonday,
                        BitIsTuesday = company.IsTuesday,
                        BitIsWednesday = company.IsWednesday,
                        BitIsThursday = company.IsThursday,
                        BitIsFriday = company.IsFriday,
                        BitIsSaturday = company.IsSaturday,
                        TimeStartTime = company.StartTime,
                        TimeEndTime = company.EndTime
                    };
                    _context.TblUserCompany.Add(userCompany);
                    _context.SaveChanges();
                }
            }
            TempData["ActiveTab"] = "UserTab";
            TempData["userSuccess"] = "User created and saved successfully.";
            //Return success (optional: redirect or return JSON)
            return Json(new { success = true });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> UserEdit(int id)
        {
            var user = await _context.TblUsers
                .Include(m => m.TblUserCompany)
                 .ThenInclude(uc => uc.FkIntCompany) // include company details
                  .FirstOrDefaultAsync(u => u.IntUserId == id);
            if (user == null)
                return NotFound();

            var model = new CreateUserViewModel
            {
                IntUserId = user.IntUserId,
                VchUsername = user.VchUsername,
                HpasswordHash = user.HpasswordHash.ToString(),
                VchFullName = user.VchFullName,
                VchEmail = user.VchEmail,
                VchMobile = user.VchMobile,
                FkRoleId = user.FkRoleId,
                // Set uploaded files if they exist
                UploadedProfileFile = user.VchProfileFileName,
                UploadedSignatureFile = user.VchSignFileName,
                ProfilePath = user.VchProfileFileAddress,
                SignaturePath = user.VchSignFileAddress,
            };
            // get all companies
            var allCompanies = await _context.IndusCompanies.ToListAsync();
            // build company selection list
            //model.UserCompanies = allCompanies.Select(c => new CompanySelectionViewModel
            //{
            //    CompanyId = c.IntPk,
            //    CompanyName = c.Descript,
            //    IsSelected = user.TblUserCompany.Any(uc => uc.FkIntCompanyId == c.IntPk),
            //    VchDoctorCode = user.TblUserCompany
            //                      .FirstOrDefault(uc => uc.FkIntCompanyId == c.IntPk)?.VchDoctorCode
            //}).ToList();

            //load role dropdown
            LoadDropdowns();
            model.UserCompanies = allCompanies.Select(c => {

                // Find the saved record for this user and company, if it exists
                var existingMapping = user.TblUserCompany
                                          .FirstOrDefault(uc => uc.FkIntCompanyId == c.IntPk);

                return new CompanySelectionViewModel
                {
                    //--- Existing Code ---
                    CompanyId = c.IntPk,
                    CompanyName = c.Descript,
                    IsSelected = existingMapping != null, // This makes the checkbox ticked
                    VchDoctorCode = existingMapping?.VchDoctorCode,

                   
                    // Load the saved schedule data. If no record exists, default to false/null
                    IsSunday = existingMapping?.BitIsSunday ?? false,
                    IsMonday = existingMapping?.BitIsMonday ?? false,
                    IsTuesday = existingMapping?.BitIsTuesday ?? false,
                    IsWednesday = existingMapping?.BitIsWednesday ?? false,
                    IsThursday = existingMapping?.BitIsThursday ?? false,
                    IsFriday = existingMapping?.BitIsFriday ?? false,
                    IsSaturday = existingMapping?.BitIsSaturday ?? false,
                    StartTime = existingMapping?.TimeStartTime,
                    EndTime = existingMapping?.TimeEndTime
                };
            }).ToList();
            return PartialView("_PartialCreateUser", model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> EditUser(CreateUserViewModel model)
        {
            //if (!ModelState.IsValid)
            //{
            //    LoadDropdowns();
            //    return PartialView("_PartialCreateUser", model);
            //}
            // --- VALIDATION BLOCK ---
            if (HasScheduleConflict(model, out string conflictDay))
            {
                // A conflict was found. Add a model error and return.
                ModelState.AddModelError("", $"Schedule Conflict: This user is assigned to multiple companies on {conflictDay}.");
                LoadDropdowns();
                return PartialView("_PartialCreateUser", model);
            }
            var user = await _context.TblUsers
            .Include(u => u.TblUserCompany)
            .FirstOrDefaultAsync(u => u.IntUserId == model.IntUserId);
            if (user == null)
                return NotFound();
            //Update main fields
            user.VchFullName = model.VchFullName;
            user.VchEmail = model.VchEmail;
            user.VchMobile = model.VchMobile;
            user.FkRoleId = model.FkRoleId;
            // password update (only if provided)
            if (!string.IsNullOrEmpty(model.HpasswordHash))
            {
                user.HpasswordHash = PasswordHelper.ConvertHashPassword(model.HpasswordHash);
            }
            //Profile file
            var FinalProfileFile = string.Empty;
            var FinalSignatureFile = string.Empty;
            var ProfileFolder = Path.Combine(_WebHostEnvironment.WebRootPath, "uploads", "Profile");
            var SignatureFolder = Path.Combine(_WebHostEnvironment.WebRootPath, "uploads", "Signature");
            if (model.ProfileFile != null && model.ProfileFile.Length > 0)
            {
                //remove existing
                if (System.IO.File.Exists(model.ProfilePath))
                {
                    System.IO.File.Delete(model.ProfilePath);
                }
                var Extension = Path.GetExtension(model.ProfileFile.FileName);
                FinalProfileFile = $"{Path.GetFileNameWithoutExtension(model.ProfileFile.FileName)}-{DateTime.Now:yyyyMMddHHmmss}{Extension}";
                var filepath = Path.Combine(ProfileFolder, FinalProfileFile);
                using (var stream = model.ProfileFile.OpenReadStream())
                {
                    using (var image = Image.Load(stream))
                    {
                        // Resize the image if needed
                        var maxWidth = 200; // Set your desired max width
                        var maxHeight = 200; // Set your desired max height
                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(maxWidth, maxHeight),
                            Mode = ResizeMode.Crop //crop/fir to aspect ratio
                        })
                        );
                        var encoder = new JpegEncoder
                        {
                            Quality = 95 //Set quality for JPEG
                        };
                        // Save the resized image
                        await image.SaveAsync(filepath, encoder);
                        user.VchProfileFileAddress = ProfileFolder + "\\" + FinalProfileFile;
                        user.VchProfileFileName = FinalProfileFile.ToString();
                    }
                }
            }
            if (model.SignatureFile != null && model.SignatureFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(user.VchSignFileAddress))
                {
                    //remove existing
                    if (System.IO.File.Exists(model.SignaturePath))
                    {
                        System.IO.File.Delete(model.SignaturePath);
                    }
                    var Extension = Path.GetExtension(model.SignatureFile.FileName);
                    FinalSignatureFile = $"{Path.GetFileNameWithoutExtension(model.SignatureFile.FileName)}-{DateTime.Now:yyyyMMddHHmmss}{Extension}";
                    var filePath = Path.Combine(SignatureFolder, FinalSignatureFile);
                    //crop and grayscale the image as desired
                    using (var stream = model.SignatureFile.OpenReadStream())
                    {
                        using (var image = Image.Load(stream))
                        {
                            // Resize the image if needed
                            var maxWidth = 200; // Set your desired max width
                            var maxHeight = 100; // Set your desired max height
                            image.Mutate(x => x.Resize(new ResizeOptions
                            {
                                Size = new Size(maxWidth, maxHeight),
                                Mode = ResizeMode.Crop // crop/fir to aspect ratio
                            }).Grayscale()
                             );
                            var encoder = new JpegEncoder
                            {
                                Quality = 95 // Set quality for JPEG
                            };
                            // Save the resized image
                            await image.SaveAsync(filePath, encoder);
                            user.VchSignFileAddress = SignatureFolder + "\\" + FinalSignatureFile;
                            user.VchSignFileName = FinalSignatureFile.ToString();                            
                        }
                    }
                }
            }
            //remove company old record.
            var companyExisting=_context.TblUserCompany.Where(m=>m.FkUseriId==model.IntUserId).ToList();
            if (companyExisting.Count() != 0)
            {
                _context.TblUserCompany.RemoveRange(companyExisting);
            }                                                                      
            foreach (var company in model.UserCompanies)
            {
                if (company.IsSelected)
                {
                    user.TblUserCompany.Add(new TblUserCompany
                    {
                        FkIntCompanyId = company.CompanyId,
                        VchDoctorCode = company.VchDoctorCode,
                        BitIsSunday = company.IsSunday,
                        BitIsMonday = company.IsMonday,
                        BitIsTuesday = company.IsTuesday,
                        BitIsWednesday = company.IsWednesday,
                        BitIsThursday = company.IsThursday,
                        BitIsFriday = company.IsFriday,
                        BitIsSaturday = company.IsSaturday,
                        TimeStartTime = company.StartTime,
                        TimeEndTime = company.EndTime
                    });
                }
            }
            await _context.SaveChangesAsync();
            TempData["ActiveTab"] = "UserTab";
            TempData["userSuccess"] = "User updated successfully!";
            return Json(new { success = true });           
        }

        private bool HasScheduleConflict(CreateUserViewModel model, out string conflictDetails)
        {
            // This new structure will hold a list of time ranges for each day
            var dailySchedules = new Dictionary<DayOfWeek, List<TimeRange>>();
            conflictDetails = string.Empty;

            var selectedCompanies = model.UserCompanies.Where(c => c.IsSelected).ToList();

            foreach (var company in selectedCompanies)
            {
                bool anyDaySelected = company.IsSunday || company.IsMonday || company.IsTuesday ||
                                      company.IsWednesday || company.IsThursday || company.IsFriday ||
                                      company.IsSaturday;

                // 1. Check for data entry errors
                if (anyDaySelected && (!company.StartTime.HasValue || !company.EndTime.HasValue))
                {
                    conflictDetails = $"Data Error: Please provide both Start and End times for {company.CompanyName} on the days selected.";
                    return true; // Block saving
                }

                // 2. If no days are selected, or no times are set, skip this company
                if (!anyDaySelected || !company.StartTime.HasValue)
                {
                    continue;
                }

                // 3. Create the new time range
                var newRange = new TimeRange(company.StartTime.Value, company.EndTime.Value);

                if (newRange.Start >= newRange.End)
                {
                    conflictDetails = $"Data Error: Start Time must be before End Time for {company.CompanyName}.";
                    return true; // Block saving
                }

                // 4. Add this range to the schedule, checking for conflicts
                if (company.IsSunday)
                {
                    if (AddSchedule(dailySchedules, DayOfWeek.Sunday, newRange, out conflictDetails)) return true;
                }
                if (company.IsMonday)
                {
                    if (AddSchedule(dailySchedules, DayOfWeek.Monday, newRange, out conflictDetails)) return true;
                }
                if (company.IsTuesday)
                {
                    if (AddSchedule(dailySchedules, DayOfWeek.Tuesday, newRange, out conflictDetails)) return true;
                }
                if (company.IsWednesday)
                {
                    if (AddSchedule(dailySchedules, DayOfWeek.Wednesday, newRange, out conflictDetails)) return true;
                }
                if (company.IsThursday)
                {
                    if (AddSchedule(dailySchedules, DayOfWeek.Thursday, newRange, out conflictDetails)) return true;
                }
                if (company.IsFriday)
                {
                    if (AddSchedule(dailySchedules, DayOfWeek.Friday, newRange, out conflictDetails)) return true;
                }
                if (company.IsSaturday)
                {
                    if (AddSchedule(dailySchedules, DayOfWeek.Saturday, newRange, out conflictDetails)) return true;
                }
            }

            return false; // No conflicts found
        }

        /// <summary>
        /// Helper method to add a new time range to the schedule and check for overlaps.
        /// </summary>
        /// <returns>True if a conflict is found.</returns>
        private bool AddSchedule(Dictionary<DayOfWeek, List<TimeRange>> schedules, DayOfWeek day, TimeRange newRange, out string conflictDetails)
        {
            conflictDetails = string.Empty;

            // First time we've seen this day. Add the list and the range.
            if (!schedules.ContainsKey(day))
            {
                schedules[day] = new List<TimeRange> { newRange };
                return false; // No conflict
            }

            // Day already exists. Check all existing time ranges for an overlap.
            foreach (var existingRange in schedules[day])
            {
                if (newRange.Overlaps(existingRange))
                {
                    // CONFLICT!
                    conflictDetails = $"Schedule Conflict on {day}: The time {newRange} overlaps with the existing schedule {existingRange}.";
                    return true; // Conflict found
                }
            }

            // No overlaps found. Add the new range to the list for this day.
            schedules[day].Add(newRange);
            return false; // No conflict
        }

        // for time validations
        /// <summary>
        /// A simple struct to represent a time range and check for overlaps.
        /// </summary>
        private readonly struct TimeRange
        {
            public TimeOnly Start { get; }
            public TimeOnly End { get; }

            public TimeRange(TimeOnly start, TimeOnly end)
            {
                Start = start;
                End = end;
            }
            /// <summary>
            /// Checks if this time range overlaps with another.
            /// </summary>
            public bool Overlaps(TimeRange other)
            {
                // Two ranges overlap if [StartA < EndB] and [StartB < EndA]
                return this.Start < other.End && other.Start < this.End;
            }

            public override string ToString() => $"{Start:HH:mm} - {End:HH:mm}";
        }
        #endregion

       
    }
}