namespace BitirmeProject.IdentityService.Application.Options;

/// <summary>
/// Public-facing web application settings.
/// </summary>
public sealed class AppOptions
{
    public const string SectionName = "App";

    /// <summary>
    /// Origin of the web client, used to build links that are emailed to users
    /// (invite acceptance, email verification).
    ///
    /// This is read from configuration only and never from a request body: a link in an
    /// outbound email is attacker-controlled otherwise, which turns the service into a
    /// phishing relay carrying its own From address.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:5174";
}
