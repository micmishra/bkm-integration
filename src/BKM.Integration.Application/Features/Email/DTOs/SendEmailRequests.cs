using System.ComponentModel.DataAnnotations;

namespace BKM.Integration.Application.Features.Email.DTOs;

/// <summary>Send using a named template with token substitution.</summary>
public sealed class SendTemplateEmailRequest
{
    [Required, EmailAddress]
    public string To           { get; set; } = string.Empty;

    [Required]
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>Key/value pairs to substitute {{Key}} tokens in subject and body.</summary>
    public Dictionary<string, string> Variables { get; set; } = [];
}

/// <summary>Send a one-off email without a template.</summary>
public sealed class SendRawEmailRequest
{
    [Required, EmailAddress]
    public string To      { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Body    { get; set; } = string.Empty;

    public bool   IsHtml  { get; set; } = false;
}
