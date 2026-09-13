using BKM.Utility.Domain.Features.Email.Entities;

namespace BKM.Utility.Domain.Features.Email.Interfaces;

public interface IEmailAuditLogRepository
{
    Task WriteAsync(EmailAuditLog entry, CancellationToken ct = default);
    Task<IReadOnlyList<EmailAuditLog>> QueryAsync(
        string? toAddress, bool? success, DateTime? from, DateTime? to,
        int page, int pageSize, CancellationToken ct = default);
    Task<int> CountAsync(
        string? toAddress, bool? success, DateTime? from, DateTime? to,
        CancellationToken ct = default);
}
