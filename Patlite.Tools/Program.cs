using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

// ReSharper disable UseObjectOrCollectionInitializer
#pragma warning disable IDE0017

#pragma warning disable CA1852

var rootCommand = new RootCommand("PATLITE client");
rootCommand.AddGlobalOption(new Option<string>(["--host", "-h"], "Host") { IsRequired = true });
rootCommand.AddGlobalOption(new Option<int>(["--port", "-p"], static () => 10000, "Port"));

// Clear
var clearCommand = new Command("clear", "Clear");
clearCommand.Handler = CommandHandler.Create(static async (IConsole console, string host, int port) =>
{
    using var client = new TcpPatliteClient();
    await client.ConnectAsync(IPAddress.Parse(host), port);

    var result = await client.WriteAsync(new PatliteStatus());
    console.WriteLine(result ? "OK" : "NG");
});
rootCommand.Add(clearCommand);

// Write
var writeCommand = new Command("write", "Write");
writeCommand.AddOption(new Option<string>(["--color", "-c"], static () => string.Empty, "Color"));
writeCommand.AddOption(new Option<bool>(["--blink", "-b"], static () => false, "Blink"));
writeCommand.AddOption(new Option<int>(["--buzzer", "-z"], static () => 0, "Buzzer"));
writeCommand.AddOption(new Option<int>(["--wait", "-w"], static () => 0, "Wait"));
writeCommand.Handler = CommandHandler.Create(static async (IConsole console, string host, int port, string color, bool blink, int buzzer, int wait) =>
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
readCommand.Handler = CommandHandler.Create(static async (IConsole console, string host, int port) =>
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
