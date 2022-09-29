using System.Linq;
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
        private readonly Blockchain m_blockchain;

        public SyncCommand(
            Blockchain blockchain,
            State state,
            ILogger<QuitCommand> logger,
            ICliApplicationLifetime applicationLifetime)
        {
            m_blockchain = blockchain;
            m_applicationLifetime = applicationLifetime;
            m_logger = logger;
            m_state = state;
        }

        public async ValueTask ExecuteAsync(IConsole console)
        {
            if (!m_state.GetServer(console, out var server))
                return;

            await console.Output.WriteLineAsync("Start sync");

            var token = console.GetCancellationToken();
            Block? nextLastBlock = await server.QueryLastBlock(token);

            if (nextLastBlock == null)
            {
                await console.Output.WriteLineAsync("Somehow went wrong, cannot query current last block.");
                return;
            }

            var start = DateTimeOffset.UtcNow;

            do
            {
                await console.Output.WriteLineAsync("Trying to retrive next block.");

                var tasks = server.Connections.Where(p2p => p2p.IsFullNode)
                .Select(p2p =>
                {
                    var task = p2p.GetNextBlock(nextLastBlock.Hash, token).AsTask();
                    console.Output.WriteLine($"Request next block to {p2p.IP} server.");
                    return task.ContinueWith(task =>
                    {
                        task.Wait();
                        var block = task.Result;
                        if (block != null)
                            console.Output.WriteLine($"{p2p.IP} server returns the block {block.Hash}.");
                        return block;
                    });
                })
                .ToArray();

                Task.WaitAll(tasks);

                IEnumerable<Block> results = tasks
                    .Where(task => task.Result != null && task.Result.IsValid(m_blockchain.Difficulty))
                    .Select(task => task.Result);

                nextLastBlock = results.GroupBy(block => block.Hash)
                            .OrderBy(group => group.Count())
                            .Select(group => group.FirstOrDefault())
                            .FirstOrDefault();

                if (nextLastBlock != null)
                {
                    await console.Output.WriteLineAsync($"Add block {nextLastBlock.Hash}");
                    await m_blockchain.PostBlock(nextLastBlock, token);
                }
            } while (nextLastBlock != null);

            var end = DateTimeOffset.UtcNow;
            var timeSpent = (end - start).TotalSeconds;
            var strTime = timeSpent.ToString("0.00") + " seconds";

            if (timeSpent <= 1f)
                strTime = (timeSpent * 1000).ToString("0.00") + " miliseconds";
            else if (timeSpent >= 60)
            {
                var minutes = (int)(timeSpent / 60);
                var seconds = timeSpent - minutes * 60;
                strTime = minutes.ToString() + " minutes and " + seconds.ToString("0.00") + " seconds.";
            }

            await console.Output.WriteLineAsync($"Sync complete in {strTime}!");
        }
    }
}