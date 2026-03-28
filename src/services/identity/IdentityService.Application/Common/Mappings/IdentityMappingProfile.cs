using AutoMapper;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Application.Features.Users.Commands.UpdateUser;
using BitirmeProject.IdentityService.Domain.Entities;

namespace BitirmeProject.IdentityService.Application.Common.Mappings;

public sealed class IdentityMappingProfile : Profile
{
    public IdentityMappingProfile()
    {
        CreateMap<User, UserDto>();
        CreateMap<UpdateUserCommand, User>()
            .ForMember(d => d.Id, opt => opt.Ignore());

        CreateMap<Role, RoleDto>();
    }
}