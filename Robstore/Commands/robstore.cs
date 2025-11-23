using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System.Collections.Generic;
using UnityEngine;

namespace Nexus.Robstore
{
    public class RobStoreCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "robstore";
        public string Help => "Attempt to rob the store you are currently inside.";
        public string Syntax => "/robstore";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "robstore.rob" };

        private string T(string key, params object[] args) => RobStorePlugin.Instance?.T(key, args);

        [System.Obsolete]
        public void Execute(IRocketPlayer caller, string[] command)
        {
            var player = caller as UnturnedPlayer;
            if (player == null) return;

            if (RobStorePlugin.Instance == null)
            {
                ChatUtil.Warning(player, T("rob_plugin_not_loaded"));
                return;
            }

            if (RobStorePlugin.Instance.IsOnCooldown(player, out var remaining))
            {
                ChatUtil.Denied(player, T("rob_cooldown", Mathf.CeilToInt((float)remaining.TotalSeconds)));
                return;
            }

            var stores = RobStorePlugin.Instance.Stores;
            if (stores == null || stores.Count == 0)
            {
                ChatUtil.Warning(player, T("rob_no_stores"));
                return;
            }

            string storename = null;
            StoreData bestStore = null;
            float bestDist = float.MaxValue;
            var pos = player.Position;

            foreach (var kv in stores)
            {
                float dist = Vector3.Distance(pos, kv.Value.Position);
                if (dist <= kv.Value.Radius && dist < bestDist)
                {
                    bestDist = dist;
                    storename = kv.Key;
                    bestStore = kv.Value;
                }
            }

            if (bestStore == null)
            {
                ChatUtil.Denied(player, T("rob_not_in_radius"));
                return;
            }

            bool success = RobStorePlugin.Instance.TryRobStore(player, storename);
            if (success)
            {
                // Show UI immediately
                EffectManager.sendUIEffect(
                    RobStorePlugin.Instance.Configuration.Instance.RobberyUIEffectID,
                    RobStorePlugin.Instance.Configuration.Instance.RobberyUIEffectKey,
                    player.CSteamID,
                    true,
                    RobStorePlugin.Instance.RobHoldTimeSeconds.ToString(), // {0} store name
                    storename // {1} seconds
                );
            }
        }
    }
}