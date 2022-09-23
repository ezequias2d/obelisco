using System.Text;
using Microsoft.Extensions.Logging;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Obelisco.App.Commands;

[Command("create account", Description = "Create a account identity.")]
public class CreateAccountCommand : ICommand
{
	private readonly ILogger m_logger;
	private readonly ICliApplicationLifetime m_applicationLifetime;
	private readonly State m_state;

	public CreateAccountCommand(
		State state,
		ILogger<CreateAccountCommand> logger,
		ICliApplicationLifetime applicationLifetime)
	{
		m_applicationLifetime = applicationLifetime;
		m_logger = logger;
		m_state = state;
	}
	
	[CommandParameter(0, Description = "The account password to use encrypt private key.")]
	public string Password { get; set; }
	
	[CommandParameter(1, Description = "Output file.")]
	public string OutputFile { get; set; }

	public async ValueTask ExecuteAsync(IConsole console)
	{
		var account = new Account();
		var privateKey = account.ExportEncryptedPkcs8PrivateKey(Encoding.UTF8.GetBytes(Password));
			
		var overwrite = false;
		if (File.Exists(OutputFile))
		{
			overwrite = console.ConfirmMessage("Do you want to overwrite existing file?");
		}
		else 
			overwrite = true;
		
		if (overwrite)
		{
			using var file = File.Create(OutputFile);
			
			
		
			file.Write(privateKey, 0, privateKey.Length);
			
			console.BackgroundColor = ConsoleColor.White;
			console.ForegroundColor = ConsoleColor.Red;
			var path = Path.GetFullPath(OutputFile);
			console.Output.Write($"Account created on file {path}.");
			console.ResetColor();
			console.Output.WriteLine();
		}
		else 
		{
			console.BackgroundColor = ConsoleColor.White;
			console.ForegroundColor = ConsoleColor.Yellow;
			var path = Path.GetFullPath(OutputFile);
			console.Output.Write("Account not created.");
			console.ResetColor();	
			console.Output.WriteLine();
		}
	}
}
