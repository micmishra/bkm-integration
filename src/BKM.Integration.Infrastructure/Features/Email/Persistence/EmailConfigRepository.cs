using BKM.Integration.Domain.Features.Email.Entities;
using BKM.Integration.Domain.Features.Email.Interfaces;
using BKM.Integration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BKM.Integration.Infrastructure.Features.Email.Persistence;

public sealed class EmailConfigRepository(AppDbContext db) : IEmailConfigRepository
{
    public Task<EmailConfig?> GetActiveAsync(CancellationToken ct = default)
        => db.EmailConfigs.AsNoTracking()
             .FirstOrDefaultAsync(x => x.IsActive, ct);

    public Task<IReadOnlyList<EmailConfig>> GetAllAsync(CancellationToken ct = default)
        => db.EmailConfigs.AsNoTracking()
             .OrderByDescending(x => x.IsActive)
             .ThenBy(x => x.Name)
             .ToListAsync(ct)
             .ContinueWith<IReadOnlyList<EmailConfig>>(t => t.Result, ct);

    public Task<EmailConfig?> GetByIdAsync(int id, CancellationToken ct = default)
        => db.EmailConfigs.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<EmailConfig> SaveAsync(EmailConfig config, CancellationToken ct = default)
    {
        if (config.Id == 0)
            db.EmailConfigs.Add(config);
        else
            db.EmailConfigs.Update(config);

        await db.SaveChangesAsync(ct);
        return config;
    }

    public async Task<bool> SetActiveAsync(int id, CancellationToken ct = default)
    {
        // Deactivate all, then activate the target — one transaction
        await db.EmailConfigs
            .Where(x => x.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false), ct);

        var rows = await db.EmailConfigs
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, true), ct);

        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var rows = await db.EmailConfigs.Where(x => x.Id == id).ExecuteDeleteAsync(ct);
        return rows > 0;
    }
}
