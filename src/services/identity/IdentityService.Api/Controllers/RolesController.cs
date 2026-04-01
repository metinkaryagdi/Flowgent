using AutoMapper;
using BitirmeProject.IdentityService.Application.Abstractions;
using BitirmeProject.IdentityService.Application.DTOs;
using BitirmeProject.IdentityService.Application.Features.Roles.Commands.CreateRole;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BitirmeProject.IdentityService.Api.Controllers;

[ApiController]
[Route("api/v1/identity/roles")]
[Authorize(Roles = "Admin")]
public sealed class RolesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRoleRepository _roleRepository;
    private readonly IMapper _mapper;

    public RolesController(IMediator mediator, IRoleRepository roleRepository, IMapper mapper)
    {
        _mediator = mediator;
        _roleRepository = roleRepository;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetAll(CancellationToken cancellationToken)
    {
        var roles = await _roleRepository.GetAllAsync(cancellationToken);
        return Ok(roles.Select(r => new RoleDto { Id = r.Id, Name = r.Name }).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<RoleDto>> Create([FromBody] CreateRoleCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
