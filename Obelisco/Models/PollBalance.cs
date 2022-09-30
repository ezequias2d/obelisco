using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Obelisco;

public class PollBalance
{
    public PollBalance()
    {

    }

    public PollBalance(PollTransaction poll)
    {
        Poll = poll.Signature;
        Options = poll.Options.Select(op => new PollOptionBalance() { Index = op.Index, Votes = 0 }).ToList();
    }

    public PollBalance(PollBalance pollBalance)
    {
        Poll = pollBalance.Poll;
        Options = pollBalance.Options.ToArray();
    }

    [Key]
    public string Poll { get; set; }

    public virtual IList<PollOptionBalance> Options { get; set; }
}
