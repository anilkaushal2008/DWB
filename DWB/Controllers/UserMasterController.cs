using DWB.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
            return View(model);
        }
        [HttpGet]
        public IActionResult NewUser()
        {
            return PartialView("_PartialCreateUser",new TblUsers());
        }

        //POST: UserMasterController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult NewUser(TblUsers model)
        {
            if (_context.TblUsers.Any(d => d.VchUsername == model.VchUsername))
            {
                ModelState.AddModelError("VchUsername", model.VchUsername+" user name is already used.");
            }
            if (!ModelState.IsValid)
            {
                TempData["ActiveTab"] = "UserTab";
                return PartialView("_PartialCreateUser", model);
            }
            try
            {
                model.DtCreated = DateTime.Now;
                model.VchCreatedBy = "testing";//User.Identity.Name;
                model.VchIpUsed = "iptest12";// HttpContext.Connection.RemoteIpAddress.ToString();
                _context.TblUsers.Add(model);
                _context.SaveChanges();
                TempData["ActiveTab"] = "UserTab";
                return RedirectToAction(nameof(UserMasters));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while creating the user: " + ex.Message);
                return PartialView("_PartialCreateUser", model);
            }
        }

        // GET: UserMasterController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: UserMasterController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
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

        // GET: UserMasterController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: UserMasterController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
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
