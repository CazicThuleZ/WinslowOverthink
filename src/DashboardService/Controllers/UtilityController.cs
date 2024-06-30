using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DashboardService.Data;
using Microsoft.AspNetCore.Mvc;

namespace DashboardService.Controllers
{

    [ApiController]
    [Route("dashboard/utility")]
    public class UtilityController : ControllerBase
    {
        private readonly DashboardDbContext _context;

        public UtilityController(DashboardDbContext context)
        {
            _context = context;
        }

        [HttpGet("health-check")]
        public IActionResult HealthCheck()
        {
            return Ok("API is up and running");
        }
    }
}