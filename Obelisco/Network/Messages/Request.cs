using System.Collections.Generic;
using System.Linq;

namespace Obelisco.Network;

public interface Request : Message { }
public class GetBlockRequest : Request
{
    public string BlockID { get; set; } = null!;
}

public class GetNextBlockRequest : Request
{
    public string BlockID { get; set; } = null!;
}

public class GetAllBlocksRequest : Request { }

public class GetLastBlockRequest : Request { }

public class GetGenesisRequest : Request { }

public class PostBlockRequest : Request
{
    public Block Block { get; set; } = null!;
}

public class GetPendingTransactionsRequest : Request { }
public class PostTransactionRequest : Request
{
    public Transaction Transaction { get; set; } = null!;
}

public class GetServersRequest : Request { }

public class PostServersRequest : Request
{
    public IEnumerable<string> Servers { get; set; } = Enumerable.Empty<string>();
}

public class GetNodeTypeRequest : Request { }
public class GetDifficultyRequest : Request { }
public class GetBalanceRequest : Request
{
    public string Owner { get; set; } = null!;
}
public class GetServerAddressRequest : Request { }

public class GetTransactionRequest : Request
{
    public string TransactionSignature { get; set; } = null!;
    public bool Pending { get; set; } = false;
}