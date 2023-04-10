using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;

namespace Doorbell;

public sealed class Plugin : IDalamudPlugin {
    public static string Name => "Doorbell";
    string IDalamudPlugin.Name => Name;

    public static Config Config { get; set; } = new();
    
    [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public static Framework Framework { get; private set; } = null!;
    [PluginService] public static ClientState ClientState { get; private set; } = null!;
    [PluginService] public static ObjectTable Objects { get; private set; } = null!;
    [PluginService] public static ChatGui Chat { get; private set; } = null!;
    [PluginService] public static CommandManager CommandManager { get; private set; } = null!;
    public static FileDialogManager FileDialogManager { get; } = new();

    private ConfigWindow configWindow = new($"{Name} Config");
    private WindowSystem windowSystem;

    public Plugin() {
        Config = (Config) (PluginInterface.GetPluginConfig() ?? new Config());
        ClientState.TerritoryChanged += OnTerritoryChanged;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigWindow;
        CommandManager.AddHandler("/doorbell", new CommandInfo((_, _) => configWindow.Toggle()) { ShowInHelp = true, HelpMessage = $"Toggle the {Name} config window." });
        windowSystem = new WindowSystem(Name);
        windowSystem.AddWindow(configWindow);
        PluginInterface.UiBuilder.Draw += windowSystem.Draw;
        PluginInterface.UiBuilder.Draw += FileDialogManager.Draw;
        
        OnTerritoryChanged(null, ClientState.TerritoryType);
    }

    private void ToggleConfigWindow() {
        configWindow.IsOpen = !configWindow.IsOpen;
    }

    private static readonly ushort[] HouseTerritoryIds = {
        // Small, Medium, Large, Chamber, Apartment
        282, 283, 284, 384, 608, // Mist
        342, 343, 344, 385, 609, // Lavender Beds
        345, 346, 347, 386, 610, // Goblet
        649, 650, 651, 652, 655, // Shirogane
        980, 981, 982, 983, 999, // Empyreum 
    };

    private class PlayerObject {
        public uint LastSeen = 0;
        public string Name = string.Empty;
    }
    
    private Dictionary<uint, PlayerObject> KnownObjects = new();
    private Stopwatch TimeInHouse = new();
    
    private void OnTerritoryChanged(object? sender, ushort territory) {
        KnownObjects.Clear();
        TimeInHouse.Stop();
        Framework.Update -= OnFrameworkUpdate;
        if (HouseTerritoryIds.Contains(territory)) {
            Framework.Update += OnFrameworkUpdate;
            TimeInHouse.Restart();
        }
    }
    
    private void OnFrameworkUpdate(Framework framework) {
        
        // Check for leavers
        foreach (var o in KnownObjects) {
            o.Value.LastSeen++;

            if (o.Value.LastSeen > 60) {
                Config.Left.DoAlert(o.Value.Name);
                KnownObjects.Remove(o.Key);
                break;
            }
        }

        // Check for new people
        foreach (var o in Objects.Where(o => o is PlayerCharacter && o.ObjectIndex is < 200 and > 0)) {
            if (!KnownObjects.ContainsKey(o.ObjectId)) {
                KnownObjects.Add(o.ObjectId, new PlayerObject() {
                    Name = o.Name.TextValue
                });
                
                if (TimeInHouse.ElapsedMilliseconds > 1000) {
                    Config.Entered.DoAlert(o.Name.TextValue);
                } else {
                    Config.AlreadyHere.DoAlert(o.Name.TextValue);
                }
            } else {
                KnownObjects[o.ObjectId].LastSeen = 0;
            }
        }
        
    }

    public void Dispose() {
        Framework.Update -= OnFrameworkUpdate;
        ClientState.TerritoryChanged -= OnTerritoryChanged;
        CommandManager.RemoveHandler("/doorbell");
        Config.Entered.DisposeSound();
        Config.Left.DisposeSound();
        Config.AlreadyHere.DisposeSound();
        FileDialogManager.Reset();
    }
}
