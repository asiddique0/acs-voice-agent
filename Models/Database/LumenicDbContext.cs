namespace LumenicBackend.Models.Database
{
    public class LumenicDbContext : DbContext
    {
        public LumenicDbContext(DbContextOptions<LumenicDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AgentTool>()
                .HasOne(at => at.Agent)
                .WithMany(a => a.AgentTools)
                .HasForeignKey(at => at.AgentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AgentTool>()
                .HasOne(at => at.Tool)
                .WithMany(t => t.AgentTools)
                .HasForeignKey(at => at.ToolId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public DbSet<Organization> Organizations { get; set; }
        public DbSet<Number> Numbers { get; set; }
        public DbSet<Tool> Tools { get; set; }
        public DbSet<Agent> Agents { get; set; }
        public DbSet<KnowledgeBase> KnowledgeBases { get; set; }
        public DbSet<AgentTool> AgentTools { get; set; }
        public DbSet<CallLedger> CallLedgers { get; set; }
    }
}
