using System;

namespace LVCMod
{
    class BotConfig
    {
        public string Token { get; set; } = "";
        public ulong MainVoiceChatId { get; set; } = 0;
        public string MainVoiceChatName { get; set; } = "Talk";
        public ulong VoiceChatsCategoryId { get; set; } = 0;
        public string VoiceChatsCategoryName { get; set; } = "Stardew Valley LVC";
        public bool DeleteVoiceChats { get; set; } = true;
    }
}
