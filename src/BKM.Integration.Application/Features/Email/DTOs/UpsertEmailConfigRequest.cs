using System.ComponentModel.DataAnnotations;
using BKM.Integration.Domain.Features.Email.Entities;

namespace BKM.Integration.Application.Features.Email.DTOs;

public sealed class UpsertEmailConfigRequest
{
    [Required, MaxLength(200)]
    public string      Name          { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string      Host          { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int         Port          { get; set; } = 465;

    public SmtpTlsMode TlsMode       { get; set; } = SmtpTlsMode.SslOnConnect;

    [Required, MaxLength(500)]
    public string      Username      { get; set; } = string.Empty;

    /// <summary>
    /// Plain-text password. Will be encrypted before persistence.
    /// Leave null or empty when updating other fields without changing the password.
    /// </summary>
    [MaxLength(1000)]
    public string?     Password      { get; set; }

    [Required, EmailAddress, MaxLength(500)]
    public string      FromAddress   { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string      FromName      { get; set; } = string.Empty;

    [Range(5, 120)]
    public int         TimeoutSeconds { get; set; } = 30;
}
