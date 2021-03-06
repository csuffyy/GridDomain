namespace GridDomain.Tools.Persistence.SqlPersistence
{
    public interface IAkkaSqlPersistenceContext : System.IDisposable
    {
        System.Data.Entity.DbSet<JournalItem> Journal { get; set; } // JournalEntry
        System.Data.Entity.DbSet<Metadata> Metadatas { get; set; } // Metadata
        System.Data.Entity.DbSet<SnapshotItem> Snapshots { get; set; } // Snapshots

        int SaveChanges();
        System.Threading.Tasks.Task<int> SaveChangesAsync();
        System.Threading.Tasks.Task<int> SaveChangesAsync(System.Threading.CancellationToken cancellationToken);
    }
}