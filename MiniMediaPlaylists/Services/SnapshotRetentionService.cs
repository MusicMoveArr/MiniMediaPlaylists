using System.Globalization;
using MiniMediaPlaylists.Models;

namespace MiniMediaPlaylists.Services;

public class SnapshotRetentionService
{
    public List<Guid> GetSnapshotsToRemove(List<SnapshotModel> snapshots, RetentionPolicy policy)
    {
        List<Guid> snapshotsToKeep = new List<Guid>();
        
        //hourly
        snapshotsToKeep.AddRange(GetSnapshotsToKeep(snapshots, snapshotsToKeep, policy.KeepHourly, 
            x => new DateTime(x.CreatedAt.Year, x.CreatedAt.Month, x.CreatedAt.Day, x.CreatedAt.Hour, 0, 0)));
        
        //daily
        snapshotsToKeep.AddRange(GetSnapshotsToKeep(snapshots, snapshotsToKeep, policy.KeepDaily, 
            x => x.CreatedAt.Date));
        
        //weekly
        snapshotsToKeep.AddRange(GetSnapshotsToKeep(snapshots, snapshotsToKeep, policy.KeepWeekly, 
            x => ISOWeek.GetYear(x.CreatedAt) * 100 + ISOWeek.GetWeekOfYear(x.CreatedAt)));
        
        //monthly
        snapshotsToKeep.AddRange(GetSnapshotsToKeep(snapshots, snapshotsToKeep, policy.KeepMonthly, 
            x => x.CreatedAt.Year * 100 + x.CreatedAt.Month));
        
        //yearly
        snapshotsToKeep.AddRange(GetSnapshotsToKeep(snapshots, snapshotsToKeep, policy.KeepYearly, 
            x => x.CreatedAt.Year));
        
        snapshotsToKeep.AddRange(snapshots
            .Where(s => !s.IsComplete)
            .Where(s => DateTime.Now - s.CreatedAt < TimeSpan.FromDays(30))
            .Select(s => s.Id));

        return snapshots
            .Select(s => s.Id)
            .Except(snapshotsToKeep)
            .ToList();
    }

    private List<Guid> GetSnapshotsToKeep<GroupKey>(
        List<SnapshotModel> snapshots, 
        List<Guid> snapshotsToIgnore,
        int limit, 
        Func<SnapshotModel, GroupKey> group)
    {
        return snapshots
            .Where(s => s.IsComplete)
            .Where(s => !snapshotsToIgnore.Contains(s.Id))
            .OrderByDescending(s => s.CreatedAt)
            .GroupBy(s => group.Invoke(s))
            .OrderByDescending(s => s.Key)
            .Take(limit)
            .Select(s => s.First())
            .Select(s => s.Id)
            .ToList();
    }
}