namespace AvaloniaNES.Device.APU.Components;

public class LengthCounter
{
    public byte Counter { get; private set; }
    public bool Halt { get; set; }
    public bool Enabled { get; private set; }

    // Length counter lookup table
    private static readonly byte[] LengthTable =
    {
        10, 254, 20, 2, 40, 4, 80, 6, 160, 8, 60, 10, 14, 12, 26, 14,
        12, 16, 24, 18, 48, 20, 96, 22, 192, 24, 72, 26, 16, 28, 32, 30
    };

    public void SetEnabled(bool enabled)
    {
        Enabled = enabled;
        if (!enabled)
        {
            Counter = 0;
        }
    }

    public void Load(byte value)
    {
        if (Enabled)
        {
            Counter = LengthTable[value >> 3];
        }
    }

    public void Clock()
    {
        if (!Halt && Counter > 0)
        {
            Counter--;
        }
    }
    public void Reset()
    {
        Counter = 0;
        Halt = false;
        Enabled = false;
    }
}
