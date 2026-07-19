namespace Patlite.Client;

#pragma warning disable IDE0230
public sealed class TcpPatliteClient : IPatliteClient
{
    private Socket? socket;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(3);

    public async ValueTask ConnectAsync(IPAddress address, int port)
    {
        socket?.Dispose();
        socket = null;

        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        using var cts = new CancellationTokenSource(Timeout);
        await socket.ConnectAsync(address, port, cts.Token);
    }

    public void Dispose()
    {
        socket?.Dispose();
        socket = null;
    }

    public async ValueTask<bool> WriteAsync(PatliteStatus status)
    {
        if (socket is null)
        {
            return false;
        }

        var buffer = ArrayPool<byte>.Shared.Rent(256);
        try
        {
            using var cts = new CancellationTokenSource(Timeout);
            await socket.SendAsync(new[] { (byte)'W', PatliteStatusHelper.ToByte(status) }, SocketFlags.None, cts.Token);

            var received = 0;
            while (received < 3)
            {
                var receive = await socket.ReceiveAsync(buffer.AsMemory(received, buffer.Length - received), SocketFlags.None, cts.Token);
                if (receive == 0)
                {
                    return false;
                }

                received += receive;
            }

            return buffer.AsSpan(0, 3).SequenceEqual("ACK"u8);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public async ValueTask<bool> ReadAsync(PatliteStatus status)
    {
        if (socket is null)
        {
            return false;
        }

        var buffer = ArrayPool<byte>.Shared.Rent(256);
        try
        {
            using var cts = new CancellationTokenSource(Timeout);
            await socket.SendAsync(new[] { (byte)'R' }, SocketFlags.None, cts.Token);

            var received = 0;
            while (received < 2)
            {
                var receive = await socket.ReceiveAsync(buffer.AsMemory(received, buffer.Length - received), SocketFlags.None, cts.Token);
                if (receive == 0)
                {
                    return false;
                }

                received += receive;
            }

            var success = buffer[0] == (byte)'R';
            if (success)
            {
                PatliteStatusHelper.FromByte(status, buffer[1]);
            }

            return success;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
#pragma warning restore IDE0230
