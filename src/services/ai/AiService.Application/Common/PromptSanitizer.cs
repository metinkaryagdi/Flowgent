namespace BitirmeProject.AiService.Application.Common;

public static class PromptSanitizer
{
    private const int MaxLength = 500;

    /// <summary>
    /// Sanitizes user-supplied text before embedding into an AI prompt.
    /// Truncates to 500 chars and wraps in [USER_DATA] delimiters.
    /// </summary>
    public static string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var sanitized = input.Trim();
        if (sanitized.Length > MaxLength)
            sanitized = sanitized[..MaxLength];

        return $"[USER_DATA]{sanitized}[/USER_DATA]";
    }
}
