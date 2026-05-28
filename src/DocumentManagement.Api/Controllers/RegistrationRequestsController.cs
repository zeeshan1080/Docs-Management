using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DocumentManagement.Application.Abstractions;
using DocumentManagement.Application.Registration;
using DocumentManagement.Domain;

namespace DocumentManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Management)]
public class RegistrationRequestsController : ControllerBase
{
    private readonly IRegistrationAdminService _registration;

    public RegistrationRequestsController(IRegistrationAdminService registration) => _registration = registration;

    [HttpGet("pending/count")]
    public async Task<IActionResult> PendingCount(CancellationToken ct) =>
        Ok(new { count = await _registration.GetPendingCountAsync(ct) });

    [HttpGet("pending")]
    public async Task<IActionResult> Pending(CancellationToken ct) => Ok(await _registration.GetPendingAsync(ct));

    [HttpPost("{id:int}/review")]
    public async Task<IActionResult> Review(int id, [FromBody] ReviewRegistrationRequest body, CancellationToken ct)
    {
        var mgr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (mgr == null) return Unauthorized();
        var (ok, err) = await _registration.ReviewAsync(mgr, id, body, ct);
        if (!ok) return BadRequest(new { error = err });
        return Ok();
    }
}
