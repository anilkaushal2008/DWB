using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DWB.Models;
using DWB.GroupModels;
using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;


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
        public IActionResult Index(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                //check user name
                var user = (from u in _context.TblUsers
                            join c in _context.TblUserCompany on u.IntUserId equals c.FkUseriId
                            where u.VchUsername == model.Username && c.FkIntCompanyId == model.fk_intPK
                            select u).FirstOrDefault();
                if (user == null)
                {
                    ModelState.AddModelError("Username", "Invalid username or company selection.");
                    return View(model);
                }
                else
                {
                    //check password
                    if (user.HpasswordHash == null || user.HpasswordHash == "")
                    {
                        var company1 = _groupcontext.IndusCompanies.Where(m => new[] { 2, 3, 4, 14, 15, 21, 22, 23, 24, 25 }.Contains(m.IntPk)).ToList();
                        ViewBag.Company = new SelectList(company1, "IntPk", "Descript");
                        ModelState.AddModelError("Password", "Password is not set for this user.");
                        return View(model);
                    }
                    if (!Utility.PasswordHelper.VerifyPassword(user.HpasswordHash, model.Password))
                    {
                        var company2 = _groupcontext.IndusCompanies.Where(m => new[] { 2, 3, 4, 14, 15, 21, 22, 23, 24, 25 }.Contains(m.IntPk)).ToList();
                        ViewBag.Company = new SelectList(company2, "IntPk", "Descript");
                        ModelState.AddModelError("Password", "Invalid password.");
                        return View(model);
                    }
                }
                    //If valid, redirect to the dashboard or another page
                    return RedirectToAction("Dashboard");
            }
            // If validation fails, return to the login view with the model
            var company = _groupcontext.IndusCompanies.Where(m => new[] { 2, 3, 4, 14, 15, 21, 22, 23, 24, 25 }.Contains(m.IntPk)).ToList();
            ViewBag.Company = new SelectList(company, "IntPk", "Descript");
            return View(model);
        }
        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult UpdatePassword()
        {
            return View();
        }

        public IActionResult ForgotPassword()
        {
            return View();
        }

       



       
    }
}
