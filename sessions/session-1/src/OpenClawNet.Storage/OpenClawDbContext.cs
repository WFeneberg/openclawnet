using Microsoft.EntityFrameworkCore;
using OpenClawNet.Storage.Entities;

namespace OpenClawNet.Storage;

public class OpenClawDbContext : DbContext
{
    public OpenClawDbContext(DbContextOptions<OpenClawDbContext> options) : base(options) { }
    
    public DbSet<ChatSession> Sessions => Set<ChatSession>();
    public DbSet<ChatMessageEntity> Messages => Set<ChatMessageEntity>();
    public DbSet<SessionSummary> Summaries => Set<SessionSummary>();
    public DbSet<ToolCallRecord> ToolCalls => Set<ToolCallRecord>();
    public DbSet<ScheduledJob> Jobs => Set<ScheduledJob>();
    public DbSet<JobRun> JobRuns => Set<JobRun>();
    public DbSet<ProviderSetting> ProviderSettings => Set<ProviderSetting>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatSession>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasMany(s => s.Messages).WithOne(m => m.Session).HasForeignKey(m => m.SessionId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(s => s.Summaries).WithOne(s => s.Session).HasForeignKey(s => s.SessionId).OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<ChatMessageEntity>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasIndex(m => new { m.SessionId, m.OrderIndex });
        });
        
        modelBuilder.Entity<SessionSummary>(e =>
        {
            e.HasKey(s => s.Id);
        });
        
        modelBuilder.Entity<ToolCallRecord>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.SessionId);
        });
        
        modelBuilder.Entity<ScheduledJob>(e =>
        {
            e.HasKey(j => j.Id);
            e.HasMany(j => j.Runs).WithOne(r => r.Job).HasForeignKey(r => r.JobId).OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<JobRun>(e =>
        {
            e.HasKey(r => r.Id);
        });
        
        modelBuilder.Entity<ProviderSetting>(e =>
        {
            e.HasKey(s => s.Key);
        });
    }
}
