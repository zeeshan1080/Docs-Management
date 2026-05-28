using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DocumentManagement.Application.Abstractions;
using DocumentManagement.Application.Folders;
using DocumentManagement.Domain;

namespace DocumentManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FoldersController : ControllerBase
{
    private readonly IFolderService _folders;
    private readonly IAccessControlService _access;

    public FoldersController(IFolderService folders, IAccessControlService access)
    {
        _folders = folders;
        _access = access;
    }

    /// <summary>Whether the current user may upload into this folder (full folder access, or Management).</summary>
    [HttpGet("{folderId:int}/upload-allowed")]
    public async Task<IActionResult> UploadAllowed(int folderId, CancellationToken ct)
    {
        var id = UserId();
        if (id == null) return Unauthorized();
        var allowed = await _access.CanAccessFolderAsync(id, folderId, ct);
        return Ok(new { allowed });
    }

    [HttpGet("tree")]
    public async Task<IActionResult> Tree(CancellationToken ct)
    {
        var id = UserId();
        if (id == null) return Unauthorized();
        return Ok(await _folders.GetTreeAsync(id, ct));
    }

    /// <summary>Counts and sizes for the folder (documents in this folder only, not descendants).</summary>
    [HttpGet("{folderId:int}/stats")]
    public async Task<IActionResult> Stats(int folderId, CancellationToken ct)
    {
        var id = UserId();
        if (id == null) return Unauthorized();
        var stats = await _folders.GetStatsAsync(id, folderId, ct);
        if (stats == null) return NotFound();
        return Ok(stats);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Management)]
    public async Task<ActionResult<FolderDto>> Create([FromBody] CreateFolderRequest request, CancellationToken ct)
    {
        var id = UserId();
        if (id == null) return Unauthorized();
        var created = await _folders.CreateAsync(id, request, ct);
        if (created == null) return BadRequest();
        return CreatedAtAction(nameof(Tree), created);
    }

    [HttpPut("{folderId:int}")]
    [Authorize(Roles = AppRoles.Management)]
    public async Task<ActionResult<FolderDto>> Rename(
        int folderId,
        [FromBody] RenameFolderRequest request,
        CancellationToken ct)
    {
        var id = UserId();
        if (id == null) return Unauthorized();
        var updated = await _folders.RenameAsync(id, folderId, request, ct);
        if (updated == null) return BadRequest();
        return Ok(updated);
    }

    [HttpDelete("{folderId:int}")]
    [Authorize(Roles = AppRoles.Management)]
    public async Task<IActionResult> Delete(int folderId, CancellationToken ct)
    {
        var id = UserId();
        if (id == null) return Unauthorized();
        var ok = await _folders.DeleteAsync(id, folderId, ct);
        if (!ok) return BadRequest();
        return NoContent();
    }

    private string? UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
}
