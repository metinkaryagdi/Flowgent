using BitirmeProject.StorageService.Application.Abstractions;
using BitirmeProject.StorageService.Application.DTOs;
using BitirmeProject.StorageService.Application.Features.Files.Commands.DeleteFile;
using BitirmeProject.StorageService.Application.Features.Files.Commands.UploadFile;
using BitirmeProject.StorageService.Application.Features.Files.Queries.GetFileById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Abstractions.Exceptions;

namespace BitirmeProject.StorageService.Api.Controllers;

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
        [FromForm] Guid uploadedByUserId,
        CancellationToken cancellationToken)
    {
        if (file.Length <= 0)
            throw new BusinessRuleException("File is empty.");

        await using var stream = file.OpenReadStream();
        var storagePath = await _fileStorage.SaveAsync(stream, file.FileName, cancellationToken);

        var result = await _mediator.Send(new UploadFileCommand(
            file.FileName,
            file.ContentType,
            file.Length,
            storagePath,
            uploadedByUserId), cancellationToken);

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

        var stream = await _fileStorage.OpenReadAsync(file.StoragePath, cancellationToken);
        if (stream is null)
            return NotFound();

        return File(stream, file.ContentType, file.FileName);
    }

    [HttpDelete("files/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteFileCommand(id), cancellationToken);
        return NoContent();
    }
}
