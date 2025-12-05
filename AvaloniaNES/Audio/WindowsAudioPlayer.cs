using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace AvaloniaNES.Audio;

public class WindowsAudioPlayer : IAudioPlayer
{
    private const int SAMPLE_RATE = 44100;
    private const int CHANNELS = 1;
    private const int BITS_PER_SAMPLE = 16;
    private const int BUFFER_COUNT = 3;
    private const int BUFFER_SIZE = 4096; // Samples per buffer

    private IntPtr _hWaveOut;
    private readonly WaveHeader[] _buffers;
    private readonly short[] _sampleBuffer;
    private int _currentBufferIndex;
    private bool _disposed;

    // P/Invoke definitions
    [StructLayout(LayoutKind.Sequential)]
    private struct WaveFormatEx
    {
        public ushort wFormatTag;
        public ushort nChannels;
        public uint nSamplesPerSec;
        public uint nAvgBytesPerSec;
        public ushort nBlockAlign;
        public ushort wBitsPerSample;
        public ushort cbSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WaveHeader
    {
        public IntPtr lpData;
        public uint dwBufferLength;
        public uint dwBytesRecorded;
        public IntPtr dwUser;
        public uint dwFlags;
        public uint dwLoops;
        public IntPtr lpNext;
        public IntPtr reserved;
    }

    private delegate void WaveCallback(IntPtr hWaveOut, uint uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2);

    [DllImport("winmm.dll")]
    private static extern int waveOutOpen(out IntPtr hWaveOut, int uDeviceID, ref WaveFormatEx lpFormat, WaveCallback dwCallback, IntPtr dwInstance, int fdwOpen);

    [DllImport("winmm.dll")]
    private static extern int waveOutPrepareHeader(IntPtr hWaveOut, ref WaveHeader lpWaveOutHdr, int uSize);

    [DllImport("winmm.dll")]
    private static extern int waveOutWrite(IntPtr hWaveOut, ref WaveHeader lpWaveOutHdr, int uSize);

    [DllImport("winmm.dll")]
    private static extern int waveOutUnprepareHeader(IntPtr hWaveOut, ref WaveHeader lpWaveOutHdr, int uSize);

    [DllImport("winmm.dll")]
    private static extern int waveOutClose(IntPtr hWaveOut);

    [DllImport("winmm.dll")]
    private static extern int waveOutReset(IntPtr hWaveOut);

    public WindowsAudioPlayer()
    {
        _buffers = new WaveHeader[BUFFER_COUNT];
        _sampleBuffer = new short[BUFFER_SIZE];

        WaveFormatEx format = new WaveFormatEx
        {
            wFormatTag = 1, // PCM
            nChannels = CHANNELS,
            nSamplesPerSec = SAMPLE_RATE,
            wBitsPerSample = BITS_PER_SAMPLE,
            nBlockAlign = (ushort)(CHANNELS * (BITS_PER_SAMPLE / 8)),
            nAvgBytesPerSec = (uint)(SAMPLE_RATE * CHANNELS * (BITS_PER_SAMPLE / 8)),
            cbSize = 0
        };

        int result = waveOutOpen(out _hWaveOut, -1, ref format, null, IntPtr.Zero, 0);
        if (result != 0)
        {
            throw new Exception($"Failed to open waveOut device. Error code: {result}");
        }

        for (int i = 0; i < BUFFER_COUNT; i++)
        {
            _buffers[i] = new WaveHeader
            {
                dwBufferLength = (uint)(BUFFER_SIZE * 2), // Bytes
                dwFlags = 0
            };
            
            _buffers[i].lpData = Marshal.AllocHGlobal((int)_buffers[i].dwBufferLength);
            
            result = waveOutPrepareHeader(_hWaveOut, ref _buffers[i], Marshal.SizeOf(typeof(WaveHeader)));
            if (result != 0)
            {
                throw new Exception($"Failed to prepare wave header. Error code: {result}");
            }
        }
    }

    public void AddSample(float sample)
    {
        if (_disposed) return;

        // Clamp sample to -1.0 to 1.0
        if (sample > 1.0f) sample = 1.0f;
        if (sample < -1.0f) sample = -1.0f;

        // Convert to 16-bit PCM
        short pcm = (short)(sample * 32767);

        _sampleBuffer[_currentBufferIndex++] = pcm;

        if (_currentBufferIndex >= BUFFER_SIZE)
        {
            PlayBuffer();
            _currentBufferIndex = 0;
        }
    }

    private int _nextBuffer = 0;

    private void PlayBuffer()
    {
        // Blocking wait for the next buffer to be free
        while ((_buffers[_nextBuffer].dwFlags & 0x10) != 0) // WHDR_INQUEUE
        {
            // Busy wait / Sleep
            // This syncs the emulation speed to the audio consumption
            Thread.Sleep(1);
        }

        // Copy data to the buffer
        Marshal.Copy(_sampleBuffer, 0, _buffers[_nextBuffer].lpData, BUFFER_SIZE);

        // Write to device
        waveOutWrite(_hWaveOut, ref _buffers[_nextBuffer], Marshal.SizeOf(typeof(WaveHeader)));

        // Advance to next buffer
        _nextBuffer = (_nextBuffer + 1) % BUFFER_COUNT;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        waveOutReset(_hWaveOut);

        for (int i = 0; i < BUFFER_COUNT; i++)
        {
            waveOutUnprepareHeader(_hWaveOut, ref _buffers[i], Marshal.SizeOf(typeof(WaveHeader)));
            Marshal.FreeHGlobal(_buffers[i].lpData);
        }

        waveOutClose(_hWaveOut);
    }
}
