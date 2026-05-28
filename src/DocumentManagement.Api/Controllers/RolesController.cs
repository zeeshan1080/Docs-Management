using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DocumentManagement.Application.Abstractions;

namespace DocumentManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly IRolesService _roles;

    public RolesController(IRolesService roles) => _roles = roles;

    [HttpGet("options")]
    [AllowAnonymous]
    public async Task<IActionResult> Options(CancellationToken ct) => Ok(await _roles.GetAllAsync(ct));
}
