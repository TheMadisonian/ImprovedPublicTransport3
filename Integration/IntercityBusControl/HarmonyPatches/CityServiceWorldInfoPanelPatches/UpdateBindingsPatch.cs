using ColossalFramework.UI;
using HarmonyLib;
using ImprovedPublicTransport.Util;

namespace IntercityBusControl.HarmonyPatches.CityServiceWorldInfoPanelPatches
{
    
    [HarmonyPatch(typeof(CityServiceWorldInfoPanel))]
    [HarmonyPatch("UpdateBindings")]
    internal static class UpdateBindingsPatch
    {
        private static string _originalLabel;
        private static string _originalTooltip;
        
        
        internal static void Postfix(CityServiceWorldInfoPanel __instance, InstanceID ___m_InstanceID, UIPanel ___m_intercityTrainsPanel)
        {
            // Defensive checks: panel or label might be unavailable in certain game versions or due to other mods
            if (___m_intercityTrainsPanel == null)
            {
                Utils.LogWarning("Intercity Bus Control - intercity trains panel not found");
                return;
            }

            var label = ___m_intercityTrainsPanel.Find<UILabel>("Label");
            if (label == null)
            {
                Utils.LogWarning("Intercity Bus Control - label not found on intercity trains panel");
                return;
            }

            _originalLabel ??= label.text;
            _originalTooltip ??= label.tooltip;

            var building1 = ___m_InstanceID.Building;
            var instance = BuildingManager.instance;
            var building2 = instance.m_buildings.m_buffer[building1];
            var info = building2.Info;
            var buildingAi = info.m_buildingAI;
            var transportStationAi = buildingAi as TransportStationAI;
            if (transportStationAi == null)
            {
                label.text = _originalLabel;
                label.tooltip = _originalTooltip;
                return;
            }

            var transportLineInfo1 = transportStationAi.GetTransportLineInfo();
            var transportLineInfo2 = transportStationAi.GetSecondaryTransportLineInfo();

            var ships =
                transportLineInfo1 != null && transportLineInfo1.m_class.m_subService ==
                ItemClass.SubService.PublicTransportShip || transportLineInfo2 != null &&
                transportLineInfo2.m_class.m_subService == ItemClass.SubService.PublicTransportShip;

            var intercityTrains = transportLineInfo1 != null && transportLineInfo1.m_class.m_subService == ItemClass.SubService.PublicTransportTrain || transportLineInfo2 != null && transportLineInfo2.m_class.m_subService == ItemClass.SubService.PublicTransportTrain;

            var intercityBus1 = transportLineInfo1 != null && transportLineInfo1.m_class.m_subService == ItemClass.SubService.PublicTransportBus && transportStationAi.m_maxVehicleCount > 0;
            var intercityBus2 = transportLineInfo2 != null && transportLineInfo2.m_class.m_subService == ItemClass.SubService.PublicTransportBus && transportStationAi.m_maxVehicleCount2 > 0;

            // Show the toggle only on buildings our mod explicitly patched (PatchedBuildingNames).
            // We do NOT gate on !ships/!intercityTrains here — mixed hubs (ferry+bus, train+bus)
            // are in PatchedBuildingNames and should show the toggle. Native intercity bus stations
            // were never added to PatchedBuildingNames so they are correctly excluded.
            var intercityBuses = (intercityBus1 || intercityBus2)
                && StationPatcher.PatchedBuildingNames.Contains(info.name);
            var isVisible = intercityBuses;

            if (isVisible)
            {
                ___m_intercityTrainsPanel.parent.isVisible = true;
                ___m_intercityTrainsPanel.isVisible = true;
            }

            if (intercityBuses)
            {
                label.text = ImprovedPublicTransport.Localization.Get("CITYSERVICE_ACCEPTINTERCITYBUSES");
                label.tooltip = ImprovedPublicTransport.Localization.Get("CITYSERVICE_ACCEPTINTERCITYBUSES_TOOLTIP");
            }
            else
            {
                label.text = _originalLabel;
                label.tooltip = _originalTooltip;
            }
        }
    }
}