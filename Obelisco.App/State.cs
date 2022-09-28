using System.Diagnostics.CodeAnalysis;
using Typin.Console;

namespace Obelisco
{
    public class State
    {
        private Client? m_client;
        private Server? m_server;
        public Client? Client { set => m_client = value; }
        public Server? Server { set => m_server = value; }

        public bool GetClient(IConsole console, [NotNullWhen(true)] out Client? client)
        {
            if (!TryGetClient(out client))
            {
                console.Error.WriteLine("You must init a server or client before connect.");
                return false;
            }
            return true;
        }

        public bool TryGetClient([NotNullWhen(true)] out Client? client)
        {
            if (m_client == null)
            {
                client = null;
                return false;
            }
            client = m_client;
            return true;
        }

        public bool GetServer(IConsole console, [NotNullWhen(true)] out Server? server)
        {
            if (!TryGetServer(out server))
            {
                console.Error.WriteLine("You must init a server or client before connect.");
                return false;
            }
            return true;
        }

        public bool TryGetServer([NotNullWhen(true)] out Server? server)
        {
            if (m_server == null)
            {
                server = null;
                return false;
            }
            server = m_server;
            return true;
        }
    }
}