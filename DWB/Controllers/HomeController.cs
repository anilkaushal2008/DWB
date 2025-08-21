using DWB.GroupModels;
using DWB.Models;
using FluentAssertions.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.Design;
using System.Linq;
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
                HttpContext.Session.SetString("SelectedCompanyId", model.fk_intPK.ToString());
                //get selected company from group context
                var selectedCompany = _groupcontext.IndusCompanies.FirstOrDefault(m => m.IntPk == model.fk_intPK);
                if (selectedCompany == null)
                {
                    var company1 = _groupcontext.IndusCompanies.Where(m => new[] { 2, 3, 4, 14, 15, 21, 22, 23, 24, 25 }.Contains(m.IntPk)).ToList();
                    ViewBag.Company = new SelectList(company1, "IntPk", "Descript");
                    ModelState.AddModelError("fk_intPK", "Invalid company selection.");
                    return View(model);
                }
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
                var permissions = _context.TblRoleModuleMap
                    .Where(p => p.FkRoleId == user.FkRoleId && !p.BitIsDeactivated)
                    .Select(p => new
                    {
                        p.FkModule.VchMasterModule,
                        p.FkModule.VchModule,
                        p.FkModule.VchSubModule,
                        p.BitView,
                        p.BitAdd,
                        p.BitEdit,
                        p.BitDelete,
                        p.BitStatus
                    }).ToList();
                //Get all company which is mapped to current user
                var UserCompanies = (from uc in _context.TblUserCompany                 
                where uc.FkUseriId == user.IntUserId
                select uc.FkIntCompanyId).ToList();
                //get all user assigned companies
                string companyIdList = string.Join(",", UserCompanies);

                //string companyIdList = string.Join(",", UserCompanies);
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.VchUsername),
                    //user id
                    new Claim("UserId", user.IntUserId.ToString()),
                    //set Unit intPK
                    new Claim("UnitId", intcode),
                    //set unit HMS code
                    new Claim("HMScode", selectedCompany.Id.ToString()), 
                    //set loggedin company name
                    new Claim("CompName",selectedCompany.Descript),
                    //set unit base API                   
                    new Claim("BaseAPI", selectedCompany.VchDwbApi.ToString()??"null"),                   
                    //All User assigned companies
                    new Claim("AllCompanyIds", companyIdList),
                    //CHeck profile, if avilable add to claim
                    new Claim("ProfilePath", user.VchProfileFileName ?? string.Empty),
                    //set role
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
                        claims.Add(new Claim("Permission", $"{permission.VchModule}:{permission.VchSubModule}:Status:{permission.BitStatus}"));
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
                #region for dropdown
                ////get selected company
                //int selectedCompanyId = selectedCompany.IntPk;
                //HttpContext.Session.SetString("SelectedCompanyId", selectedCompanyId.ToString());
                ////if valid give option to change company according to user assigned company
                //var CompUserCompanies = _context.TblUserCompany
                // .Where(x => x.FkUseriId == user.IntUserId)
                // .ToList();

                //// get allowed company IDs (int list)
                //var allowedCompanyIds = CompUserCompanies
                //    .Select(x => x.FkIntCompanyId) // or FkIntCompany if that's the correct name
                //    .Distinct()
                //    .ToList();

                //// now get company dropdown from IndusCompanies or wherever the full company details are
                //var companyDropdown = _context.IndusCompanies
                //    .Where(c => allowedCompanyIds.Contains(c.IntPk)) // ✅ match int with int
                //    .Select(c => new SelectListItem
                //    {
                //        Value = c.IntPk.ToString(),
                //        Text = c.Descript//,
                //        //Selected=Convert.ToBoolean(selectedCompanyId)
                //    }).ToList();
                //
                //ViewBag.CompanyList =companyDropdown;
                #endregion

                //If valid, redirect to the dashboard or another page
                return RedirectToAction("Dashboard");
            }
            // If validation fails, return to the login view with the model
            var company = _groupcontext.IndusCompanies.Where(m => new[] { 2, 3, 4, 14, 15, 21, 22, 23, 24, 25 }.Contains(m.IntPk)).ToList();
            ViewBag.Company = new SelectList(company, "IntPk", "Descript");
            return View(model);
        }
       
        [Authorize(Roles ="Admin, Nursing, Billing")]
        public async Task<IActionResult> Dashboard()
        {
            //Get Opd Count //api format Base+opdCount?code=1
            string BaseAPI = (User.FindFirst("BaseAPI")?.Value ?? string.Empty).Replace("\n", "").Replace("\r", "").Trim();
            //get current ihms code
            int code = Convert.ToInt32(User.FindFirst("HMScode")?.Value);
            var finalURL = BaseAPI + "opdCount?code=" + code;
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(finalURL);
                var response = await client.GetAsync(finalURL);
                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    ViewBag.OPDCount = result;
                }
                else
                {
                    ViewBag.OPDCount = "Error: " + response.StatusCode;
                }
            }
            //get Today Nursing assessment completed count
            var aajkidate = DateOnly.FromDateTime(DateTime.Now).ToString("dd/MM/yyyy");
            var nursAssessmentCount = _context.TblNsassessment.Count(m => m.BitIsCompleted && m.VchHmsdtEntry == aajkidate);
            ViewBag.NSAssessment = nursAssessmentCount.ToString();
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

        [HttpPost]
        public async Task<IActionResult> ChangeCompany(int CompanyId)
        {
            HttpContext.Session.SetString("SelectedCompanyId", CompanyId.ToString());
            //Fetch company details  
            var selectedCompany = await _groupcontext.IndusCompanies.FindAsync(CompanyId);
            if (selectedCompany == null)
            {
                return BadRequest("Invalid company");
            }
            else
            {
                //Fetch new values  
                string intcode = selectedCompany.IntPk.ToString(); // Ensure intcode is defined here  
                string hmsCode = selectedCompany.Id.ToString();
                string compName = selectedCompany.Descript;
                string baseApi = selectedCompany.VchDwbApi;

                //Get current claims and identity  
                var identity = User.Identity as ClaimsIdentity;
                if (identity == null)
                {
                    return BadRequest("User identity is not valid.");
                }
                var claims = identity.Claims.ToList();

                //Remove old company-related claims  
                claims.RemoveAll(c =>
                    c.Type == "UnitId" ||
                    c.Type == "HMScode" ||
                    c.Type == "CompName" ||
                    c.Type == "BaseAPI"
                );

                //Add updated claims  
                claims.Add(new Claim("UnitId", intcode));
                claims.Add(new Claim("HMScode", hmsCode));
                claims.Add(new Claim("CompName", compName));
                if(baseApi != null)
                    claims.Add(new Claim("BaseAPI", baseApi));

                //Re-sign in with updated claims  
                var newIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var newPrincipal = new ClaimsPrincipal(newIdentity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, newPrincipal, new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddHours(1)
                });

                // Redirect to the referring page  
                return Redirect(Request.Headers["Referer"].ToString());
            }
        }
    }
}
