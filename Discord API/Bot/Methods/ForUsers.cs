using Discord.Rest;
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
        /// Move a Farmer to a voice chat
        /// </summary>
        /// <param name="playerId">Farmer Id</param>
        /// <param name="newLocation">New Location of the User</param>
        /// <returns>Task</returns>
        public async Task MoveToVoice(long playerId, string newLocation)
        {
            newLocation = MergeLocations(newLocation);
            _ = MoveToVoice(GetDiscordFarmerId(playerId), newLocation);
        }

        private static string MergeLocations(string currentLocation) {
            if (currentLocation.Contains("UndergroundMine")) {
                return currentLocation.Substring(0, currentLocation.Length - 1);
            } else if (currentLocation == "FarmHouse") {
                return "Cabin";
            }

            return currentLocation;
        }

        public async Task MoveToVoice(ulong playerId, string? newLocation)
        {
            SocketGuildUser? user = Guild.GetUser(playerId);

            if (user is null)
                return;

            if (user.VoiceChannel is null)
                return;

            SocketVoiceChannel? voiceChannel = GetVoiceChannelByName(newLocation);

            if (voiceChannel is null)
            {
                RestVoiceChannel channelId = await CreateVoiceChannel(newLocation);

                voiceChannel = Guild.GetVoiceChannel(channelId.Id);
            }

            SocketVoiceChannel previousVoiceChannel = user.VoiceChannel;

            await user.ModifyAsync(x => x.Channel = voiceChannel);

            if (previousVoiceChannel is null)
                return;

            await DeleteVoiceChannel(
                previousVoiceChannel.Name,
                () =>
                {
                    if (previousVoiceChannel.Users.Count == 0)
                        return true;

                    return false;
                }
            );
        }

        /// <summary>
        /// Mute a user
        /// </summary>
        /// <param name="playerId">Stardew Valley ID</param>
        /// <returns>Task</returns>
        public async Task MuteUser(long playerId)
        {
            await MuteUser(GetDiscordFarmerId(playerId));
        }

        /// <summary>
        /// Mute a user
        /// </summary>
        /// <param name="userId">Discord ID</param>
        /// <returns>Task</returns>
        public async Task MuteUser(ulong userId)
        {
            SocketGuildUser? user = Guild.GetUser(userId);

            if (user is null)
                return;

            await user.ModifyAsync(u => u.Mute = true);
        }

        /// <summary>
        /// Unmute a user
        /// </summary>
        /// <param name="playerId">Stardew Valley ID</param>
        /// <returns>Task</returns>
        public async Task UnmuteUser(long playerId)
        {
            await UnmuteUser(GetDiscordFarmerId(playerId));
        }

        /// <summary>
        /// Unmute a user
        /// </summary>
        /// <param name="userId">Discord ID</param>
        /// <returns>Task</returns>
        public async Task UnmuteUser(ulong userId)
        {
            SocketGuildUser? user = Guild.GetUser(userId);

            if (user is null)
                return;

            await user.ModifyAsync(u => u.Mute = false);
        }

        /// <summary>
        /// Change the mute state of a user
        /// </summary>
        /// <param name="playerId">Stardew Valley ID</param>
        /// <param name="state">Mute State</param>
        /// <returns>Task</returns>
        public async Task ChangeMuteUserState(long playerId, bool state)
        {
            await ChangeMuteUserState(GetDiscordFarmerId(playerId), state);
        }

        /// <summary>
        /// Change the mute state of a user
        /// </summary>
        /// <param name="playerId">Discord ID</param>
        /// <param name="state">Mute State</param>
        /// <returns>Task</returns>
        public async Task ChangeMuteUserState(ulong playerId, bool state)
        {
            if (state)
            {
                await UnmuteUser(playerId);
                return;
            }

            await MuteUser(playerId);
        }

        /// <summary>
        /// Deaf a user
        /// </summary>
        /// <param name="playerId">Stardew Valley ID</param>
        /// <returns>Task</returns>
        public async Task DeafUser(long playerId)
        {
            await DeafUser(GetDiscordFarmerId(playerId));
        }

        /// <summary>
        /// Deaf a user
        /// </summary>
        /// <param name="userId">Discord ID</param>
        /// <returns>Task</returns>
        public async Task DeafUser(ulong userId)
        {
            SocketGuildUser? user = Guild.GetUser(userId);

            if (user is null)
                return;

            await user.ModifyAsync(u => u.Deaf = true);
        }

        /// <summary>
        /// Undeaf a user
        /// </summary>
        /// <param name="playerId">Stardew Valley ID</param>
        /// <returns>Task</returns>
        public async Task UndeafUser(long playerId)
        {
            await UndeafUser(GetDiscordFarmerId(playerId));
        }

        /// <summary>
        /// Undeaf a user
        /// </summary>
        /// <param name="userId">Discord ID</param>
        /// <returns>Task</returns>
        public async Task UndeafUser(ulong userId)
        {
            SocketGuildUser? user = Guild.GetUser(userId);

            if (user is null)
                return;

            await user.ModifyAsync(u => u.Deaf = false);
        }

        /// <summary>
        /// Change the deaf state of a user
        /// </summary>
        /// <param name="playerId">Stardew Valley ID</param>
        /// <param name="state">Deaf State</param>
        /// <returns>Task</returns>
        public async Task ChangeDeaferUserState(long playerId, bool state)
        {
            await ChangeDeaferUserState(GetDiscordFarmerId(playerId), state);
        }

        /// <summary>
        /// Change the deaf state of a user
        /// </summary>
        /// <param name="playerId">Discord ID</param>
        /// <param name="state">Deaf State</param>
        /// <returns>Task</returns>
        public async Task ChangeDeaferUserState(ulong playerId, bool state)
        {
            if (!state)
            {
                await DeafUser(playerId);
                return;
            }

            await UndeafUser(playerId);
        }

        public async Task ChangeBothUserStates(long playerId, bool muteState, bool deafState)
        {
            _ = ChangeBothUserStates(GetDiscordFarmerId(playerId), muteState, deafState);
        }

        public async Task ChangeBothUserStates(ulong playerId, bool muteState, bool deafState)
        {
            _ = ChangeMuteUserState(playerId, muteState);
            _ = ChangeDeaferUserState(playerId, deafState);
        }

        public async Task ResetUsersState()
        {
            foreach (KeyValuePair<long, ulong> playerInfo in Mod.Config.Host.SavesData[Game1.uniqueIDForThisGame].Players)
            {
                _ = UnmuteUser(playerInfo.Value);
                _ = UndeafUser(playerInfo.Value);
            }
        }
    }
}
