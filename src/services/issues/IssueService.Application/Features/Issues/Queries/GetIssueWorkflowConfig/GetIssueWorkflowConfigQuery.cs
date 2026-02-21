using BitirmeProject.IssueService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssueWorkflowConfig;

public sealed record GetIssueWorkflowConfigQuery : IRequest<WorkflowConfigDto>;
