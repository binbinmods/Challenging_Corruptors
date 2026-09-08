using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Challenging_Corruptors
{
    [BepInPlugin("com.meds.challengingcorruptors", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> EnableMod { get; set; }
        public static ConfigEntry<bool> EnableDebugging { get; set; }
        public static ConfigEntry<bool> ForceUpwind { get; set; }
        private readonly Harmony harmony = new("com.meds.challengingcorruptors");
        public static string PluginName;
        public static string PluginVersion;
        public static string PluginGUID;

        internal static int ModDate = int.Parse(DateTime.Today.ToString("yyyyMMdd"));
        internal static ManualLogSource Log;
        public static ConfigEntry<int> medsMinimumCorruptor { get; private set; }
        public static string debugBase = $"{PluginInfo.PLUGIN_GUID} ";

        private void Awake()
        {

            // The Logger will allow you to print things to the LogOutput (found in the BepInEx directory)
            Log = Logger;

            // Sets the title, default values, and descriptions
            string modName = "Challenging Corruptors";
            EnableMod = Config.Bind(new ConfigDefinition(modName, "EnableMod"), true, new ConfigDescription("Enables the mod. If false, the mod will not work then next time you load the game."));
            EnableDebugging = Config.Bind(new ConfigDefinition(modName, "EnableDebugging"), false, new ConfigDescription("Enables the debugging"));
            medsMinimumCorruptor = Config.Bind("General", "Minimum Corruptor Difficulty", 4, new ConfigDescription("1: easy (vanilla); 2: average; 3: hard; 4: extreme; 5: tabula rasa only o:", new AcceptableValueRange<int>(1, 5)));
            ForceUpwind = Config.Bind(new ConfigDefinition(modName, "ForceUpwind"), false, new ConfigDescription("Forces Upwind to always be an option for a corruptor."));

            medsMinimumCorruptor.SettingChanged += (obj, args) => { Challenging_Corruptors.UpdateChallengeCorruptors(); };
            ForceUpwind.SettingChanged += (obj, args) => { Challenging_Corruptors.UpdateChallengeCorruptors(); };

            // apply patches, this functionally runs all the code for Harmony, running your mod
            PluginName = PluginInfo.PLUGIN_NAME;
            PluginVersion = PluginInfo.PLUGIN_VERSION;
            PluginGUID = PluginInfo.PLUGIN_GUID;
            if (EnableMod.Value)
            {
                if (EssentialsCompatibility.Enabled)
                    EssentialsCompatibility.EssentialsRegister();
                else
                    LogInfo($"{PluginGUID} {PluginVersion} has loaded!");
                harmony.PatchAll();
            }
        }
        internal static void LogDebug(string msg, [CallerMemberName] string caller = "")
        {
            if (EnableDebugging.Value)
            {
                Log.LogDebug($"{debugBase}- {caller} - {msg}");
            }
        }
        internal static void LogInfo(string msg)
        {
            Log.LogInfo(debugBase + msg);
        }
        internal static void LogError(string msg, [CallerMemberName] string caller = "")
        {
            Log.LogError($"{debugBase}- {caller} - {msg}");
        }

        public static IEnumerator RunAfter(IEnumerator original, Action action)
        {
            while (original.MoveNext())
                yield return original.Current;
            // The original enumerator has finished. _NPCsSource is set now.
            action();
        }
    }



    [HarmonyPatch]
    public class Challenging_Corruptors
    {
        public static List<string> medsCorruptors = new();

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Globals), "CreateGameContentRoutine")]
        public static void CreateGameContentRoutinePrefix(ref IEnumerator __result)
        {
            Plugin.LogDebug("Creating game content routine");
            __result = Plugin.RunAfter(__result, CreateGameContentPostfix);
        }

        public static void CreateGameContentPostfix()
        {
            medsCorruptors = Globals.Instance.CardListByType[Enums.CardType.Corruption];
            UpdateChallengeCorruptors();
        }

        public static void UpdateChallengeCorruptors()
        {
            Plugin.LogDebug("Updating challenge corruptors");
            Globals.Instance.CardListByType[Enums.CardType.Corruption] = new();
            if (Plugin.medsMinimumCorruptor.Value == 5)
            {
                AddCorruptor("windsofamnesia");
            }
            else
            {
                foreach (string cor in medsCorruptors)
                {
                    Cards.CardRealtimeData card = Globals.Instance.GetCardData(cor);
                    if (card != (Cards.CardRealtimeData)null && ((card.CardRarity == Enums.CardRarity.Common && Plugin.medsMinimumCorruptor.Value == 1) || (card.CardRarity == Enums.CardRarity.Uncommon && Plugin.medsMinimumCorruptor.Value <= 2) || (card.CardRarity == Enums.CardRarity.Rare && Plugin.medsMinimumCorruptor.Value <= 3) || (card.CardRarity == Enums.CardRarity.Epic)))
                        Globals.Instance.CardListByType[Enums.CardType.Corruption].Add(cor);
                }

            }
            if (Plugin.ForceUpwind.Value)
            {
                if (Plugin.medsMinimumCorruptor.Value == 4)
                {
                    MakeCardEpic("upwind");
                    MakeCardEpic("upwinda");
                    MakeCardEpic("upwindb");
                    MakeCardEpic("upwindrare");
                }
                AddCorruptor("upwind");
            }
        }

        public static void AddCorruptor(string corruptorID)
        {
            if (!Globals.Instance.CardListByType[Enums.CardType.Corruption].Contains(corruptorID))
            {
                Plugin.LogDebug($"Adding corruptor {corruptorID} and variants");
                Globals.Instance.CardListByType[Enums.CardType.Corruption].Add(corruptorID);
                Globals.Instance.CardListByType[Enums.CardType.Corruption].Add(corruptorID + "a");
                Globals.Instance.CardListByType[Enums.CardType.Corruption].Add(corruptorID + "b");
                Globals.Instance.CardListByType[Enums.CardType.Corruption].Add(corruptorID + "rare");
            }
            else
            {
                Plugin.LogDebug($"Corruptor {corruptorID} already present");
            }
        }

        public static void MakeCardEpic(string cardID)
        {
            Cards.CardRealtimeData card = Globals.Instance.GetCardData(cardID);
            Dictionary<string, Cards.CardRealtimeData> allCards = Traverse.Create(Globals.Instance).Field("_Cards").GetValue<Dictionary<string, Cards.CardRealtimeData>>();
            Dictionary<string, CardDataNew> allCardsSource = Traverse.Create(Globals.Instance).Field("_CardsSource").GetValue<Dictionary<string, CardDataNew>>();
            Cards.CardRealtimeData newCardRealtime = CloneWithRarity(card, Enums.CardRarity.Epic);
            CardDataNew newCard = allCardsSource.TryGetValue(cardID, out CardDataNew cardData) ? cardData : null;

            if (newCardRealtime != (Cards.CardRealtimeData)null)
            {
                allCards[cardID] = newCardRealtime;
                // allCardsSource[cardID] = newCardRealtime;
                Traverse.Create(Globals.Instance).Field("_Cards").SetValue(allCards);
                Traverse.Create(Globals.Instance).Field("_CardsSource").SetValue(allCardsSource);
                Plugin.LogDebug($"Made Realtime {cardID} epic");
            }

            if (newCard != (CardDataNew)null)
            {
                Traverse.Create(newCard).Field("cardRarity").SetValue(Enums.CardRarity.Epic);
                allCardsSource[cardID] = newCard;
                Traverse.Create(Globals.Instance).Field("_CardsSource").SetValue(allCardsSource);
                Plugin.LogDebug($"Made New {cardID} epic");
            }
        }



        public static Cards.CardRealtimeData CloneWithRarity(Cards.CardRealtimeData card, Enums.CardRarity newRarity)
        {
            var cloneMethod = typeof(Cards.CardRealtimeData).GetMethod(
                "MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);

            var newCard = (Cards.CardRealtimeData)cloneMethod.Invoke(card, null);
            var field = typeof(Cards.CardRealtimeData).GetField(
                "<CardRarity>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

            if (field == null)
                throw new InvalidOperationException("Could not find backing field for CardRarity — check the actual field name.");

            field.SetValue(newCard, newRarity);

            return newCard;
        }
    }
}
