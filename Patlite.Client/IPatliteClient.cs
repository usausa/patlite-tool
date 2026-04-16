namespace Patlite.Client;

public interface IPatliteClient : IDisposable
{
    TimeSpan Timeout { get; set; }

    ValueTask ConnectAsync(IPAddress address, int port);

    ValueTask<bool> WriteAsync(PatliteStatus status);

    ValueTask<bool> ReadAsync(PatliteStatus status);
}
