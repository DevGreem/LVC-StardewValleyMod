using StardewModdingAPI;
using System;

namespace LVCMod
{
    class UserConfig
    {
        public ulong DiscordId { get; set; } = 0;

        public string Team { get; set; } = "Blue"; // Blue, Red, Green, Yellow

        public bool MicrophoneActivated { get; set; } = true;

        public bool DeaferDesactivated { get; set; } = true;

        public SButton ChangeStateMicrophone { get; set; } = SButton.H;

        public SButton ChangeStateAudio { get; set; } = SButton.J;
    }
}
