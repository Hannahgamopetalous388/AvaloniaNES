using System;

namespace AvaloniaNES.Audio;

public interface IAudioPlayer : IDisposable
{
    void AddSample(float sample);
}
