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
            try
            {
                var savedPlayers = Mod.PlayerDataCache;
                if (savedPlayers != null && savedPlayers.Players.TryGetValue(farmerId, out var info))
                    return info.DiscordId;
            }
            catch (Exception)
            {
                // ignore and fallthrough
            }
            return 0;
        }

        private string GetFarmerTeam(long farmerId)
        {
            try
            {
                var savedPlayers = Mod.PlayerDataCache;
                if (savedPlayers != null && savedPlayers.Players.TryGetValue(farmerId, out var info))
                    return info.Team;
            }
            catch (Exception)
            {
                // ignore and fallthrough
            }

            return "None";
        }

        /// <summary>
        /// Get a voice channel by name
        /// </summary>
        /// <param name="channelName">Channel Name</param>
        /// <returns>SocketVoiceChannel</returns>
        private SocketVoiceChannel GetVoiceChannelByName(string channelName)
        {
            // look at the dictionary in the cache
            if (Mod.LocationChannelsCache.TryGetValue(channelName, out ulong id))
            {
                var ch = Guild.GetVoiceChannel(id);
                if (ch != null) return ch;
            }

            // If not found, search by name
            var foundChannel = Guild.VoiceChannels.FirstOrDefault(c => c.Name == channelName);

            if (foundChannel != null)
            {
                // Save ONLY if this name is not in the dictionary or the ID is different
                if (!Mod.LocationChannelsCache.ContainsKey(channelName) || Mod.LocationChannelsCache[channelName] != foundChannel.Id)
                {
                    Mod.LocationChannelsCache[channelName] = foundChannel.Id;
                    // persist to channels.json
                    try
                    {
                        Mod.WritePerSaveFile("channels.json", Mod.LocationChannelsCache);
                    }
                    catch (Exception ex)
                    {
                        Mod.Monitor.Log($"{Mod.Helper.Translation.Get("log.warn.persist-location-channel-mapping-failed", new { reason = ex.Message })}", StardewModdingAPI.LogLevel.Warn);
                    }
                    Mod.BroadcastLocationChannels();
                    Mod.Monitor.Log($"{Mod.Helper.Translation.Get("log.info.channel-registered", new { channelName, channelId = foundChannel.Id })}", StardewModdingAPI.LogLevel.Info);
                }
            }

            return foundChannel;
        }

        /// <summary>
        /// Get a category by name
        /// </summary>
        /// <param name="categoryName">Category Name</param>
        /// <returns>SocketCategoryChannel</returns>
        private SocketCategoryChannel GetCategoryByName(string categoryName)
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
