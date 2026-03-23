using System;
using System.ComponentModel;
using System.Xml.Serialization;
using ColossalFramework.PlatformServices;
using ImprovedPublicTransport.OptionsFramework;
using ImprovedPublicTransport.OptionsFramework.Attibutes;

namespace ImprovedPublicTransport.Settings
{
    [Options("ModsSettings/IPT/ImprovedPublicTransport")]
    public class Settings
    {
        private const string SETTINGS_COMMON = "SETTINGS";
        private const string SETTINGS_UI = "SETTINGS_UI";
        private const string SETTINGS_BUDGET = "SETTINGS_BUDGET";
        private const string SETTINGS_UNBUNCHING = "SETTINGS_UNBUNCHING";
        private const string SETTINGS_LINE_DELETION_TOOL = "SETTINGS_LINE_DELETION_TOOL";
        private const string SETTINGS_EBS_GROUP_BUS = "SETTINGS_EBS_GROUP_BUS";
        private const string SETTINGS_EBS_GROUP_TRAM = "SETTINGS_EBS_GROUP_TRAM";
        private const string SETTINGS_STOPS = "SETTINGS_STOPS";
        private const string SETTINGS_AUTO_LINE = "SETTINGS_AUTO_LINE";

        public enum VehicleSpeedUnits
        {
            [Description("SETTINGS_SPEED_KPH")]
            KPH = 0,
            [Description("SETTINGS_SPEED_MPH")]
            MPH = 1
        }

        public enum BbspLogicModes
        {
            [Description("SETTINGS_BBSP_MODE_DISABLED")]
            Disabled = 0,
            [Description("SETTINGS_BBSP_MODE_ORIGINAL")]
            OriginalLogic = 1,
            // [Description("SETTINGS_BBSP_MODE_UPDATED")]
            // UpdatedLogic = 2
        }

        public enum WalkingSpeedModes
        {
            [Description("SETTINGS_WALKING_SPEED_MODE_VANILLA")]
            Vanilla = 0,
            [Description("SETTINGS_WALKING_SPEED_MODE_REALISTIC")]
            Realistic = 1
        }

        [Description("SETTINGS_SPEED_TOOLTIP")]
        [DropDown("SETTINGS_SPEED", nameof(VehicleSpeedUnits), SETTINGS_COMMON)]
        public int SpeedUnit { get; set; } = (int)VehicleSpeedUnits.MPH;

        [XmlIgnore]
        public string SpeedString => SpeedUnit == (int)VehicleSpeedUnits.KPH ? Localization.Get("SETTINGS_SPEED_KPH") : Localization.Get("SETTINGS_SPEED_MPH");

        [Description("SETTINGS_BBSP_TOOLTIP")]
        [DropDown("SETTINGS_BBSP", nameof(BbspLogicModes), SETTINGS_COMMON)]
        public int BbspLogic { get; set; } = (int)BbspLogicModes.OriginalLogic;

        [Description("SETTINGS_WALKING_SPEED_TOOLTIP")]
        [DropDown("SETTINGS_WALKING_SPEED", nameof(WalkingSpeedModes), SETTINGS_COMMON, nameof(SettingsActions), nameof(SettingsActions.OnRealisticWalkingSpeedChanged))]
        public int WalkingSpeedMode { get; set; } = (int)WalkingSpeedModes.Realistic;

        [Description("SETTINGS_AUTOSHOW_LINE_INFO_TOOLTIP")]
        [Checkbox("SETTINGS_AUTOSHOW_LINE_INFO", SETTINGS_AUTO_LINE)]
        public bool ShowLineInfo { get; set; } = true;

        public enum BudgetControlModes
        {
            [Description("SETTINGS_BUDGET_CONTROL_DISABLED")]
            Disabled = 0,
            [Description("SETTINGS_BUDGET_CONTROL_ENABLED")]
            Enabled = 1
        }

        [Description("SETTINGS_BUDGET_CONTROL_TOOLTIP")]
        [DropDown("SETTINGS_ENABLE_BUDGET_CONTROL", nameof(BudgetControlModes), SETTINGS_COMMON, nameof(SettingsActions), nameof(SettingsActions.OnBudgetModeChanged))]
        public int BudgetControl { get; set; } = (int)BudgetControlModes.Enabled;


        [Description("SETTINGS_VEHICLE_EDITOR_POSITION_TOOLTIP")]
        [DropDown("SETTINGS_VEHICLE_EDITOR_POSITION", nameof(VehicleEditorPositions), SETTINGS_UI)]
        public int VehicleEditorPosition { get; set; } = (int) VehicleEditorPositions.Bottom;

        [Description("SETTINGS_VEHICLE_EDITOR_HIDE_TOOLTIP")]
        [Checkbox("SETTINGS_VEHICLE_EDITOR_HIDE", SETTINGS_UI)]
        public bool HideVehicleEditor { get; set; }


        [AggressionDescription]
        [Slider("SETTINGS_UNBUNCHING_AGGRESSION", 0.0f, 52.0f, 1.0f, SETTINGS_UNBUNCHING)]
        public byte IntervalAggressionFactor { get; set; } = 52; //TODO(): convert into max seconds at stop

        [Description("SETTINGS_VEHICLE_COUNT_TOOLTIP")]
        [Slider("SETTINGS_VEHICLE_COUNT", 0.0f, 100.0f, 1.0f, SETTINGS_UNBUNCHING, nameof(SettingsActions), nameof(SettingsActions.OnDefaultVehicleCountSubmitted))]
        public int DefaultVehicleCount { get; set; } = 0;

        [Description("SETTINGS_SPAWN_TIME_INTERVAL_TOOLTIP")]
        [Slider("SETTINGS_SPAWN_TIME_INTERVAL", 0.0f, 100.0f, 1.0f, SETTINGS_UNBUNCHING)]
        public int SpawnTimeInterval { get; set; } = 10;

        [Description("SETTINGS_UNBUNCHING_RESET_BUTTON_TOOLTIP")]
        // Reset button is in the Unbunching section (same group as the sliders)
        [Button("SETTINGS_RESET", SETTINGS_UNBUNCHING, nameof(SettingsActions), nameof(SettingsActions.OnResetButtonClick))]
        [XmlIgnore]
        public object UnbunchingOptionsResetButton { get; } = null;

        // Express Bus Services settings
        [Description("SETTINGS_EBS_TOOLTIP_UNBUNCHING_MODE")]
        [DropDown("SETTINGS_EBS_DROPDOWN_UNBUNCHING_MODE", nameof(ExpressBusServicesModes), SETTINGS_EBS_GROUP_BUS)]
        public int ExpressBusUnbunchingMode { get; set; } = 0; // 0 = Disabled, 1 = Prudential, 2 = Aggressive

        [Description("SETTINGS_EBS_TOOLTIP_SELFBAL")]
        [Checkbox("SETTINGS_EBS_ENABLE_SELFBAL", SETTINGS_EBS_GROUP_BUS)]
        public bool ExpressBusEnableSelfBalancing { get; set; } = true;

        [Description("SETTINGS_EBS_TOOLTIP_SELFBAL_TARGETMID")]
        [Checkbox("SETTINGS_EBS_ENABLE_SELFBAL_TARGETMID", SETTINGS_EBS_GROUP_BUS)]
        public bool ExpressBusAllowMiddleStopBalancing { get; set; } = true;

        [Description("SETTINGS_EBS_TOOLTIP_MINIBUS")]
        [Checkbox("SETTINGS_EBS_ENABLE_MINIBUS", SETTINGS_EBS_GROUP_BUS)]
        public bool ExpressBusEnableMinibusMode { get; set; } = true;

        [Description("SETTINGS_EBS_TOOLTIP_TRAM_UNBUNCHING")]
        [DropDown("SETTINGS_EBS_DROPDOWN_TRAM_UNBUNCHING_MODE", nameof(ExpressTramServicesModes), SETTINGS_EBS_GROUP_TRAM)]
        public int ExpressTramUnbunchingMode { get; set; } = 0; // 0 = Disabled, 1 = Light Rail, 2 = True Tram

        public enum ExpressBusServicesModes
        {
            [Description("SETTINGS_EBS_MODE_NONE")]
            None = 0,
            [Description("SETTINGS_EBS_MODE_PRUDENTIAL")]
            Prudential = 1,
            [Description("SETTINGS_EBS_MODE_AGGRESSIVE")]
            Aggressive = 2,
        }

        public enum ExpressTramServicesModes
        {
            [Description("SETTINGS_EBS_TRAM_MODE_NONE")]
            Disabled = 0,
            [Description("SETTINGS_EBS_TRAM_MODE_LIGHT_RAIL")]
            LightRail = 1,
            [Description("SETTINGS_EBS_TRAM_MODE_TRAM")]
            TrueTram = 2,
        }

        public bool Unbunching { get; } = true; //hidden

        public int StatisticWeeks { get; set; } = 10; //hidden


        [Description("SETTINGS_LINE_DELETION_TOOL_BUTTON_TOOLTIP")]
        [Button("SETTINGS_DELETE", SETTINGS_LINE_DELETION_TOOL, nameof(SettingsActions), nameof(SettingsActions.OnDeleteLinesClick))]
        [XmlIgnore]
        public object DeleteLinesButton { get; } = null;

        [XmlIgnore]
        [Checkbox("INFO_PUBLICTRANSPORT_BUS", SETTINGS_LINE_DELETION_TOOL)]
        [Description("SETTINGS_DELETE_BUS_TOOLTIP")]
        public bool DeleteBusLines { get; set; }

        [XmlIgnore]
        [Checkbox("SETTINGS_DELETE_SIGHTSEEING_BUS_LABEL", SETTINGS_LINE_DELETION_TOOL)]
        [Description("SETTINGS_DELETE_SIGHTSEEING_BUS_TOOLTIP")]
        public bool DeleteSightseeingBusLines { get; set; }

        [HideIfSnowfallNotOwned]
        [XmlIgnore]
        [Checkbox("INFO_PUBLICTRANSPORT_TRAM", SETTINGS_LINE_DELETION_TOOL)]
        [Description("SETTINGS_DELETE_TRAM_TOOLTIP")]
        public bool DeleteTramLines { get; set; }

        [XmlIgnore]
        [Checkbox("INFO_PUBLICTRANSPORT_TROLLEYBUS", SETTINGS_LINE_DELETION_TOOL)]
        [Description("SETTINGS_DELETE_TROLLEYBUS_TOOLTIP")]
        public bool DeleteTrolleybusLines { get; set; }

        [XmlIgnore]
        [Checkbox("INFO_PUBLICTRANSPORT_TRAIN", SETTINGS_LINE_DELETION_TOOL)]
        [Description("SETTINGS_DELETE_TRAIN_TOOLTIP")]
        public bool DeleteTrainLines { get; set; }

        [XmlIgnore]
        [Checkbox("INFO_PUBLICTRANSPORT_METRO", SETTINGS_LINE_DELETION_TOOL)]
        [Description("SETTINGS_DELETE_METRO_TOOLTIP")]
        public bool DeleteMetroLines { get; set; }

        [HideIfMassTransitNotOwned]
        [XmlIgnore]
        [Checkbox("INFO_PUBLICTRANSPORT_MONORAIL", SETTINGS_LINE_DELETION_TOOL)]
        [Description("SETTINGS_DELETE_MONORAIL_TOOLTIP")]
        public bool DeleteMonorailLines { get; set; }

        [XmlIgnore]
        [Checkbox("SETTINGS_DELETE_FERRY_LABEL", SETTINGS_LINE_DELETION_TOOL)]
        [Description("SETTINGS_DELETE_SHIP_TOOLTIP")]
        public bool DeleteShipLines { get; set; }

        [XmlIgnore]
        [Checkbox("SETTINGS_DELETE_HELICOPTER_LABEL", SETTINGS_LINE_DELETION_TOOL)]
        [Description("SETTINGS_DELETE_HELICOPTER_TOOLTIP")]
        public bool DeleteHelicopterLines { get; set; }

        [XmlIgnore]
        [Checkbox("SETTINGS_DELETE_BLIMP_LABEL", SETTINGS_LINE_DELETION_TOOL)]
        [Description("SETTINGS_DELETE_BLIMP_TOOLTIP")]
        public bool DeleteBlimpLines { get; set; }





        [AttributeUsage(AttributeTargets.All)]
        public class BudgetDescriptionAttribute : DontTranslateDescriptionAttribute
        {
            public BudgetDescriptionAttribute() : 
                base(Localization.Get("SETTINGS_BUDGET_CONTROL_TOOLTIP") + Environment.NewLine + Localization.Get("EXPLANATION_BUDGET_CONTROL"))
            {
                
            }
        }

        [AttributeUsage(AttributeTargets.All)]
        public class AggressionDescriptionAttribute : DontTranslateDescriptionAttribute
        {
            public AggressionDescriptionAttribute() :
                base(Localization.Get("SETTINGS_UNBUNCHING_AGGRESSION_TOOLTIP") + Environment.NewLine + Localization.Get("EXPLANATION_UNBUNCHING"))
            {

            }
        }

        [AttributeUsage(AttributeTargets.All)]
        public class HideIfMassTransitNotOwnedAttribute : HideConditionAttribute
        {
            public override bool IsHidden()
            {
                return !PlatformService.IsDlcInstalled(SteamHelper.kMotionDLCAppID);
            }
        }

        [AttributeUsage(AttributeTargets.All)]
        public class HideIfSnowfallNotOwnedAttribute : HideConditionAttribute
        {
            public override bool IsHidden()
            {
                return !PlatformService.IsDlcInstalled(SteamHelper.kWinterDLCAppID);
            }
        }

        // 'What's new' version tracker for notifications.
        public string WhatsNewLastSeenVersion { get; set; } = "0.0.0";

        // --- Ticket Price Customizer nested settings
        public TicketPriceCustomizerSettings TicketPriceCustomizer { get; set; } = new TicketPriceCustomizerSettings();

        public class TicketPriceCustomizerSettings
        {
            // Day multipliers
            public float TaxiMultiplier { get; set; } = 1.0f;
            public float BusMultiplier { get; set; } = 1.0f;
            public float IntercityBusMultiplier { get; set; } = 1.0f;
            public float MetroMultiplier { get; set; } = 1.0f;
            public float TrainMultiplier { get; set; } = 1.0f;
            public float TramMultiplier { get; set; } = 1.0f;
            public float MonorailMultiplier { get; set; } = 1.0f;
            public float ShipMultiplier { get; set; } = 1.0f;
            public float FerryMultiplier { get; set; } = 1.0f;
            public float PlaneMultiplier { get; set; } = 1.0f;
            public float CableCarMultiplier { get; set; } = 1.0f;
            public float SightseeingBusMultiplier { get; set; } = 1.0f;
            public float TrolleybusMultiplier { get; set; } = 1.0f;
            public float BlimpMultiplier { get; set; } = 1.0f;
            public float HelicopterMultiplier { get; set; } = 1.0f;
            // Night multipliers (After Dark)
            public float TaxiNightMultiplier { get; set; } = 1.0f;
            public float BusNightMultiplier { get; set; } = 1.0f;
            public float IntercityBusNightMultiplier { get; set; } = 1.0f;
            public float MetroNightMultiplier { get; set; } = 1.0f;
            public float TrainNightMultiplier { get; set; } = 1.0f;
            public float TramNightMultiplier { get; set; } = 1.0f;
            public float MonorailNightMultiplier { get; set; } = 1.0f;
            public float ShipNightMultiplier { get; set; } = 1.0f;
            public float FerryNightMultiplier { get; set; } = 1.0f;
            public float PlaneNightMultiplier { get; set; } = 1.0f;
            public float CableCarNightMultiplier { get; set; } = 1.0f;
            public float SightseeingBusNightMultiplier { get; set; } = 1.0f;
            public float TrolleybusNightMultiplier { get; set; } = 1.0f;
            public float BlimpNightMultiplier { get; set; } = 1.0f;
            public float HelicopterNightMultiplier { get; set; } = 1.0f;
        }

        // --- AutoLineColor integration settings
        public enum AutoLineColorStrategy
        {
            [Description("AUTOLINECOLOR_STRATEGY_RANDOM_HUE")]
            RandomHue = 0,
            [Description("AUTOLINECOLOR_STRATEGY_RANDOM_COLOR")]
            RandomColor = 1,
            [Description("AUTOLINECOLOR_STRATEGY_CATEGORISED")]
            CategorisedColor = 2,
            [Description("AUTOLINECOLOR_STRATEGY_NAMED")]
            NamedColors = 3,
        }

        public enum AutoLineColorNamingStrategy
        {
            [Description("AUTOLINECOLOR_NAMING_NONE")]
            None = 0,
            [Description("AUTOLINECOLOR_NAMING_DISTRICTS")]
            Districts = 1,
            [Description("AUTOLINECOLOR_NAMING_LONDON")]
            London = 2,
            [Description("AUTOLINECOLOR_NAMING_ROADS")]
            Roads = 3,
            [Description("AUTOLINECOLOR_NAMING_COLORS")]
            NamedColors = 4,
        }

        [Description("AUTOLINECOLOR_COLOR_STRATEGY_TOOLTIP")]
        [DropDown("AUTOLINECOLOR_COLOR_STRATEGY", nameof(AutoLineColorStrategy), SETTINGS_AUTO_LINE)]
        public int AutoLineColorColorStrategy { get; set; } = (int)AutoLineColorStrategy.RandomColor;

        [Description("AUTOLINECOLOR_NAMING_STRATEGY_TOOLTIP")]
        [DropDown("AUTOLINECOLOR_NAMING_STRATEGY", nameof(AutoLineColorNamingStrategy), SETTINGS_AUTO_LINE)]
        public int AutoLineColorNamingStrategyMode { get; set; } = (int)AutoLineColorNamingStrategy.Districts;

        [Description("AUTOLINECOLOR_MIN_COLOR_DIFF_TOOLTIP")]
        [Slider("AUTOLINECOLOR_MIN_COLOR_DIFF", 1f, 50f, 1f, SETTINGS_AUTO_LINE)]
        public int AutoLineColorMinColorDiffPercentage { get; set; } = 5;

        [Description("AUTOLINECOLOR_MAX_COLOR_PICK_TOOLTIP")]
        [Slider("AUTOLINECOLOR_MAX_COLOR_PICK", 1f, 50f, 1f, SETTINGS_AUTO_LINE)]
        public int AutoLineColorMaxDiffColorPickAttempt { get; set; } = 10;

        // --- StopsAndStations integration settings
        private const string SETTINGS_STOPS_PASSENGERS = "SETTINGS_STOPS_PASSENGERS";

        [Description("SETTINGS_MAX_PASSENGERS_BUS_TOOLTIP")]
        [Slider("SETTINGS_MAX_PASSENGERS_BUS", 10f, 500f, 5f, SETTINGS_STOPS_PASSENGERS)]
        public int MaxWaitingPassengersBus { get; set; } = 50;

        [Description("SETTINGS_MAX_PASSENGERS_TROLLEYBUS_TOOLTIP")]
        [Slider("SETTINGS_MAX_PASSENGERS_TROLLEYBUS", 10f, 500f, 5f, SETTINGS_STOPS_PASSENGERS)]
        public int MaxWaitingPassengersTrolleybus { get; set; } = 50;

        [Description("SETTINGS_MAX_PASSENGERS_EVACUATION_BUS_TOOLTIP")]
        [Slider("SETTINGS_MAX_PASSENGERS_EVACUATION_BUS", 10f, 500f, 5f, SETTINGS_STOPS_PASSENGERS)]
        public int MaxWaitingPassengersEvacuationBus { get; set; } = 100;

        [Description("SETTINGS_MAX_PASSENGERS_TOURIST_BUS_TOOLTIP")]
        [Slider("SETTINGS_MAX_PASSENGERS_TOURIST_BUS", 10f, 500f, 5f, SETTINGS_STOPS_PASSENGERS)]
        public int MaxWaitingPassengersTouristBus { get; set; } = 50;

        [Description("SETTINGS_MAX_PASSENGERS_TRAM_TOOLTIP")]
        [Slider("SETTINGS_MAX_PASSENGERS_TRAM", 10f, 500f, 5f, SETTINGS_STOPS_PASSENGERS)]
        public int MaxWaitingPassengersTram { get; set; } = 80;

        [Description("SETTINGS_MAX_PASSENGERS_METRO_TOOLTIP")]
        [Slider("SETTINGS_MAX_PASSENGERS_METRO", 50f, 2000f, 25f, SETTINGS_STOPS_PASSENGERS)]
        public int MaxWaitingPassengersMetro { get; set; } = 250;

        [Description("SETTINGS_MAX_PASSENGERS_TRAIN_TOOLTIP")]
        [Slider("SETTINGS_MAX_PASSENGERS_TRAIN", 50f, 2000f, 25f, SETTINGS_STOPS_PASSENGERS)]
        public int MaxWaitingPassengersTrain { get; set; } = 250;

        [Description("SETTINGS_MAX_PASSENGERS_MONORAIL_TOOLTIP")]
        [Slider("SETTINGS_MAX_PASSENGERS_MONORAIL", 50f, 2000f, 25f, SETTINGS_STOPS_PASSENGERS)]
        public int MaxWaitingPassengersMonorail { get; set; } = 250;

        [Description("SETTINGS_MAX_PASSENGERS_SHIP_TOOLTIP")]
        [Slider("SETTINGS_MAX_PASSENGERS_SHIP", 50f, 1000f, 10f, SETTINGS_STOPS_PASSENGERS)]
        public int MaxWaitingPassengersShip { get; set; } = 150;

        [Description("SETTINGS_MAX_PASSENGERS_AIRPLANE_TOOLTIP")]
        [Slider("SETTINGS_MAX_PASSENGERS_AIRPLANE", 50f, 1000f, 10f, SETTINGS_STOPS_PASSENGERS)]
        public int MaxWaitingPassengersAirplane { get; set; } = 250;

        [Description("SETTINGS_MAX_PASSENGERS_CABLE_CAR_TOOLTIP")]
        [Slider("SETTINGS_MAX_PASSENGERS_CABLE_CAR", 10f, 500f, 5f, SETTINGS_STOPS_PASSENGERS)]
        public int MaxWaitingPassengersCableCar { get; set; } = 40;

        [Description("SETTINGS_MAX_PASSENGERS_HOT_AIR_BALLOON_TOOLTIP")]
        [Slider("SETTINGS_MAX_PASSENGERS_HOT_AIR_BALLOON", 10f, 500f, 5f, SETTINGS_STOPS_PASSENGERS)]
        public int MaxWaitingPassengersHotAirBalloon { get; set; } = 40;

        [Description("SETTINGS_MAX_PASSENGERS_HELICOPTER_TOOLTIP")]
        [Slider("SETTINGS_MAX_PASSENGERS_HELICOPTER", 10f, 500f, 5f, SETTINGS_STOPS_PASSENGERS)]
        public int MaxWaitingPassengersHelicopter { get; set; } = 40;

    }

}