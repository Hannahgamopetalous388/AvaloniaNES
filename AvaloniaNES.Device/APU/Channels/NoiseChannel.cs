using AvaloniaNES.Device.APU.Components;

namespace AvaloniaNES.Device.APU.Channels;

public class NoiseChannel
{
    public Envelope Envelope { get; } = new();
    public LengthCounter LengthCounter { get; } = new();

    private ushort _timer;
    private ushort _timerPeriod;
    private ushort _shiftRegister = 1;
    private bool _mode;

    private static readonly ushort[] TimerPeriodTable =
    {
        4, 8, 16, 32, 64, 96, 128, 160, 202, 254, 380, 508, 762, 1016, 2034, 4068
    };

    public void WriteControl(byte value)
    {
        LengthCounter.Halt = (value & 0x20) != 0;
        Envelope.Loop = (value & 0x20) != 0;
        Envelope.ConstantVolume = (value & 0x10) != 0;
        Envelope.VolumePeriod = (byte)(value & 0x0F);
    }

    public void WriteMode(byte value)
    {
        _mode = (value & 0x80) != 0;
        _timerPeriod = TimerPeriodTable[value & 0x0F];
    }

    public void WriteLength(byte value)
    {
        LengthCounter.Load((byte)(value >> 3));
        Envelope.Start = true;
    }

    public void Clock()
    {
        if (_timer == 0)
        {
            _timer = _timerPeriod;
            
            ushort feedback;
            if (_mode)
            {
                feedback = (ushort)((_shiftRegister & 0x01) ^ ((_shiftRegister >> 6) & 0x01));
            }
            else
            {
                feedback = (ushort)((_shiftRegister & 0x01) ^ ((_shiftRegister >> 1) & 0x01));
            }

            _shiftRegister >>= 1;
            _shiftRegister |= (ushort)(feedback << 14);
        }
        else
        {
            _timer--;
        }
    }

    public byte GetOutput()
    {
        if ((_shiftRegister & 0x01) == 0 && LengthCounter.Counter > 0)
        {
            return Envelope.Output;
        }
        return 0;
    }
    public void Reset()
    {
        _timer = 0;
        _timerPeriod = 0;
        _shiftRegister = 1;
        _mode = false;
        Envelope.Reset();
        LengthCounter.Reset();
    }
}
