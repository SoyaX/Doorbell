using Dalamud.Game.ClientState.Objects.SubKinds;
using Lumina.Excel.GeneratedSheets;

namespace Doorbell;

public class PlayerObject {
    public uint LastSeen = 0;
    public string Name;
    public uint World;
    public string WorldName => Plugin.DataManager.GetExcelSheet<World>()?.GetRow(World)?.Name?.RawString ?? $"World_{World}";

    public PlayerObject(PlayerCharacter character) {
        Name = character.Name.TextValue;
        World = character.HomeWorld.Id;
    }
}
