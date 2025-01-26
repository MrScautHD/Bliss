using MiniAudioEx;

namespace Bliss.CSharp.Audio;

public static class AudioDevice {

    public static int SampleRate => AudioContext.SampleRate;
    public static int Channels => AudioContext.Channels;
    public static float DeltaTime => AudioContext.DeltaTime;
    
    public static event DeviceDataEvent DataProcess;

    public static void Init(uint sampleRate = 44100, uint channels = 2) {
        AudioContext.Initialize(sampleRate, channels);
        AudioContext.DataProcess += (data, count) => {
            DataProcess?.Invoke(data, count);
        };
    }

    public static void Update() {
        AudioContext.Update();
    }

    public static DeviceInfo[] GetDevices() {
        return AudioContext.GetDevices();
    }

    public static float GetMasterVolume() {
        return AudioContext.MasterVolume;
    }

    public static void SetMasterVolume(float volume) {
        AudioContext.MasterVolume = volume;
    }

    public static void Destroy() {
        AudioContext.Deinitialize();
    }
}