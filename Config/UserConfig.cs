using StardewModdingAPI;
using System;

namespace LVCMod
{
    class UserConfig
    {
        public ulong DiscordId { get; set; } = 0;

        public string Team { get; set; } = "None"; // None, Blue, Red, Green, Yellow

        // Muted and Deafen are now stored per-save in players.json

        public bool EnableVoiceHotkeys { get; set; } = true;

        public SButton ChangeStateMute { get; set; } = SButton.H;

        public SButton ChangeStateDeaf { get; set; } = SButton.J;
    }
}
