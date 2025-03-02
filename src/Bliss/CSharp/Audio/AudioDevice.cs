using MiniAudioEx;

namespace Bliss.CSharp.Audio;

public static class AudioDevice {

    /// <summary>
    /// Gets the sample rate of the audio device in Hz.
    /// </summary>
    public static int SampleRate => AudioContext.SampleRate;
    
    /// <summary>
    /// Gets the number of audio channels (e.g., 2 for stereo).
    /// </summary>
    public static int Channels => AudioContext.Channels;
    
    /// <summary>
    /// Gets the delta time between audio processing frames.
    /// </summary>
    public static float DeltaTime => AudioContext.DeltaTime;
    
    /// <summary>
    /// Occurs when audio data is being processed, allowing custom audio data handling.
    /// </summary>
    public static event DeviceDataEvent DataProcess;
    
    /// <summary>
    /// Initializes the audio device with the specified sample rate, number of channels, and optional device information.
    /// </summary>
    /// <param name="sampleRate">The sample rate to use for the audio device. Defaults to 44100 Hz if not specified.</param>
    /// <param name="channels">The number of audio channels to use. Defaults to 2 channels if not specified.</param>
    /// <param name="deviceInfo">Optional device information to specify a particular audio device. If null, the default device is used.</param>
    public static void Init(uint sampleRate = 44100, uint channels = 2, DeviceInfo? deviceInfo = null) {
        AudioContext.Initialize(sampleRate, channels, deviceInfo);
        AudioContext.DataProcess += (data, count) => {
            DataProcess?.Invoke(data, count);
        };
    }
    
    /// <summary>
    /// Updates the audio device, processing audio data if necessary.
    /// </summary>
    public static void Update() {
        AudioContext.Update();
    }
    
    /// <summary>
    /// Gets a list of available audio devices.
    /// </summary>
    /// <returns>An array of <see cref="DeviceInfo"/> representing available audio devices.</returns>
    public static DeviceInfo[] GetDevices() {
        return AudioContext.GetDevices();
    }
    
    /// <summary>
    /// Gets the current master volume level of the audio device.
    /// </summary>
    /// <returns>A float representing the master volume, typically between 0.0 (muted) and 1.0 (maximum volume).</returns>
    public static float GetMasterVolume() {
        return AudioContext.MasterVolume;
    }

    /// <summary>
    /// Sets the master volume level of the audio device.
    /// </summary>
    /// <param name="volume">A float value representing the desired master volume, typically between 0.0 (muted) and 1.0 (maximum volume).</param>
    public static void SetMasterVolume(float volume) {
        AudioContext.MasterVolume = volume;
    }
    
    /// <summary>
    /// Destroys the audio device and releases any resources associated with it.
    /// </summary>
    public static void Destroy() {
        AudioContext.Deinitialize();
    }
}