using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BetterBoarding
{
    [HarmonyPatch(typeof(TramAI))]
    [HarmonyPatch("LoadPassengers", MethodType.Normal)]
    // Need to execute after IPT2; and IPT2 did not specify Priority => Priority = Normal
    [HarmonyPriority(Priority.LowerThanNormal)]
    public class LoadPassengers_TramAI
    {
        [HarmonyPrefix]
        public static bool LoadPassengersBetter(ushort vehicleID, ref Vehicle data, ushort currentStop, ushort nextStop)
        {
            BoardingUtility.HandleBetterBoarding(vehicleID, ref data, currentStop, nextStop);
            return false;
        }
    }
}
