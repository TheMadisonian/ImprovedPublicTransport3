// <copyright file="FlightRow.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace FlightTracker
{
    using AlgernonCommons.UI;
    using ColossalFramework;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// UIList row item for vehicle prefabs.
    /// </summary>
    public class FlightRow : UIListRow
    {
        /// <summary>
        /// Row height.
        /// </summary>
        public const float FlightRowHeight = 40f;

        // Layout constants - private.
        private const float VehicleSpriteSize = 40f;
        private const float ScrollMargin = 10f;

        // Components.
        private UILabel _vehicleNameLabel;
        private UILabel _statusLabel;
        private UISprite _vehicleSprite;

        // Row data.
        private FlightRowData _rowData;

        /// <summary>
        /// Gets the height for this row.
        /// </summary>
        public override float RowHeight => FlightRowHeight;

        /// <summary>
        /// Generates and displays a row.
        /// </summary>
        /// <param name="data">Object data to display.</param>
        /// <param name="rowIndex">Row index number (for background banding).</param>
        public override void Display(object data, int rowIndex)
        {
            // Perform initial setup for new rows.
            if (_vehicleNameLabel == null)
            {
                // Add text labels.
                _vehicleNameLabel = AddLabel(VehicleSpriteSize + Margin, width - Margin - VehicleSpriteSize - Margin - ScrollMargin - Margin, wordWrap: true);
                _vehicleNameLabel.height = FlightRowHeight / 2f;
                _statusLabel = AddLabel(VehicleSpriteSize + Margin, width - Margin - VehicleSpriteSize - Margin - ScrollMargin - Margin, wordWrap: true);
                _statusLabel.height = FlightRowHeight / 2f;
                _statusLabel.relativePosition += new Vector3(0f, FlightRowHeight / 2f);

                // Add preview sprite image.
                _vehicleSprite = AddUIComponent<UISprite>();
                _vehicleSprite.height = VehicleSpriteSize;
                _vehicleSprite.width = VehicleSpriteSize;
                _vehicleSprite.relativePosition = Vector2.zero;
            }

            // Set name label and vehicle sprite.
            if (data is FlightRowData flightRowData && flightRowData.Info is VehicleInfo vehicleInfo)
            {
                _rowData = flightRowData;
                _vehicleNameLabel.text = flightRowData.Name;
                _statusLabel.text = flightRowData.Status;
                _vehicleSprite.atlas = vehicleInfo.m_Atlas;
                _vehicleSprite.spriteName = vehicleInfo.m_Thumbnail;
            }
            else
            {
                // Just in case (no valid vehicle record).
                _rowData = null;
                _vehicleNameLabel.text = string.Empty;
                _vehicleSprite.isVisible = false;
            }

            // Set initial background as deselected state.
            Deselect(rowIndex);
        }

        /// <summary>
        /// Called when the row is selected (by mouse click).
        /// </summary>
        public override void Select()
        {
            base.Select();

            // Go to target vehicle if available.
            if (_rowData != null)
            {
                try
                {
                    ushort id = _rowData.ID;
                    var vehicle = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[id];
                    var pos = vehicle.GetLastFramePosition();
                    string prefabName = _rowData.Info?.name ?? "(null)";
                    ImprovedPublicTransport.Util.Utils.Log($"[FlightTracker] Selected vehicle {id}: prefab={prefabName}, flags={vehicle.m_flags}, pos={pos}");

                    InstanceID instance = default;
                    instance.Vehicle = id;
                    ToolsModifierControl.cameraController.SetTarget(instance, pos, zoomIn: true);
                }
                catch (System.Exception e)
                {
                    ImprovedPublicTransport.Util.Utils.LogError($"[FlightTracker] Error: {e.Message}");
                }
            }
        }
    }
}