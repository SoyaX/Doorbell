using Dalamud.Configuration;

namespace Doorbell; 

public class Config : IPluginConfiguration {
    public int Version { get; set; } = 1;
    
    public Alert Entered { get; set; } = new() {
        ChatEnabled = true,
        SoundEnabled = true,
        ChatFormat = "<name> has come inside.",
        SoundFile = ""
    };
    
    public Alert Left { get; set; } = new() {
        ChatEnabled = false,
        SoundEnabled = false,
        ChatFormat = "<name> has left the house."
    };
    
    public Alert AlreadyHere { get; set; } = new() {
        ChatEnabled = false,
        SoundEnabled = false,
        ChatFormat = "<name> was here when you arrived."
    };
}
