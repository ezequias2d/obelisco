using Microsoft.Extensions.Logging;
using Typin;
using Typin.Attributes;
using Typin.Console;
using Typin.Schemas;
using Typin.Modes;
using Ninja.WebSockets;

namespace Obelisco.Commands
{
    [Command(Description = "Create and start the server or client.")]
    public class DefaultCommand : ICommand
    {
        private readonly ILogger m_logger;
        private readonly IEnumerable<string> SupportedSubProtocols = new string[] { "obelisco" };
        private readonly ICliApplicationLifetime m_applicationLifetime;
        private readonly IWebSocketServerFactory m_socketServerFactory;
        private readonly IWebSocketClientFactory m_socketClientFactory;
        private readonly ILoggerFactory m_loggerFactory;
        private readonly Blockchain m_blockchain;
        private State m_state;
        public DefaultCommand(
            IWebSocketServerFactory socketServerFactory,
            IWebSocketClientFactory socketClientFactory,
            ILoggerFactory loggerFactory,
            Blockchain blockchain,
            State state,
            ICliApplicationLifetime applicationLifetime)
        {
            m_socketServerFactory = socketServerFactory;
            m_socketClientFactory = socketClientFactory;
            m_loggerFactory = loggerFactory;
            m_blockchain = blockchain;
            m_state = state;
            m_logger = loggerFactory.CreateLogger<DefaultCommand>();
            m_applicationLifetime = applicationLifetime;
        }

        [CommandOption("server", 's')]
        public bool Server { get; set; }

        [CommandOption("port", 'p', FallbackVariableName = "1234")]
        public int Port { get; set; }

        public async ValueTask ExecuteAsync(IConsole console)
        {
            if (m_state.Client != null || m_state.Server != null)
                throw new InvalidOperationException();

            m_applicationLifetime.RequestMode<InteractiveMode>();
            if (Server)
            {
                m_state.Server = new Server(m_socketServerFactory, m_socketClientFactory, m_loggerFactory, SupportedSubProtocols, m_blockchain);
                m_state.Client = m_state.Server;

                m_state.Server.Listen(Port, CancellationToken.None);

                var str = $"Listening on {m_state.Server.LocalEndpoint}";
                console.Output.WriteLine(str);
                console.Output.WriteLine("Use 'server quit' to exit."); // TODO
            }
            else
            {
                m_state.Server = null;
                m_state.Client = new Client(m_socketClientFactory, m_loggerFactory.CreateLogger<Client>());

                console.Output.WriteLine("Client started. Use 'connect' command to connect to a server.");
            }

        }
    }
}