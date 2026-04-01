using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.DTOs;
using AutoMapper;
using MediatR;

namespace BitirmeProject.IdentityService.Application.Features.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler
    : IRequestHandler<UpdateUserCommand, UserDto?>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<UserDto?> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        // 1) Kullanıcıyı bul
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (user is null)
            return null;

        // 2) Username unique mi? (kendisi hariç)
        if (await _userRepository.ExistsByUserNameAsync(
                request.UserName,
                request.Id,
                cancellationToken))
        {
            throw new InvalidOperationException("Username already exists.");
        }

        // 3) Email unique mi? (kendisi hariç)
        if (await _userRepository.ExistsByEmailAsync(
                request.Email,
                request.Id,
                cancellationToken))
        {
            throw new InvalidOperationException("Email already exists.");
        }

        // 4) Domain methods ile güncelle (ToLowerInvariant normalizasyonu korunur)
        user.SetUserName(request.UserName);
        user.SetEmail(request.Email);

        // 5) Repo + UnitOfWork ile kaydet
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 6) DTO döndür
        return _mapper.Map<UserDto>(user);
    }
}
