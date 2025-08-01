using System;
using System.Diagnostics;
using GenericModConfigMenu;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Discord.Rest;
using Discord.WebSocket;
using Discord;

namespace LVCMod
{
    internal sealed partial class ModEntry : Mod
    {
        public ModConfig Config;
        private Bot HostBot;

        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        #region EVENTS

        private async void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            if (!Context.IsMultiplayer)
                return;

            if (Config.User.DiscordId == 0)
            {
                Monitor.Log(
                    Helper.Translation.Get("no-user-id.error"),
                    LogLevel.Error
                );
                return;
            }

            LoadEvents();

            if (Context.IsMainPlayer)
            {
                if (Config.Host.DiscordGuildId == 0)
                {
                    Monitor.Log(
                        Helper.Translation.Get("no-guild-id.error"),
                        LogLevel.Error
                    );
                    UnloadEvents();
                    return;
                }

                if (Config.Bot.Token == "")
                {
                    Monitor.Log(
                        Helper.Translation.Get("no-bot-token.error"),
                        LogLevel.Error
                    );
                    UnloadEvents();
                    return;
                }

                HostBot = new Bot(this);
                await HostBot.WaitForReady();

                ulong saveId = Game1.uniqueIDForThisGame;

                Config.Host.SavesData.TryAdd(saveId, new PlayerData());

                Config.Host.SavesData[saveId].Players[Game1.MasterPlayer.UniqueMultiplayerID] = Config.User.DiscordId;

                _ = HostBot.ChangeBothUserStates(
                    Game1.MasterPlayer.UniqueMultiplayerID,
                    Config.User.MicrophoneActivated,
                    Config.User.DeaferDesactivated
                );

                SaveConfig();
                return;
            }

            SendMessageToMain(Config, MessageTypes.PlayerJoined);
        }

        private async void OnMessageReceived(object? sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID != ModManifest.UniqueID)
                return;

            if (!Context.IsMainPlayer)
                return;

            if (e.Type == (MessageType)MessageTypes.PlayerJoined)
            {
                ModConfig clientConfig = e.ReadAs<ModConfig>();

                Config.Host.SavesData[Game1.uniqueIDForThisGame].Players[e.FromPlayerID] = clientConfig.User.DiscordId;

                _ = HostBot.ChangeMuteUserState(clientConfig.User.DiscordId, clientConfig.User.MicrophoneActivated);

                SaveConfig();

                return;
            }

            if (e.Type == (MessageType)MessageTypes.PlayerWarped)
            {
                if (!Config.Bot.IsWarpActive)
                    return;

                var data = e.ReadAs<(long PlayerId, string NewLocation)>();

                _ = HostBot.MoveToVoice(data.PlayerId, data.NewLocation);

                return;
            }

            if (e.Type == (MessageType)MessageTypes.ChangePlayerMicrophoneState)
            {
                var playerInfo = e.ReadAs<(long Id, bool State)>();

                _ = HostBot.ChangeMuteUserState(playerInfo.Id, playerInfo.State);

                return;
            }

            if (e.Type == (MessageType)MessageTypes.ChangePlayerDeaferState)
            {
                var playerInfo = e.ReadAs<(long Id, bool State)>();

                _ = HostBot.ChangeDeaferUserState(playerInfo.Id, playerInfo.State);

                return;
            }
        }

        private async void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            UnloadEvents();

            if (!Context.IsMainPlayer)
                return;

            if (Config.Bot.DeleteVoiceChats)
            {
                await HostBot.ResetUsersState();
                await HostBot.CloseVoiceChat();
            }
        }

        private async void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (Context.IsMainPlayer)
            {
                if (!Config.Bot.IsWarpActive)
                    return;

                _ = HostBot.MoveToVoice(e.Player.UniqueMultiplayerID, e.NewLocation.Name);
                return;
            }

            (long, string) message = (e.Player.UniqueMultiplayerID, e.NewLocation.Name);

            SendMessageToMain(message, MessageTypes.PlayerWarped);
        }

        private async void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {

            if (e.Button == Config.User.ChangeStateMicrophone)
            {
                Config.User.MicrophoneActivated = !Config.User.MicrophoneActivated;
                SaveConfig();

                (long Id, bool State) userInfo = (Game1.player.UniqueMultiplayerID, Config.User.MicrophoneActivated);

                if (Context.IsMainPlayer)
                {
                    _ = HostBot.ChangeMuteUserState(userInfo.Id, userInfo.State);
                    return;
                }

                SendMessageToMain(userInfo, MessageTypes.ChangePlayerMicrophoneState);

                return;
            }

            if (e.Button == Config.User.ChangeStateAudio)
            {
                Config.User.DeaferDesactivated = !Config.User.DeaferDesactivated;
                SaveConfig();

                (long Id, bool State) userInfo = (Game1.player.UniqueMultiplayerID, Config.User.DeaferDesactivated);

                if (Context.IsMainPlayer)
                {
                    _ = HostBot.ChangeDeaferUserState(userInfo.Id, userInfo.State);
                    return;
                }

                SendMessageToMain(userInfo, MessageTypes.ChangePlayerDeaferState);

                return;
            }

            if(e.Button == Config.Bot.ChangeWarpState) {
                if (!Context.IsMainPlayer)
                    return;

                Config.Bot.IsWarpActive  = !Config.Bot.IsWarpActive;
                List<long> players = Config.Host.SavesData[Game1.uniqueIDForThisGame].Players.Keys.ToList();

                if (Config.Bot.IsWarpActive) {
                    foreach (var player in players) {
                        Farmer? farmer = Game1.GetPlayer(player);
                        string farmerLocation = farmer == default ? Config.Bot.MainVoiceChatName : farmer.currentLocation.Name;
                        _ = HostBot.MoveToVoice(player, farmerLocation);
                    }
                } else {
                    foreach (var player in players) {
                        _ = HostBot.MoveToVoice(player, Config.Bot.MainVoiceChatName);
                    }
                }
            }
        }

        #endregion

        #region METHODS

        private void SendMessageToMain<TMessage>(TMessage message, MessageType messageType)
        {
            Helper.Multiplayer.SendMessage(
                message,
                messageType,
                new[] { ModManifest.UniqueID },
                new[] { Game1.MasterPlayer.UniqueMultiplayerID }
            );
        }

        private void LoadEvents()
        {
            Helper.Events.Multiplayer.ModMessageReceived += OnMessageReceived;
            Helper.Events.Player.Warped += OnWarped;
            Helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            Helper.Events.Input.ButtonPressed += OnButtonPressed;
        }

        private void UnloadEvents()
        {
            Helper.Events.Multiplayer.ModMessageReceived -= OnMessageReceived;
            Helper.Events.Player.Warped -= OnWarped;
            Helper.Events.GameLoop.ReturnedToTitle -= OnReturnedToTitle;
            Helper.Events.Input.ButtonPressed -= OnButtonPressed;
        }

        private void SaveConfig()
        {
            Helper.WriteConfig(Config);
        }

        #endregion
    }
}
