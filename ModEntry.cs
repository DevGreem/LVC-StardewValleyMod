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
        public Dictionary<string, ulong> LocationChannelsCache { get; set; } = new();
        public PlayerData PlayerDataCache { get; set; } = new();
        // Keep the folder path for the current loaded save so we can write to the same
        // data/{SaveName}_{SaveId} folder even after Game1 state changes (e.g., on return to title).
        private string CurrentSaveDataFolder { get; set; }

        // Read/write per-save JSON file using SMAPI Data API.
        // Files are stored under: data/{SaveName}_{SaveId}/{fileName}
        private static string SanitizeFileName(string name)
        {
            foreach (var c in System.IO.Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        internal T ReadPerSaveFile<T>(string fileName) where T : class
        {
            try
            {
                string folder;
                if (!string.IsNullOrEmpty(CurrentSaveDataFolder))
                    folder = CurrentSaveDataFolder;
                else
                {
                    string saveName = SanitizeFileName(Game1.GetSaveGameName());
                    folder = $"data/{saveName}_{Game1.uniqueIDForThisGame}";
                    CurrentSaveDataFolder = folder; // store for later use
                }
                string path = $"{folder}/{fileName}";
                return Helper.Data.ReadJsonFile<T>(path);
            }
            catch (Exception ex)
            {
                Monitor.Log($"{Helper.Translation.Get("log.warn.read-per-save-file-failed", new { file = fileName, reason = ex.Message })}", LogLevel.Warn);
                return default;
            }
        }

        internal void WritePerSaveFile<T>(string fileName, T data) where T : class
        {
            try
            {
                string folder = CurrentSaveDataFolder ?? $"data/{SanitizeFileName(Game1.GetSaveGameName())}_{Game1.uniqueIDForThisGame}";
                // if CurrentSaveDataFolder wasn't set, set it now so future writes during return-to-title use the same folder
                CurrentSaveDataFolder ??= folder;
                string path = $"{folder}/{fileName}";
                Helper.Data.WriteJsonFile(path, data);
            }
            catch (Exception ex)
            {
                Monitor.Log($"{Helper.Translation.Get("log.warn.write-per-save-file-failed", new { file = fileName, reason = ex.Message })}", LogLevel.Warn);
            }
        }

        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        #region EVENTS

        private async void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (!Context.IsMultiplayer)
                return;

            Monitor.Log($"{Helper.Translation.Get("log.info.save-loaded", new { isMainPlayer = Context.IsMainPlayer, discordId = Config.User.DiscordId, tokenSet = !string.IsNullOrEmpty(Config.Bot.Token) })}", LogLevel.Info);

            if (Config.User.DiscordId == 0)
            {
                Monitor.Log($"{Helper.Translation.Get("no-user-id.error")}", LogLevel.Error);
                return;
            }

            LoadEvents();

            // Load per-save data (channels.json and players.json) into memory from data/{saveId}/
            try
            {
                // Store the current save folder explicitly so later writes (e.g., on return-to-title)
                // target the same folder even if Game1.GetSaveGameName()/uniqueID change.
                var explicitSaveName = SanitizeFileName(Game1.GetSaveGameName());
                CurrentSaveDataFolder = $"data/{explicitSaveName}_{Game1.uniqueIDForThisGame}";

                var channels = ReadPerSaveFile<Dictionary<string, ulong>>("channels.json") ?? new Dictionary<string, ulong>();
                LocationChannelsCache = channels;

                var players = ReadPerSaveFile<PlayerData>("players.json") ?? new PlayerData();
                PlayerDataCache = players;
                // ensure files exist
                WritePerSaveFile("channels.json", LocationChannelsCache);
                WritePerSaveFile("players.json", PlayerDataCache);
            }
            catch (Exception ex)
            {
                Monitor.Log($"{Helper.Translation.Get("log.warn.load-save-data-failed", new { reason = ex.Message })}", LogLevel.Warn);
            }

            if (Context.IsMainPlayer)
            {
                Monitor.Log($"{Helper.Translation.Get("log.info.starting-bot")}", LogLevel.Info);

                if (Config.Host.DiscordGuildId == 0)
                {
                    Monitor.Log($"{Helper.Translation.Get("no-guild-id.error")}", LogLevel.Error);
                    UnloadEvents();
                    return;
                }

                if (Config.Bot.Token == "")
                {
                    Monitor.Log($"{Helper.Translation.Get("no-bot-token.error")}", LogLevel.Error);
                    UnloadEvents();
                    return;
                }

                HostBot = new Bot(this);
                await HostBot.WaitForReady();

                // Persist the host player's info to per-save data.json
                try
                {
                    // Use existing cache values if present, otherwise default to false
                    var hostId = Game1.MasterPlayer.UniqueMultiplayerID;
                    bool hostMuted = false;
                    bool hostDeafen = false;
                    if (PlayerDataCache.Players.TryGetValue(hostId, out var existingHost))
                    {
                        hostMuted = existingHost.Muted;
                        hostDeafen = existingHost.Deafen;
                    }

                    PlayerDataCache.Players[hostId] = new FarmerInfo
                    {
                        DiscordId = Config.User.DiscordId,
                        Team = Config.User.Team,
                        Muted = hostMuted,
                        Deafen = hostDeafen
                    };

                    WritePerSaveFile("players.json", PlayerDataCache);
                }
                catch (Exception ex)
                {
                    Monitor.Log($"{Helper.Translation.Get("log.error.save-player-data-failed", new { reason = ex.Message })}", LogLevel.Error);
                }

                _ = HostBot.ResetUsersState();

                // Save config (still keep guild/token in config.json)
                SaveConfig();
                return;
            }

            Monitor.Log($"{Helper.Translation.Get("log.info.client-joining")}", LogLevel.Info);
            // Ensure local player is present in players.json with current settings
            var localId = Game1.player.UniqueMultiplayerID;
            if (!PlayerDataCache.Players.ContainsKey(localId))
            {
                PlayerDataCache.Players[localId] = new FarmerInfo { DiscordId = Config.User.DiscordId, Team = Config.User.Team, Muted = false, Deafen = false };
                WritePerSaveFile("players.json", PlayerDataCache);
            }
            SendMessageToMain(Config, MessageTypes.PlayerJoined);
        }

        private async void OnMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID != ModManifest.UniqueID)
                return;

            if (e.Type == (MessageType)MessageTypes.LocationChannelsSynced)
            {
                var channelMap = e.ReadAs<Dictionary<string, ulong>>();
                if (channelMap != null)
                {
                    Monitor.Log($"{Helper.Translation.Get("log.info.location-channels-received", new { count = channelMap.Count })}", LogLevel.Info);

                    // Persist the received mapping to local save data so clients have a local copy
                    try
                    {
                        LocationChannelsCache = channelMap;
                        // persist to channels.json
                        WritePerSaveFile("channels.json", LocationChannelsCache);
                        Monitor.Log($"{Helper.Translation.Get("log.info.config-saved")}", LogLevel.Info);
                        foreach (var mapping in channelMap)
                        {
                            Monitor.Log($"{Helper.Translation.Get("log.info.channel-mapping", new { key = mapping.Key, value = mapping.Value })}", LogLevel.Info);
                        }
                    }
                    catch (Exception ex)
                    {
                        Monitor.Log($"{Helper.Translation.Get("log.warn.write-location-channels-failed", new { reason = ex.Message })}", LogLevel.Warn);
                    }
                }
                return;
            }

            if (!Context.IsMainPlayer)
                return;

            if (e.Type == (MessageType)MessageTypes.PlayerJoined)
            {
                ModConfig clientConfig = e.ReadAs<ModConfig>();

                try
                {
                    // Client no longer sends mute/deafen via config; default to false on join
                    PlayerDataCache.Players[e.FromPlayerID] = new FarmerInfo
                    {
                        DiscordId = clientConfig.User.DiscordId,
                        Team = clientConfig.User.Team,
                        Muted = false,
                        Deafen = false
                    };

                    WritePerSaveFile("players.json", PlayerDataCache);

                    Monitor.Log($"{Helper.Translation.Get("log.info.player-joined", new { playerId = e.FromPlayerID, team = clientConfig.User.Team })}", LogLevel.Info);
                }
                catch (Exception ex)
                {
                    Monitor.Log($"{Helper.Translation.Get("log.warn.persist-joined-player-data-failed", new { reason = ex.Message })}", LogLevel.Warn);
                }

                Monitor.Log($"{Helper.Translation.Get("log.info.sending-channels", new { count = LocationChannelsCache.Count, playerId = e.FromPlayerID })}", LogLevel.Info);
                SendMessageToPlayer(LocationChannelsCache, MessageTypes.LocationChannelsSynced, e.FromPlayerID);
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
                Monitor.Log($"{Helper.Translation.Get("log.info.microphone-state-change", new { playerId = playerInfo.Id, state = playerInfo.State })}", LogLevel.Info);

                // Persist state change to players.json
                if (!PlayerDataCache.Players.ContainsKey(playerInfo.Id))
                    PlayerDataCache.Players[playerInfo.Id] = new FarmerInfo { DiscordId = 0, Team = "None" };
                PlayerDataCache.Players[playerInfo.Id].Muted = playerInfo.State;
                WritePerSaveFile("players.json", PlayerDataCache);

                _ = HostBot.ChangeMuteUserState(playerInfo.Id, playerInfo.State);

                return;
            }

            if (e.Type == (MessageType)MessageTypes.ChangePlayerDeaferState)
            {
                var playerInfo = e.ReadAs<(long Id, bool State)>();
                Monitor.Log($"{Helper.Translation.Get("log.info.deafer-state-change", new { playerId = playerInfo.Id, state = playerInfo.State })}", LogLevel.Info);

                // Persist state change to players.json
                if (!PlayerDataCache.Players.ContainsKey(playerInfo.Id))
                    PlayerDataCache.Players[playerInfo.Id] = new FarmerInfo { DiscordId = 0, Team = "None" };
                PlayerDataCache.Players[playerInfo.Id].Deafen = playerInfo.State;
                WritePerSaveFile("players.json", PlayerDataCache);

                _ = HostBot.ChangeDeaferUserState(playerInfo.Id, playerInfo.State);

                return;
            }
        }

        private async void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            UnloadEvents();

            if (!Context.IsMainPlayer)
                return;

            await HostBot.ResetUsersState();

            if (Config.Bot.DeleteVoiceChats)
            {
                // Try to move every known player to the main voice channel before deleting channels
                try
                {
                    if (Config.Bot.MainVoiceChatId != 0)
                    {
                        foreach (var kv in PlayerDataCache.Players)
                        {
                            var discordId = kv.Value.DiscordId;
                            if (discordId != 0)
                            {
                                try
                                {
                                    await HostBot.MoveToVoice(discordId, Config.Bot.MainVoiceChatId);
                                }
                                catch (Exception exMove)
                                {
                                    Monitor.Log($"{Helper.Translation.Get("log.warn.move-failed-retry", new { attempt = 0, maxAttempts = 0, userName = discordId, reason = exMove.Message })}", LogLevel.Warn);
                                }
                            }
                        }
                    }

                    LocationChannelsCache.Clear();
                    // Overwrite channels.json with empty mapping; keep players.json intact
                    WritePerSaveFile("channels.json", new Dictionary<string, ulong>());
                    // After clearing the channels for this save, clear the cached folder so future writes don't
                    // accidentally create folders for a new/other save when Game1 state changes.
                    CurrentSaveDataFolder = null;
                }
                catch (Exception ex)
                {
                    Monitor.Log($"{Helper.Translation.Get("log.warn.delete-location-channels-failed", new { reason = ex.Message })}", LogLevel.Warn);
                }
                await HostBot.CloseVoiceChat();
                SaveConfig();
            }
        }

        private async void OnWarped(object sender, WarpedEventArgs e)
        {
            if (Context.IsMainPlayer)
            {
                _ = HostBot.MoveToVoice(e.Player.UniqueMultiplayerID, e.NewLocation.Name);
                return;
            }

            string mergedLocation = MergeLocationWithTeam(e.NewLocation.Name, Config.User.Team);
            Monitor.Log($"{Helper.Translation.Get("log.info.warp-occurred", new { oldLocation = e.NewLocation.Name, mergedLocation, count = LocationChannelsCache.Count })}", LogLevel.Info);

            if (LocationChannelsCache.TryGetValue(mergedLocation, out ulong channelId) && channelId != 0)
            {
                (ulong DiscordId, ulong ChannelId) message = (Config.User.DiscordId, channelId);
                Monitor.Log($"{Helper.Translation.Get("log.info.channel-found", new { mergedLocation, channelId })}", LogLevel.Info);
                SendMessageToMain(message, MessageTypes.PlayerWarpedWithChannel);
                return;
            }

            Monitor.Log($"{Helper.Translation.Get("log.warn.channel-id-missing", new { mergedLocation })}", LogLevel.Warn);
            (long, string) fallbackMessage = (e.Player.UniqueMultiplayerID, mergedLocation);
            SendMessageToMain(fallbackMessage, MessageTypes.PlayerWarped);
        }

        private async void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Config.User.EnableVoiceHotkeys &&
                (e.Button == Config.User.ChangeStateMute || e.Button == Config.User.ChangeStateDeaf))
            {
                return;
            }

            if (e.Button == Config.User.ChangeStateMute)
            {
                // Toggle mute state for this player and persist in players.json
                var localId = Game1.player.UniqueMultiplayerID;
                bool newState = true;
                if (PlayerDataCache.Players.TryGetValue(localId, out var localInfo))
                {
                    localInfo.Muted = !localInfo.Muted;
                    newState = localInfo.Muted;
                }
                else
                {
                    // create entry with defaults as fallback
                    PlayerDataCache.Players[localId] = new FarmerInfo { DiscordId = Config.User.DiscordId, Team = Config.User.Team, Muted = false, Deafen = false };
                    newState = PlayerDataCache.Players[localId].Muted;
                }
                WritePerSaveFile("players.json", PlayerDataCache);

                (long Id, bool State) userInfo = (localId, newState);

                if (Context.IsMainPlayer)
                {
                    _ = HostBot.ChangeMuteUserState(userInfo.Id, userInfo.State);
                    return;
                }

                // Notify host of the change
                SendMessageToMain(userInfo, MessageTypes.ChangePlayerMicrophoneState);

                return;
            }

            if (e.Button == Config.User.ChangeStateDeaf)
            {
                // Toggle deafen state and persist in players.json
                var localId2 = Game1.player.UniqueMultiplayerID;
                bool newDeaf = true;
                if (PlayerDataCache.Players.TryGetValue(localId2, out var localInfo2))
                {
                    localInfo2.Deafen = !localInfo2.Deafen;
                    newDeaf = localInfo2.Deafen;
                }
                else
                {
                    // Fallback: create entry with defaults and set deafen = true (toggle)
                    PlayerDataCache.Players[localId2] = new FarmerInfo { DiscordId = Config.User.DiscordId, Team = Config.User.Team, Muted = false, Deafen = true };
                    newDeaf = PlayerDataCache.Players[localId2].Deafen;
                }
                WritePerSaveFile("players.json", PlayerDataCache);

                (long Id, bool State) userInfo2 = (localId2, newDeaf);

                if (Context.IsMainPlayer)
                {
                    _ = HostBot.ChangeDeaferUserState(userInfo2.Id, userInfo2.State);
                    return;
                }

                SendMessageToMain(userInfo2, MessageTypes.ChangePlayerDeaferState);

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

            Monitor.Log($"{Helper.Translation.Get("log.info.broadcasting-location-channels", new { count = LocationChannelsCache.Count })}", LogLevel.Info);

            // Persist mapping to channels.json
            try
            {
                WritePerSaveFile("channels.json", LocationChannelsCache);
            }
            catch (Exception ex)
            {
                Monitor.Log($"{Helper.Translation.Get("log.warn.write-location-channels-failed", new { reason = ex.Message })}", LogLevel.Warn);
            }

            Helper.Multiplayer.SendMessage(
                LocationChannelsCache,
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
