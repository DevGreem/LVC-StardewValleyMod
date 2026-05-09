using Discord.Rest;
using Discord.WebSocket;
using StardewModdingAPI;
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
        public async Task MoveToVoice(long playerId, string newLocation)
        {
            ulong discordId = GetDiscordFarmerId(playerId);
            string team = GetFarmerTeam(playerId); // Oyuncunun takımını merkezi veriden al

            if (discordId == 0)
            {
                Mod.Monitor.Log($"[LVC] HATA: {playerId} için Discord ID bulunamadı.", LogLevel.Error);
                return;
            }

            string mergedLocation = MergeLocations(newLocation, team);
            Mod.Monitor.Log($"[LVC] Taşıma: Oyuncu={playerId}, Takım={team}, Hedef={mergedLocation}", LogLevel.Info);

            await MoveToVoice(discordId, mergedLocation);
        }

        /// <summary>
        /// Move a Farmer to a voice chat
        /// </summary>
        /// <param name="playerId">Discord User Id</param>
        /// <param name="newLocation">New Location of the User</param>
        /// <returns>Task</returns>
        public async Task MoveToVoice(ulong discordId, string? newLocation)
        {
            if (string.IsNullOrEmpty(newLocation)) return;

            SocketGuildUser? user = Guild.GetUser(discordId);
            // 1. Kullanıcı seste değilse zaten dokunma
            if (user?.VoiceChannel is null) return;

            // 2. KRİTİK KONTROL: Kullanıcının şu an bulunduğu kanal, bizim config'deki kayıtlı ID'lerden biri mi?
            // Eğer oyuncunun bulunduğu kanalın ID'si, bizim LocationChannels listemizde yoksa ve Ana Kanal ID'si de değilse, bot karışmasın.
            bool isUserInModChannel = Mod.Config.Host.LocationChannels.Values.Contains(user.VoiceChannel.Id)
                                      || user.VoiceChannel.Id == Mod.Config.Bot.MainVoiceChatId;

            if (!isUserInModChannel)
            {
                return;
            }

            // 3. Eğer zaten gitmek istediği kanaldaysa yine dokunma
            if (user.VoiceChannel.Name == newLocation) return;

            SocketVoiceChannel? voiceChannel = GetVoiceChannelByName(newLocation);

            if (voiceChannel is null)
            {
                Mod.Monitor.Log($"[LVC] '{newLocation}' kanalı henüz yok, oluşturuluyor...", StardewModdingAPI.LogLevel.Info);
                var restChannel = await CreateVoiceChannel(newLocation);

                // ID'yi hemen kaydet
                Mod.Config.Host.LocationChannels[newLocation] = restChannel.Id;
                Mod.Helper.WriteConfig(Mod.Config);

                // KRİTİK DÜZELTME: Discord'un kanalı tanıması için kısa bir süre bekle 
                // ve kanal objesini tazeleyerek al.
                await Task.Delay(1000);
                voiceChannel = Guild.GetVoiceChannel(restChannel.Id);

                if (voiceChannel == null)
                {
                    Mod.Monitor.Log($"[LVC] Kanal oluşturuldu ama Discord henüz hazır değil, bir sonraki warp bekleniyor.", StardewModdingAPI.LogLevel.Warn);
                    return;
                }
            }

            if (voiceChannel != null && user.VoiceChannel.Id != voiceChannel.Id)
            {
                try
                {
                    // Discord API'yi rahatlatmak için çok kısa bir bekleme
                    await Task.Delay(250);
                    await user.ModifyAsync(x => x.Channel = voiceChannel);
                    Mod.Monitor.Log($"[LVC] BAŞARILI: {user.Username} -> {newLocation} kanalına taşındı.", StardewModdingAPI.LogLevel.Info);
                }
                catch (Exception ex)
                {
                    Mod.Monitor.Log($"[LVC] KRİTİK HATA: {user.Username} taşınırken Discord API hata döndürdü: {ex.Message}", StardewModdingAPI.LogLevel.Warn);
                }
            }
        }

        private static string MergeLocations(string currentLocation, string teamName)
        {
            if (currentLocation.Contains("UndergroundMine"))
                return "Mine";
                //return currentLocation.Substring(0, currentLocation.Length - 1);

            if (currentLocation == "FarmHouse" || currentLocation == "Cabin")
                return $"{teamName} Cabin";

            return currentLocation;
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
            // HATA BURADAYDI: playerInfo.Value artık ulong değil, FarmerInfo nesnesi.
            foreach (var playerInfo in Mod.Config.Host.SavesData[Game1.uniqueIDForThisGame].Players)
            {
                // Discord ID'sine .DiscordId diyerek erişiyoruz
                _ = UnmuteUser(playerInfo.Value.DiscordId);
                _ = UndeafUser(playerInfo.Value.DiscordId);
            }
        }
    }
}
