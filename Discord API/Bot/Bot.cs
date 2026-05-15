using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using StardewModdingAPI;
using StardewValley;

namespace LVCMod
{
    partial class Bot
    {
        private ModEntry Mod { get; set; }

        private DiscordSocketClient DiscordClient { get; set; }

        private SocketGuild Guild { get; set; }

        private TaskCompletionSource<bool> IsBotReady { get; set; } = new();

        public Bot(ModEntry modEntry)
        {
            DiscordClient = new DiscordSocketClient();

            Mod = modEntry;
            DiscordClient.Log += OnLog;
            DiscordClient.Ready += OnReady;

            var startTask = Start();
            startTask.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Mod.Monitor.Log($"{Mod.Helper.Translation.Get("log.error.bot-login-failed", new { reason = t.Exception?.Flatten().Message })}", LogLevel.Error);
                    if (t.Exception != null)
                    {
                        foreach (var ex in t.Exception.Flatten().InnerExceptions)
                            Mod.Monitor.Log(ex.ToString(), LogLevel.Error);
                    }
                }
            }, TaskScheduler.Default);
        }
    }
}