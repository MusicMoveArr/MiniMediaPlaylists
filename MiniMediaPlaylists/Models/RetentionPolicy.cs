namespace MiniMediaPlaylists.Models;

public class RetentionPolicy
{
    public int KeepHourly { get; set; }
    public int KeepDaily { get; set; }
    public int KeepWeekly { get; set; }
    public int KeepMonthly { get; set; }
    public int KeepYearly { get; set; }
}