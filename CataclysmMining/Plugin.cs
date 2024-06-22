using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;
using Wish;
using UnityEngine;
using System.ComponentModel;
using static CataclysmMining.Plugin;

namespace CataclysmMining
{
    [BepInPlugin(PluginGuid, PluginName, PluginVer)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource logger;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<float> hitCooldown;
        public static ConfigEntry<float> castCooldown;
        public static ConfigEntry<float> dropItemPickupTime;
        public static ConfigEntry<float> dropItemPickupRange;
        public static ConfigEntry<float> interactCooldown;
        public static ConfigEntry<int> miningRange;
        public static ConfigEntry<bool> miningEnabled;
        public static ConfigEntry<bool> cuttingEnabled;
        public static ConfigEntry<bool> cuttingFruitTrees;
        public static ConfigEntry<bool> cuttingNotFullyGrown;
        public static ConfigEntry<int>cuttingRange;
        public static ConfigEntry<bool> harvestingEnabled;
        public static ConfigEntry<int> harvestingRange;
        public static ConfigEntry<bool> harvestingFlowers;
        public static ConfigEntry<bool> harvestingInfuse;
        public static ConfigEntry<bool> harvestingShake;
        public static ConfigEntry<bool> pickupEnabled;
        public static ConfigEntry<int> pickupRange;

        public static bool cataclysmRunning = false;
        public static Vector2 damageRange =  new Vector2(5f, 10f);
        public static float damageRatio = 1f;
        public static float damageMultiplier = 1f;
        public static List<DecorationHitCooldown> cooldowns = new List<DecorationHitCooldown>();
        public static Queue<Decoration> interactives = new Queue<Decoration>();
        public static bool playPickupSound = false;
        public static float lastInteraction = 0f;

        private const string PluginGuid = "niratokage125.sunhaven.CataclysmMining";
        private const string PluginName = "CataclysmMining";
        private const string PluginVer = "1.0.2";
        private void Awake()
        {
            logger = Logger;
            modEnabled = Config.Bind<bool>("Common", "Mod Enabled", true, "Set to false to disable this mod.");
            hitCooldown = Config.Bind<float>("Common", "Hit Cooldown", 0.2f, "Cooldown seconds between hits.");
            interactCooldown = Config.Bind<float>("Common", "Interact Cooldown", 0.01f, "Cooldown seconds between interactions (pickup, infuse, ...). If 0, multiple interactions are executed at the same time.");
            castCooldown = Config.Bind<float>("Common", "Cast Cooldown", 0.5f, new ConfigDescription("Cast Cooldown Multiprier", new AcceptableValueRange<float>(0f, 1f)));
            dropItemPickupTime = Config.Bind<float>("Common", "Drop Items Pickup Time", 0.25f, new ConfigDescription("Drop Items Pickup Time While Cataclysm Multiprier", new AcceptableValueRange<float>(0f, 1f)));
            dropItemPickupRange = Config.Bind<float>("Common", "Drop Items Pickup Range", 3f, new ConfigDescription("Drop Items Pickup Range While Cataclysm Multiprier", new AcceptableValueRange<float>(1f, 5f)));
            miningRange = Config.Bind<int>("Mining", "Mining Range", 4, "Mining Range Radius");
            miningEnabled = Config.Bind<bool>("Mining", "Mining", true, "Set to false to disable mining.");
            cuttingEnabled = Config.Bind<bool>("Cutting", "Cutting", true, "Set to false to disable cutting trees and woods.");
            cuttingFruitTrees = Config.Bind<bool>("Cutting", "Cutting Fruit Trees", false, "Set to false to disable cutting fruit trees.");
            cuttingNotFullyGrown = Config.Bind<bool>("Cutting", "Cutting Not Fully Grown", true, "Set to false to disable cutting not fully grown trees.");
            cuttingRange = Config.Bind<int>("Cutting", "Cutting Range", 4,"Cutting Range Radius");
            harvestingEnabled = Config.Bind<bool>("Harvesting", "Harvesting", true, "Set to false to disable harvesting.");
            harvestingRange = Config.Bind<int>("Harvesting", "Harvesting Range", 4, "Harvesting Range Radius");
            harvestingFlowers = Config.Bind<bool>("Harvesting", "Harvesting Flowers", false, "Set to false to disable harvesting flowers.");
            harvestingInfuse = Config.Bind<bool>("Harvesting", "Mana Infuse", true, "Set to false to disable mana infuse crops.");
            harvestingShake = Config.Bind<bool>("Harvesting", "Shake Fruit Trees", true, "Set to false to disable shaking trees.");
            pickupEnabled = Config.Bind<bool>("Pickup", "Pickup", true, "Set to false to disable pickup.");
            pickupRange = Config.Bind<int>("Pickup", "Pickup Range", 4, "Pickup Range Radius");

            var harmony = new Harmony(PluginGuid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Logger.LogInfo($"Plugin {PluginGuid} v{PluginVer} is loaded");
        }

        public class DecorationHitCooldown
        {
            public Decoration decoration;
            public float cooldown;
        }
    }
}
