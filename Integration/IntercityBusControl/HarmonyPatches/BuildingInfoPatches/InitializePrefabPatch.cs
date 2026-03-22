using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using ImprovedPublicTransport.Util;

namespace IntercityBusControl.HarmonyPatches.BuildingInfoPatches
{
    [HarmonyPatch(typeof(BuildingInfo))]
    [HarmonyPatch(nameof(BuildingInfo.InitializePrefab))]
    internal static class InitializePrefabPatch
    {
        private static Dictionary<string, BuildingInfo> _patchedPrimary = new();
        private static Dictionary<string, BuildingInfo> _patchedSecondary = new();
        private static TransportInfo _transportInfo;

        internal static void Postfix(BuildingInfo __instance)
        {
            try
            {
                if (__instance.m_buildingAI is not TransportStationAI transportStationAi)
                {
                    return;
                }

                if (_transportInfo == null)
                {
                    _transportInfo = PrefabCollection<TransportInfo>.FindLoaded("Intercity Bus");
                }
                if (_transportInfo != null)
                {
                    foreach (var pair in _patchedPrimary)
                    {
                        var ai = pair.Value.GetAI() as TransportStationAI;
                        ai.m_transportInfo = _transportInfo;
                    }

                    foreach (var pair in _patchedSecondary)
                    {
                        var ai = pair.Value.GetAI() as TransportStationAI;
                        ai.m_secondaryTransportInfo = _transportInfo;
                    }
                    _patchedPrimary.Clear();
                    _patchedSecondary.Clear();
                }

                var transportLineInfo1 = transportStationAi.GetTransportLineInfo();
                var transportLineInfo2 = transportStationAi.GetSecondaryTransportLineInfo();
                var intercityTrains =
                    transportLineInfo1 != null && transportLineInfo1.m_class.m_subService ==
                    ItemClass.SubService.PublicTransportTrain || transportLineInfo2 != null &&
                    transportLineInfo2.m_class.m_subService == ItemClass.SubService.PublicTransportTrain;
                var ships =
                    transportLineInfo1 != null && transportLineInfo1.m_class.m_subService ==
                    ItemClass.SubService.PublicTransportShip || transportLineInfo2 != null &&
                    transportLineInfo2.m_class.m_subService == ItemClass.SubService.PublicTransportShip;
                var intercityBus1 = transportLineInfo1 != null &&
                                    transportLineInfo1.m_class.m_subService ==
                                    ItemClass.SubService.PublicTransportBus;
                var intercityBus2 = transportLineInfo2 != null &&
                                    transportLineInfo2.m_class.m_subService ==
                                    ItemClass.SubService.PublicTransportBus;
                // Allow patching when m_transportLineInfo is either unset or already points to the
                // intercity bus line. Ships/trains are intentionally NOT excluded here — mixed hubs
                // (ferry+bus, train+bus, etc.) should also get intercity bus support.
                var shouldPatch = (intercityBus1 ^ intercityBus2) &&
                                  (transportStationAi.m_transportLineInfo == null ||
                                   transportStationAi.m_transportLineInfo.name == Mod.IntercityBusLine);
                Utils.Log($"Intercity Bus Control - {__instance.name} - trains: {intercityTrains}, bus1: {intercityBus1}, bus2: {intercityBus2}");
                if (!shouldPatch)
                {
                    Utils.Log($"Intercity Bus Control - {__instance.name} does not require patching");
                    return;
                }
                Utils.Log($"Intercity Bus Control - patching {__instance.name}...");
                var itemClasses = ((Dictionary<string, ItemClass>)typeof(ItemClassCollection).GetField("m_classDict", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null));
                if (!itemClasses.ContainsKey("Intercity Bus"))
                {
                    throw new Exception("Intercity Bus Control - Sunset Harbor 'Intercity Bus' item class is not found! Is Sunset Harbor DLC installed & enabled?");
                }

                // Set m_transportLineInfo only when it isn't already configured (avoids overwriting
                // stations that were already set up by NetInfoPatches or the asset definition).
                if (transportStationAi.m_transportLineInfo == null)
                {
                    var lineInfo = PrefabCollection<NetInfo>.FindLoaded(Mod.IntercityBusLine);
                    if (lineInfo == null)
                    {
                        Utils.LogWarning($"Intercity Bus Control - {Mod.IntercityBusLine} NetInfo not found!");
                        return;
                    }
                    transportStationAi.m_transportLineInfo = lineInfo;
                }

                if (intercityBus1)
                {
                    __instance.m_class = itemClasses["Intercity Bus"];
                    if (_transportInfo == null)
                    {
                        _patchedPrimary.Add(__instance.name, __instance);
                    }
                    else
                    {
                        transportStationAi.m_transportInfo = _transportInfo;
                    }
                    transportStationAi.m_maxVehicleCount = 100000;
                    Utils.Log($"Intercity Bus Control - patched {__instance.name} primary transport with intercity bus support");
                } 
                else if (intercityBus2)
                {
                    if (_transportInfo == null)
                    {
                        _patchedSecondary.Add(__instance.name, __instance);
                    }
                    else
                    {
                        transportStationAi.m_secondaryTransportInfo = _transportInfo;
                    }
                    transportStationAi.m_maxVehicleCount2 = 100000;
                    Utils.Log($"Intercity Bus Control - patched {__instance.name} secondary transport with intercity bus support");
                }
                StationPatcher.PatchedBuildingNames.Add(__instance.name);
                Utils.Log($"Intercity Bus Control - {__instance.name} was successfully patched");
            }
            catch (Exception e)
            {
                Utils.LogError($"Intercity Bus Control - InitializePrefabPatch error: {e.Message}");
            }
        }
        
        public static void Reset()
        {
            _patchedPrimary.Clear();
            _patchedSecondary.Clear();
            _transportInfo = null;
            StationPatcher.Reset();
        }
    }
}