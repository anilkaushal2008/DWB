using DWB.GroupModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using DWB.Models;
using System.Linq;
using System.Security.Claims;
using IndusCompanies = DWB.GroupModels.IndusCompanies;

namespace DWB.Components
{
    public class CompanyDropdownViewComponent : ViewComponent
    {
        private readonly GroupEntity _context;
        private readonly DWBEntity DbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public CompanyDropdownViewComponent(GroupEntity context, DWBEntity dWBEntity, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            DbContext = dWBEntity;
            _httpContextAccessor = httpContextAccessor;
        }
        //public IViewComponentResult Invoke()
        //{
        //    var claimValue = HttpContext.User.FindFirst("AllCompanyIds")?.Value; //15,4,2,14,3,21,23,22,25

        //    string? idString = HttpContext.User.FindFirst("UserId")?.Value;
        //    // Safely parse it into a list of integers
        //    var allowedIds = claimValue?
        //        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        //        .Select(id => int.TryParse(id, out var i) ? i : 0)
        //        .Where(i => i > 0)
        //        .ToList() ?? new List<int>();

        //    // Filter only allowed companies from DB
        //    var companies = _context.IndusCompanies
        //        .Where(c => allowedIds.Contains(c.IntPk))
        //        .Select(c => new SelectListItem
        //        {
        //            Value = c.IntPk.ToString(),
        //            Text = c.Descript
        //        })
        //        .ToList();
        //    //return View(companies);
        //    ViewBag.Selected = _httpContextAccessor.HttpContext.Session.GetString("SelectedCompanyId");

        //    return View(companies);
        //}



        public IViewComponentResult Invoke()
        {
           
            //get selected company
            string selectedCompanyId = HttpContext.Session.GetString("SelectedCompanyId") ?? "";
            //get selected user
            string? idString = HttpContext.User.FindFirst("UserId")?.Value;
            //get all assigned company (user company)
            var claimValue = HttpContext.User.FindFirst("AllCompanyIds")?.Value; //15,4,2,14,3,21,23,22,25
            var allowedIds = claimValue?.Split(',')
            .Select(id => int.TryParse(id, out var i) ? i : 0)
            .Where(i => i > 0)
            .ToList() ?? new List<int>();
            var allCompanies = _context.IndusCompanies.ToList();
            var filteredCompanies = new List<IndusCompanies>();
            foreach (var id in allowedIds)
            {
                var comp = allCompanies.FirstOrDefault(c => c.IntPk == id);
                if (comp != null)
                {
                   filteredCompanies.Add(comp);
                }
            }
            var companies = filteredCompanies.Select(c => new CompanyDto
            {
                CompId = c.IntPk,
                CompName = c.Descript,
                IsSelected=(c.IntPk.ToString()==selectedCompanyId)
            }).ToList();
            return View(companies);
        }
    }
    public class CompanyDto
    {
        public int CompId { get; set; }
        public string CompName { get; set; } = string.Empty;

        public bool IsSelected { get; set; }
    }
}

