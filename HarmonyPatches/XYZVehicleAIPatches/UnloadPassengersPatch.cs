using System;
using HarmonyLib;
using ImprovedPublicTransport.Data;
using ImprovedPublicTransport.Util;
using UnityEngine;

namespace ImprovedPublicTransport.HarmonyPatches.XYZVehicleAIPatches
{
    public class UnloadPassengersPatch
    {
        private const string UnloadPassengersMethod = "UnloadPassengers";

        public static void Apply()
        {
            PatchUnloadPassengers(typeof(BusAI));
            PatchUnloadPassengers(typeof(TrolleybusAI));
            PatchUnloadPassengers(typeof(TramAI));
            PatchUnloadPassengers(typeof(PassengerTrainAI));
            PatchUnloadPassengers(typeof(PassengerPlaneAI));
            PatchUnloadPassengers(typeof(PassengerHelicopterAI));
            PatchUnloadPassengers(typeof(PassengerBlimpAI));
            PatchUnloadPassengers(typeof(PassengerFerryAI));
            PatchUnloadPassengers(typeof(PassengerShipAI));
        }

        public static void Undo()
        {
            UnpatchUnloadPassengers(typeof(BusAI));
            UnpatchUnloadPassengers(typeof(TrolleybusAI));
            UnpatchUnloadPassengers(typeof(TramAI));
            UnpatchUnloadPassengers(typeof(PassengerTrainAI));
            UnpatchUnloadPassengers(typeof(PassengerPlaneAI));
            UnpatchUnloadPassengers(typeof(PassengerHelicopterAI));
            UnpatchUnloadPassengers(typeof(PassengerBlimpAI));
            UnpatchUnloadPassengers(typeof(PassengerFerryAI));
            UnpatchUnloadPassengers(typeof(PassengerShipAI));
        }

        public static bool UnloadPassengersPre(ushort vehicleID, ushort currentStop, out State __state)
        {
            if (VehicleManager.instance.m_vehicles.m_buffer[vehicleID].m_leadingVehicle != 0)
            {
                __state = new State { vehicleID = 0 };  // Mark as trailer with 0
                return true;
            }

            __state = new State()
            {
                vehicleID = vehicleID,
                currentStop = currentStop,
                currentPassengers = VehicleUtil.GetTotalPassengerCount(vehicleID, CachedVehicleData.MaxVehicleCount)
            };
            return true;
        }

        public static void UnloadPassengersPost(State __state)
        {
            // Skip if this was a trailer (vehicleID == 0 from pre-function's marker)
            if (__state.vehicleID == 0)
            {
                return;
            }

            if (VehicleManager.instance.m_vehicles.m_buffer[__state.vehicleID].m_leadingVehicle != 0)
            {
                return;
            }

            var currentPassengers =
                VehicleUtil.GetTotalPassengerCount(__state.vehicleID, CachedVehicleData.MaxVehicleCount);
            var passengersOut = Mathf.Max(0, __state.currentPassengers - currentPassengers);
            CachedVehicleData.m_cachedVehicleData[__state.vehicleID]
                .DisembarkPassengers(passengersOut, __state.currentStop);
            CachedNodeData.m_cachedNodeData[__state.currentStop].PassengersOut += passengersOut;
        }

        public struct State
        {
            public ushort vehicleID;
            public ushort currentPassengers;
            public ushort currentStop;
        }

        private static void PatchUnloadPassengers(Type type)
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(type, UnloadPassengersMethod),
                new PatchUtil.MethodDefinition(typeof(UnloadPassengersPatch), nameof(UnloadPassengersPre), priority: Priority.Normal),
                new PatchUtil.MethodDefinition(typeof(UnloadPassengersPatch), nameof(UnloadPassengersPost), priority: Priority.Normal)
            );
        }

        private static void UnpatchUnloadPassengers(Type type)
        {
            PatchUtil.Unpatch(new PatchUtil.MethodDefinition(type, UnloadPassengersMethod));
        }
    }
}