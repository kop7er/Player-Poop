using Newtonsoft.Json;

using System.Collections.Generic;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Player Poop", "Kopter", "2.0.0")]
    [Description("Makes A Player Poop After Eating Food")]

    public class PlayerPoop : RustPlugin
    {
        #region Variables

        private int poopingProbability = 50;

        private const string horseDungShortname = "horsedung";

        private const string ignorePermission = "playerpoop.ignore";
        private const string canPoopPermission = "playerpoop.canpoop";

        private const string screamSound = "assets/bundled/prefabs/fx/player/beartrap_scream.prefab";

        private const string invisibleChairPrefab = "assets/bundled/prefabs/static/chair.invisible.static.prefab";

        private readonly List<BaseMountable> spawnedChairs = new List<BaseMountable> { };

        #endregion

        #region Oxide Hooks

        private void Init()
        {
            if (config.probabilityOfPooping < 1 || config.probabilityOfPooping > 100) 
                Puts("The probability of pooping must be between 1 and 100! Loaded default value: 50!");
          
            else poopingProbability = config.probabilityOfPooping;

            permission.RegisterPermission(ignorePermission, this);
            permission.RegisterPermission(canPoopPermission, this);
        }

        private void OnItemUse(Item item, int amount)
        {
            if (item == null || item.info.category != ItemCategory.Food || item.info.shortname.Contains("seed") || Random.Range(0, 100) < poopingProbability) return;

            ItemContainer container = item.GetRootContainer();

            if (container == null) return;

            BasePlayer player = container.GetOwnerPlayer();

            if (player == null || ((player.metabolism.calories.value != player.metabolism.calories.max) && config.maxFullBar) || permission.UserHasPermission(player.UserIDString, ignorePermission) || (config.requiresPermission && !permission.UserHasPermission(player.UserIDString, canPoopPermission))) return;
            
            Item horseDung = ItemManager.CreateByName(horseDungShortname);

            if (horseDung == null) return;

            horseDung.name = "Human Poop";

            if (config.playScreamSound) Effect.server.Run(screamSound, player.transform.position);

            if (config.sitPlayer && !(player.isMounted || player.IsFlying || player.IsSwimming()))
            {
                BaseMountable invisibleChair = GameManager.server.CreateEntity(invisibleChairPrefab, player.transform.position, player.transform.rotation) as BaseMountable;

                if (invisibleChair == null)
                {
                    horseDung.Drop(player.transform.position, player.GetDropVelocity());
                    return;
                }

                invisibleChair.Spawn();

                spawnedChairs.Add(invisibleChair);

                player.MountObject(invisibleChair);

                timer.Once(3f, () => {

                    invisibleChair?.Kill();

                    spawnedChairs.Remove(invisibleChair);

                    horseDung.Drop(player.transform.position, player.GetDropVelocity());

                });
            }

            else horseDung.Drop(player.transform.position, player.GetDropVelocity());
        }

        private void Unload()
        {
            foreach (var chair in spawnedChairs)
                chair?.Kill();

            spawnedChairs.Clear();
        }

        #endregion

        #region Config

        private ConfigData config = new ConfigData();

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Requires Permission To Poop")]
            public bool requiresPermission = false;

            [JsonProperty(PropertyName = "Probability Of Pooping (1-100)")]
            public int probabilityOfPooping = 50;

            [JsonProperty(PropertyName = "Only Poop If The Food Bar Is Full")]
            public bool maxFullBar = false;

            [JsonProperty(PropertyName = "Sit Player When Pooping (3 Seconds)")]
            public bool sitPlayer = false;

            [JsonProperty(PropertyName = "Play Scream Sound When Pooping")]
            public bool playScreamSound = false;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigData>();

                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintError("Configuration file is corrupt, check your config file at https://jsonlint.com/!");
                LoadDefaultConfig();
                return;
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => config = new ConfigData();

        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion
    }
}