using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DWB.Models;
using System;


namespace DWB.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly DWBEntity _context;
              
        public HomeController(ILogger<HomeController> logger, DWBEntity dWBEntity)
        {
            _logger = logger;
            _context = dWBEntity;
        }
        public IActionResult Index()
        {
            
                return View();
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
