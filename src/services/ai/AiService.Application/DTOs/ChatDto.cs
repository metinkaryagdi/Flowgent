namespace BitirmeProject.AiService.Application.DTOs;

public sealed class ChatResponseDto
{
    public Guid SessionId { get; init; }
    public string Message { get; init; } = null!;
    public string Answer { get; init; } = null!;
    public DateTime Timestamp { get; init; }
}

public sealed class ChatHistoryDto
{
    public Guid SessionId { get; init; }
    public List<ChatTurnDto> Turns { get; init; } = new();
}

public sealed class ChatTurnDto
{
    public string Prompt { get; init; } = null!;
    public string Answer { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
}
