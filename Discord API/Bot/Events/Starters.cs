using Discord;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewModdingAPI;

namespace LVCMod
{
    partial class Bot
    {
        private Task OnLog(LogMessage message)
        {
            LogLevel level = message.Severity switch
            {
                LogSeverity.Critical => LogLevel.Error,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Warning => LogLevel.Warn,
                LogSeverity.Info => LogLevel.Info,
                LogSeverity.Verbose => LogLevel.Trace,
                LogSeverity.Debug => LogLevel.Trace,
                _ => LogLevel.Info
            };

            string text = $"[LVC-BOT] {message.Source}: {message.Message}";
            if (message.Exception != null)
                text += $" | Exception: {message.Exception.Message}";

            Mod.Monitor.Log(text, level);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Get the Main Player Guild and channels
        /// </summary>
        /// <returns>Task</returns>
        private async Task OnReady()
        {
            Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.info.bot-ready")}", LogLevel.Info);

            Guild = DiscordClient.GetGuild(Mod.Config.Host.DiscordGuildId);
            if (Guild is null)
            {
                Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.error.guild-not-found", new { guildId = Mod.Config.Host.DiscordGuildId })}", LogLevel.Error);
            }
            else
            {
                Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.info.guild-connected", new { guildName = Guild.Name, guildId = Guild.Id })}", LogLevel.Info);
            }

            var category = GetCategoryByName(Mod.Config.Bot.VoiceChatsCategoryName);
            if (category is null)
            {
                var newCat = await CreateVoiceChatsCategory();
                Mod.Config.Bot.VoiceChatsCategoryId = newCat.Id;
                Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.info.category-created", new { categoryName = newCat.Name, categoryId = newCat.Id })}", LogLevel.Info);
            }
            else
            {
                Mod.Config.Bot.VoiceChatsCategoryId = category.Id;
                Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.info.category-found", new { categoryName = category.Name, categoryId = category.Id })}", LogLevel.Info);
            }

            var mainChannel = GetVoiceChannelByName(Mod.Config.Bot.MainVoiceChatName);
            if (mainChannel is null)
            {
                var newChan = await CreateVoiceChannel(Mod.Config.Bot.MainVoiceChatName, false);
                Mod.Config.Bot.MainVoiceChatId = newChan.Id;
                Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.info.main-channel-created", new { channelName = newChan.Name, channelId = newChan.Id })}", LogLevel.Info);
            }
            else
            {
                Mod.Config.Bot.MainVoiceChatId = mainChannel.Id;
                Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.info.main-channel-found", new { channelName = mainChannel.Name, channelId = mainChannel.Id })}", LogLevel.Info);
            }

            Mod.Helper.WriteConfig(Mod.Config);
            await DiscordClient.SetCustomStatusAsync($"{Mod.Helper.Translation.Get("host.bot.activity.label")}");
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
            try
            {
                Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.info.bot-starting")}", LogLevel.Info);
                await DiscordClient.LoginAsync(TokenType.Bot, Mod.Config.Bot.Token);
                await DiscordClient.StartAsync();
                Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.info.bot-login-complete")}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Mod.Monitor.Log($"[LVC] {Mod.Helper.Translation.Get("log.error.bot-login-failed", new { reason = ex.Message })}", LogLevel.Error);
                Mod.Monitor.Log(ex.ToString(), LogLevel.Error);
                IsBotReady.TrySetException(ex);
                throw;
            }
        }
    }
}
