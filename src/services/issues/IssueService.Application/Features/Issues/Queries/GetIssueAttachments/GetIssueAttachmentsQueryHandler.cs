using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssueAttachments;

public sealed class GetIssueAttachmentsQueryHandler : IRequestHandler<GetIssueAttachmentsQuery, IReadOnlyList<IssueAttachmentDto>>
{
    private readonly IIssueAttachmentRepository _repository;
    private readonly IMapper _mapper;

    public GetIssueAttachmentsQueryHandler(IIssueAttachmentRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<IssueAttachmentDto>> Handle(GetIssueAttachmentsQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.GetByIssueIdAsync(request.IssueId, cancellationToken);
        return items.Select(item => _mapper.Map<IssueAttachmentDto>(item)).ToList();
    }
}
