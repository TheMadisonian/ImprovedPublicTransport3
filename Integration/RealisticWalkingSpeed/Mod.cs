using CitiesHarmony.API;
using ICities;
using RealisticWalkingSpeed.Patches;

namespace RealisticWalkingSpeed
{
    /// <summary>
    /// RealisticWalkingSpeedMod entry points for IPT-managed lifecycle. This class is intentionally NOT an IUserMod
    /// so IPT can control initialization and teardown.
    /// </summary>
    public static class RealisticWalkingSpeedMod
    {
        public static void EnableRealisticWalkingSpeedMod()
        {
            HarmonyHelper.DoOnHarmonyReady(() => Patcher.PatchAll());
        }

        public static void DisableRealisticWalkingSpeedMod()
        {
            if (!HarmonyHelper.IsHarmonyInstalled)
                return;

            Patcher.UnpatchAll();
        }

        public static void OnLevelLoaded(LoadMode mode)
        {
            if (mode != LoadMode.NewGame && mode != LoadMode.NewGameFromScenario && mode != LoadMode.LoadGame)
                return;

            new CitizenWalkingSpeedInGamePatch(new SpeedData()).Apply();
        }
    }
}
