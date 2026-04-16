namespace Patlite.Service;

internal sealed class PatliteSetting
{
    public string Host { get; set; } = default!;

    public int Port { get; set; }

    public bool Udp { get; set; }

    public int Timeout { get; set; }
}
