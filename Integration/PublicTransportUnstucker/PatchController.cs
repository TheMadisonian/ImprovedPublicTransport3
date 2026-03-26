using HarmonyLib;
using System.Reflection;

namespace PublicTransportUnstucker
{
    internal static class PatchController
    {
        public static string HarmonyModID => "com.vectorial1024.cities.ptu";

        /*
         * The "singleton" design is pretty straight-forward.
         */

        private static Harmony harmony;

        public static Harmony GetHarmonyInstance()
        {
            if (harmony == null)
            {
                harmony = new Harmony(HarmonyModID);
            }

            return harmony;
        }

        private static bool _isActive;

        public static void Activate()
        {
            if (_isActive)
            {
                return;
            }

            GetHarmonyInstance().PatchAll(Assembly.GetExecutingAssembly());
            RoguePassengerTable.EnsureTableExists();
            _isActive = true;
        }

        public static void Deactivate()
        {
            if (!_isActive)
            {
                return;
            }

            GetHarmonyInstance().UnpatchAll(HarmonyModID);
            RoguePassengerTable.WipeTable();
            _isActive = false;
        }
    }
}
