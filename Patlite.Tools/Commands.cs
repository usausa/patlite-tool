// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Patlite.Tools;

using System;

using Smart.CommandLine.Hosting;

public static class CommandBuilderExtensions
{
    public static void AddCommands(this ICommandBuilder commands)
    {
        commands.AddCommand<ClearCommand>();
        commands.AddCommand<WriteCommand>();
        commands.AddCommand<ReadCommand>();
    }
}

public abstract class CommandBase
{
    [Option<string>("--host", "-h", Description = "Host", IsRequired = true)]
    public string Host { get; set; } = default!;

    [Option<int>("--port", "-p", Description = "Port", DefaultValue = 10000)]
    public int Port { get; set; }
}

// Clear
[Command("clear", Description = "Clear")]
public sealed class ClearCommand : CommandBase, ICommandHandler
{
    public async ValueTask ExecuteAsync(CommandContext context)
    {
        using var client = new TcpPatliteClient();
        await client.ConnectAsync(IPAddress.Parse(Host), Port);

        var result = await client.WriteAsync(new PatliteStatus());
        Console.WriteLine(result ? "OK" : "NG");
    }
}

// Write
[Command("write", Description = "Write")]
public sealed class WriteCommand : CommandBase, ICommandHandler
{
    [Option<string>("--color", "-c", Description = "Color", DefaultValue = "")]
    public string Color { get; set; } = default!;

    [Option<bool>("--blink", "-b", Description = "Blink")]
    public bool Blink { get; set; }

    [Option<int>("--buzzer", "-b", Description = "Buzzer")]
    public int Buzzer { get; set; }

    [Option<int>("--wait", "-w", Description = "Wait")]
    public int Wait { get; set; }

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        var status = new PatliteStatus();
        if (Blink)
        {
            status.GreenBlink = Color.Contains('g', StringComparison.OrdinalIgnoreCase);
            status.YellowBlink = Color.Contains('y', StringComparison.OrdinalIgnoreCase);
            status.RedBlink = Color.Contains('r', StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            status.Green = Color.Contains('g', StringComparison.OrdinalIgnoreCase);
            status.Yellow = Color.Contains('y', StringComparison.OrdinalIgnoreCase);
            status.Red = Color.Contains('r', StringComparison.OrdinalIgnoreCase);
        }
        status.Buzzer = Buzzer;

        using var client = new TcpPatliteClient();
        await client.ConnectAsync(IPAddress.Parse(Host), Port);

        var result = await client.WriteAsync(status);
        Console.WriteLine(result ? "OK" : "NG");

        if (Wait > 0)
        {
            await Task.Delay(Wait);
            await client.WriteAsync(new PatliteStatus());
        }
    }
}

// Read
[Command("read", Description = "Read")]
public sealed class ReadCommand : CommandBase, ICommandHandler
{
    public async ValueTask ExecuteAsync(CommandContext context)
    {
        using var client = new TcpPatliteClient();
        await client.ConnectAsync(IPAddress.Parse(Host), Port);

        var status = new PatliteStatus();
        var result = await client.ReadAsync(status);

        Console.WriteLine(result ? "OK" : "NG");
        if (result)
        {
            if (status.GreenBlink)
            {
                Console.WriteLine("Green: Blink");
            }
            if (status.Green)
            {
                Console.WriteLine("Green: On");
            }
            if (status.YellowBlink)
            {
                Console.WriteLine("Yellow: Blink");
            }
            if (status.Yellow)
            {
                Console.WriteLine("Yellow: On");
            }
            if (status.RedBlink)
            {
                Console.WriteLine("Red: Blink");
            }
            if (status.Red)
            {
                Console.WriteLine("Red: On");
            }
            Console.WriteLine($"Buzzer: {status.Buzzer}");
        }
    }
}
