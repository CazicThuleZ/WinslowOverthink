using AutoMapper;
using DashboardService.Data;
using DashboardService.DTOs;
using DashboardService.Entities;
using DashboardService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DashboardService.Controllers;

[ApiController]
[Route("dashboard/diet")]
public class DietController : ControllerBase
{
    private FoodCosting _foodCosting;
    private readonly DashboardDbContext _context;
    private readonly IMapper _mapper;

    public DietController(DashboardDbContext context, IMapper mapper, FoodCosting foodCosting)
    {
        _context = context;
        _mapper = mapper;
        _foodCosting = foodCosting;
    }

    [HttpPut("update-diet-cost")]
    public async Task<ActionResult> UpdateDietCost()
    {
        try
        {
            var updatedMealLogCount = await _foodCosting.UpdateMealLogCostsAsync();
            var updatedDietStatCount = await _foodCosting.UpdateDietStatCostsAsync();

            return Ok($"Updated {updatedMealLogCount} meal logs and {updatedDietStatCount} diet stats.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
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
    public async Task<ActionResult> UpdateWeight([FromBody] WeightUpdateDto weightUpdateDto)
    {
        var snapshotDateUtc = DateTime.SpecifyKind(weightUpdateDto.Date, DateTimeKind.Utc);

        var existingDietRecord = await _context.DietStats
             .FirstOrDefaultAsync(diet => diet.SnapshotDateUTC == snapshotDateUtc);

        if (existingDietRecord == null)
            return NotFound("DietStat for the given date not found.");

        existingDietRecord.Weight = weightUpdateDto.Weight;
        _context.DietStats.Update(existingDietRecord);

        var result = await _context.SaveChangesAsync() > 0;
        if (!result)
            return BadRequest("Could not update weight.");

        return NoContent();
    }

    [HttpGet("calculate-food-price")]
    public async Task<ActionResult<decimal>> CalculateFoodPrice([FromQuery] string name, [FromQuery] string unitOfMeasure, [FromQuery] decimal quantity)
    {

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(unitOfMeasure) || quantity == 0)
            return BadRequest("Invalid input parameters.");

        var totalPrice = await _foodCosting.CalculateFoodCostAsync(name, quantity, unitOfMeasure);

        if (totalPrice == 0m)
            return NotFound("Food price not found.");

        return Ok(totalPrice);
    }

    [HttpPost("update-or-add-food-price")]
    public async Task<ActionResult<decimal>> UpdateOrAddFoodPrice([FromBody] FoodPriceDto foodPriceDto)
    {
        var foodPrice = await _context.FoodPrices
            .FirstOrDefaultAsync(fp =>
                fp.Name.ToLower() == foodPriceDto.Name.ToLower() &&
                fp.UnitOfMeasure.ToLower() == foodPriceDto.UnitOfMeasure.ToLower());

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

    [HttpPost("add-food-price")]
    public async Task<ActionResult<decimal>> AddFoodPrice([FromBody] FoodPriceDto foodPriceDto)
    {
        if (string.IsNullOrEmpty(foodPriceDto.Name) || string.IsNullOrEmpty(foodPriceDto.UnitOfMeasure))
            return BadRequest("Invalid input parameters.");

        var foodPrice = await _context.FoodPrices
            .FirstOrDefaultAsync(fp =>
                fp.Name.ToLower() == foodPriceDto.Name.ToLower() &&
                fp.UnitOfMeasure.ToLower() == foodPriceDto.UnitOfMeasure.ToLower());

        if (foodPrice == null)
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

            var result = await _context.SaveChangesAsync();
            if (result <= 0)
                return BadRequest("Could not update or create the food price record.");
        }

        return Ok(foodPrice.Price);
    }

    [HttpPost("create-meal-log-entry")]
    public async Task<ActionResult> CreateMealLogEntry([FromBody] MealLogDto mealLogDto)
    {
        var snapshotDateUtc = DateTime.SpecifyKind(mealLogDto.SnapshotDateUTC, DateTimeKind.Utc).ToUniversalTime();
        mealLogDto.SnapshotDateUTC = snapshotDateUtc;

        var mealLog = _mapper.Map<MealLog>(mealLogDto);
        _context.MealLogs.Add(mealLog);

        var result = await _context.SaveChangesAsync() > 0;
        if (!result)
            return BadRequest("Could not save meal log entry.");

        return CreatedAtAction(nameof(GetMealLog), new { date = mealLog.SnapshotDateUTC }, mealLogDto);
    }

    [HttpGet("get-meal-logs")]
    public async Task<ActionResult<List<MealLogDto>>> GetMealLog(DateTime date)
    {
        var mealDateUtc = DateTime.SpecifyKind(date, DateTimeKind.Utc);

        var query = _context.MealLogs
            .Where(meal => meal.SnapshotDateUTC == mealDateUtc);

        var mealLog = await query.ToListAsync();
        var mealLogDtos = _mapper.Map<List<MealLogDto>>(mealLog);
        return Ok(mealLogDtos);
    }
}