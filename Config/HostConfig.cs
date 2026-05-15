using System.Collections.Generic;
using System;

namespace LVCMod
{
    class HostConfig
    {
        public ulong DiscordGuildId { get; set; } = 0;
        // Per-save data (players and channels) are stored in separate files under data/{saveId}/
        // This config only keeps host-specific settings such as the guild id.
    }
}
