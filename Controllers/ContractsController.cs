using GLMS_CORE_APP.API.DTOs;
using GLMS_CORE_APP.Shared.Models.Enums;
using GLMS_CORE_APP.Shared.Data;
using GLMS_CORE_APP.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GLMS_CORE_APP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ContractsController(GlmsDbContext db, ILogger<ContractsController> logger) : ControllerBase
{
    // GET /api/contracts
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ContractDto>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] ContractStatus? status,
        [FromQuery] Guid? clientId)
    {
        var query = db.Contracts.Include(c => c.Client).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Title.Contains(search) ||
                                     c.Client.Name.Contains(search));

        if (status.HasValue)   query = query.Where(c => c.Status == status.Value);
        if (clientId.HasValue) query = query.Where(c => c.ClientId == clientId.Value);

        var contracts = await query.OrderByDescending(c => c.CreatedAt)
            .Select(c => ToDto(c)).ToListAsync();

        return Ok(contracts);
    }

    // GET /api/contracts/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ContractDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var contract = await db.Contracts
            .Include(c => c.Client)
            .FirstOrDefaultAsync(c => c.ContractId == id);

        return contract is null ? NotFound() : Ok(ToDto(contract));
    }

    // POST /api/contracts
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ContractDto), 201)]
    [ProducesResponseType(typeof(ApiErrorDto), 400)]
    public async Task<IActionResult> Create([FromBody] CreateContractDto dto)
    {
        if (dto.EndDate <= dto.StartDate)
            return BadRequest(new ApiErrorDto("End date must be after start date."));

        if (!await db.Clients.AnyAsync(c => c.ClientId == dto.ClientId && c.IsActive))
            return BadRequest(new ApiErrorDto("Client not found or inactive."));

        var contract = new Contract
        {
            ClientId          = dto.ClientId,
            Title             = dto.Title,
            Description       = dto.Description,
            StartDate         = dto.StartDate,
            EndDate           = dto.EndDate,
            Value             = dto.Value,
            BaseCurrency      = dto.BaseCurrency.ToUpper(),
            ExpiryWarningDays = dto.ExpiryWarningDays,
            Status            = dto.Status,
            CreatedAt         = DateTime.UtcNow
        };

        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        var created = await db.Contracts
            .Include(c => c.Client)
            .FirstAsync(c => c.ContractId == contract.ContractId);

        logger.LogInformation("Contract {Title} created via API", contract.Title);
        return CreatedAtAction(nameof(GetById), new { id = contract.ContractId }, ToDto(created));
    }

    // PUT /api/contracts/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ContractDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(typeof(ApiErrorDto), 400)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateContractDto dto)
    {
        if (dto.EndDate <= dto.StartDate)
            return BadRequest(new ApiErrorDto("End date must be after start date."));

        var contract = await db.Contracts
            .Include(c => c.Client)
            .FirstOrDefaultAsync(c => c.ContractId == id);

        if (contract is null) return NotFound();

        contract.Title             = dto.Title;
        contract.Description       = dto.Description;
        contract.StartDate         = dto.StartDate;
        contract.EndDate           = dto.EndDate;
        contract.Value             = dto.Value;
        contract.BaseCurrency      = dto.BaseCurrency.ToUpper();
        contract.ExpiryWarningDays = dto.ExpiryWarningDays;
        contract.Status            = dto.Status;
        contract.UpdatedAt         = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(ToDto(contract));
    }

    // PATCH /api/contracts/{id}/status
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ContractDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> PatchStatus(Guid id, [FromBody] PatchContractStatusDto dto)
    {
        var contract = await db.Contracts
            .Include(c => c.Client)
            .FirstOrDefaultAsync(c => c.ContractId == id);

        if (contract is null) return NotFound();

        contract.Status    = dto.Status;
        contract.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        logger.LogInformation("Contract {Id} status patched to {Status} via API", id, dto.Status);
        return Ok(ToDto(contract));
    }

    // DELETE /api/contracts/{id}  (soft delete — terminate)
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var contract = await db.Contracts.FindAsync(id);
        if (contract is null) return NotFound();

        contract.Status    = ContractStatus.Terminated;
        contract.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return NoContent();
    }

    private static ContractDto ToDto(Contract c) => new(
        c.ContractId, c.ClientId, c.Client?.Name ?? "",
        c.Title, c.Description, c.StartDate, c.EndDate,
        c.Status, c.Value, c.BaseCurrency,
        c.ExpiryWarningDays, c.CreatedAt, c.UpdatedAt);
}
