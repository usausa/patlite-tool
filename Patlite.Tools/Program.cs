using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

using Patlite.Client;

#pragma warning disable CA1852

var rootCommand = new RootCommand("PATLITE client");
rootCommand.AddGlobalOption(new Option<string>(new[] { "--host", "-h" }, "Host") { IsRequired = true });
rootCommand.AddGlobalOption(new Option<int>(new[] { "--port", "-p" }, static () => 10000, "Port"));
// TODO Type

// Write
var writeCommand = new Command("write", "Write");
rootCommand.AddGlobalOption(new Option<string>(new[] { "--color", "-c" }, static () => string.Empty, "Color"));
rootCommand.AddGlobalOption(new Option<bool>(new[] { "--blink", "-b" }, static () => false, "Blink"));
// TODO Buzzer
writeCommand.Handler = CommandHandler.Create(async (IConsole console, string host, int port, string color, bool blink) =>
{
    var status = new PatliteStatus();
    if (blink)
    {
        status.GreenBlink = color.Contains("g", StringComparison.OrdinalIgnoreCase);
        status.YellowBlink = color.Contains("y", StringComparison.OrdinalIgnoreCase);
        status.RedBlink = color.Contains("r", StringComparison.OrdinalIgnoreCase);
    }
    else
    {
        status.Green = color.Contains("g", StringComparison.OrdinalIgnoreCase);
        status.Yellow = color.Contains("y", StringComparison.OrdinalIgnoreCase);
        status.Red = color.Contains("r", StringComparison.OrdinalIgnoreCase);
    }

    using var client = new TcpPatliteClient();
    await client.ConnectAsync(IPAddress.Parse(host), port);

    var result = await client.WriteAsync(status);
    console.WriteLine(result ? "OK" : "NG");
});
rootCommand.Add(writeCommand);

// TODO Read

return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
