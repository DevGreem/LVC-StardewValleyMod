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

            GCMApi.AddPageLink(
                ModManifest,
                pageId: "user",
                text: () => Helper.Translation.Get("user.gmcm.title"),
                tooltip : () => Helper.Translation.Get("user.gmcm.tooltip")
            );

            GCMApi.AddPageLink(
                ModManifest,
                pageId: "bot",
                text: () => Helper.Translation.Get("host.gmcm.title"),
                tooltip : () => Helper.Translation.Get("host.bot.gmcm.title")
            );

            // --- USER SETTINGS ---
            GCMApi.AddPage(
                ModManifest,
                pageId: "user",
                pageTitle: () => Helper.Translation.Get("user.gmcm.title")
            );

            // Discord ID
            GCMApi.AddTextOption(
                ModManifest,
                name: () => "Discord ID",
                tooltip: () => Config.User.DiscordId.ToString(),
                getValue: () => Config.User.DiscordId.ToString(),
                setValue: value =>
                {
                    if (ulong.TryParse(value, out ulong id))
                        Config.User.DiscordId = id;
                    else
                        Config.User.DiscordId = 0;
                }
            );

            // Team Selection
            GCMApi.AddTextOption(
                ModManifest,
                name: () => Helper.Translation.Get("user.team-selection.label"),
                tooltip: () => Helper.Translation.Get("user.team-selection.tooltip"),
                getValue: () => Config.User.Team,
                setValue: value => Config.User.Team = value,
                allowedValues: new string[] { "None", "Blue", "Red", "Green", "Yellow" },
                formatAllowedValue: (string val) => { return Helper.Translation.Get($"user.team.{val.ToLower()}"); }
            );

            GCMApi.AddPageLink(
                ModManifest,
                pageId: "voice-chat",
                text: () => Helper.Translation.Get("voice-chat.gmcm.title"),
                tooltip : () => Helper.Translation.Get("voice-chat.gmcm.tooltip")
            );

            // --- VOICE CHAT SETTINGS ---
            GCMApi.AddPage(
                ModManifest,
                pageId: "voice-chat",
                pageTitle: () => Helper.Translation.Get("voice-chat.gmcm.title")
            );

            GCMApi.AddBoolOption(
                ModManifest,
                name: () => Helper.Translation.Get("voice-chat.microphone.label"),
                tooltip: () => Helper.Translation.Get("voice-chat.microphone.tooltip"),
                getValue: () => Config.User.Muted,
                setValue: value => Config.User.Muted = value
            );

            GCMApi.AddBoolOption(
                ModManifest,
                name: () => Helper.Translation.Get("voice-chat.deafer.label"),
                tooltip: () => Helper.Translation.Get("voice-chat.deafer.tooltip"),
                getValue: () => Config.User.Deafen,
                setValue: value => Config.User.Deafen = value
            );

            // Key Bindings
            GCMApi.AddSectionTitle(
                ModManifest,
                text: () => Helper.Translation.Get("voice-chat.bindings.title")
            );

            GCMApi.AddBoolOption(
                ModManifest,
                name: () => Helper.Translation.Get("voice-chat.bindings.enabled.label"),
                tooltip: () => Helper.Translation.Get("voice-chat.bindings.enabled.tooltip"),
                getValue: () => Config.User.EnableVoiceHotkeys,
                setValue: value => Config.User.EnableVoiceHotkeys = value
            );

            GCMApi.AddKeybind(
                ModManifest,
                name: () => Helper.Translation.Get("voice-chat.bindings.microphone-state.label"),
                tooltip: () => Helper.Translation.Get("voice-chat.bindings.microphone-state.tooltip"),
                getValue: () => Config.User.ChangeStateMute,
                setValue: value => Config.User.ChangeStateMute = value
            );

            GCMApi.AddKeybind(
                ModManifest,
                name: () => Helper.Translation.Get("voice-chat.bindings.deafer-state.label"),
                tooltip: () => Helper.Translation.Get("voice-chat.bindings.deafer-state.tooltip"),
                getValue: () => Config.User.ChangeStateDeaf,
                setValue: value => Config.User.ChangeStateDeaf = value
            );

            // --- BOT SETTINGS ---
            GCMApi.AddPage(
                ModManifest,
                pageId: "bot",
                pageTitle: () => Helper.Translation.Get("host.gmcm.title")
            );

            GCMApi.AddTextOption(
                ModManifest,
                name: () => "Discord Bot Token",
                getValue: () => Config.Bot.Token,
                setValue: value => Config.Bot.Token = value
            );

            GCMApi.AddTextOption(
                ModManifest,
                name: () => Helper.Translation.Get("host.guild-id.label"),
                tooltip: () => Config.Host.DiscordGuildId.ToString(),
                getValue: () => Config.Host.DiscordGuildId.ToString(),
                setValue: value =>
                {
                    if (ulong.TryParse(value, out ulong id))
                        Config.Host.DiscordGuildId = id;
                    else
                        Config.Host.DiscordGuildId = 0;
                }
            );

            GCMApi.AddTextOption(
                ModManifest,
                name: () => Helper.Translation.Get("host.voice-chat-category-id.label"),
                tooltip: () => Config.User.DiscordId.ToString(),
                getValue: () => Config.User.DiscordId.ToString(),
                setValue: value =>
                {
                    if (ulong.TryParse(value, out ulong id))
                        Config.Bot.VoiceChatsCategoryId = id;
                    else
                        Config.Bot.VoiceChatsCategoryId = 0;
                }
            );

            GCMApi.AddTextOption(
                ModManifest,
                name: () => Helper.Translation.Get("host.voice-chat-category-name.label"),
                tooltip: () => Helper.Translation.Get("host.voice-chat-category-name.tooltip"),
                getValue: () => Config.Bot.VoiceChatsCategoryName,
                setValue: value => Config.Bot.VoiceChatsCategoryName = value
            );

            GCMApi.AddTextOption(
                ModManifest,
                name: () => Helper.Translation.Get("host.voice-chat-channel-id.label"),
                tooltip: () => Config.User.DiscordId.ToString(),
                getValue: () => Config.User.DiscordId.ToString(),
                setValue: value =>
                {
                    if (ulong.TryParse(value, out ulong id))
                        Config.Bot.MainVoiceChatId = id;
                    else
                        Config.Bot.MainVoiceChatId = 0;
                }
            );

            GCMApi.AddTextOption(
                ModManifest,
                name: () => Helper.Translation.Get("host.voice-chat-channel-name.label"),
                tooltip: () => Helper.Translation.Get("host.voice-chat-channel-name.tooltip"),
                getValue: () => Config.Bot.MainVoiceChatName,
                setValue: value => Config.Bot.MainVoiceChatName = value
            );

            GCMApi.AddBoolOption(
                ModManifest,
                name: () => Helper.Translation.Get("host.delete-voice-chats.label"),
                tooltip: () => Helper.Translation.Get("host.delete-voice-chats.tooltip"),
                getValue: () => Config.Bot.DeleteVoiceChats,
                setValue: value => Config.Bot.DeleteVoiceChats = value
            );
        }
    }
}