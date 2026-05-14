using System.ComponentModel;

namespace LVCMod
{
    enum MessageTypes
    {
        [Description("PlayerJoined")]
        PlayerJoined,

        [Description("PlayerWarped")]
        PlayerWarped,

        [Description("PlayerWarpedWithChannel")]
        PlayerWarpedWithChannel,

        [Description("LocationChannelsSynced")]
        LocationChannelsSynced,

        [Description("ChangePlayerMicrophoneState")]
        ChangePlayerMicrophoneState,

        [Description("ChangePlayerDeaferState")]
        ChangePlayerDeaferState
    }
}