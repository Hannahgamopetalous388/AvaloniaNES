namespace AvaloniaNES.Device.APU.Components;

public class Sweeper
{
    public bool Enabled { get; set; }
    public bool Negate { get; set; }
    public byte Period { get; set; }
    public byte Shift { get; set; }
    public bool Reload { get; set; }
    public bool Mute { get; private set; }
    
    // Reference to the channel's timer/period
    private ushort _channelPeriod;
    private readonly bool _isPulse1; // Pulse 1 uses ones' complement for negate
    private byte _divider;

    public Sweeper(bool isPulse1)
    {
        _isPulse1 = isPulse1;
    }

    public void UpdateChannelPeriod(ushort period)
    {
        _channelPeriod = period;
        UpdateMute();
    }

    public ushort GetTargetPeriod()
    {
        int change = _channelPeriod >> Shift;
        
        if (Negate)
        {
            change = -change;
            if (_isPulse1)
            {
                change -= 1;
            }
        }

        return (ushort)(_channelPeriod + change);
    }

    private void UpdateMute()
    {
        if (_channelPeriod < 8)
        {
            Mute = true;
        }
        else if (!Negate && GetTargetPeriod() > 0x7FF)
        {
            Mute = true;
        }
        else
        {
            Mute = false;
        }
    }

    public ushort Clock(ushort currentPeriod)
    {
        UpdateChannelPeriod(currentPeriod);
        
        ushort newPeriod = currentPeriod;

        if (_divider == 0 && Enabled && !Mute && Shift > 0)
        {
            newPeriod = GetTargetPeriod();
        }

        if (_divider == 0 || Reload)
        {
            _divider = Period;
            Reload = false;
        }
        else
        {
            _divider--;
        }

        return newPeriod;
    }
    public void Reset()
    {
        Enabled = false;
        Negate = false;
        Period = 0;
        Shift = 0;
        Reload = false;
        Mute = false;
        _channelPeriod = 0;
        _divider = 0;
    }
}
