namespace AvaloniaNES.Device.APU.Components;

public class Envelope
{
    public bool Start { get; set; }
    public bool Loop { get; set; }
    public bool ConstantVolume { get; set; }
    public byte VolumePeriod { get; set; } // Also acts as constant volume
    public byte Output { get; private set; }

    private byte _decayLevel;
    private byte _divider;

    public void Clock()
    {
        if (!Start)
        {
            if (_divider == 0)
            {
                _divider = VolumePeriod;
                if (_decayLevel == 0)
                {
                    if (Loop)
                    {
                        _decayLevel = 15;
                    }
                }
                else
                {
                    _decayLevel--;
                }
            }
            else
            {
                _divider--;
            }
        }
        else
        {
            Start = false;
            _decayLevel = 15;
            _divider = VolumePeriod;
        }

        if (ConstantVolume)
        {
            Output = VolumePeriod;
        }
        else
        {
            Output = _decayLevel;
        }
    }
    public void Reset()
    {
        Start = false;
        Loop = false;
        ConstantVolume = false;
        VolumePeriod = 0;
        Output = 0;
        _decayLevel = 0;
        _divider = 0;
    }
}
