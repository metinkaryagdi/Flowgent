using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Domain.Entities;
using BitirmeProject.IssueService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BitirmeProject.IssueService.Infrastructure.Repositories;

public sealed class IssueAttachmentRepository : IIssueAttachmentRepository
{
    private readonly IssueDbContext _dbContext;

    public IssueAttachmentRepository(IssueDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(IssueAttachment attachment, CancellationToken cancellationToken = default)
    {
        await _dbContext.IssueAttachments.AddAsync(attachment, cancellationToken);
    }

    public async Task<IReadOnlyList<IssueAttachment>> GetByIssueIdAsync(Guid issueId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.IssueAttachments
            .Where(x => x.IssueId == issueId)
            .OrderByDescending(x => x.UploadedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid issueId, Guid fileId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.IssueAttachments
            .AnyAsync(x => x.IssueId == issueId && x.FileId == fileId, cancellationToken);
    }

    public async Task RemoveByIssueIdAsync(Guid issueId, CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.IssueAttachments
            .Where(x => x.IssueId == issueId)
            .ToListAsync(cancellationToken);

        if (items.Count > 0)
            _dbContext.IssueAttachments.RemoveRange(items);
    }
}
