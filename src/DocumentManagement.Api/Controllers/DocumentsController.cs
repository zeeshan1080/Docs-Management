using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using DocumentManagement.Application.Abstractions;
using DocumentManagement.Application.Documents;
using DocumentManagement.Infrastructure.Options;

namespace DocumentManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documents;
    private readonly IOptions<DocumentViewerOptions> _viewerOptions;

    public DocumentsController(IDocumentService documents, IOptions<DocumentViewerOptions> viewerOptions)
    {
        _documents = documents;
        _viewerOptions = viewerOptions;
    }

    [HttpGet("folder/{folderId:int}")]
    public async Task<IActionResult> ByFolder(int folderId, CancellationToken ct)
    {
        var id = UserId();
        if (id == null) return Unauthorized();
        return Ok(await _documents.ListByFolderAsync(id, folderId, ct));
    }

    [HttpGet("folder/{folderId:int}/paged")]
    public async Task<IActionResult> ByFolderPaged(
        int folderId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var id = UserId();
        if (id == null) return Unauthorized();
        var result = await _documents.ListByFolderPagedAsync(id, folderId, page, pageSize, search, ct);
        return Ok(result);
    }

    /// <summary>Add a named shortcut to an external URL (virtual document; opens in a new tab).</summary>
    [HttpPost("folder/{folderId:int}/link")]
    public async Task<IActionResult> CreateLink(int folderId, [FromBody] CreateDocumentLinkRequest body, CancellationToken ct)
    {
        var id = UserId();
        if (id == null) return Unauthorized();
        var dto = await _documents.CreateLinkAsync(id, folderId, body, ct);
        if (dto == null) return BadRequest(new { error = "Could not add link. Check the name, URL (http/https), and folder permissions." });
        return Ok(dto);
    }

    [HttpPost("folder/{folderId:int}/upload")]
    [RequestSizeLimit(209_715_200)]
    public async Task<IActionResult> Upload(int folderId, IFormFile file, CancellationToken ct)
    {
        var id = UserId();
        if (id == null) return Unauthorized();
        if (file.Length == 0) return BadRequest("Empty file.");
        await using var stream = file.OpenReadStream();
        var dto = await _documents.UploadAsync(id, folderId, stream, file.FileName, file.ContentType ?? "application/octet-stream", ct);
        if (dto == null) return BadRequest("Upload failed or not allowed.");
        return Ok(dto);
    }

    /// <summary>Start a chunked upload session (for large files). Each chunk must be ≤ MaxChunkUploadBytes.</summary>
    [HttpPost("folder/{folderId:int}/upload/chunk/init")]
    public async Task<IActionResult> InitChunkUpload(int folderId, [FromBody] InitChunkUploadRequestDto body, CancellationToken ct)
    {
        var id = UserId();
        if (id == null) return Unauthorized();
        var dto = await _documents.InitChunkUploadAsync(id, folderId, body, ct);
        if (dto == null) return BadRequest("Could not start chunked upload.");
        return Ok(dto);
    }

    /// <summary>Upload one chunk. Default limit 20 MiB per request (must exceed configured MaxChunkUploadBytes).</summary>
    [HttpPost("folder/{folderId:int}/upload/chunk")]
    [RequestSizeLimit(20_971_520)]
    public async Task<IActionResult> UploadChunk(
        int folderId,
        [FromForm] string sessionId,
        [FromForm] int chunkIndex,
        IFormFile chunk,
        CancellationToken ct)
    {
        var id = UserId();
        if (id == null) return Unauthorized();
        if (chunk == null || chunk.Length == 0) return BadRequest("Empty chunk.");
        if (!Guid.TryParse(sessionId, out var sid)) return BadRequest("Invalid session.");
        await using var stream = chunk.OpenReadStream();
        var ok = await _documents.UploadChunkAsync(id, folderId, sid, chunkIndex, stream, chunk.Length, ct);
        if (!ok) return BadRequest("Chunk rejected.");
        return NoContent();
    }

    [HttpPost("folder/{folderId:int}/upload/chunk/complete")]
    public async Task<IActionResult> CompleteChunkUpload(int folderId, [FromBody] CompleteChunkUploadRequestDto body, CancellationToken ct)
    {
        var id = UserId();
        if (id == null) return Unauthorized();
        var dto = await _documents.CompleteChunkUploadAsync(id, folderId, body.SessionId, ct);
        if (dto == null) return BadRequest("Could not finalize chunked upload.");
        return Ok(dto);
    }

    [HttpGet("{documentId:int}/download")]
    public async Task<IActionResult> Download(int documentId, CancellationToken ct)
    {
        var id = UserId();
        if (id == null) return Unauthorized();
        var result = await _documents.DownloadAsync(id, documentId, ct);
        if (result == null) return NotFound();
        var (stream, fileName, contentType) = result.Value;
        return File(stream, contentType, fileName);
    }

    /// <summary>Google Docs Viewer URL for supported types (PDF, DOC, DOCX, PNG). Requires a public API base URL reachable by Google.</summary>
    [HttpGet("{documentId:int}/viewer")]
    public async Task<IActionResult> Viewer(int documentId, CancellationToken ct)
    {
        var id = UserId();
        if (id == null) return Unauthorized();
        var token = await _documents.MintViewerTokenAsync(id, documentId, ct);
        if (token == null) return NotFound();
        var o = _viewerOptions.Value;
        var origin = !string.IsNullOrWhiteSpace(o.PublicOrigin)
            ? o.PublicOrigin.TrimEnd('/')
            : $"{Request.Scheme}://{Request.Host.Value}";
        var fileUrl = $"{origin}/api/documents/view/{token}";
        var viewerUrl = $"https://docs.google.com/viewer?url={Uri.EscapeDataString(fileUrl)}&embedded=true";
        return Ok(new { viewerUrl });
    }

    /// <summary>Anonymous fetch of file bytes for Google Docs Viewer (short-lived token from <see cref="Viewer"/>).</summary>
    [AllowAnonymous]
    [HttpGet("view/{token}")]
    public async Task<IActionResult> ViewByToken(string token, CancellationToken ct)
    {
        var result = await _documents.DownloadByViewerTokenAsync(token, ct);
        if (result == null) return NotFound();
        var (stream, _, contentType) = result.Value;
        Response.Headers.Append("Cache-Control", "no-store");
        return File(stream, string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
    }

    [HttpDelete("{documentId:int}")]
    public async Task<IActionResult> SoftDelete(int documentId, CancellationToken ct)
    {
        var id = UserId();
        if (id == null) return Unauthorized();
        var ok = await _documents.SoftDeleteAsync(id, documentId, ct);
        if (!ok) return BadRequest();
        return NoContent();
    }

    private string? UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
}
