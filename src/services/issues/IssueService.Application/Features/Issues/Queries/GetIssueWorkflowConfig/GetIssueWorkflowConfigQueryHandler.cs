using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Domain.Workflow;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssueWorkflowConfig;

public sealed class GetIssueWorkflowConfigQueryHandler : IRequestHandler<GetIssueWorkflowConfigQuery, WorkflowConfigDto>
{
    public Task<WorkflowConfigDto> Handle(GetIssueWorkflowConfigQuery request, CancellationToken cancellationToken)
    {
        var transitions = IssueWorkflow.AllowedTransitions.ToDictionary(
            kvp => kvp.Key.ToString(),
            kvp => kvp.Value.Select(v => v.ToString()).ToArray());

        var statuses = IssueWorkflow.Statuses.Select(s => s.ToString()).ToArray();

        var dto = new WorkflowConfigDto
        {
            Statuses = statuses,
            AllowedTransitions = transitions
        };

        return Task.FromResult(dto);
    }
}
