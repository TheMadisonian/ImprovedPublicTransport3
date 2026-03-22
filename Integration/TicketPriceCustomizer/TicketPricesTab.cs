using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.UI;
using ImprovedPublicTransport.OptionsFramework;
using ImprovedPublicTransport.UI.AlgernonCommons;
using ImprovedPublicTransport.Util;
using UnityEngine;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace ImprovedPublicTransport.Integration.TicketPriceCustomizer
{
    /// <summary>
    /// Adds a "Ticket Prices" tab to the Economy panel with budget-style sliders
    /// for each public transport type (0%–250%), with day/night support matching
    /// the game's budget tab visual style.</summary>
    public static class TicketPricesTab
    {
        private static bool s_initialized;
        private static UIScrollablePanel s_ticketPricesContainer;
        private static readonly List<TicketPriceSliderRow> s_sliderRows = new List<TicketPriceSliderRow>();
        private static readonly Dictionary<string, UITextureAtlas> s_customIconAtlases = new Dictionary<string, UITextureAtlas>();

        // Transport types with their sprite names and display order
        private static readonly TransportTypeInfo[] s_transportTypes = new TransportTypeInfo[]
        {
            new TransportTypeInfo("Bus",            "SubBarPublicTransportBus",          ItemClass.SubService.PublicTransportBus),
            new TransportTypeInfo("Trolleybus",     "SubBarPublicTransportTrolleybus",   ItemClass.SubService.PublicTransportTrolleybus),
            new TransportTypeInfo("Tram",           "SubBarPublicTransportTram",         ItemClass.SubService.PublicTransportTram),
            new TransportTypeInfo("Metro",          "SubBarPublicTransportMetro",        ItemClass.SubService.PublicTransportMetro),
            new TransportTypeInfo("Train",          "SubBarPublicTransportTrain",        ItemClass.SubService.PublicTransportTrain),
            new TransportTypeInfo("Monorail",       "SubBarPublicTransportMonorail",     ItemClass.SubService.PublicTransportMonorail),
            new TransportTypeInfo("CableCar",       "SubBarPublicTransportCableCar",     ItemClass.SubService.PublicTransportCableCar),
            new TransportTypeInfo("Ship",           "SubBarPublicTransportShip",         ItemClass.SubService.PublicTransportShip),
            new TransportTypeInfo("Ferry",          "SubBarPublicTransportFerry",         ItemClass.SubService.PublicTransportShip), // Custom icon
            new TransportTypeInfo("Plane",          "SubBarPublicTransportPlane",        ItemClass.SubService.PublicTransportPlane),
            new TransportTypeInfo("Blimp",          "SubBarPublicTransportBlimp",        ItemClass.SubService.PublicTransportPlane), // Custom icon
            new TransportTypeInfo("Helicopter",     "SubBarPublicTransportHelicopter",   ItemClass.SubService.PublicTransportPlane), // Custom icon
            new TransportTypeInfo("Taxi",           "SubBarPublicTransportTaxi",         ItemClass.SubService.PublicTransportTaxi),
            new TransportTypeInfo("SightseeingBus", "SubBarPublicTransportTours",        ItemClass.SubService.PublicTransportTours),
            new TransportTypeInfo("IntercityBus",   "SubBarPublicTransportIntercity",    ItemClass.SubService.PublicTransportBus), // Custom icon
        };

        /// <summary>
        /// Called from a Harmony postfix on EconomyPanel.Awake to inject the Ticket Prices tab.
        /// </summary>
        public static void InjectTab(EconomyPanel economyPanel)
        {
            try
            {
                if (s_initialized) return;

                // Load custom icon atlases
                LoadCustomIconAtlases();

                var tabStrip = economyPanel.Find<UITabstrip>("EconomyTabstrip");
                var tabContainer = economyPanel.Find<UITabContainer>("EconomyContainer");
                if (tabStrip == null || tabContainer == null)
                {
                    Utils.LogError("TicketPricesTab: Could not find EconomyTabstrip or EconomyContainer");
                    return;
                }

                // Create the tab button styled like the existing ones
                var tabButton = tabStrip.AddTab("TicketPrices");
                tabButton.text = Localization.Get("ECONOMY_TAB_TICKET_PRICES");
                // Style it to match the other economy tab buttons
                StyleTabButton(tabButton, tabStrip);

                // The UITabstrip.AddTab automatically creates a page in the tabContainer.
                // Get the last page (our new one)
                var page = tabContainer.components[tabContainer.components.Count - 1] as UIPanel;
                if (page == null)
                {
                    Utils.LogError("TicketPricesTab: Could not find newly created tab page");
                    return;
                }

                page.autoLayout = false;
                page.size = tabContainer.size;
                page.isVisible = false; // Hidden until this tab is selected; UITabstrip shows it on click

                // Build the scrollable content
                BuildTicketPricesContent(page);

                s_initialized = true;
                Utils.Log("TicketPricesTab: Successfully added Ticket Prices tab to Economy panel");
            }
            catch (Exception ex)
            {
                Utils.LogError($"TicketPricesTab: Failed to inject tab: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Cleanup when level unloads.
        /// </summary>
        public static void Cleanup()
        {
            s_initialized = false;
            s_ticketPricesContainer = null;
            s_sliderRows.Clear();
            s_customIconAtlases.Clear();
        }

        /// <summary>
        /// Loads custom icons (Ferry, Blimp, Heli) into per-sprite atlases.
        /// Uses PluginManager to get the correct mod folder path (Assembly.Location/CodeBase
        /// return the game directory in CS1's Mono runtime, not the mod folder).
        /// </summary>
        private static void LoadCustomIconAtlases()
        {
            if (s_customIconAtlases.Count > 0) return;
            try
            {
                // PluginManager is the correct CS1 way to get the mod's folder path
                var modDir = TranslationFramework.Util.AssemblyPath(typeof(ImprovedPublicTransportMod));
                var iconsDir = Path.Combine(modDir, "Resources");

                if (!Directory.Exists(iconsDir))
                {
                    Utils.LogError($"TicketPricesTab: Resources directory not found at {iconsDir}");
                    return;
                }

                var iconFiles = new[]
                {
                    new { SpriteName = "SubBarPublicTransportFerry", FileName = "SubBarPublicTransportFerry.png" },
                    new { SpriteName = "SubBarPublicTransportBlimp", FileName = "SubBarPublicTransportBlimp.png" },
                    new { SpriteName = "SubBarPublicTransportHelicopter",  FileName = "SubBarPublicTransportHelicopter.png" },
                    new { SpriteName = "SubBarPublicTransportIntercity", FileName = "SubBarPublicTransportIntercity.png" },
                };

                foreach (var info in iconFiles)
                {
                    var pngPath = Path.Combine(iconsDir, info.FileName);
                    if (!File.Exists(pngPath))
                    {
                        Utils.LogError($"TicketPricesTab: Icon file not found: {pngPath}");
                        continue;
                    }

                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!texture.LoadImage(File.ReadAllBytes(pngPath)))
                    {
                        Utils.LogError($"TicketPricesTab: LoadImage failed for {pngPath}");
                        continue;
                    }
                    texture.name = info.SpriteName;

                    // Create a single-sprite atlas: clone the default atlas material (same shader/blend),
                    // set its texture to our PNG, add the sprite with full-UV region, rebuild the index.
                    var atlas = ScriptableObject.CreateInstance<UITextureAtlas>();
                    atlas.name = info.SpriteName;
                    atlas.material = UnityEngine.Object.Instantiate(UIView.GetAView().defaultAtlas.material);
                    atlas.material.mainTexture = texture;
                    atlas.AddSprite(new UITextureAtlas.SpriteInfo
                    {
                        name    = info.SpriteName,
                        texture = texture,
                        region  = new Rect(0f, 0f, 1f, 1f),
                    });
                    atlas.RebuildIndexes();

                    s_customIconAtlases[info.SpriteName] = atlas;
                    Utils.Log($"TicketPricesTab: Loaded icon {info.SpriteName} ({texture.width}x{texture.height})");
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"TicketPricesTab: Failed loading custom icon atlases: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void StyleTabButton(UIButton tabButton, UITabstrip tabStrip)
        {
            // Copy style from an existing tab button
            var budgetBtn = tabStrip.Find<UIButton>("Budget");
            if (budgetBtn != null)
            {
                tabButton.normalBgSprite = budgetBtn.normalBgSprite;
                tabButton.focusedBgSprite = budgetBtn.focusedBgSprite;
                tabButton.hoveredBgSprite = budgetBtn.hoveredBgSprite;
                tabButton.pressedBgSprite = budgetBtn.pressedBgSprite;
                tabButton.disabledBgSprite = budgetBtn.disabledBgSprite;
                tabButton.textColor = budgetBtn.textColor;
                tabButton.hoveredTextColor = budgetBtn.hoveredTextColor;
                tabButton.pressedTextColor = budgetBtn.pressedTextColor;
                tabButton.focusedTextColor = budgetBtn.focusedTextColor;
                tabButton.disabledTextColor = budgetBtn.disabledTextColor;
                tabButton.textScale = budgetBtn.textScale;
                tabButton.textPadding = budgetBtn.textPadding;
                tabButton.autoSize = budgetBtn.autoSize;
                tabButton.size = budgetBtn.size;
            }
        }

        private static void BuildTicketPricesContent(UIPanel page)
        {
            page.autoLayout = false;
            // Ensure page has a valid size (if not set, defer initialization)
            if (page.width <= 0 || page.height <= 0)
            {
                Utils.LogError("TicketPricesTab: Page has invalid size at initialization");
                return;
            }

            // Check if After Dark is installed (for day/night sliders)
            bool hasAfterDark = SteamHelper.IsDLCOwned(SteamHelper.DLC.AfterDarkDLC);

            // Load current settings
            var settings = OptionsWrapper<Settings.Settings>.Options;
            if (settings.TicketPriceCustomizer == null)
                settings.TicketPriceCustomizer = new Settings.Settings.TicketPriceCustomizerSettings();

            // Create main container with padding matching budget panel
            const float SIDE_PAD = 45f;        // Outer left/right padding
            const float COL_GAP = 55f;         // Gap between columns
            const float COL_PAD = 15f;         // Internal padding within each column
            
            var mainContainer = page.AddUIComponent<UIPanel>();
            mainContainer.autoLayout = false;
            mainContainer.relativePosition = new Vector3(SIDE_PAD, 10f);
            mainContainer.size = new Vector2(page.width - SIDE_PAD * 2f, page.height - 60f);
            // Keep sizing in sync if the economy panel resizes
            page.eventSizeChanged += (c, s) =>
            {
                if (mainContainer != null)
                {
                    mainContainer.size = new Vector2(page.width - SIDE_PAD * 2f, page.height - 60f);
                }
            };

            // Separate transport types into land (left) and air/water (right)
            var landTransport = new List<TransportTypeInfo>();
            var airWaterTransport = new List<TransportTypeInfo>();

            foreach (var transportType in s_transportTypes)
            {
                if (!HasRequiredDlc(transportType)) continue;
                if (!IsTransportLoaded(transportType)) continue;

                if (transportType.Name == "Ship" || transportType.Name == "Ferry" || 
                    transportType.Name == "Plane" || transportType.Name == "Blimp" || transportType.Name == "Helicopter")
                {
                    airWaterTransport.Add(transportType);
                }
                else
                {
                    landTransport.Add(transportType);
                }
            }

            float columnWidth = (mainContainer.width - COL_GAP) / 2f; // Two equal-width columns
            float columnHeight = mainContainer.height;

            // Left column (Land transport)
            var leftColumn = mainContainer.AddUIComponent<UIScrollablePanel>();
            leftColumn.autoLayout = true;
            leftColumn.autoLayoutDirection = LayoutDirection.Vertical;
            leftColumn.autoLayoutPadding = new RectOffset((int)COL_PAD, (int)COL_PAD, 0, 0);
            leftColumn.clipChildren = true;
            leftColumn.relativePosition = Vector3.zero;
            leftColumn.size = new Vector2(columnWidth, columnHeight);
            leftColumn.scrollWheelDirection = UIOrientation.Vertical;
            leftColumn.builtinKeyNavigation = true;

            // Right column (Air/Water transport) 
            var rightColumn = mainContainer.AddUIComponent<UIScrollablePanel>();
            rightColumn.autoLayout = true;
            rightColumn.autoLayoutDirection = LayoutDirection.Vertical;
            rightColumn.autoLayoutPadding = new RectOffset((int)COL_PAD, (int)COL_PAD, 0, 0);
            rightColumn.clipChildren = true;
            rightColumn.relativePosition = new Vector3(columnWidth + COL_GAP, 0);
            rightColumn.size = new Vector2(columnWidth, columnHeight);
            rightColumn.scrollWheelDirection = UIOrientation.Vertical;
            rightColumn.builtinKeyNavigation = true;

            int landIndex = 0;
            foreach (var transportType in landTransport)
            {
                var row = CreateSliderRow(leftColumn, transportType, hasAfterDark, landIndex);
                if (row != null)
                {
                    s_sliderRows.Add(row);
                    landIndex++;
                }
            }

            int airIndex = 0;
            foreach (var transportType in airWaterTransport)
            {
                var row = CreateSliderRow(rightColumn, transportType, hasAfterDark, airIndex);
                if (row != null)
                {
                    s_sliderRows.Add(row);
                    airIndex++;
                }
            }

            s_ticketPricesContainer = leftColumn; // Store reference to main container

            // Add reset button at the bottom center
            var buttonContainer = page.AddUIComponent<UIPanel>();
            buttonContainer.autoLayout = false;
            buttonContainer.relativePosition = new Vector3(SIDE_PAD, page.height - 45f);
            buttonContainer.size = new Vector2(page.width - SIDE_PAD * 2f, 40f);
            page.eventSizeChanged += (c, s) =>
            {
                buttonContainer.relativePosition = new Vector3(SIDE_PAD, page.height - 45f);
                buttonContainer.size = new Vector2(page.width - SIDE_PAD * 2f, 40f);
            };

            var resetBtn = buttonContainer.AddUIComponent<UIButton>();
            resetBtn.text = Localization.Get("SETTINGS_TICKETS_RESET");
            resetBtn.tooltip = Localization.Get("SETTINGS_TICKETS_RESET_TITLE");
            resetBtn.size = new Vector2(140f, 30f);
            resetBtn.relativePosition = new Vector3((buttonContainer.width - 140f) / 2f, 5f);
            resetBtn.normalBgSprite = "ButtonMenu";
            resetBtn.hoveredBgSprite = "ButtonMenuHovered";
            resetBtn.pressedBgSprite = "ButtonMenuPressed";
            resetBtn.textScale = 0.8f;
            resetBtn.eventClick += OnResetClick;
        }

        private static bool IsTransportLoaded(TransportTypeInfo info)
        {
            try
            {
                // Ferry/blimp/helicopter should always show in the ticket prices tab if this integration is active,
                // even when no vehicle is currently instantiated, so users can configure them.
                if (info.Name == "Ferry" || info.Name == "Blimp" || info.Name == "Helicopter")
                {
                    return true;
                }

                // Check if the transport info prefab exists for this type
                string transportInfoName = GetTransportInfoName(info.Name);
                if (transportInfoName == null) return true; // Unknown types always shown
                var prefab = PrefabCollection<TransportInfo>.FindLoaded(transportInfoName);
                return prefab != null;
            }
            catch
            {
                return true; // Default to showing if we can't check
            }
        }

        /// <summary>
        /// Returns true if the DLC required for this transport type is owned.
        /// Types with no DLC requirement always return true.
        /// </summary>
        private static bool HasRequiredDlc(TransportTypeInfo info)
        {
            switch (info.Name)
            {
                case "Tram":
                    return SteamHelper.IsDLCOwned(SteamHelper.DLC.SnowFallDLC);
                case "Taxi":
                    return SteamHelper.IsDLCOwned(SteamHelper.DLC.AfterDarkDLC);
                case "Ferry":
                case "Blimp":
                case "Monorail":
                case "CableCar":
                    return SteamHelper.IsDLCOwned(SteamHelper.DLC.InMotionDLC);
                case "Trolleybus":
                case "IntercityBus":
                case "Helicopter":
                    return SteamHelper.IsDLCOwned(SteamHelper.DLC.UrbanDLC);  // Sunset Harbor = UrbanDLC
                case "SightseeingBus":
                    return SteamHelper.IsDLCOwned(SteamHelper.DLC.ParksDLC);
                default:
                    return true; // Bus, Metro, Train, Ship, Plane — base game
            }
        }

        private static TicketPriceSliderRow CreateSliderRow(UIScrollablePanel container, TransportTypeInfo transportType, bool hasAfterDark, int index)
        {
            try
            {
                return CreateSliderRowFromTemplate(container, transportType, hasAfterDark, index);
            }
            catch (Exception ex)
            {
                Utils.LogError($"TicketPricesTab: BudgetItem template row failed for {transportType.Name}: {ex.Message}. Falling back to custom row.");
                return CreateSliderRowFallback(container, transportType, hasAfterDark, index);
            }
        }

        // Uses the game's own BudgetItem prefab — visually identical to the Budget panel.
        // Reflects into BudgetItem for slider/label refs, then destroys the MonoBehaviour so
        // EconomyPanel cannot call Init() and override our range/values.
        private static TicketPriceSliderRow CreateSliderRowFromTemplate(
            UIScrollablePanel container, TransportTypeInfo transportType, bool hasAfterDark, int index)
        {
            var templateGO   = UITemplateManager.GetAsGameObject("BudgetItem");
            var rowComponent = container.AttachUIComponent(templateGO);
            
            // Make rows narrower than container to prevent cutoff (leave 30px margin)
            rowComponent.width = container.width - 30f;

            // Reflect into BudgetItem to grab the serialized UI references
            var budgetItem   = ((Component)rowComponent).GetComponent<BudgetItem>();
            var biType       = typeof(BudgetItem);
            var flags        = BindingFlags.Instance | BindingFlags.NonPublic;
            var daySlider    = (UISlider)biType.GetField("m_DaySlider",           flags).GetValue(budgetItem);
            var nightSlider  = (UISlider)biType.GetField("m_NightSlidermalan",    flags).GetValue(budgetItem);
            var dayLabel     = (UILabel) biType.GetField("m_DayPercentageLabel",  flags).GetValue(budgetItem);
            var nightLabel   = (UILabel) biType.GetField("m_NightPercentageLabel",flags).GetValue(budgetItem);
            var totalLabel   = (UILabel) biType.GetField("m_TotalLabel",          flags).GetValue(budgetItem);

            // Disable + destroy BudgetItem so EconomyPanel can never call Init() on it
            ((Behaviour)budgetItem).enabled = false;
            UnityEngine.Object.Destroy(budgetItem);

            // Alternating row background — use the same colors as the Budget tab itself
            var backDivider = rowComponent.Find<UISlicedSprite>("BackDivider");
            if (backDivider != null)
            {
                var ep = ToolsModifierControl.economyPanel;
                backDivider.color = (index % 2 == 0) ? ep.m_BackDividerColor : ep.m_BackDividerAltColor;
            }

            // Transport icon
            var icon = rowComponent.Find<UISprite>("Icon");
            if (icon != null)
            {
                // Use custom atlas if available (for Ferry/Blimp/Heli), otherwise use default atlas
                if (s_customIconAtlases.ContainsKey(transportType.SpriteName))
                {
                    icon.atlas = s_customIconAtlases[transportType.SpriteName];
                }
                else
                {
                    icon.atlas = UIView.GetAView().defaultAtlas;
                }
                icon.spriteName = transportType.SpriteName;
                icon.color = Color.white;
            }
            else
            {
                Utils.LogError($"TicketPricesTab: Could not find Icon sprite in row for {transportType.Name}");
            }

            // Day slider — our range / initial value
            // Note: BudgetItem template already has a static "%" label next to the value;
            // set text to the number only to avoid showing "100%%".
            float dayPercent             = GetMultiplier(transportType.Name, false) * 100f;
            daySlider.minValue           = 0f;
            daySlider.maxValue           = 250f;
            daySlider.stepSize           = 5f;
            daySlider.scrollWheelAmount  = 5f;
            daySlider.value              = dayPercent;
            dayLabel.text                = Mathf.RoundToInt(dayPercent).ToString();
            daySlider.tooltip            = GetTransportTooltip(transportType.Name);

            // Night slider
            if (!hasAfterDark)
            {
                if (nightSlider != null) nightSlider.isVisible = false;
                if (nightLabel  != null) nightLabel.isVisible  = false;
            }
            else
            {
                float nightPercent           = GetMultiplier(transportType.Name, true) * 100f;
                nightSlider.minValue         = 0f;
                nightSlider.maxValue         = 250f;
                nightSlider.stepSize         = 5f;
                nightSlider.scrollWheelAmount = 5f;
                nightSlider.value            = nightPercent;
                nightLabel.text              = Mathf.RoundToInt(nightPercent).ToString();
                nightSlider.tooltip          = GetTransportTooltip(transportType.Name);
            }

            // Income total
            UpdateTotalLabel(transportType.Name, totalLabel);

            var row = new TicketPriceSliderRow
            {
                TransportType = transportType,
                DaySlider     = daySlider,
                DayLabel      = dayLabel,
                NightSlider   = hasAfterDark ? nightSlider : null,
                NightLabel    = hasAfterDark ? nightLabel  : null,
                TotalLabel    = totalLabel,
                HasAfterDark  = hasAfterDark
            };

            daySlider.eventValueChanged += (comp, value) =>
            {
                dayLabel.text = Mathf.RoundToInt(value).ToString();
                UpdateTotalLabel(transportType.Name, totalLabel);
                ApplyMultiplier(row);
            };

            if (hasAfterDark && nightSlider != null)
            {
                nightSlider.eventValueChanged += (comp, value) =>
                {
                    if (nightLabel != null) nightLabel.text = Mathf.RoundToInt(value).ToString();
                    UpdateTotalLabel(transportType.Name, totalLabel);
                    ApplyMultiplier(row);
                };
            }

            return row;
        }

        // Fallback custom row (used if BudgetItem template is unavailable).
        // Custom row layout matching the Budget panel style:
        // [Icon] | [Day slider ────────] 100% | [₡income]
        //        | [Night slider ──────] 100% |
        private static TicketPriceSliderRow CreateSliderRowFallback(UIScrollablePanel container, TransportTypeInfo transportType, bool hasAfterDark, int index)
        {
            const float ICON_W   = 26f;
            const float PCT_W    = 38f;   // "250%" label width
            const float TOTAL_W  = 82f;   // income display
            const float PAD      = 3f;
            const float SLD_H    = 18f;   // height of one slider track (matches BudgetItem)
            const float SLD_GAP  = 3f;    // gap between day and night sliders

            float rowH = hasAfterDark ? (SLD_H * 2f + SLD_GAP + PAD * 2f) : (SLD_H + PAD * 2f);
            rowH = Mathf.Max(rowH, 28f);

            var rowPanel = container.AddUIComponent<UIPanel>();
            rowPanel.autoLayout  = false;
            rowPanel.width       = container.width;
            rowPanel.height      = rowH;
            rowPanel.clipChildren = true;

            // Alternating row background
            var bg = rowPanel.AddUIComponent<UISlicedSprite>();
            bg.spriteName        = "GenericPanelWhite";
            bg.relativePosition  = Vector2.zero;
            bg.size              = rowPanel.size;
            bg.color = (index % 2 == 0)
                ? new Color32((byte)56, (byte)61, (byte)75, (byte)255)
                : new Color32((byte)49, (byte)52, (byte)64, (byte)255);

            // Transport icon – centred vertically (use custom atlas if available)
            var icon = rowPanel.AddUIComponent<UISprite>();
            if (s_customIconAtlases.ContainsKey(transportType.SpriteName))
            {
                icon.atlas = s_customIconAtlases[transportType.SpriteName];
            }
            else
            {
                icon.atlas = UIView.GetAView().defaultAtlas;
            }
            icon.spriteName = transportType.SpriteName;
            
            icon.size             = new Vector2(ICON_W, ICON_W);
            icon.relativePosition = new Vector3(PAD, (rowH - ICON_W) / 2f);

            // Slider area runs from icon to the labels+total on the right
            float sliderAreaX = PAD + ICON_W + PAD;
            float sliderAreaW = rowPanel.width - sliderAreaX - PAD - PCT_W - PAD - TOTAL_W - PAD;
            sliderAreaW = Mathf.Max(50f, sliderAreaW);

            float currentPercent = GetMultiplier(transportType.Name, false) * 100f;

            float daySliderY   = PAD;
            float nightSliderY = PAD + SLD_H + SLD_GAP;

            // ── Day slider ──────────────────────────────────────────────
            var daySlider = CreateTicketSlider(rowPanel, sliderAreaX, daySliderY, sliderAreaW);
            daySlider.value = currentPercent;
            daySlider.tooltip = GetTransportTooltip(transportType.Name);

            float pctX = sliderAreaX + sliderAreaW + PAD;
            var dayLabel = CreatePercentLabel(rowPanel, pctX, daySliderY, PCT_W);
            dayLabel.text = Mathf.RoundToInt(currentPercent) + "%";

            // ── Night slider (After Dark only) ──────────────────────────
            UISlider nightSlider = null;
            UILabel  nightLabel  = null;
            if (hasAfterDark)
            {
                float nightPercent = GetMultiplier(transportType.Name, true) * 100f;
                nightSlider = CreateTicketSlider(rowPanel, sliderAreaX, nightSliderY, sliderAreaW);
                nightSlider.value = nightPercent;
                nightSlider.tooltip = GetTransportTooltip(transportType.Name);

                nightLabel = CreatePercentLabel(rowPanel, pctX, nightSliderY, PCT_W);
                nightLabel.text = Mathf.RoundToInt(nightPercent) + "%";
            }

            // ── Income total box ────────────────────────────────────────
            float totalX = rowPanel.width - TOTAL_W - PAD;
            var totalBg = rowPanel.AddUIComponent<UISlicedSprite>();
            totalBg.spriteName        = "GenericPanelWhite";
            totalBg.color             = new Color32((byte)25, (byte)28, (byte)38, (byte)230);
            totalBg.size              = new Vector2(TOTAL_W, rowH - 4f);
            totalBg.relativePosition  = new Vector3(totalX, 2f);

            var totalLabel = rowPanel.AddUIComponent<UILabel>();
            totalLabel.name              = "TotalLabel";
            totalLabel.textAlignment     = UIHorizontalAlignment.Center;
            totalLabel.textScale         = 0.7f;
            totalLabel.textColor         = new Color32((byte)206, (byte)248, (byte)0, (byte)255);
            totalLabel.size              = new Vector2(TOTAL_W, rowH - 4f);
            totalLabel.relativePosition  = new Vector3(totalX, 2f);
            totalLabel.verticalAlignment = UIVerticalAlignment.Middle;
            UpdateTotalLabel(transportType.Name, totalLabel);

            // ── Wire up ─────────────────────────────────────────────────
            var row = new TicketPriceSliderRow
            {
                TransportType = transportType,
                DaySlider     = daySlider,
                DayLabel      = dayLabel,
                NightSlider   = nightSlider,
                NightLabel    = nightLabel,
                TotalLabel    = totalLabel,
                HasAfterDark  = hasAfterDark
            };

            daySlider.eventValueChanged += (comp, value) =>
            {
                dayLabel.text = Mathf.RoundToInt(value) + "%";
                UpdateTotalLabel(transportType.Name, totalLabel);
                ApplyMultiplier(row);
            };

            if (nightSlider != null)
            {
                nightSlider.eventValueChanged += (comp, value) =>
                {
                    if (nightLabel != null) nightLabel.text = Mathf.RoundToInt(value) + "%";
                    UpdateTotalLabel(transportType.Name, totalLabel);
                    ApplyMultiplier(row);
                };
            }

            return row;
        }

        private static UISlider CreateTicketSlider(UIPanel parent, float x, float y, float width)
        {
            // Wrapper panel for positioning within the row
            var sliderPanel = parent.AddUIComponent<UIPanel>();
            sliderPanel.autoLayout = false;
            sliderPanel.relativePosition = new Vector3(x, y);
            sliderPanel.size = new Vector2(width, 18f);

            var slider = sliderPanel.AddUIComponent<UISlider>();
            slider.relativePosition = Vector2.zero;
            slider.size = new Vector2(width, 18f);
            slider.minValue = 0f;
            slider.maxValue = 250f;
            slider.stepSize = 5f;
            slider.scrollWheelAmount = 5f;
            slider.orientation = UIOrientation.Horizontal;

            // Track — 9px tall, offset 4px down to centre in 18px slider (matches BudgetItem)
            var track = slider.AddUIComponent<UISlicedSprite>();
            track.atlas = UITextures.InGameAtlas;
            track.spriteName = "BudgetSlider";
            track.relativePosition = new Vector2(0f, 4f);
            track.size = new Vector2(width, 9f);

            // Thumb — UISlicedSprite with Ingame atlas so the orange BudgetItem thumb renders
            var thumb = slider.AddUIComponent<UISlicedSprite>();
            thumb.atlas = UITextures.InGameAtlas;
            thumb.spriteName = "SliderBudget";
            thumb.size = new Vector2(16f, 16f);

            slider.thumbObject = thumb;

            return slider;
        }

        private static UILabel CreatePercentLabel(UIPanel parent, float x, float y, float width)
        {
            var label = parent.AddUIComponent<UILabel>();
            label.relativePosition = new Vector3(x, y);
            label.size = new Vector2(width, 18f);
            label.textAlignment = UIHorizontalAlignment.Right;
            label.textScale = 0.75f;
            label.textColor = new Color32((byte)206, (byte)248, (byte)0, (byte)255); // Match budget green color
            return label;
        }

        private static void ApplyMultiplier(TicketPriceSliderRow row)
        {
            try
            {
                float dayMultiplier   = row.DaySlider.value / 100f;
                float nightMultiplier = (row.HasAfterDark && row.NightSlider != null)
                    ? row.NightSlider.value / 100f
                    : dayMultiplier;

                SetMultiplier(row.TransportType.Name, false, dayMultiplier);
                SetMultiplier(row.TransportType.Name, true,  nightMultiplier);

                // Apply whichever multiplier is active right now
                bool isNight = Singleton<SimulationManager>.instance.m_isNightTime;
                ApplyPriceForType(row.TransportType.Name, isNight ? nightMultiplier : dayMultiplier);

                // Save settings
                OptionsWrapper<Settings.Settings>.SaveOptions();
            }
            catch (Exception ex)
            {
                Utils.LogError($"TicketPricesTab: Error applying multiplier for {row.TransportType.Name}: {ex.Message}");
            }
        }

        private static void UpdateTotalLabel(string transportName, UILabel totalLabel)
        {
            try
            {
                long totalIncome = CalculateTotalIncomeForTransport(transportName);
                // m_ticketPrice is in centile units (100 = ₡1.00), so divide by 100
                float currency = totalIncome / 100f;

                // Use the game's fictitious currency symbol ₡ with thousand separators
                string text = "\u20a1" + currency.ToString("N2");

                totalLabel.text      = text;
                totalLabel.textColor = new Color32((byte)206, (byte)248, (byte)0, (byte)255);
            }
            catch (Exception ex)
            {
                totalLabel.text = "-";
                Utils.LogWarning($"TicketPricesTab: Failed to update total for {transportName}: {ex.Message}");
            }
        }

        private static long CalculateTotalIncomeForTransport(string transportName)
        {
            try
            {
                TransportInfo targetInfo = GetTransportInfo(transportName);
                if (targetInfo == null)
                    return 0;

                long totalIncome = 0;
                var vehicleManager = Singleton<VehicleManager>.instance;

                // Iterate through all transport lines
                for (ushort lineId = 0; lineId < TransportManager.instance.m_lines.m_size; lineId++)
                {
                    var line = TransportManager.instance.m_lines.m_buffer[lineId];
                    
                    // Skip if line isn't active or doesn't match this transport type
                    if ((line.m_flags & TransportLine.Flags.Created) == TransportLine.Flags.None || line.Info != targetInfo)
                        continue;

                    // Iterate through vehicles on this line and sum passengers
                    for (ushort vehicleId = line.m_vehicles; vehicleId != 0; vehicleId = vehicleManager.m_vehicles.m_buffer[vehicleId].m_nextLineVehicle)
                    {
                        if (vehicleId >= vehicleManager.m_vehicles.m_size)
                            break;

                        var vehicle = vehicleManager.m_vehicles.m_buffer[vehicleId];
                        
                        // Get passenger count from lead vehicle
                        ushort totalPassengers = vehicle.m_transferSize;
                        
                        // Add passengers from any trailing vehicles (for trains, etc.)
                        var trailingId = vehicle.m_trailingVehicle;
                        while (trailingId != 0 && trailingId < vehicleManager.m_vehicles.m_size)
                        {
                            var trailingVehicle = vehicleManager.m_vehicles.m_buffer[trailingId];
                            totalPassengers += trailingVehicle.m_transferSize;
                            trailingId = trailingVehicle.m_trailingVehicle;
                        }
                        
                        totalIncome += (long)totalPassengers * line.m_ticketPrice;
                    }
                }

                return totalIncome;
            }
            catch (Exception ex)
            {
                Utils.LogWarning($"TicketPricesTab: Error calculating income for {transportName}: {ex.Message}");
                return 0;
            }
        }

        private static TransportInfo GetTransportInfo(string transportName)
        {
            try
            {
                string infoPrefabName = GetTransportInfoName(transportName);
                if (infoPrefabName == null)
                    return null;
                return PrefabCollection<TransportInfo>.FindLoaded(infoPrefabName);
            }
            catch
            {
                return null;
            }
        }

        private static void OnResetClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            try
            {
                var defaults = new Settings.Settings.TicketPriceCustomizerSettings();
                var current = OptionsWrapper<Settings.Settings>.Options.TicketPriceCustomizer;
                if (current == null)
                    current = OptionsWrapper<Settings.Settings>.Options.TicketPriceCustomizer = new Settings.Settings.TicketPriceCustomizerSettings();

                // Reset all properties to defaults
                foreach (var prop in typeof(Settings.Settings.TicketPriceCustomizerSettings).GetProperties())
                {
                    if (prop.CanWrite && prop.PropertyType == typeof(float))
                    {
                        prop.SetValue(current, prop.GetValue(defaults, null), null);
                    }
                }

                OptionsWrapper<Settings.Settings>.SaveOptions();
                PriceCustomization.SetPrices(current);

                // Update all slider positions
                foreach (var row in s_sliderRows)
                {
                    float defaultDay   = GetMultiplier(row.TransportType.Name, false) * 100f;
                    float defaultNight = GetMultiplier(row.TransportType.Name, true)  * 100f;
                    row.DaySlider.value = defaultDay;
                    row.DayLabel.text = Mathf.RoundToInt(defaultDay) + "%";
                    if (row.NightSlider != null)
                    {
                        row.NightSlider.value = defaultNight;
                        row.NightLabel.text = Mathf.RoundToInt(defaultNight) + "%";
                    }
                    if (row.TotalLabel != null)
                    {
                        UpdateTotalLabel(row.TransportType.Name, row.TotalLabel);
                    }
                }

                Utils.Log("TicketPricesTab: All ticket prices reset to defaults");
            }
            catch (Exception ex)
            {
                Utils.LogError($"TicketPricesTab: Error resetting prices: {ex.Message}");
            }
        }

        #region Multiplier Mapping

        private static float GetMultiplier(string transportName, bool isNight)
        {
            var settings = OptionsWrapper<Settings.Settings>.Options.TicketPriceCustomizer;
            if (settings == null) return 1.0f;
            if (isNight)
            {
                switch (transportName)
                {
                    case "Bus":           return settings.BusNightMultiplier;
                    case "Trolleybus":    return settings.TrolleybusNightMultiplier;
                    case "Tram":          return settings.TramNightMultiplier;
                    case "Metro":         return settings.MetroNightMultiplier;
                    case "Train":         return settings.TrainNightMultiplier;
                    case "Monorail":      return settings.MonorailNightMultiplier;
                    case "CableCar":      return settings.CableCarNightMultiplier;
                    case "Ship":          return settings.ShipNightMultiplier;
                    case "Ferry":         return settings.FerryNightMultiplier;
                    case "Plane":         return settings.PlaneNightMultiplier;
                    case "Blimp":         return settings.BlimpNightMultiplier;
                    case "Helicopter":    return settings.HelicopterNightMultiplier;
                    case "Taxi":          return settings.TaxiNightMultiplier;
                    case "SightseeingBus":return settings.SightseeingBusNightMultiplier;
                    case "IntercityBus":  return settings.IntercityBusNightMultiplier;
                    default:              return 1.0f;
                }
            }
            else
            {
                switch (transportName)
                {
                    case "Bus":           return settings.BusMultiplier;
                    case "Trolleybus":    return settings.TrolleybusMultiplier;
                    case "Tram":          return settings.TramMultiplier;
                    case "Metro":         return settings.MetroMultiplier;
                    case "Train":         return settings.TrainMultiplier;
                    case "Monorail":      return settings.MonorailMultiplier;
                    case "CableCar":      return settings.CableCarMultiplier;
                    case "Ship":          return settings.ShipMultiplier;
                    case "Ferry":         return settings.FerryMultiplier;
                    case "Plane":         return settings.PlaneMultiplier;
                    case "Blimp":         return settings.BlimpMultiplier;
                    case "Helicopter":    return settings.HelicopterMultiplier;
                    case "Taxi":          return settings.TaxiMultiplier;
                    case "SightseeingBus":return settings.SightseeingBusMultiplier;
                    case "IntercityBus":  return settings.IntercityBusMultiplier;
                    default:              return 1.0f;
                }
            }
        }

        private static void SetMultiplier(string transportName, bool isNight, float value)
        {
            var settings = OptionsWrapper<Settings.Settings>.Options.TicketPriceCustomizer;
            if (settings == null) return;
            if (isNight)
            {
                switch (transportName)
                {
                    case "Bus":           settings.BusNightMultiplier            = value; break;
                    case "Trolleybus":    settings.TrolleybusNightMultiplier     = value; break;
                    case "Tram":          settings.TramNightMultiplier           = value; break;
                    case "Metro":         settings.MetroNightMultiplier          = value; break;
                    case "Train":         settings.TrainNightMultiplier          = value; break;
                    case "Monorail":      settings.MonorailNightMultiplier       = value; break;
                    case "CableCar":      settings.CableCarNightMultiplier       = value; break;
                    case "Ship":          settings.ShipNightMultiplier           = value; break;
                    case "Ferry":         settings.FerryNightMultiplier          = value; break;
                    case "Plane":         settings.PlaneNightMultiplier          = value; break;
                    case "Blimp":         settings.BlimpNightMultiplier          = value; break;
                    case "Helicopter":    settings.HelicopterNightMultiplier     = value; break;
                    case "Taxi":          settings.TaxiNightMultiplier           = value; break;
                    case "SightseeingBus":settings.SightseeingBusNightMultiplier = value; break;
                    case "IntercityBus":  settings.IntercityBusNightMultiplier   = value; break;
                }
            }
            else
            {
                switch (transportName)
                {
                    case "Bus":           settings.BusMultiplier            = value; break;
                    case "Trolleybus":    settings.TrolleybusMultiplier     = value; break;
                    case "Tram":          settings.TramMultiplier           = value; break;
                    case "Metro":         settings.MetroMultiplier          = value; break;
                    case "Train":         settings.TrainMultiplier          = value; break;
                    case "Monorail":      settings.MonorailMultiplier       = value; break;
                    case "CableCar":      settings.CableCarMultiplier       = value; break;
                    case "Ship":          settings.ShipMultiplier           = value; break;
                    case "Ferry":         settings.FerryMultiplier          = value; break;
                    case "Plane":         settings.PlaneMultiplier          = value; break;
                    case "Blimp":         settings.BlimpMultiplier          = value; break;
                    case "Helicopter":    settings.HelicopterMultiplier     = value; break;
                    case "Taxi":          settings.TaxiMultiplier           = value; break;
                    case "SightseeingBus":settings.SightseeingBusMultiplier = value; break;
                    case "IntercityBus":  settings.IntercityBusMultiplier   = value; break;
                }
            }
        }

        private static void ApplyPriceForType(string transportName, float multiplier)
        {
            switch (transportName)
            {
                case "Bus": PriceCustomization.SetBusPrice(multiplier); break;
                case "Trolleybus": PriceCustomization.SetTrolleybusPrice(multiplier); break;
                case "Tram": PriceCustomization.SetTramPrice(multiplier); break;
                case "Metro": PriceCustomization.SetMetroPrice(multiplier); break;
                case "Train": PriceCustomization.SetTrainPrice(multiplier); break;
                case "Monorail": PriceCustomization.SetMonorailPrice(multiplier); break;
                case "CableCar": PriceCustomization.SetCableCarPrice(multiplier); break;
                case "Ship": PriceCustomization.SetShipPrice(multiplier); break;
                case "Ferry": PriceCustomization.SetFerryPrice(multiplier); break;
                case "Plane": PriceCustomization.SetPlanePrice(multiplier); break;
                case "Blimp": PriceCustomization.SetBlimpPrice(multiplier); break;
                case "Helicopter": PriceCustomization.SetHelicopterPrice(multiplier); break;
                case "Taxi": PriceCustomization.SetTaxiPrice(multiplier); break;
                case "SightseeingBus": PriceCustomization.SetSightseeingBusPrice(multiplier); break;
                case "IntercityBus": PriceCustomization.SetIntercityBusPrice(multiplier); break;
            }
        }

        private static string GetTransportInfoName(string transportName)
        {
            switch (transportName)
            {
                case "Bus": return "Bus";
                case "Trolleybus": return "Trolleybus";
                case "Tram": return "Tram";
                case "Metro": return "Metro";
                case "Train": return "Train";
                case "Monorail": return "Monorail";
                case "CableCar": return "CableCar";
                case "Ship": return "Ship";
                case "Ferry": return "Ferry";
                case "Plane": return "Airplane";
                case "Blimp": return "Blimp";
                case "Helicopter": return "Passenger Helicopter";
                case "Taxi": return "Taxi";
                case "SightseeingBus": return "Sightseeing Bus";
                case "IntercityBus": return "Intercity Bus";
                default: return null;
            }
        }

        private static string GetLocalizedTransportName(string transportName)
        {
            try
            {
                switch (transportName)
                {
                    case "Bus": return Localization.Get("TICKET_PRICE_BUS");
                    case "Trolleybus": return Localization.Get("TICKET_PRICE_TROLLEYBUS");
                    case "Tram": return Localization.Get("TICKET_PRICE_TRAM");
                    case "Metro": return Localization.Get("TICKET_PRICE_METRO");
                    case "Train": return Localization.Get("TICKET_PRICE_TRAIN");
                    case "Monorail": return Localization.Get("TICKET_PRICE_MONORAIL");
                    case "CableCar": return Localization.Get("TICKET_PRICE_CABLECAR");
                    case "Ship": return Localization.Get("TICKET_PRICE_SHIP");
                    case "Ferry": return Localization.Get("TICKET_PRICE_FERRY");
                    case "Plane": return Localization.Get("TICKET_PRICE_PLANE");
                    case "Blimp": return Localization.Get("TICKET_PRICE_BLIMP");
                    case "Helicopter": return Localization.Get("TICKET_PRICE_HELICOPTER");
                    case "Taxi":
                    {
                        bool isMph = OptionsWrapper<Settings.Settings>.Options.SpeedUnit == (int)Settings.Settings.VehicleSpeedUnits.MPH;
                        return Localization.Get(isMph ? "TICKET_PRICE_TAXI_MILE" : "TICKET_PRICE_TAXI_KILOMETER");
                    }
                    case "SightseeingBus": return Localization.Get("TICKET_PRICE_SIGHTSEEING_BUS");
                    case "IntercityBus": return Localization.Get("TICKET_PRICE_INTERCITY_BUS");
                    default: return transportName;
                }
            }
            catch
            {
                return transportName;
            }
        }

        private static string GetTransportTooltip(string transportName)
        {
            try
            {
                switch (transportName)
                {
                    case "Bus": return Localization.Get("TICKET_PRICE_BUS");
                    case "Trolleybus": return Localization.Get("TICKET_PRICE_TROLLEYBUS");
                    case "Tram": return Localization.Get("TICKET_PRICE_TRAM");
                    case "Metro": return Localization.Get("TICKET_PRICE_METRO");
                    case "Train": return Localization.Get("TICKET_PRICE_TRAIN");
                    case "Monorail": return Localization.Get("TICKET_PRICE_MONORAIL");
                    case "CableCar": return Localization.Get("TICKET_PRICE_CABLECAR");
                    case "Ship": return Localization.Get("TICKET_PRICE_SHIP");
                    case "Ferry": return Localization.Get("TICKET_PRICE_FERRY");
                    case "Plane": return Localization.Get("TICKET_PRICE_PLANE");
                    case "Blimp": return Localization.Get("TICKET_PRICE_BLIMP");
                    case "Helicopter": return Localization.Get("TICKET_PRICE_HELICOPTER");
                    case "Taxi":
                    {
                        bool isMph = OptionsWrapper<Settings.Settings>.Options.SpeedUnit == (int)Settings.Settings.VehicleSpeedUnits.MPH;
                        return Localization.Get(isMph ? "TICKET_PRICE_TAXI_MILE" : "TICKET_PRICE_TAXI_KILOMETER");
                    }
                    case "SightseeingBus": return Localization.Get("TICKET_PRICE_SIGHTSEEING_BUS");
                    case "IntercityBus": return Localization.Get("TICKET_PRICE_INTERCITY_BUS");
                    default: return "";
                }
            }
            catch
            {
                return "";
            }
        }

        #endregion

        #region Data Types

        private class TransportTypeInfo
        {
            public string Name;
            public string SpriteName;
            public ItemClass.SubService SubService;

            public TransportTypeInfo(string name, string spriteName, ItemClass.SubService subService)
            {
                Name = name;
                SpriteName = spriteName;
                SubService = subService;
            }
        }

        private class TicketPriceSliderRow
        {
            public TransportTypeInfo TransportType;
            public UISlider DaySlider;
            public UILabel DayLabel;
            public UISlider NightSlider;
            public UILabel NightLabel;
            public UILabel TotalLabel;
            public bool HasAfterDark;
        }

        #endregion
    }
}
