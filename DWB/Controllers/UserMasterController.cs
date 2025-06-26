using DWB.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DWB.Utility;

namespace DWB.Controllers
{
    public class UserMasterController : Controller
    {
        private readonly DWBEntity _context;
        public UserMasterController(DWBEntity dWBEntity)
        {
            _context = dWBEntity;
        }
        public ActionResult UserMasters()
        {    
            if (!TempData.ContainsKey("ActiveTab"))
            {
                TempData["ActiveTab"] = "UserTab"; // Default first tab
            }
            //_AllUsers();
            _AllUsers();
            return View();
        }

        #region User Master Tab
        // GET: UserMasterController/AllUsers
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

        //POST: UserMasterController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewUser(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload dropdowns on validation error
                ViewBag.RoleList = new SelectList(_context.TblRoleMas.OrderBy(m => m.VchRole), "IntId", "VchRole");
                ViewBag.BranchList = new SelectList(_context.IndusCompanies.OrderBy(m => m.Descript), "IntPk", "Descript");
                ModelState.AddModelError("VchUsername", "Model error generated contact to administrator!");
                return PartialView("_PartialCreateUser", model);
            }
            //check duplicate user name
            if (_context.TblUsers.Any(d => d.VchUsername == model.VchUsername))
            {   //add model error
                ModelState.AddModelError("VchUsername", "User already existing ");
                ViewBag.RoleList = new SelectList(_context.TblRoleMas.OrderBy(m => m.VchRole), "IntId", "VchRole");
                ViewBag.BranchList = new SelectList(_context.IndusCompanies.OrderBy(m => m.Descript), "IntPk", "Descript");
                return PartialView("_PartialCreateUser", model);
            }
            //check password
            if (string.IsNullOrWhiteSpace(model.HpasswordHash))
            {
                ModelState.AddModelError("HpasswordHash", "Password is required");
            }
            if (!ModelState.IsValid)
            {
                // Reload dropdowns on validation error
                ViewBag.RoleList = new SelectList(_context.TblRoleMas.OrderBy(m => m.VchRole), "IntId", "VchRole");
                ViewBag.BranchList = new SelectList(_context.IndusCompanies.OrderBy(m => m.Descript), "IntPk", "Descript");
                return PartialView("_PartialCreateUser", model);
            }
            //Convert password to hash
             var HasedPassword = PasswordHelper.ConvertHashPassword(model.HpasswordHash);
            // Create user entity
            var user = new TblUsers
            {
                VchUsername = model.VchUsername,                         
                HpasswordHash = HasedPassword, //You may hash this
                VchFullName = model.VchFullName,
                VchEmail = model.VchEmail,
                VchMobile = model.VchMobile,
                FkRoleId = model.FkRoleId,
                //BitIsDeActived = model.BitIsDeActived,
                DtCreated = DateTime.Now,
                VchCreatedBy = User.Identity?.Name ?? "system",
                VchIpUsed = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            _context.TblUsers.Add(user);
            await _context.SaveChangesAsync();

            // Save assigned branches
            foreach (var companyId in model.SelectedCompanyIds)
            {
                var assignment = new TblUserCompany
                {
                    FkUseriId = user.IntUserId,
                    FkIntCompanyId = companyId,
                    DtCreated = DateTime.Now,
                    VchCreatedBy = User.Identity?.Name ?? "system",
                    VchIpUsed = HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                _context.TblUserCompany.Add(assignment);
                await _context.SaveChangesAsync();
            }
            TempData["ActiveTab"] = "UserTab";
            TempData["userSuccess"] = "User created and saved successfully.";
            // Return success (optional: redirect or return JSON)
            return Json(new { success = true });
        }


        // GET: UserMasterController/Edit/5
        public async Task<IActionResult> UserEdit(int id)
        {
            var user = await _context.TblUsers
                .Include(u => u.TblUserCompany)
                .FirstOrDefaultAsync(u => u.IntUserId == id);

            if (user == null)
            {
                var msg = "User not found contact to aministrator!";
                TempData["ActiveTab"] = "UserTab";
                ModelState.AddModelError("VchUsername", msg.ToString());
                return PartialView("_PartialCreateUser");
            }

            var model = new CreateUserViewModel
            {
                IntUserId = user.IntUserId,
                VchUsername = user.VchUsername,
                HpasswordHash = user.HpasswordHash,
                VchFullName = user.VchFullName,
                VchEmail = user.VchEmail,
                VchMobile = user.VchMobile,
                FkRoleId = user.FkRoleId,
                SelectedCompanyIds = user.TblUserCompany.Select(uc => uc.FkIntCompanyId).ToList()
            };
            ViewBag.RoleList = new SelectList(_context.TblRoleMas.OrderBy(m => m.VchRole), "IntId", "VchRole", model.FkRoleId);
            ViewBag.BranchList = new SelectList(_context.IndusCompanies.OrderBy(m => m.Descript), "IntPk", "Descript");
            return PartialView("_PartialCreateUser", model);
        }

        // POST: UserMasterController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UserEdit(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Repopulate dropdowns before returning the view
                ViewBag.RoleList = new SelectList(_context.TblRoleMas.OrderBy(m => m.VchRole), "IntId", "VchRole", model.FkRoleId);
                ViewBag.BranchList = new SelectList(_context.IndusCompanies.OrderBy(m => m.Descript), "IntPk", "Descript");
                return PartialView("_PartialCreateUser", model);
            }

            var existingUser = _context.TblUsers
                .Include(u => u.TblUserCompany)
                .FirstOrDefault(u => u.IntUserId == model.IntUserId);

            if (existingUser == null)
            {
                return NotFound();
            }

            // Update basic properties
            existingUser.VchUsername = model.VchUsername;
            existingUser.VchFullName = model.VchFullName;
            existingUser.VchEmail = model.VchEmail;
            existingUser.VchMobile = model.VchMobile;
            existingUser.FkRoleId = model.FkRoleId;
            existingUser.DtUpdated = DateTime.Now;
            existingUser.VchUpdatedBy = User.Identity?.Name ?? "system";
            existingUser.VchIpUpdated = HttpContext.Connection.RemoteIpAddress?.ToString();
            // Optional: update password if needed (you may check if model.HpasswordHash is not empty)
            if (!string.IsNullOrEmpty(model.HpasswordHash))
            {
                existingUser.HpasswordHash = PasswordHelper.ConvertHashPassword(model.HpasswordHash); // Hash if needed
            }
            // Update branch/company mapping
            // First, remove all existing mappings
            var userCompanies = _context.TblUserCompany.Where(x => x.FkUseriId == model.IntUserId).ToList();
            _context.TblUserCompany.RemoveRange(userCompanies);
            // Then, add selected companies again
            if (model.SelectedCompanyIds != null)
            {
                foreach (var companyId in model.SelectedCompanyIds)
                {
                    _context.TblUserCompany.Add(new TblUserCompany
                    {
                        FkUseriId = model.IntUserId,
                        FkIntCompanyId = companyId,
                        DtUpdated = DateTime.Now,
                        VchUpdatedBy = User.Identity?.Name ?? "system",
                        VchIpUpdated = HttpContext.Connection.RemoteIpAddress?.ToString()
                    });
                }
            }

            _context.SaveChanges();
            TempData["ActiveTab"] = "UserTab";
            TempData["userSuccess"] = "User updated successfully.";
            // Return success (optional: redirect or return JSON)
            return Json(new { success = true });
        }

        public IActionResult UserDeactivate(int id, int code)
        {
            var GetUser = _context.TblUsers.Find(id);
            if (GetUser != null)
            {
                GetUser.BitIsDeActived = true;
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
                GetUser.BitIsDeActived = false;
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
    }
}

