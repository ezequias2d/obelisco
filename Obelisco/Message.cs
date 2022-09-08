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

    public class PostTransactionRequest : Request
    {
        public PendingTransaction Transaction { get; set; } = null!;
    }

    public class GetServersRequest : Request { }
#endregion
}