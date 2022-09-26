using System;

using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Obelisco;

public class BlockchainContext : DbContext
{
    public BlockchainContext(DbContextOptions<BlockchainContext> options) : base(options)
    {
        Database.EnsureCreated();
    }

    public DbSet<Block> Blocks { get; set; } = null!;
    public DbSet<Balance> Balances { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<PollTransaction> PollTransactions { get; set; } = null!;
    public DbSet<VoteTransaction> VoteTransactions { get; set; } = null!;
    public DbSet<TicketTransaction> TicketTransactions { get; set; } = null!;
    public DbSet<PollOption> PollOptions { get; set; } = null!;

    public Transaction? FindTransaction(Transaction transaction)
    {
        switch (transaction)
        {
            case PollTransaction pt:
                return PollTransactions.Find(pt.Signature);
            case VoteTransaction vt:
                return VoteTransactions.Find(vt.Signature);
            default:
                return null;
        }
    }

    public Transaction? FindTransaction(string signature)
    {
        Transaction? result = PollTransactions.Find(signature);
        if (result == null)
            result = VoteTransactions.Find(signature);
        return result;
    }

    public async ValueTask AddTransactionAsync(Transaction transaction)
    {
        switch (transaction)
        {
            case PollTransaction pt:
                await PollTransactions.AddAsync(pt);
                break;
            case VoteTransaction vt:
                await VoteTransactions.AddAsync(vt);
                break;
            default:
                break;
        }
    }

    public void UpdateTransaction(Transaction transaction)
    {
        switch (transaction)
        {
            case PollTransaction pt:
                PollTransactions.Update(pt);
                break;
            case VoteTransaction vt:
                VoteTransactions.Update(vt);
                break;
            default:
                throw new InvalidOperationException();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PollOption>()
            .HasOne<PollTransaction>(op => op.Poll)
            .WithMany(pl => pl.Options)
            .HasForeignKey(op => op.PollId);
    }
}
