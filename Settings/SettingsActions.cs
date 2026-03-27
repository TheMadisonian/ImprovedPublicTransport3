using System;
using System.Linq;
using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;
using ImprovedPublicTransport.OptionsFramework;
using ImprovedPublicTransport.Data;
using RealisticWalkingSpeed;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace ImprovedPublicTransport.Settings
{
    public static class SettingsActions
    {
        // Reference to vehicle count slider for enabling/disabling when budget control state changes
        public static UISlider VehicleCountSlider { get; set; }

        public static void OnBudgetModeChanged(int mode)
        {
            var isBudgetOn = (mode == (int)ImprovedPublicTransport.Settings.Settings.BudgetControlModes.Enabled);
            
            // Update slider state immediately
            if (VehicleCountSlider != null)
            {
                var activeTrackColor = new Color32(100, 100, 100, 255);
                var inactiveTrackColor = new Color32(50, 50, 50, 255);
                var activeThumbColor = new Color32(255, 255, 255, 255);
                var inactiveThumbColor = new Color32(60, 60, 60, 255);

                // Set both normal and disabled colors because disabled rendering uses disabledColor.
                VehicleCountSlider.color = isBudgetOn ? inactiveTrackColor : activeTrackColor;
                VehicleCountSlider.disabledColor = inactiveTrackColor;

                if (VehicleCountSlider.thumbObject != null)
                {
                    VehicleCountSlider.thumbObject.color = isBudgetOn ? inactiveThumbColor : activeThumbColor;
                    VehicleCountSlider.thumbObject.disabledColor = inactiveThumbColor;
                }

                VehicleCountSlider.isEnabled = !isBudgetOn;
            }
            
            if (!ImprovedPublicTransportMod.InGame)
            {
                return;
            }
            
            SimulationManager.instance.AddAction(() =>
            {
                var instance = Singleton<TransportManager>.instance;
                if (instance == null)
                {
                    Utils.LogWarning("SettingsActions: OnBudgetModeChanged called before TransportManager is available.");
                    return;
                }

                int length = instance.m_lines.m_buffer.Length;
                for (int index = 0; index < length; ++index)
                {
                    CachedTransportLineData.SetBudgetControlState((ushort) index, isBudgetOn);
                    if (isBudgetOn)
                        CachedTransportLineData.ClearEnqueuedVehicles((ushort) index);
                }
            });
        }

        public static void OnTicketPriceCustomizerChanged(int mode)
        {
            bool enabled = mode == (int)ImprovedPublicTransport.Settings.Settings.TicketPriceCustomizerModes.Enabled;

            // Update UI tab immediately on main thread (the dropdown callback runs on UI thread)
            ImprovedPublicTransport.Integration.TicketPriceCustomizer.TicketPricesTab.UpdateTabState();

            // Update day/night watcher immediately, on UI thread too (safe for component operations)
            if (ImprovedPublicTransportMod.IptGameObject != null)
            {
                var watcher = ImprovedPublicTransportMod.IptGameObject.GetComponent<ImprovedPublicTransport.Integration.TicketPriceCustomizer.DayNightPriceWatcher>();
                if (enabled)
                {
                    if (watcher == null)
                    {
                        ImprovedPublicTransportMod.IptGameObject.AddComponent<ImprovedPublicTransport.Integration.TicketPriceCustomizer.DayNightPriceWatcher>();
                    }
                }
                else
                {
                    if (watcher != null)
                    {
                        UnityEngine.Object.Destroy(watcher);
                    }
                }
            }

            if (!ImprovedPublicTransportMod.InGame)
            {
                return;
            }

            // Apply ticket multipliers in simulation thread (game data manipulation)
            SimulationManager.instance.AddAction(() =>
            {
                try
                {
                    if (enabled)
                    {
                        ImprovedPublicTransport.Integration.TicketPriceCustomizer.PriceCustomization.SetPrices(OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.TicketPriceCustomizer);
                        Utils.Log("SettingsActions: TicketPriceCustomizer enabled.");
                    }
                    else
                    {
                        // Revert to vanilla prices when disabling
                        ImprovedPublicTransport.Integration.TicketPriceCustomizer.PriceCustomization.ResetToVanilla();
                        Utils.Log("SettingsActions: TicketPriceCustomizer disabled and prices reset to vanilla.");
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"SettingsActions: OnTicketPriceCustomizerChanged failed: {ex.Message}");
                }
            });

        }

        public static void OnPublicTransportUnstuckerChanged(int value)
        {
            if (!ImprovedPublicTransportMod.InGame)
            {
                return;
            }

            SimulationManager.instance.AddAction(() =>
            {
                if (value != 0)
                {
                    Utils.Log("SettingsActions: Enabling PublicTransportUnstucker");
                    PublicTransportUnstucker.PublicTransportUnstuckerIntegration.Activate();
                }
                else
                {
                    Utils.Log("SettingsActions: Disabling PublicTransportUnstucker");
                    PublicTransportUnstucker.PublicTransportUnstuckerIntegration.Deactivate();
                }
            });
        }

        public static void OnRealisticWalkingSpeedChanged(int walkingSpeedMode)
        {
            Utils.Log($"SettingsActions: OnRealisticWalkingSpeedChanged called with mode {walkingSpeedMode}");
            
            if (!ImprovedPublicTransportMod.InGame)
            {
                Utils.Log("SettingsActions: Not in-game, changes will be applied when game loads");
                return;
            }
            
            SimulationManager.instance.AddAction(() =>
            {
                try
                {
                    if (walkingSpeedMode == (int)ImprovedPublicTransport.Settings.Settings.WalkingSpeedModes.Realistic)
                    {
                        Utils.Log("SettingsActions: Enabling Realistic Walking Speed");
                        RealisticWalkingSpeedMod.EnableRealisticWalkingSpeedMod();
                    }
                    else
                    {
                        Utils.Log("SettingsActions: Disabling Realistic Walking Speed");
                        RealisticWalkingSpeedMod.DisableRealisticWalkingSpeedMod();
                    }
                }
                catch (System.Exception ex)
                {
                    Utils.LogError($"Failed to toggle Realistic Walking Speed: {ex.Message}\n{ex.StackTrace}");
                }
            });
        }

        public static void OnDefaultVehicleCountSubmitted(int count)
        {
            if (!ImprovedPublicTransportMod.InGame)
            {
                return;
            }
            SimulationManager.instance.AddAction(() =>
            {
                TransportManager instance = Singleton<TransportManager>.instance;
                int length = instance.m_lines.m_buffer.Length;
                for (int index = 0; index < length; ++index)
                {
                    if (!instance.m_lines.m_buffer[index].Complete)
                        CachedTransportLineData.SetTargetVehicleCount((ushort) index, count);
                }
            });
        }


        public static void OnResetButtonClick()
        {
            if (!ImprovedPublicTransportMod.InGame)
            {
                return;
            }
            SimulationManager.instance.AddAction(() =>
            {
                // Reset options to their defaults
                var options = OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options;
                options.IntervalAggressionFactor = 52;
                options.DefaultVehicleCount = 0;
                options.SpawnTimeInterval = 10;
                OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.SaveOptions();

                // Apply immediate effects to existing lines
                int length = Singleton<TransportManager>.instance.m_lines.m_buffer.Length;
                for (int index = 0; index < length; ++index)
                {
                    CachedTransportLineData.SetNextSpawnTime((ushort) index, 0.0f);
                    CachedTransportLineData.SetTargetVehicleCount((ushort) index, options.DefaultVehicleCount);
                }

                Localization.Get("Unbunching settings reset to defaults.");
            });
        }


        public static void OnDeleteLinesClick()
        {
            if (!ImprovedPublicTransportMod.InGame)
            {
                return;
            }
            if (!OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.DeleteBusLines &&
                !OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.DeleteSightseeingBusLines &&
                !OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.DeleteTramLines &&
                !OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.DeleteTrolleybusLines &&
                !OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.DeleteTrainLines &&
                !OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.DeleteMetroLines &&
                !OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.DeleteMonorailLines &&
                !OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.DeleteShipLines &&
                !OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.DeleteHelicopterLines &&
                !OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.DeleteBlimpLines)
            {
                return;
            }
            WorldInfoPanel.Hide<PublicTransportWorldInfoPanel>();
            ConfirmPanel.ShowModal(Localization.Get("SETTINGS_LINE_DELETION_TOOL_CONFIRM_TITLE"),
                Localization.Get("SETTINGS_LINE_DELETION_TOOL_CONFIRM_MSG"), (s, r) =>
                {
                    if (r != 1)
                        return;
                    Singleton<SimulationManager>.instance.AddAction(() =>
                    {
                        SimulationManager.instance.AddAction(DeleteLines);
                    });
                });
        }

        private static void DeleteLines()
        {
            TransportManager instance = Singleton<TransportManager>.instance;
            int length = instance.m_lines.m_buffer.Length;
            for (int index = 0; index < length; ++index)
            {
                TransportInfo info = instance.m_lines.m_buffer[index].Info;
                if (info == null || instance.m_lines.m_buffer[index].m_flags == TransportLine.Flags.None)
                {
                    continue;
                }
                bool flag = false;
                var subService = info.GetSubService();
                var service = info.GetService();
                var level = info.GetClassLevel();
                if (service == ItemClass.Service.PublicTransport) //TODO(): handle evacuation buses
                {
                    if (level == ItemClass.Level.Level1)
                    {
                        switch (subService)
                        {
                            case ItemClass.SubService.PublicTransportBus:
                                flag = OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.DeleteBusLines;
                                break;
                            case ItemClass.SubService.PublicTransportMetro:
                                flag = OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.DeleteMetroLines;
                                break;
                            case ItemClass.SubService.PublicTransportTrain:
                                flag = OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.DeleteTrainLines;
                                break;
                            case ItemClass.SubService.PublicTransportShip:
                                flag = OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.DeleteShipLines;
                                break;
                            case ItemClass.SubService.PublicTransportPlane:
                                if (info.m_vehicleType == VehicleInfo.VehicleType.Helicopter)
                                    flag = OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.DeleteHelicopterLines;
                                else if (info.m_vehicleType == VehicleInfo.VehicleType.Blimp)
                                    flag = OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.DeleteBlimpLines;
                                break;
                            case ItemClass.SubService.PublicTransportTram:
                                flag = OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.DeleteTramLines;
                                break;
                            case ItemClass.SubService.PublicTransportMonorail:
                                flag = OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.DeleteMonorailLines;
                                break;
                            case ItemClass.SubService.PublicTransportTrolleybus:
                                flag = OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.DeleteTrolleybusLines;
                                break;
                        }
                    }
                    else if (level == ItemClass.Level.Level2)
                    {
                        switch (subService)
                        {
                            case ItemClass.SubService.PublicTransportBus:
                                flag = OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.DeleteBusLines;
                                break;
                            case ItemClass.SubService.PublicTransportShip:
                                flag = OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.DeleteShipLines;
                                break;
                            case ItemClass.SubService.PublicTransportPlane:
                                if (info.m_vehicleType == VehicleInfo.VehicleType.Helicopter)
                                    flag = OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.DeleteHelicopterLines;
                                else if (info.m_vehicleType == VehicleInfo.VehicleType.Blimp)
                                    flag = OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.DeleteBlimpLines;
                                break;
                            case ItemClass.SubService.PublicTransportTrain:
                                flag = OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.DeleteTrainLines;
                                break;
                        }
                    }
                    else if (level == ItemClass.Level.Level3)
                    {
                        switch (subService)
                        {
                            case ItemClass.SubService.PublicTransportTours:
                                flag = OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.DeleteSightseeingBusLines;
                                break;
                            case ItemClass.SubService.PublicTransportPlane:
                                if (info.m_vehicleType == VehicleInfo.VehicleType.Helicopter)
                                    flag = OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.DeleteHelicopterLines;
                                else if (info.m_vehicleType == VehicleInfo.VehicleType.Blimp)
                                    flag = OptionsWrapper<ImprovedPublicTransport.Settings.Settings>.Options.DeleteBlimpLines;
                                break;
                        }
                    }
                    if (flag)
                    {
                        instance.ReleaseLine((ushort) index); //TODO(): make sure that outside connection lines don't get deleted
                    }
                }
            }
        }
    }
}