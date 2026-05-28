using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DocumentManagement.Application.Abstractions;
using DocumentManagement.Application.Admin;
using DocumentManagement.Domain;

namespace DocumentManagement.Api.Controllers;

[ApiController]
[Route("api/admin/settings")]
[Authorize(Roles = AppRoles.Management)]
public class AdminSettingsController : ControllerBase
{
    private readonly ISettingsAdminService _settings;
    private readonly ILocationService _locations;

    public AdminSettingsController(ISettingsAdminService settings, ILocationService locations)
    {
        _settings = settings;
        _locations = locations;
    }

    /// <param name="includeInactive">When true, lists inactive locations for the Locations settings screen only.</param>
    [HttpGet("locations")]
    public async Task<IActionResult> ListLocations([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(User.FindFirstValue(ClaimTypes.NameIdentifier))) return Unauthorized();
        return Ok(await _locations.GetAllForAdminAsync(includeInactive, ct));
    }

    [HttpPost("locations")]
    public async Task<IActionResult> AddLocation([FromBody] CreateLocationRequest request, CancellationToken ct)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id)) return Unauthorized();
        var (ok, error, newId) = await _settings.AddLocationAsync(id, request, ct);
        if (!ok) return BadRequest(new { error });
        return Ok(new { id = newId });
    }

    [HttpPut("locations/{id:int}")]
    public async Task<IActionResult> UpdateLocation(int id, [FromBody] UpdateLocationRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var (ok, error) = await _settings.UpdateLocationAsync(userId, id, request, ct);
        if (!ok) return BadRequest(new { error });
        return NoContent();
    }

    [HttpPost("roles")]
    public async Task<IActionResult> AddRole([FromBody] CreateRoleRequest request, CancellationToken ct)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id)) return Unauthorized();
        var (ok, error, roleId) = await _settings.AddRoleAsync(id, request, ct);
        if (!ok) return BadRequest(new { error });
        return Ok(new { roleId });
    }

    [HttpPut("roles/{roleId}")]
    public async Task<IActionResult> UpdateRole(string roleId, [FromBody] UpdateRoleRequest request, CancellationToken ct)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id)) return Unauthorized();
        var (ok, error) = await _settings.UpdateRoleAsync(id, roleId, request, ct);
        if (!ok) return BadRequest(new { error });
        return NoContent();
    }
}
