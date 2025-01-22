using Verse;
using UnityEngine;

namespace CaravanMyWay
{
    public class CaravanMyWaySettings : ModSettings
    {
        public bool enableEnhancedMenus = true;
        public bool enableWaitToSend = true;
        public bool enableDebugMode = false;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableEnhancedMenus, "enableEnhancedMenus", true);
            Scribe_Values.Look(ref enableWaitToSend, "enableWaitToSend", true);
            Scribe_Values.Look(ref enableDebugMode, "enableDebugMode", false);
            base.ExposeData();
        }
    }
}