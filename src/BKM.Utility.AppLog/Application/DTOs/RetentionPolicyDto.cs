namespace BKM.Utility.Application.Features.AppLog.DTOs;

public sealed class RetentionPolicyDto
{
    public int      Id                     { get; init; }
    public string?  Feature                { get; init; }
    public int      DbRetentionDays        { get; init; }
    public int      FileRetentionDays      { get; init; }
    public string?  Description            { get; init; }
    public DateTime LastUpdatedAt          { get; init; }
    public DateTime? LastPurgedAt          { get; init; }
    public long     LastPurgeDeletedCount  { get; init; }
}
