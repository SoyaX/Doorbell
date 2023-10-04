using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Doorbell; 

public class ConfigWindow : Window {
    public ConfigWindow(string name) : base(name, ImGuiWindowFlags.AlwaysAutoResize) {
        
    }

    private void DrawAlertConfig(Alert alert) {
        ImGui.Checkbox("Play a Sound", ref alert.SoundEnabled);

        if (alert.SoundEnabled) {
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Music)) {
                alert.PlaySound();
            }

            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Test Sound");
            }
        } else {
            ImGui.BeginDisabled();
        }
        
        ImGui.Indent();
        if (ImGui.InputTextWithHint("##Sound File", "Default Doorbell", ref alert.SoundFile, 512)) {
            alert.DisposeSound();
        }
        ImGui.SameLine();
        if (ImGuiComponents.IconButton(FontAwesomeIcon.FolderOpen)) {
            Plugin.FileDialogManager.OpenFileDialog("Select a Sound File", "Sound Files (wav mp3 ogg){.wav,.mp3,.ogg}", (b, s) => {
                alert.SoundFile = s;
            });
        }
        ImGui.SameLine();
        ImGui.Text("Sound File");

        if (ImGui.SliderFloat("Volume", ref alert.SoundVolume, 0, alert.SoundFile.IsHttpUrl() ? 1 : MathF.Max(3, alert.SoundVolume + 0.01f))) {
            if (alert.SoundVolume < 0) alert.SoundVolume = 0;
            if (alert.SoundVolume > 1 && alert.SoundFile.IsHttpUrl()) alert.SoundVolume = 1;
            alert.DisposeSound();
        }
        
        ImGui.Unindent();
        if (!alert.SoundEnabled) ImGui.EndDisabled();
        
        ImGui.Checkbox("Show a chat message", ref alert.ChatEnabled);
        ImGui.Indent();
        if (!alert.ChatEnabled) ImGui.BeginDisabled();
        ImGui.InputText("Message", ref alert.ChatFormat, 200);
        if (ImGui.IsItemHovered()) { 
            ImGui.BeginTooltip();
            ImGui.Text("Use <link> as a placeholder to link to the player.");
            ImGui.Text("Use <name> as a placeholder for the players name.");
            ImGui.Text("Use <world> as a placeholder for the players world.");
            ImGui.EndTooltip();
        }
        if (!alert.ChatEnabled) ImGui.EndDisabled();
        ImGui.Unindent();
    }
    
    private int silenceMinutesSlider;
    public override void Draw() {
        if (Plugin.Silenced) {
            ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudRed) & 0x55FFFFFF);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            var timeRemaining = (Plugin.SilenceTimeSpan == null ? TimeSpan.Zero : Plugin.SilenceTimeSpan - Plugin.SilencedFor.Elapsed).Value;
            var str = string.Join(':', new[] { timeRemaining.Days, timeRemaining.Hours, timeRemaining.Minutes, timeRemaining.Seconds }.Select(i => $"{i:00}")).TrimStart('0', ':');
            ImGui.DragInt("##silenceMinutes", ref silenceMinutesSlider, 0, 0, 0, Plugin.SilenceTimeSpan == null ? "Silenced until leaving a house." : $"Silenced for {str}", ImGuiSliderFlags.NoInput);
            ImGui.PopStyleColor();
            if (ImGui.Button("Unsilence Doorbell", new Vector2(ImGui.GetContentRegionAvail().X, 24 * ImGuiHelpers.GlobalScale))) {
                Plugin.UnSilence();
            }
        } else {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            var timeSpan = TimeSpan.FromMinutes(silenceMinutesSlider);
            var str = string.Join(':', new[] { timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds }.Select(i => $"{i:00}")).TrimStart('0', ':');
            ImGui.SliderInt("##silenceMinutes", ref silenceMinutesSlider, 0, Math.Max(60, silenceMinutesSlider + 1), silenceMinutesSlider <= 0 ? "Until leaving a house," : $"For {str},", ImGuiSliderFlags.NoInput);
            if (ImGui.Button("Silence Doorbell", new Vector2(ImGui.GetContentRegionAvail().X, 24 * ImGuiHelpers.GlobalScale))) {
                if (silenceMinutesSlider <= 0) {
                    Plugin.Silence();
                } else {
                    Plugin.Silence(TimeSpan.FromMinutes(silenceMinutesSlider));
                }
            }
        }

        ImGui.Separator();
        ImGui.PushID("Doorbell_Config_Entered");;
        ImGui.Text("When a player enters a house: ");
        ImGui.Indent();
        DrawAlertConfig(Plugin.Config.Entered);
        ImGui.Unindent();
        
        ImGui.Separator();
        
        ImGui.PushID("Doorbell_Config_Left");
        ImGui.Text("When a player leaves a house: ");
        ImGui.Indent();
        DrawAlertConfig(Plugin.Config.Left);
        ImGui.Unindent();
        ImGui.PopID();
        
        ImGui.Separator();
        
        ImGui.PushID("Doorbell_Config_AlreadyHere");
        ImGui.Text("When entering a house with people already inside: ");
        ImGui.Indent();
        DrawAlertConfig(Plugin.Config.AlreadyHere);
        ImGui.Unindent();
        ImGui.PopID();
        
        ImGui.Separator();

        if (ImGui.Button("Save")) {
            Plugin.PluginInterface.SavePluginConfig(Plugin.Config);
        }

        ImGui.SameLine();
        if (ImGui.Button("Save & Close")) {
            Plugin.PluginInterface.SavePluginConfig(Plugin.Config);
            IsOpen = false;
        }
    }
}
