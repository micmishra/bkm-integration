using BKM.Utility.Application.Features.AppLog.DTOs;

namespace BKM.Utility.Application.Features.AppLog.Interfaces;

public interface IAppLogService
{
    Task<(IReadOnlyList<AppLogEntryDto> Items, int TotalCount)> QueryAsync(
        AppLogQueryRequest request,
        CancellationToken ct = default);
}
