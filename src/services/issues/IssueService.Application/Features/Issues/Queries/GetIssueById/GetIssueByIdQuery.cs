using BitirmeProject.IssueService.Application.DTOs;
using MediatR;

namespace BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssueById;

public sealed record GetIssueByIdQuery(Guid Id) : IRequest<IssueDto?>;