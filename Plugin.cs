using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;

namespace Challenging_Corruptors
{
    [BepInPlugin("com.meds.challengingcorruptors", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new("com.meds.challengingcorruptors");
        internal static ManualLogSource Log;
        public static ConfigEntry<int> medsMinimumCorruptor { get; private set; }
        private void Awake()
        {
            // Plugin startup logic
            Log = Logger;
            medsMinimumCorruptor = Config.Bind("General", "Minimum Corruptor Difficulty", 5, new ConfigDescription("1: easy (vanilla); 2: average; 3: hard; 4: extreme; 5: tabula rasa only o:", new AcceptableValueRange<int>(1, 5)));
            medsMinimumCorruptor.SettingChanged += (obj, args) => { Challenging_Corruptors.UpdateChallengeCorruptors(); };
            harmony.PatchAll();
            Logger.LogInfo($"Challenging Corruptors enabled!");
        }
    }
    [HarmonyPatch]
    public class Challenging_Corruptors
    {
        public static List<string> medsCorruptors = new();

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Globals), "CreateGameContent")]
        public static void CreateGameContentPostfix()
        {
            medsCorruptors = Globals.Instance.CardListByType[Enums.CardType.Corruption];
            UpdateChallengeCorruptors();
        }

        public static void UpdateChallengeCorruptors()
        {
            Globals.Instance.CardListByType[Enums.CardType.Corruption] = new();
            if (Plugin.medsMinimumCorruptor.Value == 5)
            {
                Globals.Instance.CardListByType[Enums.CardType.Corruption].Add("windsofamnesia");
                Globals.Instance.CardListByType[Enums.CardType.Corruption].Add("windsofamnesiaa");
                Globals.Instance.CardListByType[Enums.CardType.Corruption].Add("windsofamnesiab");
                Globals.Instance.CardListByType[Enums.CardType.Corruption].Add("windsofamnesiarare");
            }
            else
            {
                foreach (string cor in medsCorruptors)
                {
                    CardData card = Globals.Instance.GetCardData(cor);
                    if (card != (CardData)null && ((card.CardRarity == Enums.CardRarity.Common && Plugin.medsMinimumCorruptor.Value == 1) || (card.CardRarity == Enums.CardRarity.Uncommon && Plugin.medsMinimumCorruptor.Value <= 2) || (card.CardRarity == Enums.CardRarity.Rare && Plugin.medsMinimumCorruptor.Value <= 3) || (card.CardRarity == Enums.CardRarity.Epic)))
                        Globals.Instance.CardListByType[Enums.CardType.Corruption].Add(cor);
                }
            }
        }
    }
}
