namespace OpenClawNet.Storage.Entities;

public sealed class JobRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public string Status { get; set; } = "running";
    public string? Result { get; set; }
    public string? Error { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    public ScheduledJob Job { get; set; } = null!;
}
