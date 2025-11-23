using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;

namespace Nexus.Robstore
{
    public static class RobberyUiService
    {
        private const string ProgressBarElementPrefix = "ProgressBarImage_";
        private const int ProgressBarSegmentCount = 100;
        private const string PlaceElementName = "PlaceText";
        private const string CashElementName = "CashText";
        private const string ProgressElementName = "ProgressText";

        public static void Initialize(UnturnedPlayer player, RobSession session)
        {
            var config = RobStorePlugin.Instance?.Configuration?.Instance;
            if (player == null || session == null || config == null)
            {
                return;
            }

            string placeText = string.IsNullOrWhiteSpace(session.StoreName)
                ? "Robbery in progress"
                : $"Robbing {session.StoreName}";

            UpdateUiText(player, PlaceElementName, placeText);
            UpdateProgress(player, 0, session.Reward);
        }

        public static void UpdateProgress(UnturnedPlayer player, int progressPercent, int totalReward)
        {
            var config = RobStorePlugin.Instance?.Configuration?.Instance;
            if (player == null || config == null)
            {
                return;
            }

            int clamped = Mathf.Clamp(progressPercent, 0, 100);
            UpdateUiText(player, ProgressElementName, $"{clamped}%");
            UpdateCashProgress(player, totalReward, clamped);
            UpdateProgressBarSegments(player, clamped);
        }

        private static void UpdateCashProgress(UnturnedPlayer player, int totalReward, int clampedPercent)
        {
            int reward = Mathf.Max(0, totalReward);
            int currentReward = Mathf.RoundToInt(reward * (clampedPercent / 100f));
            UpdateUiText(player, CashElementName, FormatCurrency(currentReward));
        }

        private static void UpdateUiText(UnturnedPlayer player, string elementName, string value)
        {
            var config = RobStorePlugin.Instance?.Configuration?.Instance;
            if (player == null || config == null || string.IsNullOrWhiteSpace(elementName))
            {
                return;
            }

            EffectManager.sendUIEffectText(
                config.RobberyUIEffectKey,
                player.CSteamID,
                true,
                elementName,
                value ?? string.Empty
            );
        }

        private static void UpdateProgressBarSegments(UnturnedPlayer player, int progressPercent)
        {
            var config = RobStorePlugin.Instance?.Configuration?.Instance;
            if (player == null || config == null)
            {
                return;
            }

            int segmentCount = Mathf.Clamp(ProgressBarSegmentCount, 0, int.MaxValue);
            if (segmentCount == 0 || string.IsNullOrWhiteSpace(ProgressBarElementPrefix))
            {
                return;
            }

            int visibleSegments = Mathf.Clamp(
                Mathf.CeilToInt(progressPercent / 100f * segmentCount),
                0,
                segmentCount
            );

            for (int i = 0; i < segmentCount; i++)
            {
                string elementName = $"{ProgressBarElementPrefix}{i}";
                bool isVisible = i < visibleSegments;
                EffectManager.sendUIEffectVisibility(
                    config.RobberyUIEffectKey,
                    player.CSteamID,
                    true,
                    elementName,
                    isVisible
                );
            }
        }

        private static string FormatCurrency(int amount)
        {
            return $"${Mathf.Max(0, amount):N0}";
        }
    }
}

