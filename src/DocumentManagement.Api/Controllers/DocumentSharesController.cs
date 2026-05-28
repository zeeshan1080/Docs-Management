using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DocumentManagement.Application.Abstractions;
using DocumentManagement.Application.Sharing;
using DocumentManagement.Domain;

namespace DocumentManagement.Api.Controllers;

[ApiController]
[Route("api/documents/{documentId:int}/shares")]
[Authorize(Roles = AppRoles.Management)]
public class DocumentSharesController : ControllerBase
{
    private readonly IDocumentShareService _shares;

    public DocumentSharesController(IDocumentShareService shares) => _shares = shares;

    [HttpGet]
    public async Task<IActionResult> List(int documentId, CancellationToken ct)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id == null) return Unauthorized();
        return Ok(await _shares.ListAsync(id, documentId, ct));
    }

    [HttpPost]
    public async Task<IActionResult> Add(int documentId, [FromBody] CreateDocumentShareRequest body, CancellationToken ct)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id == null) return Unauthorized();
        var (ok, err, shareId) = await _shares.AddAsync(id, documentId, body, ct);
        if (!ok) return BadRequest(new { error = err });
        return Ok(new { id = shareId });
    }

    [HttpDelete("{shareId:int}")]
    public async Task<IActionResult> Remove(int documentId, int shareId, CancellationToken ct)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id == null) return Unauthorized();
        var ok = await _shares.RemoveAsync(id, documentId, shareId, ct);
        if (!ok) return NotFound();
        return NoContent();
    }
}
