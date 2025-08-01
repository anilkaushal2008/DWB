using DWB.APIModel;
using DWB.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Reflection.Metadata;

namespace DWB.Controllers
{
    public class AssessmentController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly DWBEntity _context;
        public AssessmentController(IConfiguration configuration, DWBEntity dWBEntity)
        {
            _configuration = configuration;
            _context = dWBEntity;
        }
        //GET:AssessmentController
        public async Task<IActionResult> NursingAssessment(string dateRange)
        {
            List<SP_OPD> patients = new List<SP_OPD>();
            //Get branch ihms code
            int code = Convert.ToInt32(User.FindFirst("HMScode")?.Value);
            //Get Deafult OPD API
            string BaseAPI = (User.FindFirst("BaseAPI")?.Value ?? string.Empty).Replace("\n", "").Replace("\r", "").Trim();
            string finalURL = string.Empty;
            if (dateRange != null)
            {
                var dates = dateRange.Split(" - ");
                DateTime Sdate = DateTime.ParseExact(dates[0], "dd/MM/yyyy", null);
                DateTime Edate = DateTime.ParseExact(dates[1], "dd/MM/yyyy", null);
                string finalSdate = Convert.ToDateTime(Sdate).ToString("dd-MM-yyyy");
                string finalEdate = Convert.ToDateTime(Edate).ToString("dd-MM-yyyy");
                finalURL = BaseAPI + "opd?&sdate=" + finalSdate + "&edate=" + finalEdate + "&code=" + code + "&uhidno=null";
            }
            else
            {
                //set today date patient view               
                string today = DateTime.Now.ToString("dd-MM-yyyy");
                //format = opd?sdate=12-07-2025&edate=12-07-2025&code=1&uhidno=null
                finalURL = BaseAPI + "opd?&sdate=" + today + "&edate=" + today + "&code=" + code + "&uhidno=null";
            }
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(finalURL);
                var response = await client.GetAsync(finalURL);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(jsonString)) // Ensure jsonString is not null or empty
                    {
                        patients = JsonConvert.DeserializeObject<List<SP_OPD>>(jsonString) ?? new List<SP_OPD>(); // Handle possible null value
                    }
                }
            }
            //get assessed patients
            var intcode = Convert.ToInt32(User.FindFirst("HMScode")?.Value);
            List<TblNsassessment> tbllist = new List<TblNsassessment>();
            tbllist = await _context.TblNsassessment
               .Where(a => a.IntCode == intcode && a.BitIsCompleted == true)
               .ToListAsync();
            if (tbllist.Count() != 0)
            {
                var result = from p in tbllist
                             join t in patients on p.VchUhidNo equals t.opdno into pt
                             from t in pt.DefaultIfEmpty()
                             select new
                             {


                             };
                return View(result);
            }
            return View(patients);
        }

        [HttpGet]
        public IActionResult NAssessmentCreate(string uhid, string pname,string visit,string gender, string age,string jdate, string timein, string consultant, string category)
        {
            var model = new TblNsassessment
            {
                VchUhidNo = uhid,
                VchHmsname = pname,
                VchHmsage = age,
                VchHmsdtEntry = DateTime.ParseExact(jdate, "MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"),
                VchIhmintime = timein,
                VchHmsconsultant = consultant,
                VchHmscategory = category,
                DtStartTime = DateTime.Now,                
                VchCreatedBy = User.Identity.Name.ToString(),
                VchIpUsed = HttpContext.Connection.RemoteIpAddress.ToString()                
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult NAssessmentCreate(TblNsassessment model)
        {
            if (ModelState.IsValid)
            {
                // Save logic here, for example:
                model.DtEndTime = DateTime.Now; // Set end time to now
                model.VchTat = null; // Set TAT to null initially
                model.VchCreatedBy = User.Identity.Name; // Set created by user
                model.VchIpUsed = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"; // Get IP address
                model.BitIsCompleted = true;
                model.IntCode= Convert.ToInt32(User.FindFirst("HMScode")?.Value);
                //model.IntYr= get year from OPD api (pending in api)
                _context.TblNsassessment.Add(model);
                _context.SaveChanges();
                TempData["Success"] = "Nursing assessment saved successfully!";
                return RedirectToAction("NursingAssessment"); // or redirect to listing or details
            }
            // Model is invalid; re-display the form with errors
            return View(model);
        }
    }
}
