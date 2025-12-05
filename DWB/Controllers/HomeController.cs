using DWB.GroupModels;
using DWB.Models;
using FluentAssertions.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Net;
using System.Reflection.Emit;
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
                string doctorCode = (from uc in _context.TblUserCompany
                                     where uc.FkUseriId == user.IntUserId && uc.FkIntCompanyId == model.fk_intPK
                                     select uc.VchDoctorCode).FirstOrDefault() ?? string.Empty;

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
                    //Check profile, if avilable add to claim
                    new Claim("ProfilePath", user.VchProfileFileName ?? string.Empty),
                    //add doctor code as selected company code
                    new Claim("DoctorCode", doctorCode),
                    //add user signature
                    new Claim("SignatureName", user.VchSignFileName ?? string.Empty),
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
                //get client detail 
                // Get IP
                string? ip = null;

                // Check X-Forwarded-For header
                if (HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                {
                    ip = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(ip))
                        ip = ip.Split(',').FirstOrDefault()?.Trim();
                }

                // Fallback to RemoteIpAddress
                if (string.IsNullOrEmpty(ip) && HttpContext.Connection.RemoteIpAddress != null)
                {
                    var remoteIp = HttpContext.Connection.RemoteIpAddress;
                    // Check if the address is IPv6 loopback (::1)
                    if (remoteIp.Equals(IPAddress.IPv6Loopback))
                        remoteIp = IPAddress.Loopback;
                    ip = remoteIp.ToString();
                }

                string clientIp = ip ?? "Unknown";

                // Resolve hostname
                string clientHost = clientIp;
                try
                {
                    if (clientIp != "Unknown")
                    {
                        var hostEntry = System.Net.Dns.GetHostEntry(clientIp);
                        clientHost = hostEntry.HostName;
                    }
                }
                catch
                {
                    // fallback to IP if hostname lookup fails
                    clientHost = clientIp;
                }

                // Store in Session
                HttpContext.Session.SetString("ClientHost", clientHost);
                HttpContext.Session.SetString("ClientIp", clientIp);

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
        public async Task<IActionResult> Dashboard()
        {
            // 1. SETUP COMMON VARIABLES (User ID, Base URL, Dates)
            // These are fast and needed for almost everyone, so we keep them here.
            string BaseAPI = (User.FindFirst("BaseAPI")?.Value ?? string.Empty).Replace("\n", "").Replace("\r", "").Trim();
            int code = Convert.ToInt32(User.FindFirst("HMScode")?.Value);
            string DocCode = User.FindFirst("DoctorCode")?.Value ?? "null";

            DateTime Sdate = DateTime.Today;
            string finalSdate = Sdate.ToString("dd-MM-yyyy");
            string finalEdate = Sdate.ToString("dd-MM-yyyy");
            var todayStr = DateOnly.FromDateTime(DateTime.Now).ToString("dd/MM/yyyy");

            // 2. DEFINE BOOLEANS FOR ROLES (For cleaner if-statements)
            bool isAdmin = User.IsInRole("Admin");
            bool isNursing = User.IsInRole("Nursing");
            bool isConsultant = User.IsInRole("Consultant");
            bool isCounselor = User.IsInRole("Counselor"); // Assuming you have this role
            bool isEmergency = User.IsInRole("Emergency"); // Assuming you have this role

            // --- LOGIC BLOCK 1: General OPD Data ---
            // (Visible to: Admin, Nursing, Consultant)
            if (isAdmin || isNursing || isConsultant)
            {
                // 1. Fetch API Data
                ViewBag.OPDCount = await GetApiCount(BaseAPI, finalSdate, finalEdate, code, DocCode, "");

                // 2. Fetch Nursing Assessment (Only needed if Admin or Nurse)
                if (isAdmin || isNursing)
                {
                    var nursAssessmentCount = _context.TblNsassessment
                        .Count(m => m.BitIsCompleted == true && m.VchHmsdtEntry == todayStr);
                    ViewBag.NSAssessment = nursAssessmentCount.ToString();
                }

                // 3. Fetch Doctor Assessment (Only needed if Admin or Consultant)
                if (isAdmin || isConsultant)
                {
                    var doctorAssessmentCount = _context.TblDoctorAssessment
                        .Count(m => m.BitAsstCompleted == true && m.DtHmsentry == todayStr);
                    ViewBag.DocAssessment = doctorAssessmentCount.ToString();
                }
            }

            // --- LOGIC BLOCK 2: Emergency Data ---
            // (Visible to: Admin, Emergency Staff)
            if (isAdmin || isEmergency)
            {
                // Fetch API Data for Emergency
                ViewBag.EmergencyCount = await GetApiCount(BaseAPI, finalSdate, finalEdate, code, DocCode, "Emergency");

                // Fetch Database Data for Emergency
                // var emergencyAssessedCount = _context.TblTriage.Count(m => m.EntryDate == todayStr && m.IsAssessed == true);
                ViewBag.EmergencyAssessed = "0"; // Placeholder
            }

            // --- LOGIC BLOCK 3: Counseling Data ---
            // (Visible to: Admin, Counselor)
            if (isAdmin || isCounselor)
            {
                var counselingCount = _context.PatientEstimateRecord.Count();
                ViewBag.Coun = counselingCount.ToString();
            }

            return View();
        }

        // Keep the helper function exactly the same
        private async Task<string> GetApiCount(string baseUrl, string sDate, string eDate, int hmsCode, string docCode, string opdType)
        {
            string countResult = "0";
            // Check if BaseAPI is valid before calling to prevent crashes
            if (string.IsNullOrEmpty(baseUrl)) return countResult;

            string url = $"{baseUrl}opdCount?sdate={sDate}&edate={eDate}&code={hmsCode}&uhidno=null&doccode={docCode}&opdType={opdType}";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Optional: Set a timeout so the dashboard doesn't hang if API is slow
                    client.Timeout = TimeSpan.FromSeconds(3);
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        countResult = await response.Content.ReadAsStringAsync();
                    }
                }
                catch (Exception)
                {
                    countResult = "0";
                }
            }
            return countResult;
        }




        //[Authorize(Roles ="Admin, Nursing, Billing, Consultant")]
        //public async Task<IActionResult> Dashboard()
        //{
        //    //Get Opd Count //api format Base+opdCount?code=1
        //    string BaseAPI = (User.FindFirst("BaseAPI")?.Value ?? string.Empty).Replace("\n", "").Replace("\r", "").Trim();
        //    //get current ihms code
        //    int code = Convert.ToInt32(User.FindFirst("HMScode")?.Value);
        //    //get doctor code if available
        //    string DocCode = User.FindFirst("DoctorCode")?.Value ?? String.Empty;
        //    var finalURL = string.Empty;
        //    var finalOpdType = string.Empty;
        //    DateTime Sdate = DateTime.Today;
        //    DateTime Edate = DateTime.Today;
        //    string finalSdate = Convert.ToDateTime(Sdate).ToString("dd-MM-yyyy");
        //    string finalEdate = Convert.ToDateTime(Edate).ToString("dd-MM-yyyy");
        //    if (DocCode != null)
        //    {
        //        finalURL = BaseAPI + "opdCount?&sdate=" + finalSdate + "&edate=" + finalEdate + "&code=" + code + "&uhidno=null" + "&doccode=" + DocCode+"&opdType="+finalOpdType;
        //    }
        //    else
        //    {
        //        finalURL = BaseAPI + "opdCount?&sdate=" + finalSdate + "&edate=" + finalEdate + "&code=" + code + "&uhidno=null" + "&doccode=null&opdType=" + finalOpdType;
        //    }               
        //    using (HttpClient client = new HttpClient())
        //    {
        //        try
        //        {
        //            var response = await client.GetAsync(finalURL);

        //            if (!response.IsSuccessStatusCode)
        //            {
        //                // 🚨 API is not responding
        //                return RedirectToAction("ApiDown", "Error");
        //            }

        //            string result = await response.Content.ReadAsStringAsync();
        //            ViewBag.OPDCount = result.ToString();
        //        }
        //        catch(Exception e)
        //        {
        //            TempData["Error"] = "The error generated as :" + e.Message;
        //            // 🚨 API unreachable (network error / timeout)
        //            return RedirectToAction("ApiDown", "Error");
        //        }
        //    }
        //    //get Today Nursing assessment completed count
        //    var aajkidate = DateOnly.FromDateTime(DateTime.Now).ToString("dd/MM/yyyy");
        //    var nursAssessmentCount = _context.TblNsassessment.Count(m => m.BitIsCompleted==true && m.VchHmsdtEntry == aajkidate);
        //    var doctorAssessmentCount = _context.TblDoctorAssessment.Count(m => m.BitAsstCompleted==true && m.DtHmsentry == aajkidate);
        //    var CounselingCount = _context.PatientEstimateRecord.Count();
        //    ViewBag.NSAssessment = nursAssessmentCount.ToString();
        //    ViewBag.DocAssessment = doctorAssessmentCount.ToString();
        //    ViewBag.CounselingCount = CounselingCount.ToString();
        //    return View();
        //}

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

            //get user id
            int UserId= Convert.ToInt32(User.FindFirst("UserId")?.Value);
            // Get logged-in username from claims
            var username = User.Identity?.Name;
            var user = await _context.TblUsers.Where(e=>e.IntUserId==UserId).FirstOrDefaultAsync(u => u.VchUsername == username);
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
