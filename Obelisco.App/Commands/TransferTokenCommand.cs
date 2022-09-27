using System.Globalization;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Obelisco.App.Commands;

[Command("transfer", Description = "Create a account identity.")]
public class TransferTokenCommand : ICommand
{
    private readonly State m_state;

    public TransferTokenCommand(State state)
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
        if (!m_state.TryGetClient(console, out var client))
            return;

        // login with password and account file
        if (!console.Login(Password, AccountFile, out var account))
            return;

        // get balance for account
        var publicKey = Convert.ToBase64String(account.PublicKey);
        var balance = await client.QueryBalance(publicKey, token);

        console.ForegroundColor = ConsoleColor.Yellow;
        console.WithForegroundColor(ConsoleColor.Yellow, async c =>
        {
            for (var i = 0; i < balance.Polls.Count; i++)
            {
                var poll = balance.Polls[i];
                await c.Output.WriteLineAsync($"{i}:\t{poll.Title}\n\t{poll.Description}\n");

                foreach (var option in poll.Options)
                    await c.Output.WriteLineAsync($"\t{i}:\t{option.Title}\n\t\t{option.Description}\n");

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

            if (index < 0 || index >= balance.Polls.Count)
            {
                console.Error.WriteLine($"The input index is not between 0 and {balance.Polls.Count}.");
                return (false, index);
            }

            return (true, index);
        }, out var pollIndex))
            return;

        if (!console.Read("Target", (str) =>
        {
            try
            {
                var key = Convert.FromBase64String(str);
            }
            catch (Exception ex)
            {
                console.Error.WriteLine(ex.Message);
                return (false, string.Empty);
            }
            return (true, str);
        }, out var target))
            return;


        await console.Output.WriteLineAsync("Creating ticket transaction...");
        var pollId = balance.Polls[pollIndex].Signature;
        var transaction = new TicketTransaction(balance.Nonce++, target, pollId, DateTimeOffset.UtcNow);

        await console.Output.WriteLineAsync("Signing ticket transaction...");
        transaction.Sign(account);

        await console.Output.WriteLineAsync("Broadcast ticket transaction...");
        await client.BroadcastTransation(transaction, token);

        await console.Output.WriteLineAsync("Finish broadcast.");
    }
}
