using ColossalFramework;
using ColossalFramework.Plugins;
using HarmonyLib;
using System.Linq;

namespace ExpressBusServices
{
    internal static class ModDetector
    {
        public static bool TransportLinesManagerIsLoaded()
        {
            // TLM support has been removed from this mod; always report as not loaded.
            return false;
        }

        private static bool VerifyModEnabled(ulong modId)
        {
            PluginManager.PluginInfo pluginInfo = Singleton<PluginManager>.instance.GetPluginsInfo().FirstOrDefault((PluginManager.PluginInfo pi) => pi.publishedFileID.AsUInt64 == modId);
            return pluginInfo != null && pluginInfo.isEnabled && pluginInfo.overrideState == PluginManager.OverrideState.Enabled;
        }
    }
}
