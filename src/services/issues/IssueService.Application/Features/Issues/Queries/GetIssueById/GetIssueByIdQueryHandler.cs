using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssueById;

public sealed class GetIssueByIdQueryHandler : IRequestHandler<GetIssueByIdQuery, IssueDto?>
{
    private readonly IIssueRepository _repository;
    private readonly IMapper _mapper;

    public GetIssueByIdQueryHandler(IIssueRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IssueDto?> Handle(GetIssueByIdQuery request, CancellationToken cancellationToken)
    {
        var issue = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return issue is null ? null : _mapper.Map<IssueDto>(issue);
    }
}