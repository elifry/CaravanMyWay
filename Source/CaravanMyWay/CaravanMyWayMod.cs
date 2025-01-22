using Verse;
using HarmonyLib;

namespace CaravanMyWay
{
    public class CaravanMyWayMod : Mod
    {
        public CaravanMyWayMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("LucidBound.CaravanMyWay");
            harmony.PatchAll();
            Log.Message("[CaravanMyWay] Initialized.");
        }
    }
}