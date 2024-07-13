using AutoMapper;
using DashboardService;
using DashboardService.Data;
using DashboardService.DTOs;
using DashboardService.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("dashboard/activity")]
public class ActivityController : ControllerBase
{
    private readonly DashboardDbContext _context;
    private readonly IMapper _mapper;

    public ActivityController(DashboardDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpPost("add-or-update-activityDuration")]
    public async Task<ActionResult> AddOrUpdateActivityDuration([FromBody] ActivityDurationDto activityDurationDto)
    {
        var snapshotDateUtc = DateTime.SpecifyKind(activityDurationDto.Date, DateTimeKind.Utc).ToUniversalTime();

        var existingActivity = await _context.ActivityDuration
            .FirstOrDefaultAsync(ad =>
                ad.SnapshotDateUTC == snapshotDateUtc &&
                ad.Name.Equals(activityDurationDto.Name, StringComparison.OrdinalIgnoreCase));

        if (existingActivity != null)
        {
            existingActivity.Duration += activityDurationDto.Duration;
            _context.ActivityDuration.Update(existingActivity);
        }
        else
        {
            var newActivity = new ActivityDuration
            {
                Id = Guid.NewGuid(),
                SnapshotDateUTC = snapshotDateUtc,
                Name = activityDurationDto.Name,
                Duration = activityDurationDto.Duration
            };
            _context.ActivityDuration.Add(newActivity);
        }

        var result = await _context.SaveChangesAsync() > 0;
        if (!result)
            return BadRequest("Could not save activity entry.");

        return Ok("Activity entry updated successfully.");
    }

    [HttpPost("add-or-update-activityCount")]
    public async Task<ActionResult> AddOrUpdateActivityCount([FromBody] ActivityCountDto activityCountDto)
    {
        var snapshotDateUtc = DateTime.SpecifyKind(activityCountDto.Date, DateTimeKind.Utc).ToUniversalTime();

        // var existingActivity = await _context.ActivityCount
        //     .FirstOrDefaultAsync(ad =>
        //         ad.SnapshotDateUTC == snapshotDateUtc &&
        //         ad.Name.Equals(activityCountDto.Name, StringComparison.OrdinalIgnoreCase));

        var existingActivity = _context.ActivityCount
            .Where(ad => ad.SnapshotDateUTC.Date == snapshotDateUtc.Date)
            .AsEnumerable() // Switch to client-side evaluation
            .FirstOrDefault(ad => ad.Name.Equals(activityCountDto.Name, StringComparison.OrdinalIgnoreCase));

        if (existingActivity != null)
        {
            existingActivity.Count += activityCountDto.Count;
            _context.ActivityCount.Update(existingActivity);
        }
        else
        {
            var newActivity = new ActivityCount
            {
                Id = Guid.NewGuid(),
                SnapshotDateUTC = snapshotDateUtc,
                Name = activityCountDto.Name,
                Count = activityCountDto.Count
            };
            _context.ActivityCount.Add(newActivity);
        }

        var result = await _context.SaveChangesAsync() > 0;
        if (!result)
            return BadRequest("Could not save activity entry.");

        return Ok("Activity entry updated successfully.");
    }

    [HttpPost("add-voice-memo")]
    public async Task<ActionResult> AddVoiceMemo([FromBody] VoiceLogDto voiceLogDto)
    {
        var snapshotDateUtc = DateTime.SpecifyKind(voiceLogDto.SnapshotDateUTC, DateTimeKind.Utc).ToUniversalTime();

        try
        {
            var newMemo = new VoiceLog
            {
                Id = Guid.NewGuid(),
                SnapshotDateUTC = snapshotDateUtc,
                ActivityName = voiceLogDto.ActivityName,
                Quantity = voiceLogDto.Quantity,
                UnitOfMeasure = voiceLogDto.UnitOfMeasure
            };

            _context.VoiceLogs.Add(newMemo);

            var result = await _context.SaveChangesAsync() > 0;
            if (!result)
                return BadRequest("Could not save activity entry.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }

        return Ok("Voice Log created successfully.");
    }
}
