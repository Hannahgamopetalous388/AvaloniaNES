using AvaloniaNES.Device.APU.Components;

namespace AvaloniaNES.Device.APU.Channels;

public class PulseChannel
{
    public Envelope Envelope { get; } = new();
    public LengthCounter LengthCounter { get; } = new();
    public Sweeper Sweeper { get; }

    private ushort _timer;
    private ushort _timerPeriod;
    private byte _dutyMode;
    private byte _dutyValue;
    
    private static readonly byte[][] DutyTable =
    {
        new byte[] { 0, 1, 0, 0, 0, 0, 0, 0 }, // 12.5%
        new byte[] { 0, 1, 1, 0, 0, 0, 0, 0 }, // 25%
        new byte[] { 0, 1, 1, 1, 1, 0, 0, 0 }, // 50%
        new byte[] { 1, 0, 0, 1, 1, 1, 1, 1 }  // 25% negated
    };

    public PulseChannel(bool isPulse1)
    {
        Sweeper = new Sweeper(isPulse1);
    }

    public void WriteControl(byte value)
    {
        _dutyMode = (byte)((value >> 6) & 0x03);
        LengthCounter.Halt = (value & 0x20) != 0;
        Envelope.Loop = (value & 0x20) != 0;
        Envelope.ConstantVolume = (value & 0x10) != 0;
        Envelope.VolumePeriod = (byte)(value & 0x0F);
    }

    public void WriteSweep(byte value)
    {
        Sweeper.Enabled = (value & 0x80) != 0;
        Sweeper.Period = (byte)((value >> 4) & 0x07);
        Sweeper.Negate = (value & 0x08) != 0;
        Sweeper.Shift = (byte)(value & 0x07);
        Sweeper.Reload = true;
    }

    public void WriteTimerLow(byte value)
    {
        _timerPeriod = (ushort)((_timerPeriod & 0xFF00) | value);
        Sweeper.UpdateChannelPeriod(_timerPeriod);
    }

    public void WriteTimerHigh(byte value)
    {
        _timerPeriod = (ushort)((_timerPeriod & 0x00FF) | ((value & 0x07) << 8));
        LengthCounter.Load((byte)(value >> 3));
        _timer = _timerPeriod;
        Envelope.Start = true;
        _dutyValue = 0;
        Sweeper.UpdateChannelPeriod(_timerPeriod);
    }

    public void Clock()
    {
        if (_timer == 0)
        {
            _timer = _timerPeriod;
            _dutyValue = (byte)((_dutyValue + 1) & 0x07);
        }
        else
        {
            _timer--;
        }
    }

    public void ClockSweep()
    {
        _timerPeriod = Sweeper.Clock(_timerPeriod);
    }

    public byte GetOutput()
    {
        if (LengthCounter.Counter == 0 || Sweeper.Mute || _timerPeriod < 8)
        {
            return 0;
        }

        if (DutyTable[_dutyMode][_dutyValue] == 0)
        {
            return 0;
        }

        return Envelope.Output;
    }
    public void Reset()
    {
        _timer = 0;
        _timerPeriod = 0;
        _dutyMode = 0;
        _dutyValue = 0;
        Envelope.Reset();
        LengthCounter.Reset();
        Sweeper.Reset();
    }
}
