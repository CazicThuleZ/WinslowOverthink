using AutoMapper;
using DashboardService.Data;
using DashboardService.DTOs;
using DashboardService.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DashboardService.Controllers
{
    [ApiController]
    [Route("dashboard/crypto")]
    public class CryptoController : ControllerBase
    {
        private readonly DashboardDbContext _context;
        private readonly IMapper _mapper;

        public CryptoController(DashboardDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet("get-crypto-prices")]
        public async Task<ActionResult<List<CryptoPriceDto>>> GetCryptoPrices(DateTime beginDate, DateTime endDate, string cryptoId = null)
        {
            var beginDateUtc = DateTime.SpecifyKind(beginDate, DateTimeKind.Utc);
            var endDateUtc = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

            var query = _context.CryptoPrices
                .Where(cp => cp.SnapshotDateUTC >= beginDateUtc && cp.SnapshotDateUTC <= endDateUtc);

            if (!string.IsNullOrEmpty(cryptoId))
                query = query.Where(cp => cp.CryptoId == cryptoId);

            var cryptoPrices = await query.ToListAsync();
            var cryptoPriceDtos = _mapper.Map<List<CryptoPriceDto>>(cryptoPrices);
            return Ok(cryptoPriceDtos);
        }

        [HttpGet("distinct-crypto-ids")]
        public async Task<ActionResult<List<string>>> GetDistinctCryptoIds()
        {
            var distinctCryptoIds = await _context.CryptoPrices
                .Select(cp => cp.CryptoId)
                .Distinct()
                .ToListAsync();

            return Ok(distinctCryptoIds);
        }

        [HttpPost("create-crypto-price-snapshot")]
        public async Task<ActionResult> CreateCryptoPriceSnapshot([FromBody] CryptoPriceDto cryptoPriceDto)
        {
            var snapshotDateUtc = DateTime.SpecifyKind(cryptoPriceDto.SnapshotDateUTC, DateTimeKind.Utc).ToUniversalTime();
            cryptoPriceDto.SnapshotDateUTC = snapshotDateUtc;

            var cryptoPrice = _mapper.Map<CryptoPrice>(cryptoPriceDto);

            var existingCryptoPrice = await _context.CryptoPrices
                .FirstOrDefaultAsync(cp => cp.CryptoId == cryptoPrice.CryptoId && cp.SnapshotDateUTC == cryptoPrice.SnapshotDateUTC);

            if (existingCryptoPrice != null)
                _context.Entry(existingCryptoPrice).CurrentValues.SetValues(cryptoPrice);
            else
                _context.CryptoPrices.Add(cryptoPrice);

            var result = await _context.SaveChangesAsync() > 0;
            if (!result)
                return BadRequest("Could not save crypto price.");

            return CreatedAtAction(nameof(GetCryptoPrices), new { beginDate = cryptoPrice.SnapshotDateUTC, endDate = cryptoPrice.SnapshotDateUTC, cryptoId = cryptoPrice.CryptoId }, cryptoPriceDto);
        }

        [HttpDelete("delete-by-date")]
        public async Task<ActionResult> DeleteByDate(DateTime date)
        {
            var dateUtc = DateTime.SpecifyKind(date, DateTimeKind.Utc).ToUniversalTime();

            var cryptoPricesToDelete = await _context.CryptoPrices
                .Where(cp => cp.SnapshotDateUTC.Date == dateUtc.Date)
                .ToListAsync();

            if (cryptoPricesToDelete.Count == 0)
                return NotFound("No records found for the specified date.");

            _context.CryptoPrices.RemoveRange(cryptoPricesToDelete);
            var result = await _context.SaveChangesAsync() > 0;

            if (!result)
                return BadRequest("Could not delete records.");

            return Ok("Records deleted successfully.");
        }
    }
}
