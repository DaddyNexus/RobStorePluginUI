using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;

namespace Nexus.Robstore
{
    public class StoreCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "store";
        public string Help => "Create or remove robbable stores.";
        public string Syntax => "/store <create/remove> <storeName> [radius]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "robstore.manage" };

        public async void Execute(IRocketPlayer caller, string[] command)
        {
            var player = caller as UnturnedPlayer;
            if (player == null) return;

            if (RobStorePlugin.Instance == null)
            {
                player.Player.sendBrowserRequest("RobStore plugin not loaded.", "OK");
                return;
            }

            if (command.Length < 2)
            {
                ChatUtil.Warning(player, $"Usage: {Syntax}");
                return;
            }

            string action = command[0].ToLower();
            string[] args = command.Skip(1).ToArray();

            switch (action)
            {
                case "create":
                    await HandleCreate(player, args);
                    break;

                case "remove":
                    await HandleRemove(player, args);
                    break;

                default:
                    ChatUtil.Warning(player, $"Invalid subcommand '{action}'. Use create/remove.");
                    break;
            }
        }

        private async Task HandleCreate(UnturnedPlayer player, string[] args)
        {
            if (args.Length < 1)
            {
                ChatUtil.Warning(player, $"Usage: /store create <storeName> [radius]");
                return;
            }

            float radius = 10f;
            bool hasRadius = false;

            if (args.Length >= 2 &&
                float.TryParse(args[args.Length - 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedRadius))
            {
                hasRadius = true;
                radius = Mathf.Max(0.1f, parsedRadius);
            }

            string storeName = hasRadius
                ? string.Join(" ", args.Take(args.Length - 1)).Trim()
                : string.Join(" ", args).Trim();

            if (string.IsNullOrWhiteSpace(storeName))
            {
                ChatUtil.Warning(player, $"Usage: /store create <storeName> [radius]");
                return;
            }

            if (RobStorePlugin.Instance.Stores.ContainsKey(storeName))
            {
                ChatUtil.Denied(player, $"A store named '{storeName}' already exists.");
                return;
            }

            Vector3 position = player.Position;

            // Add to in-memory dictionary
            RobStorePlugin.Instance.Stores[storeName] = new StoreData
            {
                Name = storeName,
                Position = position,
                Radius = radius
            };

            // Save config
            RobStorePlugin.Instance.SaveConfig();

            ChatUtil.Success(player, $"Store '{storeName}' created at your location (radius: {radius.ToString(CultureInfo.InvariantCulture)}).");
        }

        private async Task HandleRemove(UnturnedPlayer player, string[] args)
        {
            if (args.Length < 1)
            {
                ChatUtil.Warning(player, $"Usage: /store remove <storeName>");
                return;
            }

            string storeName = string.Join(" ", args).Trim();

            if (string.IsNullOrWhiteSpace(storeName))
            {
                ChatUtil.Warning(player, $"Usage: /store remove <storeName>");
                return;
            }

            if (!RobStorePlugin.Instance.Stores.ContainsKey(storeName))
            {
                ChatUtil.Denied(player, $"Store '{storeName}' does not exist.");
                return;
            }

            // Remove from memory
            RobStorePlugin.Instance.Stores.Remove(storeName);

            // Save config
            RobStorePlugin.Instance.SaveConfig();

            ChatUtil.Success(player, $"Store '{storeName}' removed successfully.");
        }
    }
}
