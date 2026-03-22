// <copyright file="FlightRowData.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace FlightTracker
{
    using AlgernonCommons.Utils;

    /// <summary>
    /// Flight item record.
    /// </summary>
    public class FlightRowData
    {
        // Vehicle info.
        private readonly ushort _vehicleID;
        private readonly FlightStatus _status;
        private VehicleInfo _vehicleInfo;
        private string _vehicleName;

        /// <summary>
        /// Initializes a new instance of the <see cref="FlightRowData"/> class.
        /// </summary>
        /// <param name="vehicleID">Vehicle ID for this item.</param>
        /// <param name="prefab">Vehicle prefab.</param>
        /// <param name="status">Vehicle flight status.</param>
        public FlightRowData(ushort vehicleID, VehicleInfo prefab, FlightStatus status)
        {
            _vehicleID = vehicleID;
            _status = status;
            Info = prefab;
        }

        /// <summary>
        /// Flight status enum.
        /// </summary>
        public enum FlightStatus : int
        {
            /// <summary>
            /// No status.
            /// </summary>
            None = 0,

            /// <summary>
            /// Flight is incoming.
            /// </summary>
            Incoming,

            /// <summary>
            /// Flight is incoming and has landed.
            /// </summary>
            Landed,

            /// <summary>
            /// Flight is at the gate.
            /// </summary>
            AtGate,

            /// <summary>
            /// Flight has departed.
            /// </summary>
            Departed,
        }

        /// <summary>
        /// Gets the vehicle's name (empty string if none).
        /// </summary>
        public string Name => _vehicleName;

        /// <summary>
        /// Gets the vehicle's ID.
        /// </summary>
        public ushort ID => _vehicleID;

        /// <summary>
        /// Gets the vehicle's status text.
        /// </summary>
        public string Status
        {
            get
            {
                switch (_status)
                {
                    default:
                    case FlightStatus.None:
                        return ImprovedPublicTransport.Localization.Get("FLIGHT_STATUS_NONE");
                    case FlightStatus.Incoming:
                        return ImprovedPublicTransport.Localization.Get("FLIGHT_STATUS_INCOMING");
                    case FlightStatus.Landed:
                        return ImprovedPublicTransport.Localization.Get("FLIGHT_STATUS_LANDED");
                    case FlightStatus.AtGate:
                        return ImprovedPublicTransport.Localization.Get("FLIGHT_STATUS_AT_GATE");
                    case FlightStatus.Departed:
                        return ImprovedPublicTransport.Localization.Get("FLIGHT_STATUS_DEPARTED");
                }
            }
        }

        /// <summary>
        /// Gets or sets the vehicle prefab for this record.
        /// </summary>
        public VehicleInfo Info
        {
            get => _vehicleInfo;

            set
            {
                _vehicleInfo = value;

                // Set display name.
                _vehicleName = PrefabUtils.GetDisplayName(value);
            }
        }
    }
}