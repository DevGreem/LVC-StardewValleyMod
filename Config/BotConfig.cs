using System;

namespace LVCMod
{
    class BotConfig
    {
        public string Token { get; set; } = "";
        public string MainVoiceChatName { get; set; } = "Talk";
        public ulong MainVoiceChatId { get; set; } = 0;
        public string VoiceChatsCategoryName { get; set; } = "Stardew Valley LVC";
        public ulong VoiceChatsCategoryId { get; set; } = 0;
        public bool DeleteVoiceChats { get; set; } = true;
    }
}
