using System;
using System.Collections.Generic;
using System.Reflection;

namespace IntercityBusControl
{
    // Static config holder for IntercityBusControl integration.
    // Not an IUserMod so the IBC module won't appear as a separate mod in the Mods list.
    public static class Mod
    {
        public const string IntercityBusLine = "Intercity Bus Line";
        public const string Name = "Intercity Bus Control";
        public const string Description = "Intercity Bus Control";

        // Returns true when the Sunset Harbor 'Intercity Bus' item class exists (DLC available)
        public static bool IsSunsetHarborInstalled()
        {
            try
            {
                var itemClasses = (Dictionary<string, ItemClass>)typeof(ItemClassCollection).GetField("m_classDict", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                return itemClasses != null && itemClasses.ContainsKey("Intercity Bus");
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}