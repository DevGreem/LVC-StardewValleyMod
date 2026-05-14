using Discord.Rest;
using Discord.WebSocket;
using StardewModdingAPI;
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
        /// Move a Farmer to a voice chat by player ID
        /// </summary>
        /// <param name="playerId">Stardew Valley player ID</param>
        /// <param name="newLocation">New location name</param>
        /// <returns>Task</returns>
        public async Task MoveToVoice(long playerId, string newLocation)
        {
            ulong discordId = GetDiscordFarmerId(playerId);
            string team = GetFarmerTeam(playerId); // Get player's team from central data

            if (discordId == 0)
            {
                Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.error.discord-id-not-found", new { playerId })}", LogLevel.Error);
                return;
            }

            string mergedLocation = MergeLocations(newLocation, team);
            Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.info.moving-player", new { playerId, team, target = mergedLocation })}", LogLevel.Info);

            await MoveToVoice(discordId, mergedLocation);
        }

        /// <summary>
        /// Move a Farmer to a voice chat by Discord ID
        /// </summary>
        /// <param name="discordId">Discord user ID</param>
        /// <param name="newLocation">New location name</param>
        /// <returns>Task</returns>
        public async Task MoveToVoice(ulong discordId, string? newLocation)
        {
            if (string.IsNullOrEmpty(newLocation)) return;

            SocketGuildUser? user = Guild.GetUser(discordId);
            // 1. Don't touch if user is not in voice
            if (user?.VoiceChannel is null) return;

            // 2. CRITICAL CHECK: Check if user's current channel is one of our config IDs
            // If the user's current channel ID is not in our LocationChannels list and not the Main Channel ID, don't interfere
            bool isUserInModChannel = Mod.Config.Host.LocationChannels.Values.Contains(user.VoiceChannel.Id)
                                      || user.VoiceChannel.Id == Mod.Config.Bot.MainVoiceChatId;

            if (!isUserInModChannel)
            {
                return;
            }

            // 3. Don't touch if already in target channel
            if (user.VoiceChannel.Name == newLocation) return;

            SocketVoiceChannel? voiceChannel = GetVoiceChannelByName(newLocation);

            if (voiceChannel is null)
            {
                Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.info.creating-channel", new { location = newLocation })}", StardewModdingAPI.LogLevel.Info);
                var restChannel = await CreateVoiceChannel(newLocation);

                // Save ID immediately
                Mod.Config.Host.LocationChannels[newLocation] = restChannel.Id;
                Mod.Helper.WriteConfig(Mod.Config);
                Mod.BroadcastLocationChannels();

                // CRITICAL FIX: Wait a short time for Discord to recognize the channel
                // and get the channel object fresh
                await Task.Delay(1000);
                voiceChannel = Guild.GetVoiceChannel(restChannel.Id);

                if (voiceChannel == null)
                {
                    Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.warn.channel-not-ready")}", StardewModdingAPI.LogLevel.Warn);
                    return;
                }
            }

            if (voiceChannel != null && user.VoiceChannel.Id != voiceChannel.Id)
            {
                int retryCount = 0;
                const int maxRetries = 3;

                while (retryCount < maxRetries)
                {
                    try
                    {
                        // Short wait to relax Discord API
                        await Task.Delay(250 + (retryCount * 500)); // Longer wait for each retry
                        await user.ModifyAsync(x => x.Channel = voiceChannel);
                        Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.info.move-success", new { userName = user.Username, location = newLocation })}", StardewModdingAPI.LogLevel.Info);
                        return; // Exit if successful
                    }
                    catch (Exception ex)
                    {
                        retryCount++;
                        Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.warn.move-failed-retry", new { attempt = retryCount, maxAttempts = maxRetries, userName = user.Username, reason = ex.Message })}", StardewModdingAPI.LogLevel.Warn);

                        if (retryCount >= maxRetries)
                        {
                            Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.error.move-all-attempts-failed", new { userName = user.Username })}", StardewModdingAPI.LogLevel.Error);
                        }
                        else
                        {
                            // Wait longer if connection is lost
                            await Task.Delay(2000);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Move a user to a voice channel by channel ID
        /// </summary>
        /// <param name="discordId">Discord user ID</param>
        /// <param name="channelId">Target channel ID</param>
        /// <returns>Task</returns>
        public async Task MoveToVoice(ulong discordId, ulong channelId)
        {
            Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.info.move-channel-id", new { discordId, channelId })}", LogLevel.Info);

            if (channelId == 0)
            {
                Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.warn.channel-id-zero")}", LogLevel.Warn);
                return;
            }

            SocketGuildUser? user = Guild.GetUser(discordId);
            if (user?.VoiceChannel is null)
            {
                Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.warn.user-not-in-voice", new { discordId })}", LogLevel.Warn);
                return;
            }

            if (user.VoiceChannel.Id == channelId)
            {
                Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.info.user-already-in-channel", new { channelId })}", LogLevel.Info);
                return;
            }

            SocketVoiceChannel? voiceChannel = Guild.GetVoiceChannel(channelId);
            if (voiceChannel is null)
            {
                Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.error.channel-not-found", new { channelId })}", LogLevel.Error);
                return;
            }

            try
            {
                int retryCount = 0;
                const int maxRetries = 3;

                while (retryCount < maxRetries)
                {
                    try
                    {
                        await Task.Delay(250 + (retryCount * 500));
                        await user.ModifyAsync(x => x.Channel = voiceChannel);
                        Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.info.move-success", new { userName = user.Username, location = voiceChannel.Name })}", StardewModdingAPI.LogLevel.Info);
                        return;
                    }
                    catch (Exception ex)
                    {
                        retryCount++;
                        Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.warn.move-failed-retry", new { attempt = retryCount, maxAttempts = maxRetries, userName = user.Username, reason = ex.Message })}", StardewModdingAPI.LogLevel.Warn);

                        if (retryCount >= maxRetries)
                        {
                            Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.error.move-all-attempts-failed", new { userName = user.Username })}", StardewModdingAPI.LogLevel.Error);
                        }
                        else
                        {
                            await Task.Delay(2000);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.error.general-move-error", new { userName = user.Username, reason = ex.Message })}", StardewModdingAPI.LogLevel.Error);
            }
        }

        /// <summary>
        /// Merge location name with team name for channel naming
        /// </summary>
        /// <param name="currentLocation">Current location name</param>
        /// <param name="teamName">Team name</param>
        /// <returns>Merged location string</returns>
        private static string MergeLocations(string currentLocation, string teamName)
        {
            if (currentLocation.Contains("UndergroundMine"))
                return "Mine";
                //return currentLocation.Substring(0, currentLocation.Length - 1);

            if (currentLocation == "FarmHouse" || currentLocation == "Cabin")
            {
                if (teamName == "None")
                    return "Cabin";

                return $"{teamName} Cabin";
            }

            return currentLocation;
        }

        /// <summary>
        /// Mute a user by Stardew Valley player ID
        /// </summary>
        /// <param name="playerId">Stardew Valley player ID</param>
        /// <returns>Task</returns>
        public async Task MuteUser(long playerId)
        {
            await MuteUser(GetDiscordFarmerId(playerId));
        }

        /// <summary>
        /// Mute a user by Discord ID
        /// </summary>
        /// <param name="userId">Discord user ID</param>
        /// <returns>Task</returns>
        public async Task MuteUser(ulong userId)
        {
            SocketGuildUser? user = Guild.GetUser(userId);

            if (user is null)
                return;

            await user.ModifyAsync(u => u.Mute = true);
        }

        /// <summary>
        /// Unmute a user by Stardew Valley player ID
        /// </summary>
        /// <param name="playerId">Stardew Valley player ID</param>
        /// <returns>Task</returns>
        public async Task UnmuteUser(long playerId)
        {
            await UnmuteUser(GetDiscordFarmerId(playerId));
        }

        /// <summary>
        /// Unmute a user by Discord ID
        /// </summary>
        /// <param name="userId">Discord user ID</param>
        /// <returns>Task</returns>
        public async Task UnmuteUser(ulong userId)
        {
            SocketGuildUser? user = Guild.GetUser(userId);

            if (user is null)
                return;

            await user.ModifyAsync(u => u.Mute = false);
        }

        /// <summary>
        /// Change the mute state of a user by Stardew Valley player ID
        /// </summary>
        /// <param name="playerId">Stardew Valley player ID</param>
        /// <param name="state">Mute state (true = mute, false = unmute)</param>
        /// <returns>Task</returns>
        public async Task ChangeMuteUserState(long playerId, bool state)
        {
            await ChangeMuteUserState(GetDiscordFarmerId(playerId), state);
        }

        /// <summary>
        /// Change the mute state of a user by Discord ID
        /// </summary>
        /// <param name="playerId">Discord user ID</param>
        /// <param name="state">Mute state (true = mute, false = unmute)</param>
        /// <returns>Task</returns>
        public async Task ChangeMuteUserState(ulong playerId, bool state)
        {
            try
            {
                if (state)
                {
                    Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.info.muting-user", new { playerId })}", LogLevel.Info);
                    await MuteUser(playerId);
                }
                else
                {
                    Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.info.unmuting-user", new { playerId })}", LogLevel.Info);
                    await UnmuteUser(playerId);
                }
            }
            catch (Exception ex)
            {
                Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.error.mute-state-failed", new { playerId, reason = ex.Message })}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Deaf a user by Stardew Valley player ID
        /// </summary>
        /// <param name="playerId">Stardew Valley player ID</param>
        /// <returns>Task</returns>
        public async Task DeafUser(long playerId)
        {
            await DeafUser(GetDiscordFarmerId(playerId));
        }

        /// <summary>
        /// Deaf a user by Discord ID
        /// </summary>
        /// <param name="userId">Discord user ID</param>
        /// <returns>Task</returns>
        public async Task DeafUser(ulong userId)
        {
            SocketGuildUser? user = Guild.GetUser(userId);

            if (user is null)
                return;

            await user.ModifyAsync(u => u.Deaf = true);
        }

        /// <summary>
        /// Undeaf a user by Stardew Valley player ID
        /// </summary>
        /// <param name="playerId">Stardew Valley player ID</param>
        /// <returns>Task</returns>
        public async Task UndeafUser(long playerId)
        {
            await UndeafUser(GetDiscordFarmerId(playerId));
        }

        /// <summary>
        /// Undeaf a user by Discord ID
        /// </summary>
        /// <param name="userId">Discord user ID</param>
        /// <returns>Task</returns>
        public async Task UndeafUser(ulong userId)
        {
            SocketGuildUser? user = Guild.GetUser(userId);

            if (user is null)
                return;

            await user.ModifyAsync(u => u.Deaf = false);
        }

        /// <summary>
        /// Change the deaf state of a user by Stardew Valley player ID
        /// </summary>
        /// <param name="playerId">Stardew Valley player ID</param>
        /// <param name="state">Deaf state (true = deaf, false = undeaf)</param>
        /// <returns>Task</returns>
        public async Task ChangeDeaferUserState(long playerId, bool state)
        {
            await ChangeDeaferUserState(GetDiscordFarmerId(playerId), state);
        }

        /// <summary>
        /// Change the deaf state of a user by Discord ID
        /// </summary>
        /// <param name="playerId">Discord user ID</param>
        /// <param name="state">Deaf state (true = deaf, false = undeaf)</param>
        /// <returns>Task</returns>
        public async Task ChangeDeaferUserState(ulong playerId, bool state)
        {
            try
            {
                if (state)
                {
                    Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.info.deafening-user", new { playerId })}", LogLevel.Info);
                    await DeafUser(playerId);
                }
                else
                {
                    Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.info.undeafening-user", new { playerId })}", LogLevel.Info);
                    await UndeafUser(playerId);
                }
            }
            catch (Exception ex)
            {
                Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.error.deaf-state-failed", new { playerId, reason = ex.Message })}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Change both mute and deaf states of a user by Stardew Valley player ID
        /// </summary>
        /// <param name="playerId">Stardew Valley player ID</param>
        /// <param name="muteState">Mute state</param>
        /// <param name="deafState">Deaf state</param>
        /// <returns>Task</returns>
        public async Task ChangeBothUserStates(long playerId, bool muteState, bool deafState)
        {
            _ = ChangeBothUserStates(GetDiscordFarmerId(playerId), muteState, deafState);
        }

        /// <summary>
        /// Change both mute and deaf states of a user by Discord ID
        /// </summary>
        /// <param name="playerId">Discord user ID</param>
        /// <param name="muteState">Mute state</param>
        /// <param name="deafState">Deaf state</param>
        /// <returns>Task</returns>
        public async Task ChangeBothUserStates(ulong playerId, bool muteState, bool deafState)
        {
            _ = ChangeMuteUserState(playerId, muteState);
            _ = ChangeDeaferUserState(playerId, deafState);
        }

        /// <summary>
        /// Reset all users' mute and deaf states to unmuted/undeafened
        /// </summary>
        /// <returns>Task</returns>
        public async Task ResetUsersState()
        {
            foreach (var playerInfo in Mod.Config.Host.SavesData[Game1.uniqueIDForThisGame].Players)
            {
                _ = UnmuteUser(playerInfo.Value.DiscordId);
                _ = UndeafUser(playerInfo.Value.DiscordId);
            }
        }
    }
}
