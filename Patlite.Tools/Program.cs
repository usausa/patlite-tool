using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

using Patlite.Client;

// ReSharper disable UseObjectOrCollectionInitializer
#pragma warning disable IDE0017

#pragma warning disable CA1852

var rootCommand = new RootCommand("PATLITE client");
rootCommand.AddGlobalOption(new Option<string>(new[] { "--host", "-h" }, "Host") { IsRequired = true });
rootCommand.AddGlobalOption(new Option<int>(new[] { "--port", "-p" }, static () => 10000, "Port"));
// TODO Protocol type

// Clear
var clearCommand = new Command("clear", "Clear");
clearCommand.Handler = CommandHandler.Create(async (IConsole console, string host, int port) =>
{
    using var client = new TcpPatliteClient();
    await client.ConnectAsync(IPAddress.Parse(host), port);

    var result = await client.WriteAsync(new PatliteStatus());
    console.WriteLine(result ? "OK" : "NG");
});
rootCommand.Add(clearCommand);

// Write
var writeCommand = new Command("write", "Write");
rootCommand.AddGlobalOption(new Option<string>(new[] { "--color", "-c" }, static () => string.Empty, "Color"));
rootCommand.AddGlobalOption(new Option<bool>(new[] { "--blink", "-b" }, static () => false, "Blink"));
rootCommand.AddGlobalOption(new Option<int>(new[] { "--buzzer", "-z" }, static () => 0, "Buzzer"));
rootCommand.AddGlobalOption(new Option<int>(new[] { "--wait", "-w" }, static () => 0, "Wait"));
writeCommand.Handler = CommandHandler.Create(async (IConsole console, string host, int port, string color, bool blink, int buzzer, int wait) =>
{
    var status = new PatliteStatus();
    if (blink)
    {
        status.GreenBlink = color.Contains('g', StringComparison.OrdinalIgnoreCase);
        status.YellowBlink = color.Contains('y', StringComparison.OrdinalIgnoreCase);
        status.RedBlink = color.Contains('r', StringComparison.OrdinalIgnoreCase);
    }
    else
    {
        status.Green = color.Contains('g', StringComparison.OrdinalIgnoreCase);
        status.Yellow = color.Contains('y', StringComparison.OrdinalIgnoreCase);
        status.Red = color.Contains('r', StringComparison.OrdinalIgnoreCase);
    }
    status.Buzzer = buzzer;

    using var client = new TcpPatliteClient();
    await client.ConnectAsync(IPAddress.Parse(host), port);

    var result = await client.WriteAsync(status);
    console.WriteLine(result ? "OK" : "NG");

    if (wait > 0)
    {
        await Task.Delay(wait);
        await client.WriteAsync(new PatliteStatus());
    }
});
rootCommand.Add(writeCommand);

// Read
var readCommand = new Command("read", "Read");
readCommand.Handler = CommandHandler.Create(async (IConsole console, string host, int port) =>
{
    using var client = new TcpPatliteClient();
    await client.ConnectAsync(IPAddress.Parse(host), port);

    var status = new PatliteStatus();
    var result = await client.ReadAsync(status);

    console.WriteLine(result ? "OK" : "NG");
    if (result)
    {
        if (status.GreenBlink)
        {
            console.WriteLine("Green: Blink");
        }
        if (status.Green)
        {
            console.WriteLine("Green: On");
        }
        if (status.YellowBlink)
        {
            console.WriteLine("Yellow: Blink");
        }
        if (status.Yellow)
        {
            console.WriteLine("Yellow: On");
        }
        if (status.RedBlink)
        {
            console.WriteLine("Red: Blink");
        }
        if (status.Red)
        {
            console.WriteLine("Red: On");
        }
        console.WriteLine($"Buzzer: {status.Buzzer}");
    }
});
rootCommand.Add(readCommand);

return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
