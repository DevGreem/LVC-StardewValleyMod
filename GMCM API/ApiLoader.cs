using GenericModConfigMenu;
using StardewModdingAPI.Events;
using StardewModdingAPI;

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

            GCMApi.AddSectionTitle(
                ModManifest,
                text: () => Helper.Translation.Get("user.gmcm.title")
            );

            GCMApi.AddTextOption(
                ModManifest,
                name: () => "Discord ID",
                getValue: () => Config.User.DiscordId.ToString(),
                setValue: value =>
                {
                    ulong newValue;

                    try
                    {
                        newValue = ulong.Parse(value);
                    }
                    catch
                    {
                        newValue = 0;
                    }

                    Config.User.DiscordId = newValue;
                },
                tooltip: () => Config.User.DiscordId.ToString()
            );

            GCMApi.AddSectionTitle(
                ModManifest,
                text: () => Helper.Translation.Get("host.gmcm.title")
            );

            GCMApi.AddTextOption(
                ModManifest,
                name: () => Helper.Translation.Get("guild-id.label"),
                getValue: () => Config.Host.DiscordGuildId.ToString(),
                setValue: value =>
                {
                    ulong newValue;

                    try
                    {
                        newValue = ulong.Parse(value);
                    }
                    catch
                    {
                        newValue = 0;
                    }

                    Config.Host.DiscordGuildId = newValue;
                },
                tooltip: () => Config.Host.DiscordGuildId.ToString()
            );

            GCMApi.AddSectionTitle(
                ModManifest,
                text: () => Helper.Translation.Get("bot.gmcm.title")
            );

            GCMApi.AddTextOption(
                ModManifest,
                name: () => Helper.Translation.Get("host-bot-token.label"),
                getValue: () => Config.Bot.Token,
                setValue: value => Config.Bot.Token = value,
                tooltip: () => Config.Bot.Token
            );

            GCMApi.AddTextOption(
                ModManifest,
                name: () => Helper.Translation.Get("main-voice-chat.label"),
                getValue: () => Config.Bot.MainVoiceChatName,
                setValue: value => Config.Bot.MainVoiceChatName = value,
                tooltip: () => Config.Bot.MainVoiceChatName
            );

            GCMApi.AddTextOption(
                ModManifest,
                name: () => Helper.Translation.Get("main-voice-chat-category.label"),
                getValue: () => Config.Bot.VoiceChatsCategoryName,
                setValue: value => Config.Bot.VoiceChatsCategoryName = value,
                tooltip: () => Config.Bot.VoiceChatsCategoryName
            );

            GCMApi.AddBoolOption(
                ModManifest,
                name: () => Helper.Translation.Get("delete-voice-chats.label"),
                getValue: () => Config.Bot.DeleteVoiceChats,
                setValue: value => Config.Bot.DeleteVoiceChats = value
            );

            GCMApi.AddSectionTitle(
                ModManifest,
                text: () => Helper.Translation.Get("voice-chat.gmcm.title")
            );

            GCMApi.AddBoolOption(
                ModManifest,
                name: () => Helper.Translation.Get("microphone.label"),
                getValue: () => Config.User.MicrophoneActivated,
                setValue: value => Config.User.MicrophoneActivated = value
            );

            GCMApi.AddBoolOption(
                ModManifest,
                name: () => Helper.Translation.Get("deafer.label"),
                getValue: () => Config.User.DeaferDesactivated,
                setValue: value => Config.User.DeaferDesactivated = value
            );

            GCMApi.AddSectionTitle(
                ModManifest,
                text: () => Helper.Translation.Get("controls.title")
            );

            GCMApi.AddKeybind(
                ModManifest,
                name: () => Helper.Translation.Get("bot-state.label"),
                getValue: () => Config.Bot.ChangeWarpState,
                setValue: value => Config.Bot.ChangeWarpState = value
            );

            GCMApi.AddKeybind(
                ModManifest,
                name: () => Helper.Translation.Get("microphone-state.label"),
                getValue: () => Config.User.ChangeStateMicrophone,
                setValue: value => Config.User.ChangeStateMicrophone = value
            );

            GCMApi.AddKeybind(
                ModManifest,
                name: () => Helper.Translation.Get("deafer-state.label"),
                getValue: () => Config.User.ChangeStateAudio,
                setValue: value => Config.User.ChangeStateAudio = value
            );
        }
    }
}
