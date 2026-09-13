using BKM.Utility.Domain.Features.Email.Entities;
using BKM.Utility.Domain.Features.Email.Interfaces;
using BKM.Utility.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BKM.Utility.Infrastructure.Features.Email.Persistence;

public sealed class EmailTemplateRepository(EmailDbContext db) : IEmailTemplateRepository
{
    public Task<EmailTemplate?> GetByNameAsync(string name, CancellationToken ct = default)
        => db.EmailTemplates.AsNoTracking()
             .FirstOrDefaultAsync(x => x.Name == name && x.IsActive, ct);

    public Task<IReadOnlyList<EmailTemplate>> GetAllAsync(CancellationToken ct = default)
        => db.EmailTemplates.AsNoTracking()
             .OrderBy(x => x.Name)
             .ToListAsync(ct)
             .ContinueWith<IReadOnlyList<EmailTemplate>>(t => t.Result, ct);

    public Task<EmailTemplate?> GetByIdAsync(int id, CancellationToken ct = default)
        => db.EmailTemplates.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<EmailTemplate> SaveAsync(EmailTemplate template, CancellationToken ct = default)
    {
        if (template.Id == 0)
            db.EmailTemplates.Add(template);
        else
            db.EmailTemplates.Update(template);

        await db.SaveChangesAsync(ct);
        return template;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var rows = await db.EmailTemplates.Where(x => x.Id == id).ExecuteDeleteAsync(ct);
        return rows > 0;
    }
}
