using System.Diagnostics.CodeAnalysis;
using Typin.Console;

namespace Obelisco.App;

public static class Utils
{
	public static bool ConfirmMessage(this IConsole console, string message)
	{	
		if (console.Read($"{message} [y/N]", (str) => 
		{
			var yesOptions = new[] { "yes", "y" };
			var noOptions = new[] { "no", "n" };
			
			if (yesOptions.Any(s => str.Equals(s, StringComparison.InvariantCultureIgnoreCase)))
				return (true, true);
			else if (noOptions.Any(s => str.Equals(s, StringComparison.InvariantCultureIgnoreCase)))
				return (true, false);
			
			return (false, false);
		}, out var result))
		{
			return result;
		}
		return false;
	}
	
	public static bool Read<TResult>(this IConsole console, string message, Func<string, (bool, TResult)> func, [NotNullWhen(true)]out TResult? result) 
	{
		for (var i = 0; i < 3; i++)
		{
			console.BackgroundColor = ConsoleColor.White;
			console.ForegroundColor = ConsoleColor.Red;
			console.Output.Write($"{message}: ");
			console.ResetColor();

			var line = console.Input.ReadLine();
			if (line == null)
				continue;
				
			var r = func(line);
			if (r.Item1 && r.Item2 != null)
			{
				result = r.Item2;
				return true;
			}
		}
		
		result = default;
		return false;
	}
}
