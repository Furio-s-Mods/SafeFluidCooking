using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace SecureCookingPots.src.helpers;

public static class PlayerSoundHelper
{
    /// <summary>
    /// Plays the player's character voice sound. 
    /// Great for audio feedback when an action is blocked or unauthorized.
    /// </summary>
    /// <param name="player">The player who performed the action.</param>
    /// <param name="talkType">The tone of the talk sound. Defaults to Idle.</param>
    public static void PlayVoiceFeedback(IPlayer player, EnumTalkType talkType = EnumTalkType.IdleShort)
    {
        if (player == null) return;

        if (player.Entity is EntityPlayer entityPlayer)
        {
            PlayVoiceFeedback(entityPlayer, talkType);
        }
    }

    /// <summary>
    /// Plays the player's character voice sound directly from their EntityPlayer instance.
    /// </summary>
    public static void PlayVoiceFeedback(EntityPlayer entityPlayer, EnumTalkType talkType = EnumTalkType.IdleShort)
    {
        if (entityPlayer?.talkUtil != null)
        {
            entityPlayer.talkUtil.Talk(talkType);
        }
    }
}