namespace Patlite.Client;

#pragma warning disable IDE0230
public sealed class TcpPatliteClient : IDisposable
{
    private Socket? socket;

    public async ValueTask ConnectAsync(IPAddress address, int port)
    {
        socket?.Close();
        socket?.Dispose();

        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await socket.ConnectAsync(address, port);
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

            var receive = await socket.ReceiveAsync(buffer, SocketFlags.None);
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

            var receive = await socket.ReceiveAsync(buffer, SocketFlags.None);
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
