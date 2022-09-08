using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Obelisco
{
    [JsonConverterAttribute(typeof(InterfaceConverter<Message>))]
    public interface Message { }

#region Response messages
    public class Response : Message 
    { 
        public bool Ok { get; set; } = true;
        public string Message { get; set; } = string.Empty;
    }

    public class BlockResponse : Response
    {
        public Block Block { get; set; } = null!;
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
        public IEnumerable<PendingTransaction> Transactions { get; set; } = null!;
    }

    public class DifficultyReponse : Response
    {
        public int Difficulty { get; set; } = 2;
    }

    public class BalanceResponse : Response
    {
        public Balance Balance { get; set; } = null!;
    }
#endregion

#region Request message
    public interface Request : Message { }
    public class GetBlockRequest : Request
    {
        public string BlockID { get; set; } = null!;
    }

    public class GetNextBlockRequest : Request
    {
        public string BlockID { get; set; } = null!;
    }

    public class GetLastBlockRequest : Request { }

    public class GetGenesisRequest : Request { }

    public class PostBlockRequest : Request 
    {
        public Block Block { get; set; } = null!;
    }

    public class GetPendingTransactionsRequest : Request { }
    public class PostTransactionRequest : Request
    {
        public PendingTransaction Transaction { get; set; } = null!;
    }

    public class GetServersRequest : Request { }

    public class PostServersRequest : Request 
    {
        public IEnumerable<string> Servers { get; set; } = Enumerable.Empty<string>();
    }

    public class GetNodeTypeRequest : Request { }
    public class GetDifficultyRequest : Request { }
    public class GetBalanceRequest : Request { }
    public class GetServerAddressRequest : Request { }
#endregion
}