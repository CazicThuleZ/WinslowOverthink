using AutoMapper;
using DashboardService.Data;
using DashboardService.DTOs;
using DashboardService.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DashboardService.Controllers;

[ApiController]
[Route("dashboard/accounts")]
public class AccountsController : ControllerBase
{
    private readonly DashboardDbContext _context;
    private readonly IMapper _mapper;

    public AccountsController(DashboardDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet("get-account-balances")]
    public async Task<ActionResult<List<AccountBalanceDto>>> GetAccountBalances(DateTime beginDate, DateTime endDate, string accountName = null)
    {
        var beginDateUtc = DateTime.SpecifyKind(beginDate, DateTimeKind.Utc);
        var endDateUtc = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

        var query = _context.AccountBalances
            .Where(ab => ab.SnapshotDateUTC >= beginDateUtc && ab.SnapshotDateUTC <= endDateUtc);

        if (!string.IsNullOrEmpty(accountName))
            query = query.Where(ab => ab.AccountName == accountName);

        var accountBalances = await query.ToListAsync();
        var accountBalanceDtos = _mapper.Map<List<AccountBalanceDto>>(accountBalances);
        return Ok(accountBalanceDtos);
    }

    [HttpGet("distinct-account-names")]
    public async Task<ActionResult<List<string>>> GetDistinctAccountNames()
    {
        var distinctAccountNames = await _context.AccountBalances
            .Select(ab => ab.AccountName)
            .Distinct()
            .ToListAsync();

        return Ok(distinctAccountNames);
    }

    [HttpPost("create-account-snapshot")]
    public async Task<ActionResult> CreateAccountSnapshot([FromBody] AccountBalanceDto accountBalanceDto)
    {

        var snapshotDateUtc = DateTime.SpecifyKind(accountBalanceDto.SnapshotDateUTC, DateTimeKind.Utc).ToUniversalTime();
        accountBalanceDto.SnapshotDateUTC = snapshotDateUtc;

        var accountBalance = _mapper.Map<AccountBalance>(accountBalanceDto);

        var existingAccountBalance = await _context.AccountBalances
            .FirstOrDefaultAsync(ab => ab.AccountName == accountBalance.AccountName && ab.SnapshotDateUTC == accountBalance.SnapshotDateUTC);

        if (existingAccountBalance != null)
            _context.Entry(existingAccountBalance).CurrentValues.SetValues(accountBalance);
        else
            _context.AccountBalances.Add(accountBalance);

        var result = await _context.SaveChangesAsync() > 0;
        if (!result)
            return BadRequest("Could not save account balance.");

        return CreatedAtAction(nameof(GetAccountBalances), new { beginDate = accountBalance.SnapshotDateUTC, endDate = accountBalance.SnapshotDateUTC, accountName = accountBalance.AccountName }, accountBalanceDto);
    }
}
