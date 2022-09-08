using Microsoft.Extensions.Logging;
using Typin;
using Typin.Attributes;
using Typin.Console;
using Ninja.WebSockets;

namespace Obelisco.Commands
{
    [Command("quit", Description = "Quits the application")]
    public class QuitCommand : ICommand
    {
        private readonly ILogger m_logger;
        private readonly P2PServer? m_server;
        private readonly P2PServer? m_client;
        private readonly BlockchainContext m_context;
        private readonly ICliApplicationLifetime m_applicationLifetime;

        public QuitCommand(
            BlockchainContext context, 
            ILogger<QuitCommand> logger, 
            ICliApplicationLifetime applicationLifetime,
            P2PServer? server = null,
            P2PClient? client = null)
        {
            m_applicationLifetime = applicationLifetime;
            m_server = server;
            m_logger = logger;
            m_context = context;
        }

        public async ValueTask ExecuteAsync(IConsole console)
        {
            await m_context.SaveChangesAsync();
            
            m_server?.Dispose();
            m_client?.Dispose();

            m_applicationLifetime.RequestStop();
        }
    }
}