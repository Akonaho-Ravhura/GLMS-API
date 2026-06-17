using GLMS_CORE_APP.API.DTOs;
using GLMS_CORE_APP.API.Services;
using GLMS_CORE_APP.Shared.Data;
using GLMS_CORE_APP.Shared.Models;
using GLMS_CORE_APP.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GLMS_CORE_APP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class CurrencyController(
    GlmsDbContext db,
    ICurrencyConversionService conversionService) : ControllerBase
{
    // GET /api/currency/rates/{baseCurrency}
    [HttpGet("rates/{baseCurrency}")]
    [ProducesResponseType(typeof(IEnumerable<CurrencyRateDto>), 200)]
    public async Task<IActionResult> GetRates(string baseCurrency)
    {
        var cutoff = DateTime.UtcNow.AddHours(-2);

        var rates = await db.CurrencyRates
            .Where(r => r.FromCurrency == baseCurrency.ToUpper() &&
                        r.IsSuccessful &&
                        r.FetchedAt >= cutoff)
            .GroupBy(r => r.ToCurrency)
            .Select(g => g.OrderByDescending(r => r.FetchedAt).First())
            .OrderBy(r => r.ToCurrency)
            .Select(r => new CurrencyRateDto(
                r.FromCurrency, r.ToCurrency, r.Rate, r.FetchedAt, r.Source))
            .ToListAsync();

        return Ok(rates);
    }

    // POST /api/currency/convert
    [HttpPost("convert")]
    [ProducesResponseType(typeof(ConvertCurrencyResponseDto), 200)]
    [ProducesResponseType(typeof(ApiErrorDto), 404)]
    public async Task<IActionResult> Convert([FromBody] ConvertCurrencyDto dto)
    {
        var rate = await conversionService.GetRateAsync(dto.FromCurrency, dto.ToCurrency);

        if (rate is null)
            return NotFound(new ApiErrorDto(
                $"No current rate available for {dto.FromCurrency} → {dto.ToCurrency}. " +
                "Rates may be stale or unavailable."));

        var converted = await conversionService.ConvertAsync(
            dto.Amount, dto.FromCurrency, dto.ToCurrency);

        // Get the rate's timestamp for the response
        var cutoff     = DateTime.UtcNow.AddHours(-2);
        var rateRecord = await db.CurrencyRates
            .Where(r => r.FromCurrency == dto.FromCurrency.ToUpper() &&
                        r.ToCurrency   == dto.ToCurrency.ToUpper() &&
                        r.IsSuccessful &&
                        r.FetchedAt >= cutoff)
            .OrderByDescending(r => r.FetchedAt)
            .FirstOrDefaultAsync();

        return Ok(new ConvertCurrencyResponseDto(
            dto.Amount,
            dto.FromCurrency.ToUpper(),
            converted ?? 0,
            dto.ToCurrency.ToUpper(),
            rate.Value,
            rateRecord?.FetchedAt ?? DateTime.UtcNow));
    }
}
