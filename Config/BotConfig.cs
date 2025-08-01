using StardewModdingAPI;
using System;

namespace LVCMod
{
    class BotConfig
    {
        public string Token { get; set; } = "";

        public string MainVoiceChatName { get; set; } = "Talk";

        public string VoiceChatsCategoryName { get; set; } = "Stardew Valley LVC";

        public bool IsWarpActive { get; set; } = true;

        public SButton ChangeWarpState { get; set; } = SButton.L;

        public bool DeleteVoiceChats { get; set; } = true;
    }
}
