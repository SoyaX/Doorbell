using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Command;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace Doorbell;

public sealed class Plugin : IDalamudPlugin {
    public static string Name => "Doorbell";

    public static Config Config { get; set; } = new();
    
    [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public static IFramework Framework { get; private set; } = null!;
    [PluginService] public static IClientState ClientState { get; private set; } = null!;
    [PluginService] public static IObjectTable Objects { get; private set; } = null!;
    [PluginService] public static IChatGui Chat { get; private set; } = null!;
    [PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] public static IDataManager DataManager { get; private set; } = null!;
    [PluginService] public static IPluginLog Log { get; private set; } = null!;
    public static FileDialogManager FileDialogManager { get; } = new();

    private ConfigWindow configWindow = new($"{Name} Config");
    private WindowSystem windowSystem;

    internal static bool Silenced { get; private set; }
    internal static Stopwatch SilencedFor { get; } = new();
    internal static TimeSpan? SilenceTimeSpan { get; private set; } = new();

    public Plugin() {
        Config = (Config) (PluginInterface.GetPluginConfig() ?? new Config());
        ClientState.TerritoryChanged += OnTerritoryChanged;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigWindow;
        CommandManager.AddHandler("/doorbell", new CommandInfo(HandleCommand) { ShowInHelp = true, HelpMessage = $"Toggle the {Name} config window." });
        CommandManager.AddHandler("/doorbell silence", new CommandInfo(HandleCommand) { ShowInHelp = true, HelpMessage = $"Disable alerts until leaving a house."});
        CommandManager.AddHandler("/doorbell silence #", new CommandInfo(HandleCommand) { ShowInHelp = true, HelpMessage = $"Disable alerts for # minutes." });
        windowSystem = new WindowSystem(Name);
        windowSystem.AddWindow(configWindow);
        PluginInterface.UiBuilder.Draw += windowSystem.Draw;
        PluginInterface.UiBuilder.Draw += FileDialogManager.Draw;
        
        OnTerritoryChanged(ClientState.TerritoryType);
    }

    private void HandleCommand(string command, string arguments) {
        var args = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (args.Length == 0) {
            configWindow.Toggle();
            return;
        }

        switch (args[0].ToLower()) {
            case "silence":
                if (args.Length == 1) {
                    if (Silenced) {
                        UnSilence();
                    } else {
                        Silence();
                    }
                    return;
                }

                if (args.Length >= 2 && float.TryParse(args[1], out var minutes) && minutes >= 0) {
                    if (minutes == 0) {
                        UnSilence();
                    } else {
                        Silence(TimeSpan.FromMinutes(minutes));
                    }

                    return;
                }
                
                break;
        }
        
        Chat.PrintError("Invalid Doorbell Command - Available Options:");
        Chat.PrintError("  - /doorbell  - Toggle Config Window");
        Chat.PrintError("  - /doorbell silence   - Disables Doorbell until you next leave a house.");
        Chat.PrintError("  - /doorbell silence [minutes]   - Disables Doorbell for a specified number of minutes.");
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

    private Dictionary<uint, PlayerObject> KnownObjects = new();
    private Stopwatch TimeInHouse = new();
    
    private void OnTerritoryChanged(ushort territory) {
        KnownObjects.Clear();
        TimeInHouse.Stop();
        Framework.Update -= OnFrameworkUpdate;
        if (HouseTerritoryIds.Contains(territory)) {
            Framework.Update += OnFrameworkUpdate;
            TimeInHouse.Restart();
        } else if (Silenced && SilenceTimeSpan == null && TimeInHouse.ElapsedMilliseconds > 1000) {
            UnSilence();
        }
    }

    internal static void Silence(TimeSpan? timeSpan = null, bool quiet = false) {
        Framework.Update -= SilenceCheck;
        SilencedFor.Restart();
        SilenceTimeSpan = timeSpan;
        Silenced = true;
        if (timeSpan != null) Framework.Update += SilenceCheck;
        if (quiet) return;
        if (timeSpan == null) {
            Chat.Print("Doorbell has been silenced until you next leave a house.");
        } else {
            var silenceEnd = DateTime.Now + timeSpan.Value;
            if (timeSpan.Value.TotalDays >= 0.5f) {
                Chat.Print($"Doorbell has been silenced until {silenceEnd:g}.");
            } else {
                Chat.Print($"Doorbell has been silenced until {silenceEnd:t}.");
            }
        }
    }

    internal static void UnSilence(bool quiet = false) {
        SilencedFor.Reset();
        Silenced = false;
        SilenceTimeSpan = null;
        Chat.Print("Doorbell has been unsilenced.");
        Framework.Update -= SilenceCheck;
    }

    private static void SilenceCheck(IFramework framework) {
        if (Silenced && SilenceTimeSpan != null && SilencedFor.Elapsed > SilenceTimeSpan) {
            UnSilence();
        }
    }
    
    private void OnFrameworkUpdate(IFramework framework) {
        // Check for leavers
        foreach (var o in KnownObjects) {
            o.Value.LastSeen++;

            if (o.Value.LastSeen > 60) {
                if (!Silenced) Config.Left.DoAlert(o.Value);
                KnownObjects.Remove(o.Key);
                break;
            }
        }

        // Check for new people
        foreach (var o in Objects.Where(o => o is PlayerCharacter && o.ObjectIndex is < 200 and > 0)) {
            if (o is not PlayerCharacter pc) continue;
            if (!KnownObjects.ContainsKey(o.ObjectId)) {
                var playerObject = new PlayerObject(pc);
                KnownObjects.Add(o.ObjectId, playerObject);

                    if (Silenced) continue;
                if (TimeInHouse.ElapsedMilliseconds > 1000) {
                    Config.Entered.DoAlert(playerObject);
                } else {
                    Config.AlreadyHere.DoAlert(playerObject);
                }
            } else {
                KnownObjects[o.ObjectId].LastSeen = 0;
            }
        }
        
    }

    public void Dispose() {
        Framework.Update -= OnFrameworkUpdate;
        Framework.Update -= SilenceCheck;
        ClientState.TerritoryChanged -= OnTerritoryChanged;
        CommandManager.RemoveHandler("/doorbell");
        CommandManager.RemoveHandler("/doorbell silence");
        CommandManager.RemoveHandler("/doorbell silence #");
        Config.Entered.DisposeSound();
        Config.Left.DisposeSound();
        Config.AlreadyHere.DisposeSound();
        FileDialogManager.Reset();
    }
}
