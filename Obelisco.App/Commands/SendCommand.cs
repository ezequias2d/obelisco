using Microsoft.Extensions.Logging;
using Typin;
using Typin.Attributes;
using Typin.Console;
using Ninja.WebSockets;

namespace Obelisco.Commands
{
    [Command("send", Description = "send message")]
    public class SendCommand : ICommand
    {
        private readonly ILogger m_logger;
        private readonly BlockchainContext m_context;
        private readonly ICliApplicationLifetime m_applicationLifetime;

        public SendCommand(
            BlockchainContext context, 
            ILogger<QuitCommand> logger, 
            ICliApplicationLifetime applicationLifetime)
        {
            m_applicationLifetime = applicationLifetime;
            m_logger = logger;
            m_context = context;
        }

        public async ValueTask ExecuteAsync(IConsole console)
        {
            await m_context.SaveChangesAsync();
            

            m_applicationLifetime.RequestStop();
        }
    }
}