using DWB.APIModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DWB.Controllers
{
    public class AssessmentController : Controller
    {
        private readonly IConfiguration _configuration;

        public AssessmentController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        //GET:AssessmentController
        public async Task<IActionResult> NursingAssessment()
        {
            List<SP_OPD> patients = new List<SP_OPD>();
            //string CategoryUrl = _configuration.GetSection("DBAPI").GetSection("OPDAPI").Value ?? string.Empty;
            //set today date patient view
            int code= Convert.ToInt32(User.FindFirst("HMScode")?.Value);
            string BaseAPI=(User.FindFirst("BaseAPI")?.Value ?? string.Empty).Replace("\n","").Replace("\r","").Trim();
            string today = DateTime.Now.ToString("dd-MM-yyyy");
            //format = opd?sdate=12-07-2025&edate=12-07-2025&code=1&uhidno=null
            string fURL = BaseAPI+"opd?&sdate="+today+"&edate="+today+"&code="+code+"&uhidno=null";
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(fURL);
                var response = await client.GetAsync(fURL);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(jsonString)) // Ensure jsonString is not null or empty
                    {
                        patients = JsonConvert.DeserializeObject<List<SP_OPD>>(jsonString) ?? new List<SP_OPD>(); // Handle possible null value
                    }
                }
            }
            return View(patients);
        }

        //Other methods remain unchanged...

    }
}
