using Microsoft.AspNetCore.Mvc;

namespace WinslowUI;

public class DashboardsController : Controller
{
    private readonly ILogger<DashboardsController> _logger;

    public DashboardsController(ILogger<DashboardsController> logger)
    {
        _logger = logger;
    }

    public IActionResult Finance()
    {
        return View();
    }

    public IActionResult Health()
    {
        return View();
    }

    public IActionResult Home()
    {
        return View();
    }

    public IActionResult Media()
    {
        return View();
    }

    public IActionResult World()
    {
        return View();
    }
}
