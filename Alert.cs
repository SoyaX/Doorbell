using System;
using System.IO;
using System.Threading.Tasks;
using Dalamud.Game.Text;
using Dalamud.Logging;
using NAudio.Wave;
using Newtonsoft.Json;

namespace Doorbell; 

public class Alert {
    public bool ChatEnabled = false;
    public string ChatFormat = string.Empty;

    public bool SoundEnabled = false;
    public string SoundFile = string.Empty;
    public float SoundVolume = 1;

    [JsonIgnore] private WaveStream? audioFile;
    [JsonIgnore] private WaveOutEvent? audioEvent;

    public void DoAlert(string name) {
        PrintChat(name);
        PlaySound();
    }
    
    public void PrintChat(string name) {
        if (!ChatEnabled) return;

        var chatMessage = $"[{Plugin.Name}] {ChatFormat}"
            .Replace("<name>", name);
        
        var entry = new XivChatEntry() {
            Message = chatMessage
        };
        
        Plugin.Chat.PrintChat(entry);
    }
    
    public void PlaySound() {
        if (!SoundEnabled) return;
        Task.Run(() => {
            if (audioFile == null || audioEvent == null) SetupSound();
            if (audioFile == null || audioEvent == null) return;
            audioEvent.Stop();
            audioFile.Position = 0;
            audioEvent.Play();
        });
    }

    public void SetupSound() {
        DisposeSound();

        try {
            var file = SoundFile;
            if (string.IsNullOrWhiteSpace(file)) 
                file = Path.Join(Plugin.PluginInterface.AssemblyLocation.Directory!.FullName, "doorbell.wav");

            if (file.IsHttpUrl()) {
                audioFile = new MediaFoundationReader(file);
                audioEvent = new WaveOutEvent();
                audioEvent.Volume = MathF.Max(0, MathF.Min(1, SoundVolume));
                audioEvent.Init(audioFile);
                return;
            }

            if (!File.Exists(file)) {
                PluginLog.Warning($"{file} does not exist.");
                return;
            }

            audioFile = new AudioFileReader(file);
            (audioFile as AudioFileReader)!.Volume = SoundVolume;
            audioEvent = new WaveOutEvent();
            audioEvent.Init(audioFile);
        } catch (Exception ex) {
            PluginLog.Error(ex, "Error initalizing sound.");
        }
    }

    public void DisposeSound() {
        audioFile?.Dispose();
        audioEvent?.Dispose();

        audioFile = null;
        audioEvent = null;
    }

    
}
