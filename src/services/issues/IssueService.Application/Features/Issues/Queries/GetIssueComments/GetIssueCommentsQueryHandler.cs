using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssueComments;

public sealed class GetIssueCommentsQueryHandler : IRequestHandler<GetIssueCommentsQuery, IReadOnlyList<IssueCommentDto>>
{
    private readonly IIssueCommentRepository _repository;
    private readonly IMapper _mapper;

    public GetIssueCommentsQueryHandler(IIssueCommentRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<IssueCommentDto>> Handle(GetIssueCommentsQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.GetByIssueIdAsync(request.IssueId, cancellationToken);
        return items.Select(item => _mapper.Map<IssueCommentDto>(item)).ToList();
    }
}
