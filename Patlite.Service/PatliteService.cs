namespace Patlite.Service;

using System.Collections.Concurrent;

using Microsoft.Extensions.Options;

internal sealed class PatliteService : IDisposable
{
    private readonly ILogger<PatliteService> log;

    private readonly PatliteSetting setting;

    private readonly ConcurrentQueue<Func<CancellationToken, Task>> workItems = new();
    private readonly SemaphoreSlim signal = new(0);
    private readonly CancellationTokenSource shutdownCts = new();

    private readonly Lock sync = new();

    private readonly Task processTask;

    private CancellationTokenSource? currentCts;
    private bool isDisposed;

    public PatliteService(ILogger<PatliteService> log, IOptions<PatliteSetting> options)
    {
        this.log = log;
        setting = options.Value;

        processTask = Task.Run(ProcessWorkItemsAsync);
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;

            shutdownCts.Cancel();
            lock (sync)
            {
                currentCts?.Cancel();
            }

            try
            {
                processTask.Wait(TimeSpan.FromSeconds(5));
            }
            catch (AggregateException)
            {
            }

            lock (sync)
            {
                currentCts?.Dispose();
            }

            shutdownCts.Dispose();
            signal.Dispose();
        }
    }

    private IPatliteClient CreateClient()
    {
        var client = (IPatliteClient)(setting.Udp ? new UdpPatliteClient() : new TcpPatliteClient());
        if (setting.Timeout > 0)
        {
            client.Timeout = TimeSpan.FromMilliseconds(setting.Timeout);
        }
        return client;
    }

    private static IPAddress ResolveHost(string host) =>
        IPAddress.TryParse(host, out var address)
            ? address
            : Array.Find(Dns.GetHostAddresses(host), static x => x.AddressFamily == AddressFamily.InterNetwork)
                ?? throw new InvalidOperationException($"Host has no IPv4 address. host=[{host}]");

    public void Write(string color, bool blink, int wait)
    {
        workItems.Enqueue(async cancel =>
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

            using var client = CreateClient();
            await client.ConnectAsync(ResolveHost(setting.Host), setting.Port);

            var result = await client.WriteAsync(status);
            if (result)
            {
                log.InfoWriteSuccess(color, blink, wait);
            }
            else
            {
                log.WarnWriteFailed(color, blink, wait);
            }

            if (wait > 0)
            {
                try
                {
                    await Task.Delay(wait, cancel);
                }
                finally
                {
                    await client.WriteAsync(new PatliteStatus());
                }
            }
        });
        signal.Release();
    }

    public void Cancel()
    {
        lock (sync)
        {
            workItems.Clear();
            currentCts?.Cancel();
        }
    }

    private async Task ProcessWorkItemsAsync()
    {
        try
        {
            while (!shutdownCts.IsCancellationRequested)
            {
                await signal.WaitAsync(shutdownCts.Token).ConfigureAwait(false);

                if (workItems.TryDequeue(out var workItem))
                {
#pragma warning disable CA1031
                    var cts = CancellationTokenSource.CreateLinkedTokenSource(shutdownCts.Token);
                    try
                    {
                        lock (sync)
                        {
                            currentCts = cts;
                        }

                        await workItem(cts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        log.ErrorUnhandledException(ex);
                    }
                    finally
                    {
                        lock (sync)
                        {
                            currentCts = null;
                        }

                        cts.Dispose();
                    }
#pragma warning restore CA1031
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
