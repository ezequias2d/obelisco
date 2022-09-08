
using Microsoft.EntityFrameworkCore;

namespace Obelisco
{
    public class BlockchainContext : DbContext
    {
        public BlockchainContext(DbContextOptions<BlockchainContext> options) : base(options)
        {
            Database.EnsureCreated();
        }
        
        public DbSet<Block> Blocks { get; set; } = null!;
        public DbSet<Balance> Balances { get; set; } = null!;
        public DbSet<CompleteTransaction> CompleteTransactions { get; set; } = null!;
        public DbSet<PendingTransaction> PendingTransactions { get; set; } = null!;
    }
}