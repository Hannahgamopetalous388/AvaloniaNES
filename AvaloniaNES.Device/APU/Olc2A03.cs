using AvaloniaNES.Device.APU.Channels;
using AvaloniaNES.Device.BUS;

namespace AvaloniaNES.Device.APU;

public class Olc2A03
{
    public PulseChannel Pulse1 { get; }
    public PulseChannel Pulse2 { get; }
    public TriangleChannel Triangle { get; }
    public NoiseChannel Noise { get; }
    public DMCChannel DMC { get; }

    private readonly Bus _bus;
    private uint _clockCounter;
    private byte _frameCounterMode;
    private bool _irqInhibit;
    private bool _frameIrq;

    // Downsampling
    private float _sampleAccumulator;
    private int _sampleCount;

    // High-Pass Filter
    private float _prevSample = 0;
    private float _prevOutput = 0;

    // Lookup tables for mixer
    private static readonly float[] PulseTable = new float[31];
    private static readonly float[] TndTable = new float[203];

    public Olc2A03(Bus bus)
    {
        _bus = bus;
        Pulse1 = new PulseChannel(true);
        Pulse2 = new PulseChannel(false);
        Triangle = new TriangleChannel();
        Noise = new NoiseChannel();
        DMC = new DMCChannel(bus);

        InitializeMixerTables();
    }

    private void InitializeMixerTables()
    {
        for (int i = 0; i < 31; i++)
        {
            PulseTable[i] = 95.52f / (8128.0f / i + 100);
        }

        for (int i = 0; i < 203; i++)
        {
            TndTable[i] = 163.67f / (24329.0f / i + 100);
        }
    }

    public void Reset()
    {
        _clockCounter = 0;
        _frameCounterMode = 0;
        _irqInhibit = false;
        _frameIrq = false;
        WriteRegister(0x4015, 0x00);
        
        Pulse1.Reset();
        Pulse2.Reset();
        Triangle.Reset();
        Noise.Reset();
        DMC.Reset();

        _sampleAccumulator = 0;
        _sampleCount = 0;
        _prevSample = 0;
        _prevOutput = 0;
    }

    public void Clock()
    {
        bool quarterFrame = false;
        bool halfFrame = false;

        // Frame Counter
        // Mode 0: 4-step sequence
        // Mode 1: 5-step sequence
        
        // Approximate timing (NTSC)
        // Step 1: 7457
        // Step 2: 14913
        // Step 3: 22371
        // Step 4: 29829 (Mode 0 IRQ) / 29828 (Mode 1)
        // Step 5: 37281 (Mode 1 only)

        // Simplified logic:
        // APU clock is CPU clock / 2 (approx, actually runs at CPU speed but steps less often)
        // We'll run at CPU speed and divide by 2 for the frame counter steps?
        // Actually, the APU units (timers) run at CPU speed (or CPU/2 for some).
        // The Frame Counter runs at ~240Hz.
        // CPU Clock = 1.789773 MHz
        // 1.789773 MHz / 240 = ~7457 cycles per frame counter step.

        if (_clockCounter % 7457 == 0) // Roughly 240Hz
        {
            int step = (int)(_clockCounter / 7457) % (_frameCounterMode == 1 ? 5 : 4);

            if (_frameCounterMode == 0)
            {
                // Mode 0: 4-step
                // Step 1: Envelope, Linear
                // Step 2: Envelope, Linear, Length, Sweep
                // Step 3: Envelope, Linear
                // Step 4: Envelope, Linear, Length, Sweep, IRQ
                switch (step)
                {
                    case 0:
                    case 2:
                        quarterFrame = true;
                        break;
                    case 1:
                    case 3:
                        quarterFrame = true;
                        halfFrame = true;
                        break;
                }

                if (step == 3 && !_irqInhibit)
                {
                    _frameIrq = true;
                }
            }
            else
            {
                // Mode 1: 5-step
                // Step 1: Envelope, Linear
                // Step 2: Envelope, Linear, Length, Sweep
                // Step 3: Envelope, Linear
                // Step 4: -
                // Step 5: Envelope, Linear, Length, Sweep
                switch (step)
                {
                    case 0:
                    case 2:
                        quarterFrame = true;
                        break;
                    case 1:
                    case 4:
                        quarterFrame = true;
                        halfFrame = true;
                        break;
                }
            }
        }

        if (quarterFrame)
        {
            Pulse1.Envelope.Clock();
            Pulse2.Envelope.Clock();
            Triangle.ClockLinearCounter();
            Noise.Envelope.Clock();
        }

        if (halfFrame)
        {
            Pulse1.LengthCounter.Clock();
            Pulse1.ClockSweep();
            Pulse2.LengthCounter.Clock();
            Pulse2.ClockSweep();
            Triangle.LengthCounter.Clock();
            Noise.LengthCounter.Clock();
        }

        // Channel Clocks
        // Pulse channels run at CPU / 2
        if (_clockCounter % 2 == 0)
        {
            Pulse1.Clock();
            Pulse2.Clock();
        }
        
        // Triangle runs at CPU speed
        Triangle.Clock();

        // Noise runs at CPU speed
        Noise.Clock();

        // DMC runs at CPU speed
        DMC.Clock();

        // Accumulate sample for downsampling
        _sampleAccumulator += GetSample();
        _sampleCount++;

        _clockCounter++;
    }

    public void WriteRegister(ushort address, byte value)
    {
        switch (address)
        {
            case 0x4000: Pulse1.WriteControl(value); break;
            case 0x4001: Pulse1.WriteSweep(value); break;
            case 0x4002: Pulse1.WriteTimerLow(value); break;
            case 0x4003: Pulse1.WriteTimerHigh(value); break;

            case 0x4004: Pulse2.WriteControl(value); break;
            case 0x4005: Pulse2.WriteSweep(value); break;
            case 0x4006: Pulse2.WriteTimerLow(value); break;
            case 0x4007: Pulse2.WriteTimerHigh(value); break;

            case 0x4008: Triangle.WriteControl(value); break;
            case 0x4009: break; // Unused
            case 0x400A: Triangle.WriteTimerLow(value); break;
            case 0x400B: Triangle.WriteTimerHigh(value); break;

            case 0x400C: Noise.WriteControl(value); break;
            case 0x400D: break; // Unused
            case 0x400E: Noise.WriteMode(value); break;
            case 0x400F: Noise.WriteLength(value); break;

            case 0x4010: DMC.WriteControl(value); break;
            case 0x4011: DMC.WriteLoad(value); break;
            case 0x4012: DMC.WriteAddress(value); break;
            case 0x4013: DMC.WriteLength(value); break;

            case 0x4015: // Status
                Pulse1.LengthCounter.SetEnabled((value & 0x01) != 0);
                Pulse2.LengthCounter.SetEnabled((value & 0x02) != 0);
                Triangle.LengthCounter.SetEnabled((value & 0x04) != 0);
                Noise.LengthCounter.SetEnabled((value & 0x08) != 0);
                DMC.SetEnabled((value & 0x10) != 0);
                break;

            case 0x4017: // Frame Counter
                _frameCounterMode = (byte)((value & 0x80) >> 7);
                _irqInhibit = (value & 0x40) != 0;
                if (_irqInhibit) _frameIrq = false;
                
                if (_frameCounterMode == 1)
                {
                    // If mode 1 is set, clock immediately
                    Pulse1.Envelope.Clock();
                    Pulse2.Envelope.Clock();
                    Triangle.ClockLinearCounter();
                    Noise.Envelope.Clock();
                    
                    Pulse1.LengthCounter.Clock();
                    Pulse1.ClockSweep();
                    Pulse2.LengthCounter.Clock();
                    Pulse2.ClockSweep();
                    Triangle.LengthCounter.Clock();
                    Noise.LengthCounter.Clock();
                }
                break;
        }
    }

    public byte ReadStatus()
    {
        byte status = 0;
        if (Pulse1.LengthCounter.Counter > 0) status |= 0x01;
        if (Pulse2.LengthCounter.Counter > 0) status |= 0x02;
        if (Triangle.LengthCounter.Counter > 0) status |= 0x04;
        if (Noise.LengthCounter.Counter > 0) status |= 0x08;
        if (DMC.IsActive()) status |= 0x10;
        if (_frameIrq) status |= 0x40;
        if (DMC.IrqActive) status |= 0x80;

        _frameIrq = false;
        return status;
    }

    public float GetSample()
    {
        byte p1 = Pulse1.GetOutput();
        byte p2 = Pulse2.GetOutput();
        byte t = Triangle.GetOutput();
        byte n = Noise.GetOutput();
        byte d = DMC.GetOutput();

        float pulseOut = PulseTable[p1 + p2];
        float tndOut = TndTable[3 * t + 2 * n + d];

        return pulseOut + tndOut;
    }

    public float GetOutputSample()
    {
        if (_sampleCount == 0) return 0;
        float rawSample = _sampleAccumulator / _sampleCount;
        _sampleAccumulator = 0;
        _sampleCount = 0;

        // High-Pass Filter (First order, 44.1kHz)
        // y[n] = x[n] - x[n-1] + 0.996 * y[n-1]
        float output = rawSample - _prevSample + 0.996f * _prevOutput;
        _prevSample = rawSample;
        _prevOutput = output;

        return output;
    }
}
