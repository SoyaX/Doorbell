using System;
using Dalamud.Interface;
using Dalamud.Interface.Components;
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
            ImGui.SetTooltip("Use <name> as a placeholder for the players name.");
        }
        if (!alert.ChatEnabled) ImGui.EndDisabled();
        ImGui.Unindent();
    }
    
    public override void Draw() {
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
