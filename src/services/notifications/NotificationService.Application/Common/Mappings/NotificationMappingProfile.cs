using AutoMapper;
using BitirmeProject.NotificationService.Application.DTOs;
using BitirmeProject.NotificationService.Domain.Entities;

namespace BitirmeProject.NotificationService.Application.Common.Mappings;

public sealed class NotificationMappingProfile : Profile
{
    public NotificationMappingProfile()
    {
        CreateMap<Notification, NotificationDto>();
    }
}
