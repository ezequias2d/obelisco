using System.Collections.Generic;
using System.Linq;

namespace Obelisco.Network;

public class Response : Message
{
    public bool Ok { get; set; } = true;
    public string Message { get; set; } = string.Empty;
}

public class BlockResponse : Response
{
    public Block Block { get; set; } = null!;
}

public class BlocksResponse : Response
{
    public IEnumerable<Block> Blocks { get; set; } = Enumerable.Empty<Block>();
}

public class ServersResponse : Response
{
    public IEnumerable<string> Servers { get; set; } = Enumerable.Empty<string>();
}

public class NodeTypeResponse : Response
{
    public bool IsFullNode { get; set; }
}

public class ServerAddressResponse : Response
{
    public string Uri { get; set; } = null!;
}

public class PendingTransactionsResponse : Response
{
    public IEnumerable<Transaction> Transactions { get; set; } = null!;
}

public class DifficultyReponse : Response
{
    public int Difficulty { get; set; } = 2;
}

public class BalanceResponse : Response
{
    public Balance Balance { get; set; } = null!;
}

public class TransactionResponse : Response
{
    public Transaction Transaction { get; set; } = null!;
}