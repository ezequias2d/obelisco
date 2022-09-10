using Microsoft.Extensions.Logging;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Obelisco.Commands
{
    [Command("connect", Description = "Create and start the server.")]
    public class ConnectCommand : ICommand
    {
        private readonly ILogger m_logger;
        private readonly IEnumerable<string> SupportedSubProtocols = new string[] { "obelisco" };
        private readonly ICliApplicationLifetime m_applicationLifetime;
        private readonly State m_state;

        public ConnectCommand(
            State state,
            ILogger<ConnectCommand> logger,
            ICliApplicationLifetime applicationLifetime)
        {
            m_state = state;

            m_logger = logger;
            m_applicationLifetime = applicationLifetime;
            Uri = "ws://127.0.0.1:1234/obelisco";
        }
        
        [CommandParameter(0, Description = "The uri of server to connect.")]
        public string Uri { get; set; }

        public async ValueTask ExecuteAsync(IConsole console)
        {
            if (m_state.Client == null)
            {
                console.Output.WriteLine("You must init a server or client before connect.");
                return;
            }
            var uri = new Uri("ws://" + Uri);
            await m_state.Client.Connect(uri, CancellationToken.None);

            var str = $"Connected to {Uri}";
            console.Output.WriteLine(str);
        }
    }
}