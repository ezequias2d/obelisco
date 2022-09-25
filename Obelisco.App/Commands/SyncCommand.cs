using Microsoft.Extensions.Logging;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Obelisco.Commands
{
    [Command("sync", Description = "sync")]
    public class SyncCommand : ICommand
    {
        private readonly ILogger m_logger;
        private readonly ICliApplicationLifetime m_applicationLifetime;
        private readonly State m_state;

        public SyncCommand(
            State state,
            ILogger<QuitCommand> logger,
            ICliApplicationLifetime applicationLifetime)
        {
            m_applicationLifetime = applicationLifetime;
            m_logger = logger;
            m_state = state;
        }

        public async ValueTask ExecuteAsync(IConsole console)
        {
            // TODO
            await console.Error.WriteLineAsync("TODO!");
        }
    }
}