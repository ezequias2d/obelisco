using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Typin;
using Typin.Modes;
using Typin.Console;
using Ninja.WebSockets;
using Microsoft.EntityFrameworkCore;

namespace Obelisco
{
    public class CliStartup : ICliStartup
    {
        private readonly IEnumerable<string> SupportedSubProtocols = new string[] { "obelisco" };

        public void Configure(CliApplicationBuilder app)
        {
            app.AddCommandsFromThisAssembly()
                .UseConsole<SystemConsole>()
                .ConfigureLogging((loggingBuilder) => loggingBuilder.AddConsole())
                .UseInteractiveMode()
                .UseTitle("Obelisco")
                .UseExecutableName("Obelisco")
                .UseVersionText("v0.1")
                .UseDescription("Obelisco Blockchain");
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<BlockchainContext>(options =>
                options.UseSqlite($"Data Source={AppDomain.CurrentDomain.BaseDirectory}/data.db"));
            services.AddSingleton<IWebSocketServerFactory, WebSocketServerFactory>();
            services.AddSingleton<IWebSocketClientFactory, WebSocketClientFactory>();
            services.AddSingleton<Blockchain>();
            services.AddSingleton<State>();
        }
    }
}