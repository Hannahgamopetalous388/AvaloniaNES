using AvaloniaNES.Device.APU.Components;

namespace AvaloniaNES.Device.APU.Channels;

public class TriangleChannel
{
    public LengthCounter LengthCounter { get; } = new();
    
    private ushort _timer;
    private ushort _timerPeriod;
    private byte _linearCounter;
    private byte _linearCounterReloadValue;
    private bool _linearCounterReload;
    private bool _controlFlag; // Also acts as Length Counter Halt
    private byte _sequencer;

    private static readonly byte[] SequenceTable =
    {
        15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0,
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15
    };

    public void WriteControl(byte value)
    {
        _controlFlag = (value & 0x80) != 0;
        LengthCounter.Halt = _controlFlag;
        _linearCounterReloadValue = (byte)(value & 0x7F);
    }

    public void WriteTimerLow(byte value)
    {
        _timerPeriod = (ushort)((_timerPeriod & 0xFF00) | value);
    }

    public void WriteTimerHigh(byte value)
    {
        _timerPeriod = (ushort)((_timerPeriod & 0x00FF) | ((value & 0x07) << 8));
        LengthCounter.Load((byte)(value >> 3));
        _linearCounterReload = true;
    }

    public void ClockLinearCounter()
    {
        if (_linearCounterReload)
        {
            _linearCounter = _linearCounterReloadValue;
        }
        else if (_linearCounter > 0)
        {
            _linearCounter--;
        }

        if (!_controlFlag)
        {
            _linearCounterReload = false;
        }
    }

    public void Clock()
    {
        if (_timer == 0)
        {
            _timer = _timerPeriod;
            if (_linearCounter > 0 && LengthCounter.Counter > 0)
            {
                _sequencer = (byte)((_sequencer + 1) & 0x1F);
            }
        }
        else
        {
            _timer--;
        }
    }

    public byte GetOutput()
    {
        return SequenceTable[_sequencer];
    }
    public void Reset()
    {
        _timer = 0;
        _timerPeriod = 0;
        _linearCounter = 0;
        _linearCounterReloadValue = 0;
        _linearCounterReload = false;
        _controlFlag = false;
        _sequencer = 0;
        LengthCounter.Reset();
    }
}
