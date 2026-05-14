using System;
using System.Collections.Generic;
using System.Diagnostics;
using GenericModConfigMenu;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

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

            Monitor.Log($"[LVC] {Helper.Translation.Get("log.info.save-loaded", new { isMainPlayer = Context.IsMainPlayer, discordId = Config.User.DiscordId, tokenSet = !string.IsNullOrEmpty(Config.Bot.Token) })}", LogLevel.Info);

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
                Monitor.Log($"[LVC] {Helper.Translation.Get("log.info.starting-bot")}", LogLevel.Info);

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

                Config.Host.SavesData[saveId].Players[Game1.MasterPlayer.UniqueMultiplayerID] = new FarmerInfo
                {
                    DiscordId = Config.User.DiscordId,
                    Team = Config.User.Team
                };

                _ = HostBot.ResetUsersState();

                SaveConfig();
                return;
            }

            Monitor.Log($"[LVC] {Helper.Translation.Get("log.info.client-joining")}", LogLevel.Info);
            SendMessageToMain(Config, MessageTypes.PlayerJoined);
        }

        private async void OnMessageReceived(object? sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID != ModManifest.UniqueID)
                return;

            if (e.Type == (MessageType)MessageTypes.LocationChannelsSynced)
            {
                var channelMap = e.ReadAs<Dictionary<string, ulong>>();
                if (channelMap != null)
                {
                    Monitor.Log($"[LVC] {Helper.Translation.Get("log.info.location-channels-received", new { count = channelMap.Count })}", LogLevel.Info);
                    foreach (var mapping in channelMap)
                    {
                        Config.Host.LocationChannels[mapping.Key] = mapping.Value;
                        Monitor.Log($"[LVC] {Helper.Translation.Get("log.info.channel-mapping", new { key = mapping.Key, value = mapping.Value })}", LogLevel.Info);
                    }
                    SaveConfig();
                    Monitor.Log($"[LVC] {Helper.Translation.Get("log.info.config-saved")}", LogLevel.Info);
                }
                return;
            }

            if (!Context.IsMainPlayer)
                return;

            if (e.Type == (MessageType)MessageTypes.PlayerJoined)
            {
                ModConfig clientConfig = e.ReadAs<ModConfig>();

                Config.Host.SavesData[Game1.uniqueIDForThisGame].Players[e.FromPlayerID] = new FarmerInfo
                {
                    DiscordId = clientConfig.User.DiscordId,
                    Team = clientConfig.User.Team
                };

                Monitor.Log($"[LVC] {Helper.Translation.Get("log.info.player-joined", new { playerId = e.FromPlayerID, team = clientConfig.User.Team })}", LogLevel.Info);
                SaveConfig();

                Monitor.Log($"[LVC] {Helper.Translation.Get("log.info.sending-channels", new { count = Config.Host.LocationChannels.Count, playerId = e.FromPlayerID })}", LogLevel.Info);
                SendMessageToPlayer(Config.Host.LocationChannels, MessageTypes.LocationChannelsSynced, e.FromPlayerID);
            }


            if (e.Type == (MessageType)MessageTypes.PlayerWarped)
            {
                var data = e.ReadAs<(long PlayerId, string NewLocation)>();

                _ = HostBot.MoveToVoice(data.PlayerId, data.NewLocation);

                return;
            }

            if (e.Type == (MessageType)MessageTypes.PlayerWarpedWithChannel)
            {
                var data = e.ReadAs<(ulong DiscordId, ulong ChannelId)>();

                _ = HostBot.MoveToVoice(data.DiscordId, data.ChannelId);

                return;
            }

            if (e.Type == (MessageType)MessageTypes.ChangePlayerMicrophoneState)
            {
                var playerInfo = e.ReadAs<(long Id, bool State)>();
                Monitor.Log($"[LVC] {Helper.Translation.Get("log.info.microphone-state-change", new { playerId = playerInfo.Id, state = playerInfo.State })}", LogLevel.Info);

                _ = HostBot.ChangeMuteUserState(playerInfo.Id, playerInfo.State);

                return;
            }

            if (e.Type == (MessageType)MessageTypes.ChangePlayerDeaferState)
            {
                var playerInfo = e.ReadAs<(long Id, bool State)>();
                Monitor.Log($"[LVC] {Helper.Translation.Get("log.info.deafer-state-change", new { playerId = playerInfo.Id, state = playerInfo.State })}", LogLevel.Info);

                _ = HostBot.ChangeDeaferUserState(playerInfo.Id, playerInfo.State);

                return;
            }
        }

        private async void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            UnloadEvents();

            if (!Context.IsMainPlayer)
                return;

            await HostBot.ResetUsersState();

            if (Config.Bot.DeleteVoiceChats)
            {
                await HostBot.CloseVoiceChat();
                Config.Host.LocationChannels.Clear();
                SaveConfig();
            }
        }

        private async void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (Context.IsMainPlayer)
            {
                _ = HostBot.MoveToVoice(e.Player.UniqueMultiplayerID, e.NewLocation.Name);
                return;
            }

            string mergedLocation = MergeLocationWithTeam(e.NewLocation.Name, Config.User.Team);
            Monitor.Log($"[LVC] {Helper.Translation.Get("log.info.warp-occurred", new { oldLocation = e.NewLocation.Name, mergedLocation, count = Config.Host.LocationChannels.Count })}", LogLevel.Info);

            if (Config.Host.LocationChannels.TryGetValue(mergedLocation, out ulong channelId) && channelId != 0)
            {
                (ulong DiscordId, ulong ChannelId) message = (Config.User.DiscordId, channelId);
                Monitor.Log($"[LVC] {Helper.Translation.Get("log.info.channel-found", new { mergedLocation, channelId })}", LogLevel.Info);
                SendMessageToMain(message, MessageTypes.PlayerWarpedWithChannel);
                return;
            }

            Monitor.Log($"[LVC] {Helper.Translation.Get("log.warn.channel-id-missing", new { mergedLocation })}", LogLevel.Warn);
            (long, string) fallbackMessage = (e.Player.UniqueMultiplayerID, mergedLocation);
            SendMessageToMain(fallbackMessage, MessageTypes.PlayerWarped);
        }

        private async void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Config.User.EnableVoiceHotkeys &&
                (e.Button == Config.User.ChangeStateMute || e.Button == Config.User.ChangeStateDeaf))
            {
                return;
            }

            if (e.Button == Config.User.ChangeStateMute)
            {
                Config.User.Muted = !Config.User.Muted;
                SaveConfig();

                (long Id, bool State) userInfo = (Game1.player.UniqueMultiplayerID, Config.User.Muted);

                if (Context.IsMainPlayer)
                {
                    _ = HostBot.ChangeMuteUserState(userInfo.Id, userInfo.State);
                    return;
                }

                SendMessageToMain(userInfo, MessageTypes.ChangePlayerMicrophoneState);

                return;
            }

            if (e.Button == Config.User.ChangeStateDeaf)
            {
                Config.User.Deafen = !Config.User.Deafen;
                SaveConfig();

                (long Id, bool State) userInfo = (Game1.player.UniqueMultiplayerID, Config.User.Deafen);

                if (Context.IsMainPlayer)
                {
                    _ = HostBot.ChangeDeaferUserState(userInfo.Id, userInfo.State);
                    return;
                }

                SendMessageToMain(userInfo, MessageTypes.ChangePlayerDeaferState);

                return;
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

        private void SendMessageToPlayer<TMessage>(TMessage message, MessageType messageType, long playerId)
        {
            Helper.Multiplayer.SendMessage(
                message,
                messageType,
                new[] { ModManifest.UniqueID },
                new[] { playerId }
            );
        }

        internal void BroadcastLocationChannels()
        {
            if (!Context.IsMainPlayer)
                return;

            Monitor.Log($"[LVC] {Helper.Translation.Get("log.info.broadcasting-location-channels", new { count = Config.Host.LocationChannels.Count })}", LogLevel.Info);
            Helper.Multiplayer.SendMessage(
                Config.Host.LocationChannels,
                (MessageType)MessageTypes.LocationChannelsSynced,
                new[] { ModManifest.UniqueID }
            );
        }

        private static string MergeLocationWithTeam(string currentLocation, string teamName)
        {
            if (currentLocation.Contains("UndergroundMine"))
                return "Mine";

            if (currentLocation == "FarmHouse" || currentLocation == "Cabin")
            {
                if (teamName == "None")
                    return "Cabin";

                return $"{teamName} Cabin";
            }

            return currentLocation;
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
