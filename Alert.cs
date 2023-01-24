using System;
using System.IO;
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
    
    [JsonIgnore] private AudioFileReader? audioFile;
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
        if (audioFile == null || audioEvent == null) SetupSound();
        if (audioFile == null || audioEvent == null) return;
        audioEvent.Stop();
        audioFile.Position = 0;
        audioEvent.Play();
    }

    public void SetupSound() {
        DisposeSound();

        try {
            var file = SoundFile;
            if (string.IsNullOrWhiteSpace(file)) 
                file = Path.Join(Plugin.PluginInterface.AssemblyLocation.Directory!.FullName, "doorbell.wav");

            if (!File.Exists(file)) {
                PluginLog.Warning($"{file} does not exist.");
                return;
            }
            
            audioFile = new AudioFileReader(file);
            audioFile.Volume = SoundVolume;
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
