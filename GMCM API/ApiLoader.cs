using GenericModConfigMenu;
using StardewModdingAPI.Events;
using StardewModdingAPI;
using System;

namespace LVCMod
{
    internal sealed partial class ModEntry : Mod
    {
        private IGenericModConfigMenuApi GCMApi;

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            GCMApi = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            Config = Helper.ReadConfig<ModConfig>();

            if (GCMApi is null)
            {
                Monitor.Log(
                    Helper.Translation.Get("no-gmcm-installed.warning"),
                    LogLevel.Warn
                );
                return;
            }

            GCMApi.Register(
                ModManifest,
                () => Config = new ModConfig(),
                () => Helper.WriteConfig(Config),
                true
            );

            // --- KULLANICI AYARLARI ---
            GCMApi.AddSectionTitle(
                ModManifest,
                text: () => Helper.Translation.Get("user.gmcm.title")
            );

            // Discord ID
            GCMApi.AddTextOption(
                ModManifest,
                name: () => "Discord ID",
                getValue: () => Config.User.DiscordId.ToString(),
                setValue: value =>
                {
                    if (ulong.TryParse(value, out ulong id))
                        Config.User.DiscordId = id;
                    else
                        Config.User.DiscordId = 0;
                },
                tooltip: () => Config.User.DiscordId.ToString()
            );

            // TAKIM SEÇİMİ
            GCMApi.AddTextOption(
                ModManifest,
                name: () => Helper.Translation.Get("team-selection.label"),
                tooltip: () => Helper.Translation.Get("team-selection.tooltip"),
                getValue: () => Config.User.Team,
                setValue: value => Config.User.Team = value,
                allowedValues: new string[] { "Blue", "Red", "Green", "Yellow" }
            );

            GCMApi.AddPageLink(
                ModManifest,
                pageId: "bot",
                text: () => Helper.Translation.Get("host.gmcm.title"),
                tooltip : () => Helper.Translation.Get("bot.gmcm.title")
            );

            // --- BOT AYARLARI ---
            GCMApi.AddPage(
                ModManifest,
                pageId: "bot",
                pageTitle: () => Helper.Translation.Get("host.gmcm.title")
            );

            GCMApi.AddTextOption(
                ModManifest,
                name: () => Helper.Translation.Get("guild-id.label"),
                getValue: () => Config.Host.DiscordGuildId.ToString(),
                setValue: value =>
                {
                    if (ulong.TryParse(value, out ulong id))
                        Config.Host.DiscordGuildId = id;
                    else
                        Config.Host.DiscordGuildId = 0;
                },
                tooltip: () => Config.Host.DiscordGuildId.ToString()
            );

            GCMApi.AddTextOption(
                ModManifest,
                name: () => Helper.Translation.Get("host-bot-token.label"),
                getValue: () => Config.Bot.Token,
                setValue: value => Config.Bot.Token = value,
                tooltip: () => "Discord Bot Token"
            );

            GCMApi.AddTextOption(
                ModManifest,
                name: () => Helper.Translation.Get("main-voice-chat.label"),
                getValue: () => Config.Bot.MainVoiceChatName,
                setValue: value => Config.Bot.MainVoiceChatName = value
            );

            GCMApi.AddTextOption(
                ModManifest,
                name: () => Helper.Translation.Get("main-voice-chat-id.label"),
                getValue: () => Config.User.DiscordId.ToString(),
                setValue: value =>
                {
                    if (ulong.TryParse(value, out ulong id))
                        Config.Bot.MainVoiceChatId = id;
                    else
                        Config.Bot.MainVoiceChatId = 0;
                },
                tooltip: () => Config.User.DiscordId.ToString()
            );

            GCMApi.AddTextOption(
                ModManifest,
                name: () => Helper.Translation.Get("main-voice-chat-category.label"),
                getValue: () => Config.Bot.VoiceChatsCategoryName,
                setValue: value => Config.Bot.VoiceChatsCategoryName = value
            );

            GCMApi.AddTextOption(
                ModManifest,
                name: () => Helper.Translation.Get("main-voice-chat-category-id.label"),
                getValue: () => Config.User.DiscordId.ToString(),
                setValue: value =>
                {
                    if (ulong.TryParse(value, out ulong id))
                        Config.Bot.VoiceChatsCategoryId = id;
                    else
                        Config.Bot.VoiceChatsCategoryId = 0;
                },
                tooltip: () => Config.User.DiscordId.ToString()
            );

            GCMApi.AddBoolOption(
                ModManifest,
                name: () => Helper.Translation.Get("delete-voice-chats.label"),
                getValue: () => Config.Bot.DeleteVoiceChats,
                setValue: value => Config.Bot.DeleteVoiceChats = value
            );

            // --- SES AYARLARI ---
            //GCMApi.AddSectionTitle(
            //    ModManifest,
            //    text: () => Helper.Translation.Get("voice-chat.gmcm.title")
            //);

            //GCMApi.AddBoolOption(
            //    ModManifest,
            //    name: () => Helper.Translation.Get("microphone.label"),
            //    getValue: () => Config.User.MicrophoneActivated,
            //    setValue: value => Config.User.MicrophoneActivated = value
            //);

            //GCMApi.AddBoolOption(
            //    ModManifest,
            //    name: () => Helper.Translation.Get("deafer.label"),
            //    getValue: () => Config.User.DeaferDesactivated,
            //    setValue: value => Config.User.DeaferDesactivated = value
            //);

            // --- KONTROLLER ---
            //GCMApi.AddSectionTitle(
            //    ModManifest,
            //    text: () => Helper.Translation.Get("controls.title")
            //);

            //GCMApi.AddKeybind(
            //    ModManifest,
            //    name: () => Helper.Translation.Get("microphone-state.label"),
            //    getValue: () => Config.User.ChangeStateMicrophone,
            //    setValue: value => Config.User.ChangeStateMicrophone = value
            //);

            //GCMApi.AddKeybind(
            //    ModManifest,
            //    name: () => Helper.Translation.Get("deafer-state.label"),
            //    getValue: () => Config.User.ChangeStateAudio,
            //    setValue: value => Config.User.ChangeStateAudio = value
            //);
        }
    }
}