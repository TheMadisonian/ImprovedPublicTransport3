using System.Collections.Generic;
using System.Linq;
using ImprovedPublicTransport.Data;

namespace ImprovedPublicTransport.Query
{
    public static class AvailableVehiclesQuery
    {
        public static List<PrefabData> Query(ItemClassTriplet classTriplet)
        {
            var prefabs = VehiclePrefabs.instance.GetPrefabs(classTriplet.Service, classTriplet.SubService, classTriplet.Level);
            return prefabs.ToList();
        }
    }
}