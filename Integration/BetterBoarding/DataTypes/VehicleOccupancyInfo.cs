using System;
using ColossalFramework;
using UnityEngine;

namespace BetterBoarding.DataTypes
{
    public class VehicleOccupancyInfo
    {
        public ushort VehicleID { get; }

        public ushort BoardingVehicleID { get; }

        public bool IsProxy { get; }

        public int Occupancy { get; }

        public int ActualCapacity { get; }

        public Vector3 Position { get; }

        public bool IsFull => !IsProxy && Occupancy >= ActualCapacity;

        public VehicleOccupancyInfo(ushort vehicleID)
        {
            VehicleID = vehicleID;

            // load the relevant stats from the global table for convenience
            var vehicleManager = Singleton<VehicleManager>.instance;
            var citizenManager = Singleton<CitizenManager>.instance;

            var vehicleInstance = vehicleManager.m_vehicles.m_buffer[vehicleID];
            // Each vehicle in a chain maintains independent frame position data during simulation
            // Use frame3 as it's most recently updated; fallback through earlier frames if needed
            var frameData = vehicleInstance.GetLastFrameData();
            Position = frameData.m_position;
            Occupancy = vehicleInstance.m_transferSize;

            BoardingVehicleID = vehicleID;
            IsProxy = false;

            // iterate the list to find actual capacity
            var currentCitizenUnit = vehicleInstance.m_citizenUnits;
            var citizenUnitCount = 0;
            while (currentCitizenUnit != 0)
            {
                ++citizenUnitCount;
                currentCitizenUnit = citizenManager.m_units.m_buffer[currentCitizenUnit].m_nextUnit;
            }

            // we do this so we can catch potential edge case of not having enough citizen units, while maintaining asset-stats correctness
            var nominalCapacity = vehicleInstance.Info.m_vehicleAI.GetPassengerCapacity(false);
            ActualCapacity = Math.Min(citizenUnitCount * 5, nominalCapacity);
        }
    }
}
