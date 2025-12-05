using AvaloniaNES.Device.BUS;

namespace AvaloniaNES.Device.APU.Channels;

public class DMCChannel
{
    public bool IrqEnabled { get; private set; }
    public bool Loop { get; private set; }
    public bool IrqActive { get; private set; }

    private readonly Bus _bus;
    
    private ushort _timer;
    private ushort _timerPeriod;
    private byte _outputLevel;
    private byte _sampleBuffer;
    private bool _sampleBufferEmpty = true;
    private ushort _sampleAddress;
    private ushort _currentAddress;
    private ushort _sampleLength;
    private ushort _bytesRemaining;
    private byte _shiftRegister;
    private byte _bitsRemaining;
    private bool _silence;

    private static readonly ushort[] TimerPeriodTable =
    {
        428, 380, 340, 320, 286, 254, 226, 214, 190, 160, 142, 128, 106, 84, 72, 54
    };

    public DMCChannel(Bus bus)
    {
        _bus = bus;
    }

    public void WriteControl(byte value)
    {
        IrqEnabled = (value & 0x80) != 0;
        Loop = (value & 0x40) != 0;
        _timerPeriod = TimerPeriodTable[value & 0x0F];

        if (!IrqEnabled)
        {
            IrqActive = false;
        }
    }

    public void WriteLoad(byte value)
    {
        _outputLevel = (byte)(value & 0x7F);
    }

    public void WriteAddress(byte value)
    {
        _sampleAddress = (ushort)(0xC000 | (value << 6));
    }

    public void WriteLength(byte value)
    {
        _sampleLength = (ushort)((value << 4) | 1);
    }

    public void SetEnabled(bool enabled)
    {
        if (!enabled)
        {
            _bytesRemaining = 0;
        }
        else if (_bytesRemaining == 0)
        {
            _currentAddress = _sampleAddress;
            _bytesRemaining = _sampleLength;
        }
        IrqActive = false;
    }

    public bool IsActive()
    {
        return _bytesRemaining > 0;
    }

    public void Clock()
    {
        if (_timer == 0)
        {
            _timer = _timerPeriod;
            
            if (!_silence)
            {
                if ((_shiftRegister & 0x01) != 0)
                {
                    if (_outputLevel <= 125) _outputLevel += 2;
                }
                else
                {
                    if (_outputLevel >= 2) _outputLevel -= 2;
                }
            }

            _shiftRegister >>= 1;
            _bitsRemaining--;

            if (_bitsRemaining == 0)
            {
                _bitsRemaining = 8;
                if (_sampleBufferEmpty)
                {
                    _silence = true;
                }
                else
                {
                    _silence = false;
                    _shiftRegister = _sampleBuffer;
                    _sampleBufferEmpty = true;
                }
            }
        }
        else
        {
            _timer--;
        }

        // Fetch sample if buffer is empty and bytes remaining
        if (_sampleBufferEmpty && _bytesRemaining > 0)
        {
            // Note: In a real NES, this would steal CPU cycles.
            // For simplicity, we just read instantly here.
            _sampleBuffer = _bus.CPURead(_currentAddress, true);
            _sampleBufferEmpty = false;

            _currentAddress++;
            if (_currentAddress == 0) _currentAddress = 0x8000;

            _bytesRemaining--;
            if (_bytesRemaining == 0)
            {
                if (Loop)
                {
                    _currentAddress = _sampleAddress;
                    _bytesRemaining = _sampleLength;
                }
                else if (IrqEnabled)
                {
                    IrqActive = true;
                }
            }
        }
    }

    public byte GetOutput()
    {
        return _outputLevel;
    }
    public void Reset()
    {
        IrqEnabled = false;
        Loop = false;
        IrqActive = false;
        _timer = 0;
        _timerPeriod = 0;
        _outputLevel = 0;
        _sampleBuffer = 0;
        _sampleBufferEmpty = true;
        _sampleAddress = 0;
        _currentAddress = 0;
        _sampleLength = 0;
        _bytesRemaining = 0;
        _shiftRegister = 0;
        _bitsRemaining = 0;
        _silence = false;
    }
}
