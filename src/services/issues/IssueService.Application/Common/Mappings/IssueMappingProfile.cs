using AutoMapper;
using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Application.ReadModels;
using BitirmeProject.IssueService.Domain.Entities;

namespace BitirmeProject.IssueService.Application.Common.Mappings;

public sealed class IssueMappingProfile : Profile
{
    public IssueMappingProfile()
    {
        CreateMap<Issue, IssueDto>();
        CreateMap<IssueAudit, IssueAuditDto>();
        CreateMap<IssueBoardItem, IssueBoardItemDto>();
        CreateMap<IssueAttachment, IssueAttachmentDto>();
        CreateMap<IssueComment, IssueCommentDto>();
    }
}
