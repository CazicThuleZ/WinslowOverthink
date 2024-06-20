using AutoMapper;
using DashboardService.Data;
using DashboardService.DTOs;
using DashboardService.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DashboardService.Controllers;

[ApiController]
[Route("dashboard/diet")]
public class DietController : ControllerBase
{
    private readonly DashboardDbContext _context;
    private readonly IMapper _mapper;

    public DietController(DashboardDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet("get-diet-diary")]
    public async Task<ActionResult<List<DietStatDto>>> GetDietDiary(DateTime beginDate, DateTime endDate)
    {
        var beginDateUtc = DateTime.SpecifyKind(beginDate, DateTimeKind.Utc);
        var endDateUtc = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

        var query = _context.DietStats
            .Where(diet => diet.SnapshotDateUTC >= beginDateUtc && diet.SnapshotDateUTC <= endDateUtc);

        var dietDiary = await query.ToListAsync();
        var dietDiaryDtos = _mapper.Map<List<DietStatDto>>(dietDiary);
        return Ok(dietDiaryDtos);
    }

    [HttpPost("create-diet-diary-snapshot")]
    public async Task<ActionResult> CreateDietDiarySnapshot([FromBody] DietStatDto dietStatDto)
    {
        var snapshotDateUtc = DateTime.SpecifyKind(dietStatDto.SnapshotDateUTC, DateTimeKind.Utc).ToUniversalTime();
        dietStatDto.SnapshotDateUTC = snapshotDateUtc;

        var dietStat = _mapper.Map<DietStat>(dietStatDto);

        var existingDietRecord = await _context.DietStats
            .FirstOrDefaultAsync(diet => diet.SnapshotDateUTC == dietStat.SnapshotDateUTC);

        if (existingDietRecord != null)
            _context.Entry(existingDietRecord).CurrentValues.SetValues(dietStat);
        else
            _context.DietStats.Add(dietStat);

        var result = await _context.SaveChangesAsync() > 0;
        if (!result)
            return BadRequest("Could not save diary entry.");

        return CreatedAtAction(nameof(GetDietDiary), new { endDate = dietStat.SnapshotDateUTC }, dietStatDto);
    }
}
