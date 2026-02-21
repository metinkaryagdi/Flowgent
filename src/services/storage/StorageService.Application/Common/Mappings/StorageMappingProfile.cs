using AutoMapper;
using BitirmeProject.StorageService.Application.DTOs;
using BitirmeProject.StorageService.Domain.Entities;

namespace BitirmeProject.StorageService.Application.Common.Mappings;

public sealed class StorageMappingProfile : Profile
{
    public StorageMappingProfile()
    {
        CreateMap<StoredFile, StoredFileDto>();
    }
}
