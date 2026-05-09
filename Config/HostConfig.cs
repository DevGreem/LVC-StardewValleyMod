using System.Collections.Generic;
using System;

namespace LVCMod
{
    class HostConfig
    {
        public ulong DiscordGuildId { get; set; } = 0;
        public Dictionary<ulong, PlayerData> SavesData { get; set; } = new();

        public Dictionary<string, ulong> LocationChannels { get; set; } = new();
    }
}
