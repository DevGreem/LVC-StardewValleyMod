using StardewModdingAPI;
using System;

namespace LVCMod
{
    class UserConfig
    {
        public ulong DiscordId { get; set; } = 0;

        public string Team { get; set; } = "None"; // None, Blue, Red, Green, Yellow

        public bool Muted { get; set; } = true;

        public bool Deafen { get; set; } = true;

        public bool EnableVoiceHotkeys { get; set; } = true;

        public SButton ChangeStateMute { get; set; } = SButton.H;

        public SButton ChangeStateDeaf { get; set; } = SButton.J;
    }
}
