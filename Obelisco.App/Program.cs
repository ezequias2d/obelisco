using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ninja.WebSockets;
using Obelisco;
using Typin;
using Typin.Console;
using Typin.Modes;

namespace Obelisco {
    public class Program {
        public static async Task<int> Main(string[] args) =>
            await new CliApplicationBuilder()
                .UseStartup<CliStartup>()
                .Build()
                .RunAsync();
        
    }
}

