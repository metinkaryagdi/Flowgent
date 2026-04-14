using BitirmeProject.AiService.Application.DTOs;
using MediatR;

namespace BitirmeProject.AiService.Application.Features.Chat.Commands.SendMessage;

public sealed record SendMessageCommand(
    Guid ProjectId,
    Guid UserId,
    Guid OrganizationId,
    Guid? SessionId,
    string Message
) : IRequest<ChatResponseDto>;
