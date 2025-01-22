using UnityEngine;
using Verse;
using HarmonyLib;

namespace CaravanMyWay
{
    public class CaravanMyWayMod : Mod
    {
        public static CaravanMyWaySettings Settings { get; private set; }
        private static Harmony harmony;

        public CaravanMyWayMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<CaravanMyWaySettings>();
            ModLogger.ToggleDebug(Settings.enableDebugMode);
            
            harmony = new Harmony("LucidBound.CaravanMyWay");
            harmony.PatchAll();
            
            ModLogger.Message("Initialized!");
        }

        public override string SettingsCategory()
        {
            return "Caravan My Way";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            
            listingStandard.CheckboxLabeled(
                "Enable enhanced caravan menus", 
                ref Settings.enableEnhancedMenus,
                "Enable the enhanced menus for selecting which pawn carries what items in caravans.");
            
            listingStandard.CheckboxLabeled(
                "Enable wait to send", 
                ref Settings.enableWaitToSend,
                "Enable the option to manually control when caravans depart.");

            listingStandard.CheckboxLabeled(
                "Enable debug logging", 
                ref Settings.enableDebugMode,
                "Enable detailed debug logging (warning: very verbose!)");

            if (Settings.enableDebugMode != ModLogger.IsDebugEnabled)
            {
                ModLogger.ToggleDebug(Settings.enableDebugMode);
            }

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }
    }
}