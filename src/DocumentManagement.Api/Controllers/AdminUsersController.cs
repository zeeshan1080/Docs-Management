using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DocumentManagement.Application.Abstractions;
using DocumentManagement.Application.Admin;
using DocumentManagement.Domain;

namespace DocumentManagement.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = AppRoles.Management)]
public class AdminUsersController : ControllerBase
{
    private readonly IUserAdminService _users;

    public AdminUsersController(IUserAdminService users) => _users = users;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminUserListItemDto>>> List(CancellationToken ct)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id)) return Unauthorized();
        var list = await _users.GetUsersAsync(id, ct);
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id)) return Unauthorized();
        var (ok, error, userId) = await _users.CreateUserAsync(request, id, ct);
        if (!ok) return BadRequest(new { error });
        return Ok(new { userId });
    }

    [HttpPut("{userId}")]
    public async Task<IActionResult> Update(string userId, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id)) return Unauthorized();
        var (ok, error) = await _users.UpdateUserAsync(userId, request, id, ct);
        if (!ok) return BadRequest(new { error });
        return NoContent();
    }

    [HttpDelete("{userId}")]
    public async Task<IActionResult> Delete(string userId, CancellationToken ct)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id)) return Unauthorized();
        var (ok, error) = await _users.DeleteUserAsync(userId, id, ct);
        if (!ok) return BadRequest(new { error });
        return NoContent();
    }
}
