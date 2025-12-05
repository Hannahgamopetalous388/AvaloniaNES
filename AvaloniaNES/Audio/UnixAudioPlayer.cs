using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace AvaloniaNES.Audio;

public class UnixAudioPlayer : IAudioPlayer
{
    private const int SAMPLE_RATE = 44100;
    private const int CHANNELS = 1;
    private const int BITS_PER_SAMPLE = 16;
    private const int BUFFER_SIZE = 4096;

    private readonly Process _audioProcess;
    private readonly Stream _outputStream;
    private readonly BinaryWriter _writer;
    private readonly short[] _sampleBuffer;
    private int _currentBufferIndex;
    private bool _disposed;
    private bool _hasLoggedError = false;

    public UnixAudioPlayer()
    {
        _sampleBuffer = new short[BUFFER_SIZE];

        string command;
        string args;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            command = "play";
            args = $"-t raw -b {BITS_PER_SAMPLE} -e signed -c {CHANNELS} -r {SAMPLE_RATE} -";
        }
        else
        {
            command = "aplay";
            args = $"-f S16_LE -r {SAMPLE_RATE} -c {CHANNELS} -";
        }

        try
        {
            _audioProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardError = true, // Suppress ALSA errors
                    CreateNoWindow = true
                }
            };

            _audioProcess.ErrorDataReceived += (sender, e) => 
            { 
                if (!string.IsNullOrEmpty(e.Data) && !_hasLoggedError)
                {
                    _hasLoggedError = true;
                    Console.WriteLine($"[Warning] Audio backend error: {e.Data}");
                    Console.WriteLine("[Tip] If running on WSL, ensure you have WSLg or PulseAudio configured.");
                }
            };

            _audioProcess.Start();
            _audioProcess.BeginErrorReadLine();
            _outputStream = _audioProcess.StandardInput.BaseStream;
            _writer = new BinaryWriter(_outputStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start audio process: {ex.Message}");
            throw new Exception($"Failed to start audio player ({command}). Ensure it is installed.", ex);
        }
    }

    public void AddSample(float sample)
    {
        if (_disposed) return;

        // Clamp
        if (sample > 1.0f) sample = 1.0f;
        if (sample < -1.0f) sample = -1.0f;

        short pcm = (short)(sample * 32767);
        _sampleBuffer[_currentBufferIndex++] = pcm;

        if (_currentBufferIndex >= BUFFER_SIZE)
        {
            FlushBuffer();
            _currentBufferIndex = 0;
        }
    }

    private void FlushBuffer()
    {
        try
        {
            // Write buffer to stdout (pipe)
            // This will block if the pipe is full, providing synchronization!
            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                _writer.Write(_sampleBuffer[i]);
            }
            _outputStream.Flush();
        }
        catch (Exception)
        {
            // Process might have died
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _writer.Dispose();
            _outputStream.Dispose();
            if (!_audioProcess.HasExited)
            {
                _audioProcess.Kill();
            }
            _audioProcess.Dispose();
        }
        catch
        {
            // Ignore errors during dispose
        }
    }
}
