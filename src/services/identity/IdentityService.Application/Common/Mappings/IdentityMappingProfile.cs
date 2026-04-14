using AutoMapper;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Domain.Entities;

namespace BitirmeProject.IdentityService.Application.Common.Mappings;

public sealed class IdentityMappingProfile : Profile
{
    public IdentityMappingProfile()
    {
        CreateMap<User, UserDto>();
        CreateMap<Role, RoleDto>();

        CreateMap<Organization, OrganizationDto>()
            .ForMember(d => d.MemberCount, o => o.MapFrom(s => s.Members.Count));

        CreateMap<InviteToken, InviteDto>()
            .ForMember(d => d.Role, o => o.MapFrom(s => s.Role.ToString()));
    }
}