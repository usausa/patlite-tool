namespace Patlite.Client;

#pragma warning disable IDE0230
public sealed class UdpPatliteClient : IPatliteClient
{
    private Socket? socket;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(3);

    public ValueTask ConnectAsync(IPAddress address, int port)
    {
        socket?.Close();
        socket?.Dispose();

        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Connect(new IPEndPoint(address, port));
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        socket?.Close();
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
            await socket.SendAsync(new[] { (byte)'W', PatliteStatusHelper.ToByte(status) }, SocketFlags.None);

            using var cts = new CancellationTokenSource(Timeout);
            var receive = await socket.ReceiveAsync(buffer, SocketFlags.None, cts.Token);
            return receive >= 3 && buffer.AsSpan(0, 3).SequenceEqual("ACK"u8);
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
            await socket.SendAsync(new[] { (byte)'R' }, SocketFlags.None);

            using var cts = new CancellationTokenSource(Timeout);
            var receive = await socket.ReceiveAsync(buffer, SocketFlags.None, cts.Token);
            var success = receive >= 2 && buffer[0] == (byte)'R';
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
