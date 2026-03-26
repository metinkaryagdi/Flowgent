using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.Common.Mappings;
using BitirmeProject.IssueService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssueById;

public sealed class GetIssueByIdQueryHandler : IRequestHandler<GetIssueByIdQuery, IssueDto?>
{
    private readonly IIssueRepository _repository;
    private readonly IIssueBoardRepository _boardRepository;
    private readonly IMapper _mapper;

    public GetIssueByIdQueryHandler(IIssueRepository repository, IIssueBoardRepository boardRepository, IMapper mapper)
    {
        _repository = repository;
        _boardRepository = boardRepository;
        _mapper = mapper;
    }

    public async Task<IssueDto?> Handle(GetIssueByIdQuery request, CancellationToken cancellationToken)
    {
        var issue = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (issue is null)
            return null;

        var boardItem = await _boardRepository.GetByIssueIdAsync(issue.Id, cancellationToken);
        return IssueDtoFactory.Create(issue, boardItem?.SprintId);
    }
}
