using HarmonyLib;
using System.Reflection;

namespace MileageTaxiServices
{
    internal class PatchController
    {
        public static string HarmonyModID
        {
            get
            {
                // Use a concise Harmony ID tied to IPT3 and the mileage taxi integration
                // to avoid clashes and make ownership obvious when inspecting Harmony patches.
                return "com.ipt3.mileagetaxi";
            }
        }

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

        public static void Activate()
        {
            GetHarmonyInstance().PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void Deactivate()
        {
            GetHarmonyInstance().UnpatchAll(HarmonyModID);
        }
    }
}
