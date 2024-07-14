using Microsoft.AspNetCore.Mvc;

namespace WinslowUI;

public class DashboardsController : Controller
{
    private readonly ILogger<DashboardsController> _logger;
    private readonly IDietStatRepository _dietStatRepository;

    public DashboardsController(ILogger<DashboardsController> logger, IDietStatRepository dietStatRepository)
    {
        _logger = logger;
        _dietStatRepository = dietStatRepository;
    }

    public IActionResult Finance()
    {
        return View();
    }

    public async Task<IActionResult> Health()
    {
            var endDate = DateTime.UtcNow;
            var beginDate = endDate.AddDays(-30);

            var dietStats = await _dietStatRepository.GetDietStatsAsync(beginDate, endDate);

            return View(dietStats);        

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
