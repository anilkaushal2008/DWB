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
            //if (!ModelState.IsValid)
            //{
            //    TempData["Error"] = "Model error generated, contact to administrator!";
            //    LoadDropdowns();
            //    return PartialView("_PartialCreateUser", model);
            //}
            //check duplicate user name
            if (_context.TblUsers.Any(d => d.VchUsername == model.VchUsername))
            {   //add model error
                //TempData["ActiveTab"] = "UserTab";
                ModelState.AddModelError("VchUsername", "User already existing ");
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
                        VchIpUsed = HttpContext.Connection.RemoteIpAddress?.ToString()
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
                SignaturePath = user.VchSignFileAddress
            };
            // get all companies
            var allCompanies = await _context.IndusCompanies.ToListAsync();

            // build company selection list
            model.UserCompanies = allCompanies.Select(c => new CompanySelectionViewModel
            {
                CompanyId = c.IntPk,
                CompanyName = c.Descript,
                IsSelected = user.TblUserCompany.Any(uc => uc.FkIntCompanyId == c.IntPk),
                VchDoctorCode = user.TblUserCompany
                                  .FirstOrDefault(uc => uc.FkIntCompanyId == c.IntPk)?.VchDoctorCode
            }).ToList();
            LoadDropdowns();
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
                        VchDoctorCode = company.VchDoctorCode
                    });
                }
            }
            await _context.SaveChangesAsync();
            TempData["ActiveTab"] = "UserTab";
            TempData["userSuccess"] = "User updated successfully!";
            return Json(new { success = true });           
        }
    #endregion

    }
}