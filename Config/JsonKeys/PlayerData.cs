using System.Collections.Generic;

namespace LVCMod
{
    public class PlayerData
    {
        public Dictionary<long, FarmerInfo> Players { get; set; } = new();
    }

    public class FarmerInfo
    {
        public ulong DiscordId { get; set; }
        public string Team { get; set; } = "None";
        // Per-player voice state persisted to players.json
        public bool Muted { get; set; } = false;
        public bool Deafen { get; set; } = false;
    }
}