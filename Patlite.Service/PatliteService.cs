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

    private CancellationTokenSource? currentCts;
    private bool isDisposed;

    public PatliteService(ILogger<PatliteService> log, IOptions<PatliteSetting> options)
    {
        this.log = log;
        setting = options.Value;

        _ = Task.Run(ProcessWorkItemsAsync);
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            shutdownCts.Cancel();
            shutdownCts.Dispose();
            signal.Dispose();
            currentCts?.Dispose();

            isDisposed = true;
        }
    }

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

            using var client = new TcpPatliteClient();
            await client.ConnectAsync(IPAddress.Parse(setting.Host), setting.Port);

            var result = await client.WriteAsync(status);
            if (!result)
            {
                log.InfoWriteSuccess(color, blink, wait);
            }
            else
            {
                log.WarnWriteFailed(color, blink, wait);
            }

            if (wait > 0)
            {
                await Task.Delay(wait, cancel);
                await client.WriteAsync(new PatliteStatus());
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
                    try
                    {
                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(shutdownCts.Token);
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
