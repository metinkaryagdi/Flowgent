using BitirmeProject.IdentityService.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityService.Application.Features.Users.Commands.DeleteUser;

    public sealed record DeleteUserCommand(
        Guid UserId
        ) : IRequest<Unit>;

