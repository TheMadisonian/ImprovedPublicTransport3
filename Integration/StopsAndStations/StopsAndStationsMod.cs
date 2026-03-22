// <copyright file="StopsAndStationsMod.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace StopsAndStations
{
    using System;
    using ICities;
    using ImprovedPublicTransport.Util;

    /// <summary>
    /// Integration service for Stops and Stations passenger limiting, managed by IPT3's lifecycle.
    /// Settings are now centralized in IPT3's configuration system and accessible via the Stops options tab.
    /// Logging uses IPT3's centralized logging system.
    /// </summary>
    public sealed class StopsAndStationsIntegration
    {
        /// <summary>Singleton instance created and owned by IPT.</summary>
        public static StopsAndStationsIntegration Instance { get; private set; }

        /// <summary>Initializes a new instance.</summary>
        public StopsAndStationsIntegration()
        {
            Instance = this;
        }

        /// <summary>Called by IPT when a game level is loaded.</summary>
        public void OnLevelLoaded(LoadMode mode)
        {
            switch (mode)
            {
                case LoadMode.LoadGame:
                case LoadMode.NewGame:
                case LoadMode.LoadScenario:
                case LoadMode.NewGameFromScenario:
                    Utils.Log("StopsAndStations: OnLevelLoaded.");
                    break;
                default:
                    return;
            }
        }

        /// <summary>Called by IPT when a game level is about to be unloaded.</summary>
        public void OnLevelUnloading()
        {
            Utils.Log("StopsAndStations: OnLevelUnloading.");
        }
    }
}
