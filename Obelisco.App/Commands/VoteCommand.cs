using System.Globalization;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Obelisco.App.Commands;

[Command("vote", Description = "Spend a ticket and vote.")]
public class VoteCommand : ICommand
{
    private readonly State m_state;

    public VoteCommand(State state)
    {
        m_state = state;
    }

    [CommandParameter(0, Description = "The account password to use encrypt private key.")]
    public string Password { get; set; } = null!;

    [CommandParameter(1, Description = "Account private key file.")]
    public string AccountFile { get; set; } = null!;

    public async ValueTask ExecuteAsync(IConsole console)
    {
        // get client object
        var token = console.GetCancellationToken();
        if (!m_state.GetClient(console, out var client))
            return;

        // login with password and account file
        if (!console.Login(Password, AccountFile, out var account))
            return;

        // get balance for account
        var publicKey = Convert.ToBase64String(account.PublicKey);
        var balance = await client.QueryBalance(publicKey, token);

        var polls = new List<PollTransaction?>();
        foreach (var ticket in balance.UnusedTickets.OrderBy(t => t.Index))
        {
            var p = await client.QueryTransaction<PollTransaction>(ticket.Poll, false, token);
            if (p is null)
                await console.Error.WriteLineAsync($"There was no way to retrieve the poll '{ticket.Poll}' from the ticket '{ticket.Signature}'.");
            polls.Add(p);
        }

        console.WithForegroundColor(ConsoleColor.Yellow, async c =>
        {
            for (var i = 0; i < balance.UnusedTickets.Count; i++)
            {
                var ticket = balance.UnusedTickets[i];
                var poll = polls[ticket.Index];

                var title = poll?.Title ?? "**Undefined**";
                var description = poll?.Description ?? "**Undefined**";

                await c.Output.WriteLineAsync($"{i}:\t{title}\n\t{description}\n");

                if (poll != null)
                    for (var j = 0; j < poll.Options.Count; j++)
                    {
                        var option = poll.Options[j];
                        await c.Output.WriteLineAsync($"\t{j}:\t{option.Title}\n\t\t{option.Description}\n");
                    }

                await c.Output.WriteLineAsync();
            }
        });

        if (!console.Read("Poll", (str) =>
        {
            if (!int.TryParse(
                str,
                NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
                CultureInfo.InvariantCulture,
                out var index))
            {
                console.Error.WriteLine("The input index is not a integer number");
                return (false, 0);
            }

            if (index < 0 || index >= polls.Count)
            {
                console.Error.WriteLine($"The input index is not between 0 and {balance.Polls.Count}.");
                return (false, index);
            }

            if (polls[index] == null)
            {
                console.Error.WriteLine($"Invalid poll.");
                return (false, index);
            }

            return (true, index);
        }, out var pollIndex))
            return;

        var poll = polls[pollIndex]!;    // this is not null, because selection before

        if (!console.Read("Option", (str) =>
        {
            if (!int.TryParse(
                str,
                NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
                CultureInfo.InvariantCulture,
                out var index))
            {
                console.Error.WriteLine("The input index is not a integer number");
                return (false, 0);
            }

            if (index < 0 || index >= poll.Options.Count)
            {
                console.Error.WriteLine($"The input index is not between 0 and {poll.Options.Count}.");
                return (false, index);
            }

            return (true, index);
        }, out var optionIndex))
            return;

        var option = poll.Options[optionIndex];

        await console.Output.WriteLineAsync("Creating vote transaction...");
        var transaction = new VoteTransaction(balance.Nonce++, poll.Signature, option.Index, DateTimeOffset.UtcNow);

        await console.Output.WriteLineAsync("Signing vote transaction...");
        transaction.Sign(account);

        await console.Output.WriteLineAsync("Broadcast vote transaction...");
        await client.BroadcastTransation(transaction, token);

        await console.Output.WriteLineAsync("Finish broadcast.");
    }
}
