using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;

namespace Nexus.Robstore
{
    public static class ChatUtil
    {
        private static RobStorePlugin Plugin => RobStorePlugin.Instance;

        public static void Send(UnturnedPlayer player, string translationKey, EChatMode mode = EChatMode.SAY, params object[] args)
        {
            ChatManager.serverSendMessage(
                Plugin.Translate(translationKey, args),
                Color.white,
                null,
                player.SteamPlayer(),
                mode,
                Plugin.Configuration.Instance.IconImageUrl,
                true
            );
        }

        public static void Success(UnturnedPlayer player, string translationKey, params object[] args)
        {
            ChatManager.serverSendMessage(
                Plugin.Translate(translationKey, args),
                Color.white,
                null,
                player.SteamPlayer(),
                EChatMode.SAY,
                Plugin.Configuration.Instance.IconImageUrl,
                true
            );
        }

        public static void Warning(UnturnedPlayer player, string translationKey, params object[] args)
        {
            ChatManager.serverSendMessage(
                Plugin.Translate(translationKey, args),
                Color.white,
                null,
                player.SteamPlayer(),
                EChatMode.SAY,
                Plugin.Configuration.Instance.IconImageUrl,
                true
            );
        }

        public static void Denied(UnturnedPlayer player, string translationKey, params object[] args)
        {
            ChatManager.serverSendMessage(
                Plugin.Translate(translationKey, args),
                Color.white,
                null,
                player.SteamPlayer(),
                EChatMode.SAY,
                Plugin.Configuration.Instance.IconImageUrl,
                true
            );
        }
    }
}

