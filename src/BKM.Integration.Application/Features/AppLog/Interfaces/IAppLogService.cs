using BKM.Integration.Application.Features.AppLog.DTOs;

namespace BKM.Integration.Application.Features.AppLog.Interfaces;

public interface IAppLogService
{
    Task<(IReadOnlyList<AppLogEntryDto> Items, int TotalCount)> QueryAsync(
        AppLogQueryRequest request,
        CancellationToken ct = default);
}
