using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DocumentManagement.Application.Abstractions;
using DocumentManagement.Application.Sharing;
using DocumentManagement.Domain;

namespace DocumentManagement.Api.Controllers;

[ApiController]
[Route("api/folders/{folderId:int}/shares")]
[Authorize(Roles = AppRoles.Management)]
public class FolderSharesController : ControllerBase
{
    private readonly IFolderShareService _shares;

    public FolderSharesController(IFolderShareService shares) => _shares = shares;

    [HttpGet]
    public async Task<IActionResult> List(int folderId, CancellationToken ct)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id == null) return Unauthorized();
        return Ok(await _shares.ListAsync(id, folderId, ct));
    }

    [HttpPost]
    public async Task<IActionResult> Add(int folderId, [FromBody] CreateFolderShareRequest body, CancellationToken ct)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id == null) return Unauthorized();
        var (ok, err, shareId) = await _shares.AddAsync(id, folderId, body, ct);
        if (!ok) return BadRequest(new { error = err });
        return Ok(new { id = shareId });
    }

    [HttpDelete("{shareId:int}")]
    public async Task<IActionResult> Remove(int folderId, int shareId, CancellationToken ct)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id == null) return Unauthorized();
        var ok = await _shares.RemoveAsync(id, shareId, ct);
        if (!ok) return NotFound();
        return NoContent();
    }
}
