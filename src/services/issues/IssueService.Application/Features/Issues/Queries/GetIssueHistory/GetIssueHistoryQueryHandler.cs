using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssueHistory;

public sealed class GetIssueHistoryQueryHandler : IRequestHandler<GetIssueHistoryQuery, IReadOnlyList<IssueAuditDto>>
{
    private readonly IIssueAuditRepository _auditRepository;
    private readonly IMapper _mapper;

    public GetIssueHistoryQueryHandler(IIssueAuditRepository auditRepository, IMapper mapper)
    {
        _auditRepository = auditRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<IssueAuditDto>> Handle(GetIssueHistoryQuery request, CancellationToken cancellationToken)
    {
        var items = await _auditRepository.GetByIssueIdAsync(request.IssueId, cancellationToken);
        return items.Select(item => _mapper.Map<IssueAuditDto>(item)).ToList();
    }
}
