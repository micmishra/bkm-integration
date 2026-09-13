namespace BKM.Utility.Domain.Features.Email.Entities;

/// <summary>
/// Named email template stored in dbo.EmailTemplates.
/// Supports {{Key}} token substitution in both subject and body.
/// Body may be plain text or HTML (IsHtml flag).
///
/// Example:
///   Subject: "Welcome {{FirstName}} to {{AppName}}"
///   Body:    "&lt;h1&gt;Hi {{FirstName}}&lt;/h1&gt;&lt;p&gt;Your code is {{Code}}&lt;/p&gt;"
/// </summary>
public sealed class EmailTemplate
{
    public int     Id          { get; set; }

    /// <summary>Unique key used to reference this template, e.g. "WelcomeUser", "PasswordReset"</summary>
    public string  Name        { get; set; } = string.Empty;

    public string  Subject     { get; set; } = string.Empty;
    public string  Body        { get; set; } = string.Empty;

    /// <summary>When true, Content-Type is text/html; otherwise text/plain.</summary>
    public bool    IsHtml      { get; set; } = true;
    public bool    IsActive    { get; set; } = true;
    public string? Description { get; set; }
    public DateTime CreatedAt  { get; set; }
    public DateTime UpdatedAt  { get; set; }
}
