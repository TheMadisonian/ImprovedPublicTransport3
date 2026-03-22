// <copyright file="BuildingWorldInfoPanelPatch.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace FlightTracker
{
    using System.Collections.Generic;
    using System.Reflection;
    using HarmonyLib;

    /// <summary>
    /// Harmony patch to handle building selection changes when the info panel is open.
    /// Patches both BuildingWorldInfoPanel.OnSetTarget (covers all well-behaved subclasses via vtable)
    /// and ShelterWorldInfoPanel.OnSetTarget explicitly, because that subclass overrides the method
    /// without calling base.OnSetTarget(), bypassing the base-class patch.
    /// </summary>
    [HarmonyPatch]
    public static class BuildingWorldInfoPanelPatch
    {
        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(BuildingWorldInfoPanel), "OnSetTarget");

            // ShelterWorldInfoPanel overrides OnSetTarget without calling base — patch directly.
            var shelterType = AccessTools.TypeByName("ShelterWorldInfoPanel");
            if (shelterType != null)
                yield return AccessTools.Method(shelterType, "OnSetTarget");
        }

        /// <summary>
        /// Harmony Postfix: update tracker panel whenever any building info panel changes its target.
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix()
        {
            TrackerPanelManager.TargetChanged();
        }
    }
}