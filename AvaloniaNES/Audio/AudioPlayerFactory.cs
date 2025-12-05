using System;
using System.Runtime.InteropServices;

namespace AvaloniaNES.Audio;

public static class AudioPlayerFactory
{
    public static IAudioPlayer Create()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new WindowsAudioPlayer();
            }
            else
            {
                return new UnixAudioPlayer();
            }
        }
        catch (Exception ex)
        {
            string extraMsg = "";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                extraMsg = " On Linux, ensure 'alsa-utils' is installed (try: sudo apt-get install alsa-utils).";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                extraMsg = " On macOS, ensure 'sox' is installed (try: brew install sox).";
            }

            Console.WriteLine($"Failed to initialize audio player: {ex.Message}.{extraMsg} Audio will be disabled.");
            return new SilentAudioPlayer();
        }
    }
}
