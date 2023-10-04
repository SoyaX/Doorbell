using System;
using System.IO;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
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

    public void DoAlert(PlayerObject player) {
        PrintChat(player);
        PlaySound();
    }
    
    public void PrintChat(PlayerObject player) {
        if (!ChatEnabled) return;

        var messageBuilder = new SeStringBuilder();
        messageBuilder.AddText($"[{Plugin.Name}] ");
        
        for (var i = 0; i < ChatFormat.Length; i++) {
            if (ChatFormat[i] == '<') {
                var tagEnd = ChatFormat.IndexOf('>', i + 1);
                if (tagEnd > i) {
                    var tag = ChatFormat.Substring(i, tagEnd - i + 1);
                    switch (tag) {
                        case "<name>": {
                            messageBuilder.AddText(player.Name);
                            i = tagEnd;
                            continue;
                        }
                        case "<world>": {
                            messageBuilder.AddText(player.WorldName);
                            i = tagEnd;
                            continue;
                        }
                        case "<link>": {
                            messageBuilder.Add(new PlayerPayload(player.Name, player.World));
                            i = tagEnd;
                            continue;
                        }
                    }
                }
            }
            
            messageBuilder.AddText($"{ChatFormat[i]}");
        }
        
        var chatMessage = $"[{Plugin.Name}] {ChatFormat}"
            .Replace("<name>", player.Name);
        
        var entry = new XivChatEntry() {
            Message = messageBuilder.Build()
        };
        
        Plugin.Chat.Print(entry);
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
                Plugin.Log.Warning($"{file} does not exist.");
                return;
            }

            audioFile = new AudioFileReader(file);
            (audioFile as AudioFileReader)!.Volume = SoundVolume;
            audioEvent = new WaveOutEvent();
            audioEvent.Init(audioFile);
        } catch (Exception ex) {
            Plugin.Log.Error(ex, "Error initalizing sound.");
        }
    }

    public void DisposeSound() {
        audioFile?.Dispose();
        audioEvent?.Dispose();

        audioFile = null;
        audioEvent = null;
    }

    
}
