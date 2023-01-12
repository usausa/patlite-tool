namespace Patlite.Client;

internal static class PatliteStatusHelper
{
    public static byte ToByte(PatliteStatus status)
    {
        var b = default(byte);

        if (status.GreenBlink)
        {
            b |= 0b10000000;
        }
        if (status.YellowBlink)
        {
            b |= 0b01000000;
        }
        if (status.RedBlink)
        {
            b |= 0b00100000;
        }

        if ((status.Buzzer & 0b00000010) != 0)
        {
            b |= 0b00010000;
        }
        if ((status.Buzzer & 0b00000001) != 0)
        {
            b |= 0b00001000;
        }

        if (status.Green)
        {
            b |= 0b00000100;
        }
        if (status.Yellow)
        {
            b |= 0b00000010;
        }
        if (status.Red)
        {
            b |= 0b00000001;
        }

        return b;
    }

    public static void FromByte(PatliteStatus status, byte b)
    {
        status.GreenBlink = (b & 0b10000000) != 0;
        status.YellowBlink = (b & 0b01000000) != 0;
        status.RedBlink = (b & 0b00100000) != 0;

        status.Buzzer = 0;
        status.Buzzer += (b & 0b00010000) != 0 ? 2 : 0;
        status.Buzzer += (b & 0b00001000) != 0 ? 1 : 0;

        status.Green = (b & 0b00000100) != 0;
        status.Yellow = (b & 0b00000010) != 0;
        status.Red = (b & 0b00000001) != 0;
    }
}
