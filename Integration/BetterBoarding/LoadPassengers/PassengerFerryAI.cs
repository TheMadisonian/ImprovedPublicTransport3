using HarmonyLib;

namespace BetterBoarding
{
    [HarmonyPatch(typeof(PassengerFerryAI))]
    [HarmonyPatch("LoadPassengers", MethodType.Normal)]
    // need to execute after our other mod, Express Bus Services
    [HarmonyAfter(new string[] { PatchController.ExpressBusServicesHarmonyID })]
    public class LoadPassengers_PassengerFerryAI
    {
        [HarmonyPrefix]
        public static bool LoadPassengersBetter(ushort vehicleID, ref Vehicle data, ushort currentStop, ushort nextStop)
        {
            BoardingUtility.HandleBetterBoarding(vehicleID, ref data, currentStop, nextStop);
            return false;
        }
    }
}
