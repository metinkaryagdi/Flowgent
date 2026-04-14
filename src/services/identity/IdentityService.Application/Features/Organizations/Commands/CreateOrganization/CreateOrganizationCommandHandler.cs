using AutoMapper;
using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Domain.Entities;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Organizations.Commands.CreateOrganization;

public sealed class CreateOrganizationCommandHandler
    : IRequestHandler<CreateOrganizationCommand, OrganizationDto>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateOrganizationCommandHandler(
        IOrganizationRepository organizationRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _organizationRepository = organizationRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<OrganizationDto> Handle(
        CreateOrganizationCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.CreatedByUserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        var organization = new Organization(request.Name, request.CreatedByUserId);
        await _organizationRepository.AddAsync(organization, cancellationToken);

        // Mark this as the user's active org so login/refresh restores the correct context
        user.SetActiveOrganization(organization.Id);
        await _userRepository.UpdateAsync(user, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<OrganizationDto>(organization);
    }
}
