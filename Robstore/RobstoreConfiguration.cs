using Rocket.API;
using System.Collections.Generic;
using UnityEngine;

namespace Nexus.Robstore
{
    public class RobstoreConfiguration : IRocketPluginConfiguration
    {
        public int MinExp;
        public int MaxExp;
        public int RobCooldownSeconds;
        public int RobCheckIntervalMs;
        public int RobHoldTimeSeconds;
        public string IconImageUrl;
        public string RobberyAlertPermissionGroup;
        public List<StoreData> StoreList;
        public ushort RobberyUIEffectID { get; set; } = 22006; // Change to your effect asset ID
        public byte RobberyUIEffectKey { get; set; } = 60;       // UI channel key

        public void LoadDefaults()
        {

            MinExp = 300;
            MaxExp = 4000;
            RobCooldownSeconds = 300;
            RobHoldTimeSeconds = 15;
            RobCheckIntervalMs = 200;
            RobberyAlertPermissionGroup = "police";


            IconImageUrl = "https://example.com/denied.png";

            StoreList = new List<StoreData>
            {
                new StoreData
                {
                    Name = "Example Store",
                    Position = new Vector3(0, 0, 0),
                    Radius = 10f
                }
            };
        }
    }

    public class StoreData
    {
        public string Name;
        public Vector3 Position;
        public float Radius;
    }
}
