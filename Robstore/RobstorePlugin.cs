using Rocket.API;
using Rocket.API.Collections;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Unturned.Player;
using Rocket.Unturned.Events;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;
using Nexus.Robstore;
using Rocket.Core;
using System.Security.Cryptography.X509Certificates;

namespace Nexus.Robstore
{
    public class RobStorePlugin : RocketPlugin<RobstoreConfiguration>
    {
        public static RobStorePlugin Instance;
        public Dictionary<string, StoreData> Stores = new Dictionary<string, StoreData>();
        private const string ProgressBarElementPrefix = "ProgressBarImage_";
        private const int ProgressBarSegmentCount = 100;
        private const string PlaceElementName = "PlaceText";
        private const string CashElementName = "CashText";
        private const string ProgressElementName = "ProgressText";

        private readonly Dictionary<ulong, DateTime> _robCooldowns = new Dictionary<ulong, DateTime>();
        private readonly Dictionary<ulong, RobSession> _activeRobberies = new Dictionary<ulong, RobSession>();
        private ulong? _currentRobber = null;

        public int MinExp => Math.Max(0, Configuration?.Instance?.MinExp ?? 300);
        public int MaxExp => Math.Max(MinExp, Configuration?.Instance?.MaxExp ?? 4000);
        public int RobCooldownSeconds => Math.Max(0, Configuration?.Instance?.RobCooldownSeconds ?? 300);
        public int RobHoldTimeSeconds => Math.Max(1, Configuration?.Instance?.RobHoldTimeSeconds ?? 15);
        public int RobCheckIntervalMs => Math.Max(100, Configuration?.Instance?.RobCheckIntervalMs ?? 500);
        public override TranslationList DefaultTranslations => new TranslationList
        {
 { "rob_plugin_not_loaded", "RobStore plugin not loaded." },
     { "rob_usage", "Usage: <color=#FF0000>{0}</color>" },
     { "rob_no_stores", " <color=#FF0000>No stores are registered</color> yet." },
     { "rob_not_in_radius", "You are <color=#FF0000>not inside</color> any store radius." },
     { "rob_cooldown", " You must wait <color#FF0000>{0}s</color> before robbing again." },
     { "rob_already_attempting", " You are already attempting a robbery. <color=#FF0000>Stay in the radius</color> to finish." },
     { "rob_start", "Hold your ground inside <color=#FF0000>{0}</color> for <color=#FF0000>{1}s</color> without leaving or lowering your weapon." },
     { "rob_left_radius_cancel", " You <color=#FF0000>left</color> the store radius. Robbery <color=#FF0000>cancelled</color>." },
     { "rob_lowered_weapon_cancel", " You <color=#FF0000>lowered your weapon</color>. Robbery <color=#FF0000>cancelled</color>." },
     { "rob_failed_generic", " Robbery <color=#FF0000>failed</color>." },
     { "rob_complete", "Robbery <color=#FF0000>complete</color>! You robbed <color=#FF0000>{0}</color> and earned <color=#FF0000>{1} cash</color>. Now get out of there!" },
     { "rob_error", " An <color=#FF0000>error</color> occurred; robbery <color=#FF0000>cancelled</color>." },
     { "rob_store_not_found", " Store <color=#FF0000>'{0}'</color> not found." },
     { "rob_too_far", " You're <color=#FF0000>too far</color> from the store to rob it." },
     { "rob_need_weapon", " You must have a <color=#FF0000>weapon out</color> to rob the store." },
     { "rob_progress_tick", " Robbing <color=#FF0000>{0}</color>… <color=#FF0000>{1}s</color> remaining."},
     {"robberyalert", "<color=#FF0000>[ALERT]</color> Robbery in progress at {1}! " }
        }; 

        public string T(string key, params object[] args) => Translate(key, args);

        protected override void Load()
        {
            Instance = this;
            UnturnedPlayerEvents.OnPlayerUpdateExperience += OnPlayerUpdateExperience;

            LoadStoresFromConfig();
            Logger.Log($"--------ROBBERY STORE LOADED, MADE BY NEXUS-----------");
        }

        protected override void Unload()
        {
            foreach (var kv in _activeRobberies)
                kv.Value?.Cts?.Cancel();

            _activeRobberies.Clear();
            UnturnedPlayerEvents.OnPlayerUpdateExperience -= OnPlayerUpdateExperience;
            Logger.Log("❌ RobStore plugin unloaded.");
        }

        private void LoadStoresFromConfig()
        {
            Stores.Clear();
            if (Configuration?.Instance?.StoreList != null)
            {
                foreach (var store in Configuration.Instance.StoreList)
                {
                    Stores[store.Name] = store;
                }
            }
        }

        public void SaveConfig()
        {
            Configuration.Instance.StoreList = Stores.Values.ToList();
            SaveConfiguration();
        }

        public void SaveConfiguration()
        {
            Configuration.Save();
        }


        public Task<bool> RegisterStoreAsync(string name, Vector3 position, float radius)
        {
            Stores[name] = new StoreData { Name = name, Position = position, Radius = radius };
            SaveConfig();
            return Task.FromResult(true);
        }

        [Obsolete]
        public bool TryRobStore(UnturnedPlayer player, string storeName)
        {
            if (_currentRobber != null && _currentRobber != player.CSteamID.m_SteamID)
            {
                ChatUtil.Warning(player, "Another robbery is already in progress!");
                return false;
            }

            _currentRobber = player.CSteamID.m_SteamID;

            if (!Stores.TryGetValue(storeName, out var store))
            {
                ChatUtil.Warning(player, T("rob_store_not_found", storeName));
                return false;
            }

            if (Vector3.Distance(player.Position, store.Position) > store.Radius)
            {
                ChatUtil.Denied(player, T("rob_too_far"));
                return false;
            }

            if (!HasWeaponOut(player))
            {
                ChatUtil.Denied(player, T("rob_need_weapon"));
                return false;
            }

            if (IsOnCooldown(player, out var remaining))
            {
                ChatUtil.Denied(player, T("rob_cooldown", Math.Ceiling(remaining.TotalSeconds)));
                return false;
            }

            var steamId = player.CSteamID.m_SteamID;
            if (_activeRobberies.ContainsKey(steamId))
            {
                ChatUtil.Warning(player, T("rob_already_attempting"));
                return false;
            }

            var cts = new CancellationTokenSource();
            int reward = UnityEngine.Random.Range(MinExp, MaxExp + 1);
            var session = new RobSession
            {
                StoreName = storeName,
                StartedAtUtc = DateTime.UtcNow,
                Cts = cts,
                Reward = reward,
                DurationSeconds = RobHoldTimeSeconds
            };
            _activeRobberies[steamId] = session;

            _currentRobber = steamId; // Lock the robbery for the whole server

            ChatUtil.Warning(player, T("rob_start", storeName, RobHoldTimeSeconds));

            var config = Configuration?.Instance;
            if (config != null)
            {
                EffectManager.sendUIEffect(
                    config.RobberyUIEffectID,
                    config.RobberyUIEffectKey,
                    player.CSteamID,
                    true
                );

                InitializeRobberyUi(player, session);
            }


            string alertGroup = Configuration.Instance.RobberyAlertPermissionGroup;
            if (!string.IsNullOrEmpty(alertGroup))
            {
                foreach (var onlinePlayer in Provider.clients.Select(c => UnturnedPlayer.FromSteamPlayer(c)))
                {
                    if (R.Permissions.HasPermission(onlinePlayer, alertGroup))
                    {
                        ChatUtil.Warning(onlinePlayer, $"Robbery in progress at {storeName}! ");
                    }
                }
            }

            _ = MonitorRobberyAsync(player, store, session, cts.Token);
            return true;
        }
        [Obsolete]
        private async Task MonitorRobberyAsync(UnturnedPlayer player, StoreData store, RobSession session, CancellationToken token)
        {
            var steamId = player.CSteamID.m_SteamID;
            var config = Configuration?.Instance;
            int durationSeconds = session?.DurationSeconds > 0 ? session.DurationSeconds : RobHoldTimeSeconds;
            DateTime startTime = session?.StartedAtUtc ?? DateTime.UtcNow;
            var deadline = startTime.AddSeconds(durationSeconds);
            int lastPercent = -1;
            string storeName = session?.StoreName ?? store?.Name ?? "Unknown";

            try
            {
                while (DateTime.UtcNow < deadline)
                {
                    if (token.IsCancellationRequested) return;

                    if (player?.Player == null || player.Player.life == null || player.Player.life.isDead)
                    {
                        ChatUtil.Denied(player, T("rob_failed_generic"));
                        return;
                    }

                    if (Vector3.Distance(player.Position, store.Position) > store.Radius)
                    {
                        ChatUtil.Denied(player, T("rob_left_radius_cancel"));
                        return;
                    }

                    if (!HasWeaponOut(player))
                    {
                        ChatUtil.Denied(player, T("rob_lowered_weapon_cancel"));
                        return;
                    }

                    double elapsedSeconds = Math.Max(0, (DateTime.UtcNow - startTime).TotalSeconds);
                    int progressPercent = Math.Min(100, (int)((elapsedSeconds / durationSeconds) * 100));

                    // Only update progress if it changed
                    if (progressPercent != lastPercent)
                    {
                        int rewardSnapshot = session?.Reward ?? 0;
                        UpdateRobberyProgress(player, progressPercent, rewardSnapshot);
                        lastPercent = progressPercent;
                    }

                    await Task.Delay(RobCheckIntervalMs, token);
                }

                int reward = session?.Reward ?? UnityEngine.Random.Range(MinExp, MaxExp + 1);
                UpdateRobberyProgress(player, 100, reward);
                player.Experience += (uint)reward;

                SetCooldown(player);
                ChatUtil.Success(player, T("rob_complete", storeName, reward));
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                Logger.LogError($"Robbery monitor error for {player?.CharacterName ?? steamId.ToString()}: {ex.Message}");
                ChatUtil.Denied(player, T("rob_error"));
            }
            finally
            {
                _activeRobberies.Remove(steamId);
                if (config != null)
                {
                    EffectManager.askEffectClearByID(config.RobberyUIEffectID, player.CSteamID);
                }

                if (_currentRobber == steamId)
                    _currentRobber = null;
            }
        }

        [Obsolete]
        private bool HasWeaponOut(UnturnedPlayer player)
        {
            var eq = player.Player?.equipment;
            if (eq == null || !eq.isSelected)
                return false;

            var asset = eq.asset as ItemAsset;
            return asset is ItemGunAsset || asset is ItemMeleeAsset;
        }

        public bool IsOnCooldown(UnturnedPlayer player, out TimeSpan remaining)
        {
            ulong steamId = player.CSteamID.m_SteamID;

            if (_robCooldowns.TryGetValue(steamId, out DateTime lastRobTime))
            {
                TimeSpan cooldown = TimeSpan.FromSeconds(RobCooldownSeconds);
                TimeSpan elapsed = DateTime.UtcNow - lastRobTime;

                if (elapsed < cooldown)
                {
                    remaining = cooldown - elapsed;
                    return true;
                }
            }

            remaining = TimeSpan.Zero;
            return false;
        }

        public void SetCooldown(UnturnedPlayer player)
        {
            _robCooldowns[player.CSteamID.m_SteamID] = DateTime.UtcNow;
        }

        private void OnPlayerUpdateExperience(UnturnedPlayer player, uint experience)
        {
            Logger.Log($"{player.DisplayName}'s experience: {experience}");
        }

        private void InitializeRobberyUi(UnturnedPlayer player, RobSession session)
        {
            RobberyUiService.Initialize(player, session);
        }

        private void UpdateRobberyProgress(UnturnedPlayer player, int progressPercent, int totalReward)
        {
            RobberyUiService.UpdateProgress(player, progressPercent, totalReward);
        }

    }
}
