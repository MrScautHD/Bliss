using MiniAudioEx;

namespace Bliss.CSharp.Audio;

public class AudioDevice : Disposable {
    
    // TODO: Change Audio Lib (This is not supporting MacOS!!!)
    
    public AudioDevice(uint sampleRate, uint channels) {
        AudioContext.Initialize(sampleRate, channels);
    }

    public void Update() {
        AudioContext.Update();
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            AudioContext.Deinitialize();
        }
    }
}