namespace GridDomain.Tools.Persistence.SqlPersistence
{
  
    public class FakeAkkaSqlPersistenceContext : IAkkaSqlPersistenceContext
    {
        public System.Data.Entity.DbSet<JournalItem> Journal { get; set; }
        public System.Data.Entity.DbSet<Metadata> Metadatas { get; set; }
        public System.Data.Entity.DbSet<SnapshotItem> Snapshots { get; set; }

        public FakeAkkaSqlPersistenceContext()
        {
            Journal = new FakeDbSet<JournalItem>("PersistenceId", "SequenceNr");
            Metadatas = new FakeDbSet<Metadata>("PersistenceId", "SequenceNr");
            Snapshots = new FakeDbSet<SnapshotItem>("PersistenceId", "SequenceNr");
        }

        public int SaveChangesCount { get; private set; }
        public int SaveChanges()
        {
            ++SaveChangesCount;
            return 1;
        }

        public System.Threading.Tasks.Task<int> SaveChangesAsync()
        {
            ++SaveChangesCount;
            return System.Threading.Tasks.Task<int>.Factory.StartNew(() => 1);
        }

        public System.Threading.Tasks.Task<int> SaveChangesAsync(System.Threading.CancellationToken cancellationToken)
        {
            ++SaveChangesCount;
            return System.Threading.Tasks.Task<int>.Factory.StartNew(() => 1, cancellationToken);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}