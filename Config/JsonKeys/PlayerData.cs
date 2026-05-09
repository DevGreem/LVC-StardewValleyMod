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
        public string Team { get; set; } = "Blue";
    }
}