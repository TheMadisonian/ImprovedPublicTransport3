using ColossalFramework;
using ImprovedPublicTransport.OptionsFramework;
using UnityEngine;

namespace ImprovedPublicTransport.Integration.TicketPriceCustomizer
{
    /// <summary>
    /// MonoBehaviour that detects day/night transitions and re-applies the correct
    /// ticket price multipliers. Attached to IptGameObject on level load.
    /// </summary>
    public class DayNightPriceWatcher : MonoBehaviour
    {
        private bool _lastIsNight;

        private void Start()
        {
            _lastIsNight = Singleton<SimulationManager>.instance.m_isNightTime;
        }

        private void Update()
        {
            bool isNight = Singleton<SimulationManager>.instance.m_isNightTime;
            if (isNight == _lastIsNight) return;

            _lastIsNight = isNight;
            var settings = OptionsWrapper<Settings.Settings>.Options.TicketPriceCustomizer;
            if (settings != null)
                PriceCustomization.ApplyForCurrentTime(settings);
        }
    }
}
