using BKM.Integration.Domain.Features.Email.Entities;

namespace BKM.Integration.Domain.Features.Email.Interfaces;

public interface IEmailConfigRepository
{
    Task<EmailConfig?> GetActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<EmailConfig>> GetAllAsync(CancellationToken ct = default);
    Task<EmailConfig?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<EmailConfig> SaveAsync(EmailConfig config, CancellationToken ct = default);
    Task<bool> SetActiveAsync(int id, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
