using Microsoft.Extensions.Logging;
using Typin;
using Typin.Attributes;
using Typin.Console;
using Typin.Schemas;
using Typin.Modes;
using Ninja.WebSockets;
using Microsoft.Extensions.DependencyInjection;

namespace Obelisco.Commands
{
    [Command(Description = "Create and start the server or client.")]
    public class DefaultCommand : ICommand
    {
        private readonly ILogger m_logger;
        private readonly IEnumerable<string> SupportedSubProtocols = new string[] { "obelisco" };
        private readonly ICliApplicationLifetime m_applicationLifetime;
        private readonly ILoggerFactory m_loggerFactory;
        private readonly IServiceProvider m_serviceProvider;
        private State m_state;
        public DefaultCommand(
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider,
            State state,
            ICliApplicationLifetime applicationLifetime)
        {
            m_loggerFactory = loggerFactory;
            m_state = state;
            m_logger = loggerFactory.CreateLogger<DefaultCommand>();
            m_applicationLifetime = applicationLifetime;
            m_serviceProvider = serviceProvider;
        }

        [CommandOption("server", 's')]
        public bool Server { get; set; }

        [CommandOption("port", 'p', FallbackVariableName = "1234")]
        public int Port { get; set; }

        public async ValueTask ExecuteAsync(IConsole console)
        {
            await Task.Yield();

            if (m_state.Client != null || m_state.Server != null)
                throw new InvalidOperationException();

            m_applicationLifetime.RequestMode<InteractiveMode>();
            if (Server)
            {
                var socketServerFactory = m_serviceProvider.GetRequiredService<IWebSocketServerFactory>();
                var socketClientFactory = m_serviceProvider.GetRequiredService<IWebSocketClientFactory>();
                var blockchain = m_serviceProvider.GetRequiredService<Blockchain>();

                m_state.Server = new Server(socketServerFactory, socketClientFactory, m_loggerFactory, SupportedSubProtocols, blockchain);
                m_state.Client = m_state.Server;

                m_state.Server.Listen(Port, CancellationToken.None);

                var str = $"Listening on {m_state.Server.LocalEndpoint}";
                console.Output.WriteLine(str);
                console.Output.WriteLine("Use 'server quit' to exit."); // TODO
            }
            else
            {
                var socketClientFactory = m_serviceProvider.GetRequiredService<IWebSocketClientFactory>();

                m_state.Server = null;
                m_state.Client = new Client(socketClientFactory, m_loggerFactory.CreateLogger<Client>());

                console.Output.WriteLine("Client started. Use 'connect' command to connect to a server.");
            }

        }
    }
}