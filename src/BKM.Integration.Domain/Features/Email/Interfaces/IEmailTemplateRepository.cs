using BKM.Integration.Domain.Features.Email.Entities;

namespace BKM.Integration.Domain.Features.Email.Interfaces;

public interface IEmailTemplateRepository
{
    Task<EmailTemplate?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<EmailTemplate>> GetAllAsync(CancellationToken ct = default);
    Task<EmailTemplate?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<EmailTemplate> SaveAsync(EmailTemplate template, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
