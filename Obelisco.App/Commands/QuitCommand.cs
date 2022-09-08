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
        private readonly BlockchainContext m_context;
        private readonly ICliApplicationLifetime m_applicationLifetime;
        private readonly State m_state;

        public QuitCommand(
            BlockchainContext context, 
            ILogger<QuitCommand> logger, 
            ICliApplicationLifetime applicationLifetime,
            State state)
        {
            m_applicationLifetime = applicationLifetime;
            m_logger = logger;
            m_context = context;
            m_state = state;
        }

        public async ValueTask ExecuteAsync(IConsole console)
        {
            await m_context.SaveChangesAsync();

            m_state.Client?.Dispose();

            m_applicationLifetime.RequestStop();
        }
    }
}