using DWB.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Data;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace DWB.Controllers
{
    public class Master : Controller
    {

        private readonly DWBEntity _context;
        public Master(DWBEntity dWBEntity)
        {
            _context = dWBEntity;
        }

        //GET: All Masters
        public ActionResult Masters()
        {
            //by deafult tab opened           

            if (!TempData.ContainsKey("ActiveTab"))
            {
                TempData["ActiveTab"] = "DietTab"; // Default first tab
            }
            ViewBag.RoleList = _context.TblRoleMas
       .Where(r => r.BitIsActive)
       .Select(r => new SelectListItem
       {
           Value = r.IntId.ToString(),
           Text = r.VchRole
       }).ToList();
            _DietMasters();          
            return View();
        }

        #region Diet master
        public IActionResult _DietMasters()
        {
            //tab model       
            var Dietlist = _context.TblDietMaster.ToList();
            if (Dietlist.Count() != 0)
            {
                var viewModel = new MasterTabView
                {
                    Diets = _context.TblDietMaster.ToList(),
                    Floors = _context.TblFloorMaster.OrderBy(m => m.VchFloor).ToList(),
                    Rooms = _context.TblRoomMaster.OrderBy(d => d.FkIntFloorId).ToList(),
                    //AllPermissions = _context.TblPermissionMas.ToList(),
                    RoleMas = _context.TblRoleMas.OrderBy(d => d.VchRole).ToList()
                };
                   
                return View(viewModel);
            }
            else
            {
                TempData["DietError"] = "0 Record found in database!";
                return View();
            }
        }
        [HttpGet]
        public IActionResult DietCreate()
        {
            return PartialView("_DietCreatePartial", new TblDietMaster());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DietCreate(TblDietMaster model)
        {
            if (_context.TblDietMaster.Any(d => d.VchDietCode == model.VchDietCode))
            {
                ModelState.AddModelError("VchDietCode", "This diet code is already used.");
            }
            if (!ModelState.IsValid)
            {
                TempData["ActiveTab"] = "DietTab";
                return PartialView("_DietCreatePartial", model);
            }
            model.IntUnitCode = 15;
            _context.TblDietMaster.Add(model);
            _context.SaveChanges();
            TempData["ActiveTab"] = "DietTab";
            TempData["DietSuccess"] = "Diet created successfully.";
            return Json(new { success = true });
        }

        public IActionResult DietEdit(int id, int code)
        {
            var diet = _context.TblDietMaster.Find(id, code);
            return PartialView("_DietCreatePartial", diet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DietEdit(TblDietMaster model)
        {
            if (_context.TblDietMaster.Any(d => d.VchDietCode == model.VchDietCode && d.IntId != model.IntId))
            {
                ModelState.AddModelError("VchDietCode", "This diet code is already used.");
            }
            if (!ModelState.IsValid)
            {
                return PartialView("_DietCreatePartial", model);
            }
            var dbModel = _context.TblDietMaster.FirstOrDefault(x => x.IntId == model.IntId);
            if (dbModel == null)
                return NotFound();
            dbModel.VchDietCode = model.VchDietCode;
            dbModel.VchDietName = model.VchDietName;
            dbModel.VchDescript = model.VchDescript;
            _context.Update(dbModel);
            _context.SaveChanges();
            TempData["ActiveTab"] = "DietTab";
            TempData["DietSuccess"] = "Diet updated successfully.";
            //return RedirectToAction("Masters");
            return Json(new { success = true });
        }
        public IActionResult DietDelete(int id, int code)
        {
            var diet = _context.TblDietMaster.Find(id, code);
            if (diet != null)
            {
                _context.TblDietMaster.Remove(diet);
                _context.SaveChanges();
                TempData["ActiveTab"] = "DietTab";
                TempData["DietSuccess"] = "Deleted successfully.";
                return RedirectToAction("Masters");
            }
            return RedirectToAction("Masters");
        }
        public IActionResult DietDeactivate(int id, int code)
        {
            var diet = _context.TblDietMaster.Find(id, code);
            if (diet != null)
            {
                diet.BitIsDeactivated = true;
                _context.SaveChanges();
                TempData["ActiveTab"] = "DietTab";
                TempData["DietSuccess"] = "Diet Deactivated successfully.";
                return RedirectToAction("Masters");
            }
            return RedirectToAction("Masters");
        }
        public IActionResult DietActivate(int id, int code)
        {
            var diet = _context.TblDietMaster.Find(id, code);
            if (diet != null)
            {
                diet.BitIsDeactivated = false;
                _context.SaveChanges();
                TempData["ActiveTab"] = "DietTab";
                TempData["DietSuccess"] = "Diet Activated successfully.";
                return RedirectToAction("Masters");
            }
            return RedirectToAction("Masters");
        }

        #endregion

        #region Floor Master
        public IActionResult _FloorMasters()
        {
            var FloorList = _context.TblFloorMaster.ToList();
            if (FloorList.Count() != 0)
            {
                return View();
            }
            else
            {
                TempData["FloorError"] = "0 Record found in database!";
                return View();
            }
        }
        [HttpGet]
        public IActionResult FloorCreate()
        {
            return PartialView("_FloorCreatePartial", new TblFloorMaster());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FloorCreate(TblFloorMaster model)
        {
            if (_context.TblFloorMaster.Any(d => d.VchFloor == model.VchFloor))
            {
                ModelState.AddModelError("VchFloor", "This floor is already in use.");
            }
            if (!ModelState.IsValid)
            {
                TempData["ActiveTab"] = "FloorTab";
                return PartialView("_FloorCreatePartial", model);
            }

            //have eneter from session
            model.IntUnitCode = 15;
            _context.TblFloorMaster.Add(model);
            _context.SaveChanges();
            TempData["FloorSuccess"] = "Floor created successfully.";
            //Control tansfer to current tab
            TempData["ActiveTab"] = "FloorTab";
            return Json(new { success = true });
        }
        public IActionResult FloorEdit(int id, int code)
        {
            var Floor = _context.TblFloorMaster.Find(id, code);
            return PartialView("_FloorCreatePartial", Floor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FloorEdit(TblFloorMaster model)
        {
            if (_context.TblFloorMaster.Any(d => d.VchFloor == model.VchFloor && d.IntId != model.IntId))
            {
                ModelState.AddModelError("VchFloor", "This floor name is already in use.");
            }
            if (!ModelState.IsValid)
            {
                TempData["ActiveTab"] = "FloorTab";
                return PartialView("_FloorCreatePartial", model);
            }
            var dbModel = _context.TblFloorMaster.FirstOrDefault(x => x.IntId == model.IntId);
            if (dbModel == null)
                return NotFound();
            dbModel.VchFloor = model.VchFloor;
            _context.Update(dbModel);
            _context.SaveChanges();
            TempData["ActiveTab"] = "FloorTab";
            TempData["DietSuccess"] = "Floor updated successfully.";
            //return RedirectToAction("Masters");
            return Json(new { success = true });
        }
        public IActionResult FloorDelete(int id, int code)
        {
            var getFloor = _context.TblFloorMaster.Find(id, code);
            if (getFloor != null)
            {
                _context.TblFloorMaster.Remove(getFloor);
                _context.SaveChanges();
                TempData["ActiveTab"] = "FloorTab";
                TempData["FloorSuccess"] = "Floor deleted successfully.";
                return RedirectToAction("Masters");
            }
            return RedirectToAction("Masters");
        }
        public IActionResult FloorDeactivate(int id, int code)
        {
            var getFloor = _context.TblFloorMaster.Find(id, code);
            if (getFloor != null)
            {
                getFloor.BitIsDeactivated = true;
                _context.SaveChanges();
                TempData["ActiveTab"] = "FloorTab";
                TempData["FloorSuccess"] = "Floor Deactivated successfully.";
                return RedirectToAction("Masters");
            }
            return RedirectToAction("Masters");
        }
        public IActionResult FloorActivate(int id, int code)
        {
            var getFloor = _context.TblFloorMaster.Find(id, code);
            if (getFloor != null)
            {
                getFloor.BitIsDeactivated = false;
                _context.SaveChanges();
                TempData["ActiveTab"] = "FloorTab";
                TempData["FloorSuccess"] = "Floor Activated successfully.";
                return RedirectToAction("Masters");
            }
            return RedirectToAction("Masters");
        }
        #endregion

        #region Room master
        public IActionResult _RoomMasters()
        {
            var RoomList = _context.TblRoomMaster.OrderBy(m => m.FkIntFloorId).ThenBy(m => m.IntRoomNo).ToList();
            if (RoomList.Count() != 0)
            {
                return View(RoomList);
            }
            else
            {
                TempData["RoomError"] = "0 Record found in database!";
                return View();
            }
        }
        [HttpGet]
        public IActionResult RoomCreate()
        {
            ViewBag.FloorList = new SelectList(_context.TblFloorMaster.ToList(), "IntId", "VchFloor");
            return PartialView("_RoomCreatePartial", new TblRoomMaster());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RoomCreate(TblRoomMaster model)
        {
            if (_context.TblRoomMaster.Any(d => d.IntRoomNo == model.IntRoomNo && d.FkIntFloorId == model.FkIntFloorId))
            {
                ViewBag.FloorList = new SelectList(_context.TblFloorMaster.ToList(), "IntId", "VchFloor");
                ModelState.AddModelError("IntRoomNo", "This room is already already in use");
            }
            if (!ModelState.IsValid)
            {
                return PartialView("_RoomCreatePartial", model);
            }

            //have eneter from session
            model.IntUnitCode = 15;
            _context.TblRoomMaster.Add(model);
            _context.SaveChanges();
            TempData["RoomSuccess"] = "Room created successfully.";
            //Control tansfer to current tab
            TempData["ActiveTab"] = "RoomTab";
            return Json(new { success = true });
        }
        public IActionResult RoomEdit(int id, int code)
        {

            var Room = _context.TblRoomMaster.Find(id, code);
            ViewBag.FloorList = new SelectList(_context.TblFloorMaster.ToList(), "IntId", "VchFloor");
            return PartialView("_RoomCreatePartial", Room);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RoomEdit(TblRoomMaster model)
        {
            if (_context.TblRoomMaster.Any(d => d.IntRoomNo == model.IntRoomNo && d.FkIntFloorId != model.FkIntFloorId))
            {
                ViewBag.FloorList = new SelectList(_context.TblFloorMaster.ToList(), "IntId", "VchFloor");
                ModelState.AddModelError("IntRoomNo", "This room number already in use.");
            }
            if (!ModelState.IsValid)
            {
                TempData["ActiveTab"] = "roomTab";
                return PartialView("_RoomCreatePartial", model);
            }
            var dbModel = _context.TblRoomMaster.FirstOrDefault(x => x.IntId == model.IntId);
            if (dbModel == null)
                return NotFound();
            dbModel.IntRoomNo = model.IntRoomNo;
            _context.Update(dbModel);
            _context.SaveChanges();
            TempData["ActiveTab"] = "RoomTab";
            TempData["RoomSuccess"] = "Floor updated successfully.";
            //return RedirectToAction("Masters");
            return Json(new { success = true });
        }
        public IActionResult RoomDelete(int id, int code)
        {
            var getRoom = _context.TblRoomMaster.Find(id, code);
            if (getRoom != null)
            {
                _context.TblRoomMaster.Remove(getRoom);
                _context.SaveChanges();
                TempData["ActiveTab"] = "RoomTab";
                TempData["RoomSuccess"] = "Room deleted successfully.";
                return RedirectToAction("Masters");
            }
            TempData["ActiveTab"] = "RoomTab";
            return RedirectToAction("Masters");
        }
        public IActionResult RoomDeactivate(int id, int code)
        {
            var getRoomD = _context.TblRoomMaster.Find(id, code);
            if (getRoomD != null)
            {
                getRoomD.BitIsDeactivated = true;
                _context.SaveChanges();
                TempData["ActiveTab"] = "RoomTab";
                TempData["RoomSuccess"] = "Room Deactivated successfully.";
                return RedirectToAction("Masters");
            }
            TempData["ActiveTab"] = "RoomTab";
            return RedirectToAction("Masters");
        }
        public IActionResult RoomActivate(int id, int code)
        {
            var getRoomA = _context.TblRoomMaster.Find(id, code);
            if (getRoomA != null)
            {
                getRoomA.BitIsDeactivated = false;
                _context.SaveChanges();
                TempData["ActiveTab"] = "RoomTab";
                TempData["RoomSuccess"] = "Room Activated successfully.";
                return RedirectToAction("Masters");
            }
            TempData["ActiveTab"] = "RoomTab";
            return RedirectToAction("Masters");
        }
        #endregion

        #region Role Master
        public IActionResult _RoleMaster()
        {
            var FloorList = _context.TblRoleMas.ToList();
            if (FloorList.Count() != 0)
            {
                return View();
            }
            else
            {
                TempData["FloorError"] = "0 Record found in database!";
                return View();
            }
        }
        [HttpGet]
        public IActionResult RoleCreate()
        {
            return PartialView("_RoleCreatePartial", new TblRoleMas());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RoleCreate(TblRoleMas model)
        {
            if (_context.TblRoleMas.Any(d => d.VchRole == model.VchRole))
            {
                ModelState.AddModelError("VchRole", model.VchRole + " role is already in use.");
            }
            if (!ModelState.IsValid)
            {
                TempData["ActiveTab"] = "RoleTab";
                return PartialView("_RoleCreatePartial", model);
            }

            //have eneter from session
            //model.IntUnitCode = 15;
            model.BitIsActive = true;
            _context.TblRoleMas.Add(model);
            _context.SaveChanges();
            TempData["RoleSuccess"] = model.VchRole + " Role created successfully.";
            //Control tansfer to current tab
            TempData["ActiveTab"] = "RoleTab";
            return Json(new { success = true });
        }

        public IActionResult RoleEdit(int id, int code)
        {
            var Roles = _context.TblRoleMas.Find(id);
            return PartialView("_RoleCreatePartial", Roles);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RoleEdit(TblRoleMas model)
        {
            if (_context.TblRoleMas.Any(d => d.VchRole == model.VchRole && d.IntId == model.IntId))
            {
                ModelState.AddModelError("VchRole", model.VchRole + " role name is already in use.");
            }
            if (!ModelState.IsValid)
            {
                TempData["ActiveTab"] = "RoleTab";
                return PartialView("_RoleCreatePartial", model);
            }
            var dbModel = _context.TblRoleMas.FirstOrDefault(x => x.IntId == model.IntId);
            if (dbModel == null)
                return NotFound();
            dbModel.VchRole = model.VchRole;
            _context.Update(dbModel);
            _context.SaveChanges();
            TempData["ActiveTab"] = "RoleTab";
            TempData["RoleSuccess"] = "Role updated successfully.";
            return Json(new { success = true });
        }
        public IActionResult RoleDelete(int id)
        {
            var getRole = _context.TblRoleMas.Find(id);
            if (getRole != null)
            {
                _context.TblRoleMas.Remove(getRole);
                _context.SaveChanges();
                TempData["ActiveTab"] = "RoleTab";
                TempData["RoleSuccess"] = "Role deleted successfully.";
                return RedirectToAction("Masters");
            }
            return RedirectToAction("Masters");
        }
        public IActionResult RoleDeactivate(int id)
        {
            var getFloor = _context.TblRoleMas.Find(id);
            if (getFloor != null)
            {
                getFloor.BitIsActive = false;
                _context.SaveChanges();
                TempData["ActiveTab"] = "RoleTab";
                TempData["RoleSuccess"] = "Role Deactivated successfully.";
                return RedirectToAction("Masters");
            }
            return RedirectToAction("Masters");
        }
        public IActionResult RoleActivate(int id)
        {
            var getRole = _context.TblRoleMas.Find(id);
            if (getRole != null)
            {
                getRole.BitIsActive = true;
                _context.SaveChanges();
                TempData["ActiveTab"] = "RoleTab";
                TempData["FloorSuccess"] = "Role Activated successfully.";
                return RedirectToAction("Masters");
            }
            return RedirectToAction("Masters");
        }
        #endregion

        #region Permission Master Tab (Mapping with Role)
        // Load Partial View
        public IActionResult LoadRolePermission(int? roleId)
        {
            var model = new RolePermissionViewModel
            {
               RoleId=roleId??0
            };

            ViewBag.RoleList = _context.TblRoleMas.Where(r => r.BitIsActive)
                .Select(r => new SelectListItem
                {
                    Value = r.IntId.ToString(),
                    Text = r.VchRole
                }).ToList();

            if (roleId.HasValue)
            {
                var existingPermissions = _context.TblPermissionMas
                    .Where(p => p.FkRoleId == roleId)
                    .ToList();

                // Load default permissions to assign
                var defaultModules = new List<(string Module, string SubModule)>
                 {
                ("Master", "Diet"),
                ("Master", "Floor"),
                ("Master", "Room"),
                ("Master", "Role"),
                ("Master", "Permissions"),
                ("User", "User Master"),
                ("User", "User Test"),
                //Add more as per your modules
            };
                var permissions = new List<PermissionItem>();
                foreach (var (module, subModule) in defaultModules)
                {
                    var match = existingPermissions
                        .FirstOrDefault(p => p.VchModule == module && p.VchSubModule == subModule);
                    
                    if (match != null)
                    {
                        // Existing permission: pre-fill data
                        permissions.Add(new PermissionItem
                        {
                            PermissionId = match.IntPermissionId,
                            Module = match.VchModule,
                            SubModule = match.VchSubModule,
                            BitView = match.BitView,
                            BitAdd = match.BitAdd,
                            BitEdit = match.BitEdit,
                            BitDelete = match.BitDelete,
                            BitStatus = match.BitStatus
                        });
                    }
                    else
                    {
                        // New unassigned permission: blank checkboxes
                        permissions.Add(new PermissionItem
                        {
                            
                            PermissionId = 0,
                            Module = module,
                            SubModule = subModule,
                            BitView = false,
                            BitAdd = false,
                            BitEdit = false,
                            BitDelete = false,
                            BitStatus = false
                        });
                    }
                }                
                model.Permissions = permissions;
            }
            
            //TempData["ActiveTab"] = "PermTab";
            return PartialView("_RolePermissionPartial", model);
        }

        [HttpPost]
        public IActionResult SaveRolePermissions(RolePermissionViewModel model)
        {
            // Step 1: Remove old permissions for the selected role
            var oldPermissions = _context.TblPermissionMas
                .Where(p => p.FkRoleId == model.RoleId)
                .ToList();

            if (oldPermissions.Any())
            {
                _context.TblPermissionMas.RemoveRange(oldPermissions);
            }

            // Step 2: Add new permissions from the submitted model
            foreach (var item in model.Permissions)
            {
                var newPermission = new TblPermissionMas
                {
                    FkRoleId = model.RoleId,
                    VchModule = item.Module,
                    VchSubModule = item.SubModule,
                    BitView = item.BitView,
                    BitAdd = item.BitAdd,
                    BitEdit = item.BitEdit,
                    BitDelete = item.BitDelete,
                    BitStatus = item.BitStatus,
                    DtCreated = DateTime.Now,
                    VchCreatedBy = User.Identity?.Name ?? "System", // Optional
                    VchIpUsed = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    BitIsDeactivated = false
                };

                _context.TblPermissionMas.Add(newPermission);
            }
            _context.SaveChanges();
            TempData["PermSuccess"] = "Permissions saved successfully!";
            TempData["ActiveTab"] = "PermTab";
            return RedirectToAction("Masters"); // Or return a success response for AJAX
        }


        #endregion
    }
}
