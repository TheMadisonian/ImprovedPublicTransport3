using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ImprovedPublicTransport.Util;

namespace IntercityBusControl
{
    public static class StationPatcher
    {
        // Names of building prefabs that this mod has explicitly patched to support intercity buses.
        // Used by UpdateBindingsPatch to show the toggle only on these stations, not on buildings
        // that are natively intercity bus stations/hubs.
        public static readonly HashSet<string> PatchedBuildingNames = new HashSet<string>();

        public static void Reset()
        {
            PatchedBuildingNames.Clear();
        }

        /// <summary>
        /// Iterates all loaded BuildingInfo prefabs and applies intercity bus support to any
        /// bus station or bus hub that is not already configured as an intercity bus station.
        /// Detection is based on m_transportInfo/m_secondaryTransportInfo (no hardcoded names).
        /// Must be called after all prefabs have been loaded (e.g. in OnLevelLoaded).
        /// </summary>
        public static void PatchStations()
        {
            try
            {
                var intercityBusLine = PrefabCollection<NetInfo>.FindLoaded(Mod.IntercityBusLine);
                if (intercityBusLine == null)
                {
                    Utils.LogWarning("Intercity Bus Control - '" + Mod.IntercityBusLine + "' NetInfo not found; skipping station patching.");
                    return;
                }

                var classDict = (Dictionary<string, ItemClass>)
                    typeof(ItemClassCollection)
                    .GetField("m_classDict", BindingFlags.Static | BindingFlags.NonPublic)
                    .GetValue(null);
                if (!classDict.ContainsKey("Intercity Bus"))
                {
                    Utils.LogWarning("Intercity Bus Control - 'Intercity Bus' item class not found; Sunset Harbor DLC may not be active.");
                    return;
                }
                var intercityBusClass = classDict["Intercity Bus"];
                var intercityBusTransport = PrefabCollection<TransportInfo>.FindLoaded("Intercity Bus");

                int patched = 0;
                uint count = (uint)PrefabCollection<BuildingInfo>.LoadedCount();
                for (uint i = 0; i < count; i++)
                {
                    var info = PrefabCollection<BuildingInfo>.GetLoaded(i);
                    if (info?.m_buildingAI is TransportStationAI ai)
                    {
                        if (TryPatchStation(info, ai, intercityBusLine, intercityBusClass, intercityBusTransport))
                            patched++;
                    }
                }
                Utils.Log($"Intercity Bus Control - PatchStations complete: {patched} station(s) patched.");
            }
            catch (Exception e)
            {
                Utils.LogError($"Intercity Bus Control - PatchStations error: {e.Message}");
            }
        }

        private static bool TryPatchStation(
            BuildingInfo info, TransportStationAI ai,
            NetInfo intercityBusLine, ItemClass intercityBusClass, TransportInfo intercityBusTransport)
        {
            var ti1 = ai.m_transportInfo;
            var ti2 = ai.m_secondaryTransportInfo;

            bool isBusPrimary   = IsBusSubService(ti1, ItemClass.SubService.PublicTransportBus);
            bool isBusSecondary = IsBusSubService(ti2, ItemClass.SubService.PublicTransportBus);

            // Skip if no bus transport at all, or if somehow both transports are bus (unusual).
            // This correctly handles mixed hubs (ferry+bus, train+bus, etc.) — they qualify as long
            // as exactly one of their transport slots is bus.
            if (!(isBusPrimary ^ isBusSecondary)) return false;

            bool alreadyHasIntercityLine = ai.m_transportLineInfo?.name == Mod.IntercityBusLine;
            int  curMax = isBusPrimary ? ai.m_maxVehicleCount : ai.m_maxVehicleCount2;

            // Already fully patched — skip
            if (alreadyHasIntercityLine && curMax > 0) return false;

            // For pure bus stations (bus is primary transport): only patch if m_transportLineInfo
            // is not already assigned to a different network — this avoids rewriting regular bus
            // depots that have their own bus road configured.
            if (isBusPrimary && ai.m_transportLineInfo != null && !alreadyHasIntercityLine) return false;

            // Apply intercity bus support
            ai.m_transportLineInfo = intercityBusLine;

            if (isBusPrimary)
            {
                info.m_class = intercityBusClass;
                if (intercityBusTransport != null) ai.m_transportInfo = intercityBusTransport;
                ai.m_maxVehicleCount = 100000;
                Utils.Log($"Intercity Bus Control - StationPatcher: patched {info.name} (primary bus)");
            }
            else
            {
                // Multi-modal hub with secondary bus — don't change the primary item class
                if (intercityBusTransport != null) ai.m_secondaryTransportInfo = intercityBusTransport;
                ai.m_maxVehicleCount2 = 100000;
                Utils.Log($"Intercity Bus Control - StationPatcher: patched {info.name} (secondary bus)");
            }
            PatchedBuildingNames.Add(info.name);
            return true;
        }

        private static bool IsBusSubService(TransportInfo ti, ItemClass.SubService subService) =>
            ti != null && ti.m_class.m_subService == subService;
    }
}