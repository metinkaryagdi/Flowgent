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

    public CreateOrganizationCommandHandler(
        IOrganizationRepository organizationRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _organizationRepository = organizationRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrganizationDto> Handle(
        CreateOrganizationCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.CreatedByUserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        var existing = await _organizationRepository.GetByUserIdAsync(request.CreatedByUserId, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException("User already belongs to an organization.");

        var organization = new Organization(request.Name, request.CreatedByUserId);
        await _organizationRepository.AddAsync(organization, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new OrganizationDto
        {
            Id = organization.Id,
            Name = organization.Name,
            CreatedByUserId = organization.CreatedByUserId,
            CreatedAt = organization.CreatedAt,
            MemberCount = organization.Members.Count
        };
    }
}
