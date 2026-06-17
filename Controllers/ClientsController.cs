using GLMS_CORE_APP.API.DTOs;
using GLMS_CORE_APP.API.Services;
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
public class ClientsController(GlmsDbContext db, ILogger<ClientsController> logger) : ControllerBase
{
    // GET /api/clients
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ClientDto>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool? activeOnly)
    {
        var query = db.Clients.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search) ||
                                     c.ContactEmail.Contains(search));

        if (activeOnly == true)
            query = query.Where(c => c.IsActive);

        var clients = await query.OrderBy(c => c.Name)
            .Select(c => ToDto(c)).ToListAsync();

        return Ok(clients);
    }

    // GET /api/clients/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClientDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var client = await db.Clients.FindAsync(id);
        return client is null ? NotFound() : Ok(ToDto(client));
    }

    // POST /api/clients
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ClientDto), 201)]
    [ProducesResponseType(typeof(ApiErrorDto), 409)]
    public async Task<IActionResult> Create([FromBody] CreateClientDto dto)
    {
        if (await db.Clients.AnyAsync(c => c.ContactEmail == dto.ContactEmail))
            return Conflict(new ApiErrorDto("A client with this email already exists."));

        var client = new Client
        {
            Name              = dto.Name,
            ContactEmail      = dto.ContactEmail,
            PhoneNumber       = dto.PhoneNumber,
            CountryCode       = dto.CountryCode.ToUpper(),
            PreferredCurrency = dto.PreferredCurrency.ToUpper(),
            Address           = dto.Address,
            IsActive          = dto.IsActive,
            CreatedAt         = DateTime.UtcNow
        };

        db.Clients.Add(client);
        await db.SaveChangesAsync();

        logger.LogInformation("Client {Name} created via API", client.Name);
        return CreatedAtAction(nameof(GetById), new { id = client.ClientId }, ToDto(client));
    }

    // PUT /api/clients/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ClientDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(typeof(ApiErrorDto), 409)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClientDto dto)
    {
        var client = await db.Clients.FindAsync(id);
        if (client is null) return NotFound();

        if (await db.Clients.AnyAsync(c => c.ContactEmail == dto.ContactEmail && c.ClientId != id))
            return Conflict(new ApiErrorDto("Another client with this email already exists."));

        client.Name              = dto.Name;
        client.ContactEmail      = dto.ContactEmail;
        client.PhoneNumber       = dto.PhoneNumber;
        client.CountryCode       = dto.CountryCode.ToUpper();
        client.PreferredCurrency = dto.PreferredCurrency.ToUpper();
        client.Address           = dto.Address;
        client.IsActive          = dto.IsActive;
        client.UpdatedAt         = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(ToDto(client));
    }

    // DELETE /api/clients/{id}  (soft delete)
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var client = await db.Clients.FindAsync(id);
        if (client is null) return NotFound();

        client.IsActive  = false;
        client.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return NoContent();
    }

    private static ClientDto ToDto(Client c) => new(
        c.ClientId, c.Name, c.ContactEmail, c.PhoneNumber,
        c.CountryCode, c.PreferredCurrency, c.Address,
        c.IsActive, c.CreatedAt);
}
