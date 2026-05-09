using Discord;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LVCMod
{
    partial class Bot
    {
        private Task OnLog(LogMessage message)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Get the Main Player Guild and channels
        /// </summary>
        /// <returns>Task</returns>
        private async Task OnReady()
        {
            Guild = DiscordClient.GetGuild(Mod.Config.Host.DiscordGuildId);

            var category = GetCategoryByName(Mod.Config.Bot.VoiceChatsCategoryName);
            if (category is null)
            {
                var newCat = await CreateVoiceChatsCategory();
                Mod.Config.Bot.VoiceChatsCategoryId = newCat.Id;
            }
            else
            {
                Mod.Config.Bot.VoiceChatsCategoryId = category.Id;
            }

            var mainChannel = GetVoiceChannelByName(Mod.Config.Bot.MainVoiceChatName);
            if (mainChannel is null)
            {
                var newChan = await CreateVoiceChannel(Mod.Config.Bot.MainVoiceChatName, false);
                Mod.Config.Bot.MainVoiceChatId = newChan.Id;
            }
            else
            {
                Mod.Config.Bot.MainVoiceChatId = mainChannel.Id;
            }

            Mod.Helper.WriteConfig(Mod.Config);
            await DiscordClient.SetCustomStatusAsync("Managing conversations");
            IsBotReady.SetResult(true);
        }

        public async Task WaitForReady()
        {
            await IsBotReady.Task;
        }

        /// <summary>
        /// Start the bot
        /// </summary>
        /// <returns>Task</returns>
        private async Task Start()
        {
            await DiscordClient.LoginAsync(TokenType.Bot, Mod.Config.Bot.Token);
            await DiscordClient.StartAsync();
        }
    }
}
