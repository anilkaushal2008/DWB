using DWB.GroupModels;
using DWB.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;


namespace DWB.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly DWBEntity _context;
        private readonly GroupEntity _groupcontext;
              
        public HomeController(ILogger<HomeController> logger, DWBEntity dWBEntity, GroupEntity groupcontext)
        {
            _logger = logger;
            _context = dWBEntity;
            _groupcontext = groupcontext;
        }
        [AllowAnonymous]
        public IActionResult Index()
        {
            //get Company from group context
            var company = _groupcontext.IndusCompanies.Where(m => new[] {2,3,4,14,15,21,22,23,24,25}.Contains(m.IntPk)).ToList();
            ViewBag.Company = new SelectList(company, "IntPk", "Descript");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                //selected compay id
                string intcode = model.fk_intPK.ToString();
                //check user name
                var user = (from u in _context.TblUsers
                            join c in _context.TblUserCompany on u.IntUserId equals c.FkUseriId
                            where u.VchUsername == model.Username && c.FkIntCompanyId == model.fk_intPK
                            select u).FirstOrDefault();
                if (user == null)
                {
                    var company1 = _groupcontext.IndusCompanies.Where(m => new[] { 2, 3, 4, 14, 15, 21, 22, 23, 24, 25 }.Contains(m.IntPk)).ToList();
                    ViewBag.Company = new SelectList(company1, "IntPk", "Descript");
                    ModelState.AddModelError("Username", "Invalid username or company selection.");
                    return View(model);
                }
                else
                {
                    //check password  
                    if (string.IsNullOrEmpty(user.HpasswordHash))
                    {
                        var company1 = _groupcontext.IndusCompanies.Where(m => new[] { 2, 3, 4, 14, 15, 21, 22, 23, 24, 25 }.Contains(m.IntPk)).ToList();
                        ViewBag.Company = new SelectList(company1, "IntPk", "Descript");
                        ModelState.AddModelError("Password", "Password is not set for this user.");
                        return View(model);
                    }
                    if (string.IsNullOrEmpty(model.Password)) // || !Utility.PasswordHelper.VerifyPassword(user.HpasswordHash, model.Password))
                    {
                        var company2 = _groupcontext.IndusCompanies.Where(m => new[] { 2, 3, 4, 14, 15, 21, 22, 23, 24, 25 }.Contains(m.IntPk)).ToList();
                        ViewBag.Company = new SelectList(company2, "IntPk", "Descript");
                        ModelState.AddModelError("Password", "Invalid password.");
                        return View(model);
                    }
                }
                //get user role
                var role = _context.TblRoleMas.FirstOrDefault(r => r.IntId == user.FkRoleId);
                var roleName = role?.VchRole ?? "User";
                //get all permissions
                var permissions = _context.TblPermissionMas
                    .Where(p => p.FkRoleId == user.FkRoleId && !p.BitIsDeactivated)
                    .Select(p => new
                    {
                        p.VchModule,
                        p.VchSubModule,
                        p.BitView,
                        p.BitAdd,
                        p.BitEdit,
                        p.BitDelete,
                        p.BitStatus
                    }).ToList();
                //Get all company which is mapped to current user
                var companies = _context.TblUserCompany.Where(x => x.FkUseriId == user.IntUserId)
                    .Select(x => x.FkIntCompany)
                    .Distinct()
                    .ToList();
                string companyIdList = string.Join(",", companies);
                //set all claims in identity
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.VchUsername),
                    new Claim("UserId", user.IntUserId.ToString()),
                    new Claim("UnitId", intcode),
                    new Claim("AllCompanyIds", companyIdList),
                    new Claim(ClaimTypes.Role, roleName)
                };
                //add permissions claims
                foreach (var permission in permissions)
                {
                    if (permission.BitView)
                        claims.Add(new Claim("Permission", $"{permission.VchModule}:{permission.VchSubModule}:View:{permission.BitView}"));
                    if (permission.BitAdd)
                        claims.Add(new Claim("Permission", $"{permission.VchModule}:{permission.VchSubModule}:Add:{permission.BitAdd}"));
                    if (permission.BitEdit)
                        claims.Add(new Claim("Permission", $"{permission.VchModule}:{permission.VchSubModule}:Edit:{permission.BitEdit}"));
                    if (permission.BitDelete)
                        claims.Add(new Claim("Permission", $"{permission.VchModule}:{permission.VchSubModule}:Delete:{permission.BitDelete}"));
                    if (permission.BitStatus)
                        claims.Add(new Claim("Permission", $"{permission.VchModule}:{permission.VchSubModule}:Delete:{permission.BitStatus}"));
                }
                //set identity and principal  
                var newIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var newPrincipal = new ClaimsPrincipal(newIdentity);
                //Sign in  
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, newPrincipal, new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddHours(1)
                });
                //If valid, redirect to the dashboard or another page
                return RedirectToAction("Dashboard");
            }
            // If validation fails, return to the login view with the model
            var company = _groupcontext.IndusCompanies.Where(m => new[] { 2, 3, 4, 14, 15, 21, 22, 23, 24, 25 }.Contains(m.IntPk)).ToList();
            ViewBag.Company = new SelectList(company, "IntPk", "Descript");
            return View(model);
        }
        [Authorize(Roles ="Admin, Nursing, Billing")]
        public IActionResult Dashboard()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {

            return PartialView("_ChangePasswordPartial", new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return PartialView("_ChangePasswordPartial", model);

            // Get logged-in username from claims
            var username = User.Identity?.Name;
            var user = await _context.TblUsers.FirstOrDefaultAsync(u => u.VchUsername == username);

            if (user == null)
                return BadRequest("User not found.");

            // Validate current password
            if (!Utility.PasswordHelper.VerifyPassword(user.HpasswordHash, model.CurrentPassword))
            {
                ModelState.AddModelError("CurrentPassword", "Incorrect current password.");
                return PartialView("_ChangePasswordPartial", model);
            }
            //Hash new password
            string newHashedPassword = Utility.PasswordHelper.ConvertHashPassword(model.NewPassword);
            // Update password in DB
            user.HpasswordHash = newHashedPassword;
            _context.TblUsers.Update(user);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Password changed successfully." });
        }
        public IActionResult ForgotPassword()
        {
            return View();
        }

       
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home"); // Or wherever your login page is
        }




    }
}
