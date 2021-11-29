using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Player Poop", "Kopter", "1.0.0")]
    [Description("Makes A Player Poop After Eating Food")]

    public class PlayerPoop : RustPlugin
    {
        #region Variables

        private int poopingProbability = 50;
        private const string ignorePermission = "playerpoop.ignore";
        private const string horseDungShortname = "horsedung";

        #endregion

        #region Oxide Hooks

        private void Init()
        {
            if (config.probabilityOfPooping < 1 || config.probabilityOfPooping > 100) 
                Puts("The probability of pooping must be between 1 and 100! Loaded default value: 50!");
          
            else poopingProbability = config.probabilityOfPooping;

            permission.RegisterPermission(ignorePermission, this);
        }

        private void OnItemUse(Item item, int amount)
        {
            if (item == null || item.info.category != ItemCategory.Food || Random.Range(0, 100) < poopingProbability) return;

            ItemContainer container = item.GetRootContainer();

            if (container == null) return;

            BasePlayer player = container.GetOwnerPlayer();

            if (player == null || ((player.metabolism.calories.value != player.metabolism.calories.max) && config.maxFullBar) || permission.UserHasPermission(player.UserIDString, ignorePermission)) return;

            Item horseDung = ItemManager.CreateByName(horseDungShortname);

            if (horseDung == null) return;

            horseDung.name = "Human Poop";

            horseDung.Drop(player.transform.position, player.GetDropVelocity());
        }

        #endregion

        #region Config

        private ConfigData config = new ConfigData();

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Probability Of Pooping (1-100)")]
            public int probabilityOfPooping = 50;

            [JsonProperty(PropertyName = "Only Poop If The Food Bar Is Full")]
            public bool maxFullBar = false;
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