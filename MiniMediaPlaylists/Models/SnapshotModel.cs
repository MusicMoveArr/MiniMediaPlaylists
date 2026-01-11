
namespace MiniMediaPlaylists.Models;

public class SnapshotModel
{
    public Guid Id { get; set; }
    public Guid ServerId { get; set; }
    public string ServiceName { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsComplete { get; set; }
}