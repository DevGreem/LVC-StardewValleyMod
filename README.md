# LVCMod

## About

LVC (Location Voice Chat) adds Discord-based voice chat to multiplayer Stardew Valley by creating and moving players into Discord voice channels based on their in-game location. The mod requires one player to act as the host (main player) who runs a Discord bot that creates/moves voice channels.

Requirements

- SMAPI (install as usual)
- A Discord server where you can add a bot
- A Discord bot token (create one in the Discord Developer Portal)

Installation

- Install SMAPI.
- Place the mod folder into your `mods` directory (or install via your preferred mod manager).
- Launch the game once to generate `config.json` (or edit the provided `config.json`).

How it works

- The host (main player) must provide a Discord Guild ID and a bot token in the config (or via Generic Mod Config Menu). When the host loads the save the mod starts the bot and the bot ensures a voice category and a main voice channel exist.
- When players change location the host bot will create or reuse a voice channel for that location and move the corresponding Discord users there.
- The mod keeps a mapping of merged location names to Discord channel IDs. For team-based farms the mod merges cabin/farmhouse locations with the configured team name (for example `Blue Cabin`).
- On returning to title the bot will reset players' mute/deafen state and, if enabled, delete the created voice channels (except the main channel).

Configuration

This mod supports `Generic Mod Config Menu` and also can be configured directly in `config.json`.

Key config values and defaults (code defaults):

- User
  - `DiscordId` (ulong) - your Discord user id (default: `0`)
  - `Team` (string) - team name used to merge cabins (default: `None`)
  - `Muted` (bool) - start muted (default: `true`)
  - `Deafen` (bool) - start deafened (default: `true`)
  - `EnableVoiceHotkeys` (bool) - enable hotkeys to toggle mute/deafen (default: `true`)
  - `ChangeStateMute` (key) - default `H`
  - `ChangeStateDeaf` (key) - default `J`

- Bot
  - `Token` (string) - your bot token (default: `""`)
  - `MainVoiceChatName` (string) - name of the main control voice channel (default: `"Talk"`)
  - `VoiceChatsCategoryName` (string) - category name where voice channels are created (default: `"Stardew Valley LVC"`)
  - `DeleteVoiceChats` (bool) - delete created channels on return to title (default: `true`)

- Host
  - `DiscordGuildId` (ulong) - guild (server) id used for voice chat (default: `0`)
  - `SavesData` - internal storage for discovered players per save (do not edit manually)
  - `LocationChannels` - mapping of merged location names to Discord channel IDs (managed by the host)

Bot permissions

The bot needs permission to manage and move voice channels and to mute/deafen members. Minimum permissions required:

- Manage Channels
- Move Members
- Mute Members
- Deafen Members

You can alternatively give the bot Administrator permission.

Host vs Client behavior

- Only the main player (host) needs to set `DiscordGuildId` and the bot `Token`.
- Clients must set their own `DiscordId` in the config. When clients join they send their Discord ID and team to the host so the bot can find and move the correct Discord user.

Controls

- `H` = Toggle microphone mute/unmute (default)
- `J` = Toggle audio deaf/undeaf (default)

Notes & Troubleshooting

- Make sure the bot is invited to your server and has the required permissions.
- Ensure `DiscordId` and `DiscordGuildId` are set correctly (enable developer mode in Discord to copy these IDs).
- If the game crashes on load check the in-game SMAPI log for errors; common causes are missing/incorrect bot token, missing guild, or bad/malformed `config.json`.
- If you encounter unexpected behavior please open an issue or discussion at the repository: https://github.com/Greem3/LVC-StardewValleyMod/discussions

Limitations

- The number of voice channels the bot can create is limited by Discord server/channel limits.
- If a player does not have the mod installed they will not be moved by the bot and voice functionality will not be available for them.

License / Privacy

- Do not publish or share your bot token. Treat it like a password.

