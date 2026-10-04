using System.Text;
using Automator.Backend;

Console.InputEncoding = new UTF8Encoding(false);
Console.OutputEncoding = new UTF8Encoding(false);
Console.Error.WriteLine($"Automator.Backend process started; pid={Environment.ProcessId}");
await using var server = new BackendServer();
return await server.RunAsync();
