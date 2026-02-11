using BitirmeProject.ProjectService.Application.DTOs;
using BitirmeProject.ProjectService.Application.Features.Projects.Commands.CreateProject;
using BitirmeProject.ProjectService.Application.Features.Projects.Queries.GetProjectById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BitirmeProject.ProjectService.Api.Controllers;

[ApiController]
[Route("api/v1/projects")]
[Authorize]
public sealed class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProjectsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] CreateProjectCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDto>> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetProjectByIdQuery(id));
        if (result is null)
            return NotFound();

        return Ok(result);
    }
}

