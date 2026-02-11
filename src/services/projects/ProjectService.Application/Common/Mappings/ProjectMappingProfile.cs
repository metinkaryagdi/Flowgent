using AutoMapper;
using BitirmeProject.ProjectService.Application.DTOs;
using BitirmeProject.ProjectService.Domain.Entities;

namespace BitirmeProject.ProjectService.Application.Common.Mappings;

public sealed class ProjectMappingProfile : Profile
{
    public ProjectMappingProfile()
    {
        CreateMap<Project, ProjectDto>();
    }
}