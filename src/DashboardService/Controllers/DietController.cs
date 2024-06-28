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

    [HttpPut("update-weight")]
    public async Task<ActionResult> UpdateWeight(DateTime date, decimal weight)
    {
        var snapshotDateUtc = DateTime.SpecifyKind(date, DateTimeKind.Utc);

        var existingDietRecord = await _context.DietStats
            .FirstOrDefaultAsync(diet => diet.SnapshotDateUTC == snapshotDateUtc);

        if (existingDietRecord == null)
            return NotFound("DietStat for the given date not found.");

        existingDietRecord.Weight = weight;
        _context.DietStats.Update(existingDietRecord);

        var result = await _context.SaveChangesAsync() > 0;
        if (!result)
            return BadRequest("Could not update weight.");

        return NoContent();
    }

    [HttpPost("calculate-food-price")]
    public async Task<ActionResult<decimal>> CalculateFoodPrice([FromBody] FoodPriceDto foodPriceDto)
    {
        var foodPrice = await _context.FoodPrices
            .FirstOrDefaultAsync(fp =>
                fp.Name.Equals(foodPriceDto.Name, StringComparison.OrdinalIgnoreCase) &&
                fp.UnitOfMeasure.Equals(foodPriceDto.UnitOfMeasure, StringComparison.OrdinalIgnoreCase));

        if (foodPrice == null)
            return NotFound("Food price not found.");

        var totalPrice = foodPrice.Price * foodPriceDto.Quantity;
        return Ok(totalPrice);
    }

    [HttpPost("update-food-price")]
    public async Task<ActionResult<decimal>> UpdateFoodPrice([FromBody] FoodPriceDto foodPriceDto)
    {
        var foodPrice = await _context.FoodPrices
            .FirstOrDefaultAsync(fp =>
                fp.Name.Equals(foodPriceDto.Name, StringComparison.OrdinalIgnoreCase) &&
                fp.UnitOfMeasure.Equals(foodPriceDto.UnitOfMeasure, StringComparison.OrdinalIgnoreCase));

        if (foodPrice != null)
        {
            foodPrice.Price = foodPriceDto.Price;
            foodPrice.LastUpdateDateUTC = DateTime.UtcNow;
            _context.FoodPrices.Update(foodPrice);
        }
        else
        {
            foodPrice = new FoodPrice
            {
                Id = Guid.NewGuid(),
                Name = foodPriceDto.Name,
                UnitOfMeasure = foodPriceDto.UnitOfMeasure,
                Price = foodPriceDto.Price,
                LastUpdateDateUTC = DateTime.UtcNow
            };
            _context.FoodPrices.Add(foodPrice);
        }

        var result = await _context.SaveChangesAsync();
        if (result <= 0)
            return BadRequest("Could not update or create the food price record.");

        return Ok(foodPrice.Price);
    }
}
