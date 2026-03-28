using AutoMapper;
using BitirmeProject.SprintService.Application.DTOs;
using BitirmeProject.SprintService.Application.ReadModels;
using BitirmeProject.SprintService.Domain.Entities;

namespace BitirmeProject.SprintService.Application.Common.Mappings;

public sealed class SprintMappingProfile : Profile
{
    public SprintMappingProfile()
    {
        CreateMap<Sprint, SprintDto>();
        CreateMap<SprintIssue, SprintIssueDto>();
    }
}
