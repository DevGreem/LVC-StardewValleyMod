using Discord.Rest;
using Discord.WebSocket;
using Discord;
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
        /// Create a voice channel
        /// </summary>
        /// <param name="channelName">Channel Name</param>
        /// <param name="canTalk">Everyone can talk in the voice chat</param>
        /// <returns>RestVoiceChannel</returns>
        public async Task<RestVoiceChannel> CreateVoiceChannel(string channelName, bool canTalk = true)
        {
            RestVoiceChannel newChannel = await Guild.CreateVoiceChannelAsync(
                channelName,
                c =>
                {
                    c.CategoryId = GetCategoryByName(Mod.Config.Bot.VoiceChatsCategoryName).Id;

                    if (canTalk)
                        return;

                    c.PermissionOverwrites = new Optional<IEnumerable<Overwrite>>(
                        new List<Overwrite>()
                        {
                            new Overwrite(
                                Guild.EveryoneRole.Id,
                                PermissionTarget.Role,
                                new OverwritePermissions(
                                    speak: PermValue.Deny,
                                    sendMessages: PermValue.Deny,
                                    useExternalSounds: PermValue.Deny
                                    )
                            )
                        }
                    );
                }
                );

            return newChannel;
        }

        /// <summary>
        /// Create the Main Category for the bot voice channels
        /// </summary>
        /// <returns>RestCategoryChannel</returns>
        public async Task<RestCategoryChannel> CreateVoiceChatsCategory()
        {
            RestCategoryChannel newCategory = await Guild.CreateCategoryChannelAsync(Mod.Config.Bot.VoiceChatsCategoryName);

            return newCategory;
        }

        /// <summary>
        /// Delete a voice channel if the condition is true or null
        /// </summary>
        /// <param name="voiceChannel">Voice Channel To Delete</param>
        /// <param name="beforeCondition">Condition to delete</param>
        /// <returns>Task</returns>
        public static async Task DeleteVoiceChannel(SocketVoiceChannel voiceChannel, Func<bool> beforeCondition = null)
        {
            if (beforeCondition is not null)
            {
                if (!beforeCondition())
                    return;
            }

            _ = voiceChannel.DeleteAsync();
        }

        /// <summary>
        /// Delete a voice channel searching by name if the condition is true or null
        /// </summary>
        /// <param name="voiceChannelName">Channel Name to Search</param>
        /// <param name="beforeCondition">Condition</param>
        /// <returns>Task</returns>
        public async Task DeleteVoiceChannel(string voiceChannelName, Func<bool> beforeCondition = null)
        {
            SocketVoiceChannel voiceChannel = GetVoiceChannelByName(voiceChannelName);

            if (voiceChannel is null)
                return;

            _ = DeleteVoiceChannel(voiceChannel, beforeCondition);
        }

        /// <summary>
        /// Delete the voice channels that the bot created for the mod (Except the main one)
        /// </summary>
        /// <returns>Task</returns>
        public async Task CloseVoiceChat()
        {
            SocketVoiceChannel[] voiceChannels = Guild.VoiceChannels
                .Where(c => c.Category.Name == Mod.Config.Bot.VoiceChatsCategoryName)
                .Where(c => c.Name != Mod.Config.Bot.MainVoiceChatName)
                .ToArray();

            foreach (SocketVoiceChannel voiceChannel in voiceChannels)
            {
                await DeleteVoiceChannel(voiceChannel);
            }
        }
    }
}
