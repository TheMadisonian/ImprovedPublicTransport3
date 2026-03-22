using HarmonyLib;

namespace BetterBoarding
{
    [HarmonyPatch(typeof(TrolleybusAI))]
    [HarmonyPatch("LoadPassengers", MethodType.Normal)]
    public class LoadPassengers_TrolleybusAI
    {
        [HarmonyPrefix]
        public static bool LoadPassengersBetter(ushort vehicleID, ref Vehicle data, ushort currentStop, ushort nextStop)
        {
            BoardingUtility.HandleBetterBoarding(vehicleID, ref data, currentStop, nextStop);
            return false;
        }
    }
}
