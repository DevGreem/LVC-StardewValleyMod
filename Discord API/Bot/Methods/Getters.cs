using Discord.WebSocket;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LVCMod
{
    partial class Bot
    {
        /// <summary>
        /// Get the Discord Id of a Farmer
        /// </summary>
        /// <param name="farmerId">Farmer Id</param>
        /// <returns>ulong</returns>
        private ulong GetDiscordFarmerId(long farmerId)
        {
            if (Mod.Config.Host.SavesData.TryGetValue(Game1.uniqueIDForThisGame, out var data))
            {
                if (data.Players.TryGetValue(farmerId, out var info))
                    return info.DiscordId;
            }
            return 0;
        }

        private string GetFarmerTeam(long farmerId)
        {
            if (Mod.Config.Host.SavesData.TryGetValue(Game1.uniqueIDForThisGame, out var data))
            {
                if (data.Players.TryGetValue(farmerId, out var info))
                    return info.Team;
            }
            return "None";
        }

        /// <summary>
        /// Get a voice channel by name
        /// </summary>
        /// <param name="channelName">Channel Name</param>
        /// <returns>SocketVoiceChannel</returns>
        private SocketVoiceChannel? GetVoiceChannelByName(string channelName)
        {
            // look at the dictionary in the cache
            if (Mod.Config.Host.LocationChannels.TryGetValue(channelName, out ulong id))
            {
                var ch = Guild.GetVoiceChannel(id);
                if (ch != null) return ch;
            }

            // If not found, search by name
            var foundChannel = Guild.VoiceChannels.FirstOrDefault(c => c.Name == channelName);

            if (foundChannel != null)
            {
                // Save ONLY if this name is not in the dictionary or the ID is different
                if (!Mod.Config.Host.LocationChannels.ContainsKey(channelName) || Mod.Config.Host.LocationChannels[channelName] != foundChannel.Id)
                {
                    Mod.Config.Host.LocationChannels[channelName] = foundChannel.Id;
                    Mod.Helper.WriteConfig(Mod.Config);
                    Mod.BroadcastLocationChannels();
                    Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.info.channel-registered", new { channelName, channelId = foundChannel.Id })}", StardewModdingAPI.LogLevel.Info);
                }
            }

            return foundChannel;
        }

        /// <summary>
        /// Get a category by name
        /// </summary>
        /// <param name="categoryName">Category Name</param>
        /// <returns>SocketCategoryChannel</returns>
        private SocketCategoryChannel? GetCategoryByName(string categoryName)
        {
            if (Mod.Config.Bot.VoiceChatsCategoryId != 0)
            {
                var cat = Guild.GetCategoryChannel(Mod.Config.Bot.VoiceChatsCategoryId);
                if (cat != null) return cat;
            }
            return Guild.CategoryChannels.FirstOrDefault(c => c.Name == categoryName);
        }
    }
}
