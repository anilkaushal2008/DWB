using DWB.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DWB.Controllers
{
    public class UserMasterController : Controller
    {
        private readonly DWBEntity _context;
        public UserMasterController(DWBEntity dWBEntity)
        {
            _context = dWBEntity;
        }
        // GET: UserMasterController tab
        public ActionResult UserMasters()
        {
            //by deafult tab opened
            if (!TempData.ContainsKey("UserTab"))
            {
                TempData["ActiveTab"] = "UserTab"; // Default first tab
            }
            _AllUsers();
            return View();
        }

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
            else {    
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
            if (!ModelState.IsValid)
            {
                // Reload dropdowns on validation error
                ViewBag.RoleList = new SelectList(_context.TblRoleMas.OrderBy(m => m.VchRole), "IntId", "VchRole");
                ViewBag.BranchList = new SelectList(_context.IndusCompanies.OrderBy(m => m.Descript), "IntPk", "Descript");
                return PartialView("_PartialCreateUser", model);
            }
            // Create user entity
            var user = new TblUsers
            {
                VchUsername = model.VchUsername,
                HpasswordHash = model.HpasswordHash, //You may hash this
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
            return Json(new { success = true});
        }
        

        // GET: UserMasterController/Edit/5
        public ActionResult EditUser(int id)
        {
            return View();
        }

        // POST: UserMasterController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUser(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }     

        
    }
}
