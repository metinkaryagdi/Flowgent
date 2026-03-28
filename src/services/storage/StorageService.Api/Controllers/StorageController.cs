using BitirmeProject.StorageService.Application.Abstractions;
using BitirmeProject.StorageService.Application.DTOs;
using BitirmeProject.StorageService.Application.Features.Files.Commands.DeleteFile;
using BitirmeProject.StorageService.Application.Features.Files.Commands.FinalizeFile;
using BitirmeProject.StorageService.Application.Features.Files.Commands.UploadFile;
using BitirmeProject.StorageService.Application.Features.Files.Queries.GetFileById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Abstractions.Exceptions;
using Shared.Common.Extensions;

namespace BitirmeProject.StorageService.Api.Controllers;

/// <summary>
/// Manages file uploads, finalization, metadata retrieval, and deletion.
///
/// AUTHORIZATION BOUNDARY:
/// StorageService enforces ownership-only authorization:
///   - Upload:   any authenticated user may upload; UploadedByUserId is derived from Claims.
///   - Finalize: only the uploader or an Admin may finalize.
///   - GetById:  any authenticated user may read file metadata (needed by IssueService HTTP client).
///   - Download: only the uploader or an Admin may stream file content.
///   - Delete:   only the uploader or an Admin may delete.
///
/// Parent-entity authorization (e.g. "is this user a member of the project that owns this issue?")
/// is NOT enforced here. That responsibility belongs to IssueService:
///   - IssueService.AttachFileCommandHandler verifies file ownership before attaching.
///   - Download access for issue members must be mediated through IssueService, which
///     confirms project membership before proxying or redirecting to this endpoint.
/// </summary>
[ApiController]
[Route("api/v1/storage")]
[Authorize]
public sealed class StorageController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IStorageRepository _repository;
    private readonly IFileStorage _fileStorage;

    public StorageController(IMediator mediator, IStorageRepository repository, IFileStorage fileStorage)
    {
        _mediator = mediator;
        _repository = repository;
        _fileStorage = fileStorage;
    }

    [HttpPost("files")]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult<StoredFileDto>> Upload(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length <= 0)
            throw new BusinessRuleException("File is empty.");

        // UploadedByUserId must come from authenticated Claims, never from the request body.
        var uploadedByUserId = User.GetUserId();

        await using var stream = file.OpenReadStream();
        var storagePath = await _fileStorage.SaveTemporaryAsync(stream, file.FileName, cancellationToken);

        var result = await _mediator.Send(new UploadFileCommand(
            file.FileName,
            file.ContentType,
            file.Length,
            storagePath,
            uploadedByUserId), cancellationToken);

        return Ok(result);
    }

    [HttpPost("files/{id:guid}/finalize")]
    public async Task<ActionResult<StoredFileDto>> Finalize(Guid id, CancellationToken cancellationToken)
    {
        var file = await _repository.GetByIdAsync(id, cancellationToken);
        if (file is null)
            return NotFound();

        var requesterId = User.TryGetUserId();
        if (!User.HasRole("Admin") && file.UploadedByUserId != requesterId)
            return Forbid();

        var result = await _mediator.Send(new FinalizeFileCommand(id), cancellationToken);
        return Ok(result);
    }

    [HttpGet("files/{id:guid}")]
    public async Task<ActionResult<StoredFileDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFileByIdQuery(id), cancellationToken);
        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet("files/{id:guid}/content")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var file = await _repository.GetByIdAsync(id, cancellationToken);
        if (file is null)
            return NotFound();

        // Authorization: only the uploader or an Admin may download the file.
        var requesterId = User.TryGetUserId();
        if (!User.HasRole("Admin") && file.UploadedByUserId != requesterId)
            return Forbid();

        var stream = await _fileStorage.OpenReadAsync(file.StoragePath, cancellationToken);
        if (stream is null)
            return NotFound();

        return File(stream, file.ContentType, file.FileName);
    }

    [HttpDelete("files/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var file = await _repository.GetByIdAsync(id, cancellationToken);
        if (file is null)
            return NotFound();

        // Authorization: only the uploader or an Admin may delete the file.
        var requesterId = User.TryGetUserId();
        if (!User.HasRole("Admin") && file.UploadedByUserId != requesterId)
            return Forbid();

        await _mediator.Send(new DeleteFileCommand(id), cancellationToken);
        return NoContent();
    }
}
