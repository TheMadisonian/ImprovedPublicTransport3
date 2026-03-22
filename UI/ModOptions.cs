using ColossalFramework.UI;
using ICities;
using ImprovedPublicTransport.Settings;
using ImprovedPublicTransport.OptionsFramework.Extensions;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.ComponentModel;
using ImprovedPublicTransport.OptionsFramework;
using ImprovedPublicTransport.UI.AlgernonCommons;
using UnityEngine;
using ImprovedPublicTransport.Util;
using StopsAndStations;
using ExpressBusServices;

namespace ImprovedPublicTransport.UI
{
    public class ModOptions
    {
        // Helper class for storing slider reset data
        private class SliderResetData
        {
            public UISlider Slider { get; set; }
            public UILabel ValueLabel { get; set; }
            public int DefaultValue { get; set; }
            public Action<int> Setter { get; set; }
        }

        public ModOptions(UIHelperBase helper, string title)
        {
            // Create main group
            var group = helper.AddGroup(title) as UIHelper;
            var panel = group.self as UIPanel;

            // Create tab strip
            UITabstrip tabStrip = panel.AddUIComponent<UITabstrip>();
            tabStrip.relativePosition = new Vector3(0, 0);
            tabStrip.size = new Vector2(panel.width, 50);
            tabStrip.height = 50;

            UITabContainer tabContainer = panel.AddUIComponent<UITabContainer>();
            tabContainer.relativePosition = new Vector3(0, 50);
            float containerHeight = Mathf.Max(panel.height - 50, 600);
            tabContainer.size = new Vector2(panel.width, containerHeight);
            tabContainer.height = containerHeight;
            tabStrip.tabPages = tabContainer;

            // Helper to add a tab button
            Func<string, UIButton> addTab = (string tabName) =>
            {
                UIButton tab = tabStrip.AddTab(tabName);
                tab.normalBgSprite = "GenericTab";
                tab.focusedBgSprite = "GenericTabFocused";
                tab.hoveredBgSprite = "GenericTabHovered";
                tab.pressedBgSprite = "GenericTabPressed";
                tab.textPadding = new RectOffset(14, 14, 10, 10);
                tab.textScale = 0.9f;
                tab.autoSize = true;
                UILabel label = tab.components.FirstOrDefault(c => c is UILabel) as UILabel;
                if (label != null)
                {
                    label.autoSize = true;
                    label.PerformLayout();
                    tab.tooltip = label.text;
                }
                return tab;
            };

            // Create tabs: General, Stops, Unbunching, Delete Lines
            addTab(Localization.Get("SETTINGS_TAB_GENERAL"));
            addTab(Localization.Get("SETTINGS_TAB_STOPS"));
            addTab(Localization.Get("SETTINGS_TAB_UNBUNCHING"));
            addTab(Localization.Get("SETTINGS_TAB_DELETE"));

            // Configure pages
            // index 0 -> General, 1 -> Stops, 2 -> Unbunching, 3 -> Delete Lines
            for (int i = 0; i < 4; i++)
            {
                var page = tabContainer.components[i] as UIPanel;
                page.autoLayout = false;
                page.width = panel.width;
                page.height = tabContainer.height;
                page.clipChildren = true;
                page.isVisible = (i == 0); // show General by default
            }

            // General content panel
            var generalPage = tabContainer.components[0] as UIPanel;
            var generalContent = generalPage.AddUIComponent<UIPanel>();
            generalContent.size = new Vector2(generalPage.width - 12, generalPage.height - 8);
            generalContent.relativePosition = new Vector3(0, 0);
            generalContent.autoLayout = true;
            generalContent.autoLayoutDirection = LayoutDirection.Vertical;
            generalContent.autoLayoutPadding = new RectOffset(5, 5, 0, 5);
            generalContent.autoLayoutStart = LayoutStart.TopLeft;
            generalContent.clipChildren = true;
            generalPage.eventSizeChanged += (c, s) => { generalContent.size = new Vector2(generalPage.width - 12, generalPage.height - 8); };

            var generalHelper = new UIHelper(generalContent);
            // Keep all current options in General except the Delete Lines group, EBS groups, Unbunching, Stops, and Auto Line (manual)
            AddOptionsGroupExcluding<Settings.Settings>(generalHelper, "SETTINGS_LINE_DELETION_TOOL|SETTINGS_UNBUNCHING|SETTINGS_EBS_GROUP_BUS|SETTINGS_EBS_GROUP_TRAM|SETTINGS_STOPS|SETTINGS_STOPS_PASSENGERS|SETTINGS_AUTO_LINE", Localization.Get);
            try { AddAutoLineSection(generalHelper); } catch (Exception ex) { Debug.LogError($"IPT: AddAutoLineSection failed: {ex.Message}"); }

            // Stops page
            var stopsPage = tabContainer.components[1] as UIPanel;
            var stopsContent = stopsPage.AddUIComponent<UIPanel>();
            stopsContent.size = new Vector2(stopsPage.width - 12, stopsPage.height - 8);
            stopsContent.relativePosition = new Vector3(0, 0);
            stopsContent.autoLayout = true;
            stopsContent.autoLayoutDirection = LayoutDirection.Vertical;
            stopsContent.autoLayoutPadding = new RectOffset(5, 5, 0, 5); 
            stopsContent.autoLayoutStart = LayoutStart.TopLeft;
            stopsContent.clipChildren = true;
            stopsPage.eventSizeChanged += (c, s) => { stopsContent.size = new Vector2(stopsPage.width - 12, stopsPage.height - 8); };

            // Stops tab: inject Stops and Stations passenger limit sliders
            // Add click handler to rebuild when tab is clicked (handles late instance initialization)
            try
            {
                UIButton stopsTab = tabStrip.tabs[1] as UIButton;
                AddStopsAndStationsSliders(stopsContent);
                stopsTab.eventClick += (c, e) =>
                {
                    // Clear existing children and rebuild
                    foreach (UIComponent child in stopsContent.components.ToList())
                    {
                        UnityEngine.Object.Destroy(child.gameObject);
                    }
                    try
                    {
                        AddStopsAndStationsSliders(stopsContent);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"IPT: Failed to rebuild Stops and Stations UI: {ex.Message}");
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"IPT: Failed to build Stops and Stations UI: {ex.Message}");
            }

            // Unbunching page
            var unbunchPage = tabContainer.components[2] as UIPanel;
            var unbunchContent = unbunchPage.AddUIComponent<UIPanel>();
            unbunchContent.size = new Vector2(unbunchPage.width - 12, unbunchPage.height - 8);
            unbunchContent.relativePosition = new Vector3(0, 0);
            unbunchContent.autoLayout = true;
            unbunchContent.autoLayoutDirection = LayoutDirection.Vertical;
            unbunchContent.autoLayoutPadding = new RectOffset(5, 5, 0, 5);
            unbunchContent.autoLayoutStart = LayoutStart.TopLeft;
            unbunchContent.clipChildren = true;
            unbunchPage.eventSizeChanged += (c, s) => { unbunchContent.size = new Vector2(unbunchPage.width - 12, unbunchPage.height - 8); };
            
            // Unbunching tab: three sections via AddGroup so headers and separators match
            try
            {
                var unbunchHelper = new UIHelper(unbunchContent);

                // Unbunching section
                try
                {
                    var unbunchGroup = unbunchHelper.AddGroup(Localization.Get("SETTINGS_UNBUNCHING"));
                    var unbunchGroupPanel = (UIPanel)((UIHelper)unbunchGroup).self;
                    AddUnbunchingSliders(unbunchGroupPanel);
                }
                catch (Exception ex) { Debug.LogError($"IPT: Failed to build Unbunching group: {ex.Message}"); }

                // EBS section
                try
                {
                    var ebsBusGroup = unbunchHelper.AddGroup(Localization.Get("SETTINGS_EBS_GROUP_BUS"));
                    if (ebsBusGroup != null)
                    {
                        // Create a space panel for the dropdown
                        var busDropdownRow = (UIPanel)ebsBusGroup.AddSpace(34);
                        var busDropdown = ImprovedPublicTransport.UI.AlgernonCommons.UIDropDowns.AddLabelledDropDown(busDropdownRow, 0f, 2f, Localization.Get("SETTINGS_EBS_DROPDOWN_UNBUNCHING_MODE"), 220f, 25f);
                        try { if (busDropdown is UIDropDown dd) dd.tooltip = Localization.Get("SETTINGS_EBS_TOOLTIP_UNBUNCHING_MODE"); } catch { }
                        
                        UICheckBox selfBalCheckbox = null;
                        UICheckBox selfBalMidCheckbox = null;
                        UICheckBox minibusCheckbox = null;

                        try
                        {
                            busDropdown.items = new string[] { Localization.Get("SETTINGS_EBS_MODE_NONE"), Localization.Get("SETTINGS_EBS_MODE_PRUDENTIAL"), Localization.Get("SETTINGS_EBS_MODE_AGGRESSIVE") };
                            busDropdown.selectedIndex = OptionsWrapper<Settings.Settings>.Options.ExpressBusUnbunchingMode;
                            busDropdown.eventSelectedIndexChanged += (c, idx) =>
                            {
                                try
                                {
                                    OptionsWrapper<Settings.Settings>.Options.ExpressBusUnbunchingMode = idx;
                                    OptionsWrapper<Settings.Settings>.SaveOptions();
                                    EBSModConfig.CurrentExpressBusMode = (EBSModConfig.ExpressMode)idx;
                                    
                                    // Enable/disable the EBS feature checkboxes based on whether EBS is disabled
                                    bool isEnabled = (idx != 0);
                                    if (selfBalCheckbox != null) selfBalCheckbox.isEnabled = isEnabled;
                                    if (selfBalMidCheckbox != null) selfBalMidCheckbox.isEnabled = isEnabled;
                                    if (minibusCheckbox != null) minibusCheckbox.isEnabled = isEnabled;
                                }
                                catch { }
                            };
                        }
                        catch { }

                        // Add checkboxes for bus options
                        var selfBalToggle = ebsBusGroup.AddCheckbox(Localization.Get("SETTINGS_EBS_ENABLE_SELFBAL"), OptionsWrapper<Settings.Settings>.Options.ExpressBusEnableSelfBalancing, (newValue) =>
                        {
                            try { OptionsWrapper<Settings.Settings>.Options.ExpressBusEnableSelfBalancing = newValue; OptionsWrapper<Settings.Settings>.SaveOptions(); } catch { }
                        });
                        if (selfBalToggle is UICheckBox cb1) 
                        { 
                            selfBalCheckbox = cb1;
                            cb1.tooltip = Localization.Get("SETTINGS_EBS_TOOLTIP_SELFBAL");
                            cb1.isEnabled = (OptionsWrapper<Settings.Settings>.Options.ExpressBusUnbunchingMode != 0);
                        }

                        var selfBalMidToggle = ebsBusGroup.AddCheckbox(Localization.Get("SETTINGS_EBS_ENABLE_SELFBAL_TARGETMID"), OptionsWrapper<Settings.Settings>.Options.ExpressBusAllowMiddleStopBalancing, (newValue) =>
                        {
                            try { OptionsWrapper<Settings.Settings>.Options.ExpressBusAllowMiddleStopBalancing = newValue; OptionsWrapper<Settings.Settings>.SaveOptions(); } catch { }
                        });
                        if (selfBalMidToggle is UICheckBox cb2) 
                        { 
                            selfBalMidCheckbox = cb2;
                            cb2.tooltip = Localization.Get("SETTINGS_EBS_TOOLTIP_SELFBAL_TARGETMID");
                            cb2.isEnabled = (OptionsWrapper<Settings.Settings>.Options.ExpressBusUnbunchingMode != 0);
                        }

                        var minibusToggle = ebsBusGroup.AddCheckbox(Localization.Get("SETTINGS_EBS_ENABLE_MINIBUS"), OptionsWrapper<Settings.Settings>.Options.ExpressBusEnableMinibusMode, (newValue) =>
                        {
                            try { OptionsWrapper<Settings.Settings>.Options.ExpressBusEnableMinibusMode = newValue; OptionsWrapper<Settings.Settings>.SaveOptions(); } catch { }
                        });
                        if (minibusToggle is UICheckBox cb3) 
                        { 
                            minibusCheckbox = cb3;
                            cb3.tooltip = Localization.Get("SETTINGS_EBS_TOOLTIP_MINIBUS");
                            cb3.isEnabled = (OptionsWrapper<Settings.Settings>.Options.ExpressBusUnbunchingMode != 0);
                        }
                    }
                }
                catch (Exception ex) { Debug.LogError($"IPT: Failed to build EBS Bus group: {ex.Message}"); }

                // ETS section
                try
                {
                    var ebsTramGroup = unbunchHelper.AddGroup(Localization.Get("SETTINGS_EBS_GROUP_TRAM"));
                    if (ebsTramGroup != null)
                    {
                        // Create a space panel for the dropdown
                        var tramDropdownRow = (UIPanel)ebsTramGroup.AddSpace(34);
                        var tramDropdown = ImprovedPublicTransport.UI.AlgernonCommons.UIDropDowns.AddLabelledDropDown(tramDropdownRow, 0f, 2f, Localization.Get("SETTINGS_EBS_DROPDOWN_TRAM_UNBUNCHING_MODE"), 220f, 25f);
                        try { if (tramDropdown is UIDropDown td) td.tooltip = Localization.Get("SETTINGS_EBS_TOOLTIP_TRAM_UNBUNCHING"); } catch { }
                        
                        try
                        {
                            tramDropdown.items = new string[] { Localization.Get("SETTINGS_EBS_TRAM_MODE_NONE"), Localization.Get("SETTINGS_EBS_TRAM_MODE_LIGHT_RAIL"), Localization.Get("SETTINGS_EBS_TRAM_MODE_TRAM") };
                            tramDropdown.selectedIndex = OptionsWrapper<Settings.Settings>.Options.ExpressTramUnbunchingMode;
                            tramDropdown.eventSelectedIndexChanged += (c, idx) =>
                            {
                                try
                                {
                                    OptionsWrapper<Settings.Settings>.Options.ExpressTramUnbunchingMode = idx;
                                    OptionsWrapper<Settings.Settings>.SaveOptions();
                                }
                                catch { }
                            };
                        }
                        catch { }
                    }
                }
                catch (Exception ex) { Debug.LogError($"IPT: Failed to build EBS Tram group: {ex.Message}"); }
            }
            catch (Exception) { }

            // Delete Lines page
            var deletePage = tabContainer.components[3] as UIPanel;
            var deleteContent = deletePage.AddUIComponent<UIPanel>();
            deleteContent.size = new Vector2(deletePage.width - 12, deletePage.height - 8);
            deleteContent.relativePosition = new Vector3(0, 0);
            deleteContent.autoLayout = true;
            deleteContent.autoLayoutDirection = LayoutDirection.Vertical;
            deleteContent.autoLayoutPadding = new RectOffset(5, 5, 0, 5);
            deleteContent.autoLayoutStart = LayoutStart.TopLeft;
            deleteContent.clipChildren = true;
            deletePage.eventSizeChanged += (c, s) => { deleteContent.size = new Vector2(deletePage.width - 12, deletePage.height - 8); };
            // Delete Lines tab: render only the deletion-related options
            try
            {
                var deleteHelper = new UIHelper(deleteContent);
                AddOptionsGroupOnly<Settings.Settings>(deleteHelper, "SETTINGS_LINE_DELETION_TOOL", Localization.Get);
            }
            catch (Exception) { }

        }

        // Add all options except those in the excluded group(s). Pass pipe-separated group names in excludeGroup.
        private static void AddOptionsGroupExcluding<T>(UIHelperBase helper, string excludeGroup, Func<string, string> translator = null)
        {
            var excludeSet = new HashSet<string>((excludeGroup ?? string.Empty).Split(new[] {'|' }, StringSplitOptions.RemoveEmptyEntries));

            var properties = typeof(T).GetProperties().Where(property =>
            {
                var attrs = (ImprovedPublicTransport.OptionsFramework.Attibutes.AbstractOptionsAttribute[])property.GetCustomAttributes(typeof(ImprovedPublicTransport.OptionsFramework.Attibutes.AbstractOptionsAttribute), false);
                return attrs.Any();
            }).Select(p => p.Name);

            var groups = new Dictionary<string, UIHelperBase>();
            foreach (var propertyName in properties)
            {
                var groupName = OptionsWrapper<T>.Options.GetPropertyGroup(propertyName);
                if (groupName != null && excludeSet.Contains(groupName)) // skip excluded groups
                    continue;

                var property = typeof(T).GetProperty(propertyName);

                // Skip if HideConditionAttribute says to hide
                var hideConditions = (ImprovedPublicTransport.OptionsFramework.Attibutes.HideConditionAttribute[])property.GetCustomAttributes(typeof(ImprovedPublicTransport.OptionsFramework.Attibutes.HideConditionAttribute), false);
                if (hideConditions.Any(a => a.IsHidden())) continue;

                var description = OptionsWrapper<T>.Options.GetPropertyDescription(propertyName);
                if (translator != null)
                {
                    description = translator.Invoke(description);
                }
                var targetHelper = helper;
                if (groupName != null)
                {
                    var displayGroup = translator == null ? groupName : translator.Invoke(groupName);
                    if (!groups.ContainsKey(displayGroup)) groups[displayGroup] = helper.AddGroup(displayGroup);
                    targetHelper = groups[displayGroup];
                }

                UIComponent created = null;

                // Checkbox
                var checkboxAttr = OptionsWrapper<T>.Options.GetAttribute<T, ImprovedPublicTransport.OptionsFramework.Attibutes.CheckboxAttribute>(propertyName);
                if (checkboxAttr != null)
                {
                    var initial = (bool)property.GetValue(OptionsWrapper<T>.Options, null);
                    created = (UIComponent)targetHelper.AddCheckbox(description, initial, b =>
                    {
                        property.SetValue(OptionsWrapper<T>.Options, b, null);
                        OptionsWrapper<T>.SaveOptions();
                        checkboxAttr.Action<bool>().Invoke(b);
                    });
                    SetTooltip(created, property, translator);
                    continue;
                }

                // Textfield
                var textAttr = OptionsWrapper<T>.Options.GetAttribute<T, ImprovedPublicTransport.OptionsFramework.Attibutes.TextfieldAttribute>(propertyName);
                if (textAttr != null)
                {
                    var initial = Convert.ToString(property.GetValue(OptionsWrapper<T>.Options, null));
                    created = (UIComponent)targetHelper.AddTextfield(description, initial, s => { }, s =>
                    {
                        object value;
                        if (property.PropertyType == typeof(int)) value = Convert.ToInt32(s);
                        else if (property.PropertyType == typeof(short)) value = Convert.ToInt16(s);
                        else if (property.PropertyType == typeof(double)) value = Convert.ToDouble(s);
                        else if (property.PropertyType == typeof(float)) value = Convert.ToSingle(s);
                        else value = s;
                        property.SetValue(OptionsWrapper<T>.Options, value, null);
                        OptionsWrapper<T>.SaveOptions();
                        textAttr.Action<string>().Invoke(s);
                    });
                    SetTooltip(created, property, translator);
                    continue;
                }

                // DropDown (inline style like EBS)
                var ddAttr = OptionsWrapper<T>.Options.GetAttribute<T, ImprovedPublicTransport.OptionsFramework.Attibutes.DropDownAttribute>(propertyName);
                if (ddAttr != null)
                {
                    var items = ddAttr.GetItems(translator);
                    var defaultCode = (int)property.GetValue(OptionsWrapper<T>.Options, null);
                    int defaultIndex = 0;
                    for (int i = 0; i < items.Count; i++) if (items[i].Value == defaultCode) { defaultIndex = i; break; }
                    var names = items.Select(kvp => kvp.Key).ToArray();
                    
                    // Create inline-style dropdown with label and dropdown on same row
                    var dropdownRow = (UIPanel)targetHelper.AddSpace(34);
                    created = ImprovedPublicTransport.UI.AlgernonCommons.UIDropDowns.AddLabelledDropDown(dropdownRow, 0f, 2f, description, 220f, 25f);
                    
                    if (created is UIDropDown dropdown)
                    {
                        dropdown.items = names;
                        dropdown.selectedIndex = defaultIndex;
                        dropdown.eventSelectedIndexChanged += (c, sel) =>
                        {
                            try
                            {
                                var code = items[sel].Value;
                                property.SetValue(OptionsWrapper<T>.Options, code, null);
                                OptionsWrapper<T>.SaveOptions();
                                ddAttr.Action<int>().Invoke(code);
                            }
                            catch { }
                        };
                        SetTooltip(dropdown, property, translator);
                    }
                    continue;
                }

                // Slider
                var sliderAttr = OptionsWrapper<T>.Options.GetAttribute<T, ImprovedPublicTransport.OptionsFramework.Attibutes.SliderAttribute>(propertyName);
                if (sliderAttr != null)
                {
                    var value = property.GetValue(OptionsWrapper<T>.Options, null);
                    float finalValue;
                    switch (value)
                    {
                        case float f: finalValue = f; break;
                        case byte b: finalValue = b; break;
                        case int i: finalValue = i; break;
                        default: throw new Exception("Unsupported numeric type for slider!");
                    }
                    created = (UIComponent)targetHelper.AddSlider(description, sliderAttr.Min, sliderAttr.Max, sliderAttr.Step, Mathf.Clamp(finalValue, sliderAttr.Min, sliderAttr.Max), f =>
                    {
                        if (value is float) property.SetValue(OptionsWrapper<T>.Options, f, null);
                        else if (value is byte) property.SetValue(OptionsWrapper<T>.Options, (byte)f, null);
                        else if (value is int) property.SetValue(OptionsWrapper<T>.Options, (int)f, null);
                        OptionsWrapper<T>.SaveOptions();
                        sliderAttr.Action<float>().Invoke(f);
                    });
                    SetTooltip(created, property, translator);
                    continue;
                }

                // Button
                var btnAttr = OptionsWrapper<T>.Options.GetAttribute<T, ImprovedPublicTransport.OptionsFramework.Attibutes.ButtonAttribute>(propertyName);
                if (btnAttr != null)
                {
                    created = (UIComponent)targetHelper.AddButton(description, () => { btnAttr.Action().Invoke(); });
                    SetTooltip(created, property, translator);
                    continue;
                }

                // Label
                var labelAttr = OptionsWrapper<T>.Options.GetAttribute<T, ImprovedPublicTransport.OptionsFramework.Attibutes.LabelAttribute>(propertyName);
                if (labelAttr != null)
                {
                    var space = (UIPanel)targetHelper.AddSpace(20);
                    var valueLabel = space.AddUIComponent<UILabel>();
                    valueLabel.AlignTo(space, UIAlignAnchor.TopLeft);
                    valueLabel.relativePosition = new Vector3(0, 0, 0);
                    valueLabel.text = description;
                    continue;
                }
            }
        }

        private class PendingButton
        {
            public UIHelperBase Helper;
            public string Description;
            public string PropertyName;
            public PendingButton(UIHelperBase helper, string description, string propertyName)
            {
                Helper = helper;
                Description = description;
                PropertyName = propertyName;
            }
        }

        // Add only properties that belong to the given group name
        private static void AddOptionsGroupOnly<T>(UIHelperBase helper, string onlyGroup, Func<string, string> translator = null)
        {
            if (string.IsNullOrEmpty(onlyGroup)) return;
            var properties = typeof(T).GetProperties().Where(property =>
            {
                var attrs = (ImprovedPublicTransport.OptionsFramework.Attibutes.AbstractOptionsAttribute[])property.GetCustomAttributes(typeof(ImprovedPublicTransport.OptionsFramework.Attibutes.AbstractOptionsAttribute), false);
                return attrs.Any();
            }).Select(p => p.Name);

            // create a titled group for this section
            var displayGroup = translator == null ? onlyGroup : translator.Invoke(onlyGroup);
            var groupHelper = helper.AddGroup(displayGroup);

            var pendingButtons = new List<PendingButton>();
            foreach (var propertyName in properties)
            {
                var groupName = OptionsWrapper<T>.Options.GetPropertyGroup(propertyName);
                if (groupName == null || groupName != onlyGroup) continue;

                var property = typeof(T).GetProperty(propertyName);

                // Skip if HideConditionAttribute says to hide
                var hideConditions = (ImprovedPublicTransport.OptionsFramework.Attibutes.HideConditionAttribute[])property.GetCustomAttributes(typeof(ImprovedPublicTransport.OptionsFramework.Attibutes.HideConditionAttribute), false);
                if (hideConditions.Any(a => a.IsHidden())) continue;

                var description = OptionsWrapper<T>.Options.GetPropertyDescription(propertyName);
                var targetHelper = groupHelper;

                // translate display text if needed
                if (translator != null)
                {
                    description = translator.Invoke(description);
                }

                UIComponent created = null;

                // Checkbox
                var checkboxAttr = OptionsWrapper<T>.Options.GetAttribute<T, ImprovedPublicTransport.OptionsFramework.Attibutes.CheckboxAttribute>(propertyName);
                if (checkboxAttr != null)
                {
                    var initial = (bool)property.GetValue(OptionsWrapper<T>.Options, null);
                    created = (UIComponent)targetHelper.AddCheckbox(description, initial, b =>
                    {
                        property.SetValue(OptionsWrapper<T>.Options, b, null);
                        OptionsWrapper<T>.SaveOptions();
                        checkboxAttr.Action<bool>().Invoke(b);
                    });
                }

                // Textfield
                var textAttr = OptionsWrapper<T>.Options.GetAttribute<T, ImprovedPublicTransport.OptionsFramework.Attibutes.TextfieldAttribute>(propertyName);
                if (textAttr != null)
                {
                    var initial = Convert.ToString(property.GetValue(OptionsWrapper<T>.Options, null));
                    created = (UIComponent)targetHelper.AddTextfield(description, initial, s => { }, s =>
                    {
                        object value;
                        if (property.PropertyType == typeof(int)) value = Convert.ToInt32(s);
                        else if (property.PropertyType == typeof(short)) value = Convert.ToInt16(s);
                        else if (property.PropertyType == typeof(double)) value = Convert.ToDouble(s);
                        else if (property.PropertyType == typeof(float)) value = Convert.ToSingle(s);
                        else value = s;
                        property.SetValue(OptionsWrapper<T>.Options, value, null);
                        OptionsWrapper<T>.SaveOptions();
                        textAttr.Action<string>().Invoke(s);
                    });
                }

                // DropDown (inline style like EBS)
                var ddAttr = OptionsWrapper<T>.Options.GetAttribute<T, ImprovedPublicTransport.OptionsFramework.Attibutes.DropDownAttribute>(propertyName);
                if (ddAttr != null)
                {
                    var items = ddAttr.GetItems(translator);
                    var defaultCode = (int)property.GetValue(OptionsWrapper<T>.Options, null);
                    int defaultIndex = 0;
                    for (int i = 0; i < items.Count; i++) if (items[i].Value == defaultCode) { defaultIndex = i; break; }
                    var names = items.Select(kvp => kvp.Key).ToArray();
                    
                    // Create inline-style dropdown with label and dropdown on same row
                    var dropdownRow = (UIPanel)targetHelper.AddSpace(34);
                    created = ImprovedPublicTransport.UI.AlgernonCommons.UIDropDowns.AddLabelledDropDown(dropdownRow, 0f, 2f, description, 220f, 25f);
                    
                    if (created is UIDropDown dropdown)
                    {
                        dropdown.items = names;
                        dropdown.selectedIndex = defaultIndex;
                        dropdown.eventSelectedIndexChanged += (c, sel) =>
                        {
                            try
                            {
                                var code = items[sel].Value;
                                property.SetValue(OptionsWrapper<T>.Options, code, null);
                                OptionsWrapper<T>.SaveOptions();
                                ddAttr.Action<int>().Invoke(code);
                            }
                            catch { }
                        };
                    }
                }

                // Slider
                var sliderAttr = OptionsWrapper<T>.Options.GetAttribute<T, ImprovedPublicTransport.OptionsFramework.Attibutes.SliderAttribute>(propertyName);
                if (sliderAttr != null)
                {
                    var value = property.GetValue(OptionsWrapper<T>.Options, null);
                    float finalValue;
                    switch (value)
                    {
                        case float f: finalValue = f; break;
                        case byte b: finalValue = b; break;
                        case int i: finalValue = i; break;
                        default: throw new Exception("Unsupported numeric type for slider!");
                    }
                    created = (UIComponent)targetHelper.AddSlider(description, sliderAttr.Min, sliderAttr.Max, sliderAttr.Step, Mathf.Clamp(finalValue, sliderAttr.Min, sliderAttr.Max), f =>
                    {
                        if (value is float) property.SetValue(OptionsWrapper<T>.Options, f, null);
                        else if (value is byte) property.SetValue(OptionsWrapper<T>.Options, (byte)f, null);
                        else if (value is int) property.SetValue(OptionsWrapper<T>.Options, (int)f, null);
                        OptionsWrapper<T>.SaveOptions();
                        sliderAttr.Action<float>().Invoke(f);
                    });
                }

                // Button
                var btnAttr = OptionsWrapper<T>.Options.GetAttribute<T, ImprovedPublicTransport.OptionsFramework.Attibutes.ButtonAttribute>(propertyName);
                if (btnAttr != null)
                {
                    // defer buttons until after checkboxes and sliders so button appears at the bottom
                    pendingButtons.Add(new PendingButton(targetHelper, description, propertyName));
                }

                // Label
                var labelAttr = OptionsWrapper<T>.Options.GetAttribute<T, ImprovedPublicTransport.OptionsFramework.Attibutes.LabelAttribute>(propertyName);
                if (labelAttr != null)
                {
                    var space = (UIPanel)targetHelper.AddSpace(20);
                    var valueLabel = space.AddUIComponent<UILabel>();
                    valueLabel.AlignTo(space, UIAlignAnchor.TopLeft);
                    valueLabel.relativePosition = new Vector3(0, 0, 0);
                    valueLabel.text = description;
                    created = valueLabel;
                }

                // Apply tooltip from DescriptionAttribute if present
                var descriptionAttribute = OptionsWrapper<T>.Options.GetAttribute<T, System.ComponentModel.DescriptionAttribute>(propertyName);
                if (created != null && descriptionAttribute != null)
                {
                    created.tooltip = (translator == null || descriptionAttribute is ImprovedPublicTransport.OptionsFramework.Attibutes.DontTranslateDescriptionAttribute) ? descriptionAttribute.Description : translator.Invoke(descriptionAttribute.Description);
                }
            }

            // render deferred buttons at the end
            foreach (var pb in pendingButtons)
            {
                var targetHelper = pb.Helper;
                var description = pb.Description;
                var propertyName = pb.PropertyName;
                var property = typeof(T).GetProperty(propertyName);
                var btnAttr = OptionsWrapper<T>.Options.GetAttribute<T, ImprovedPublicTransport.OptionsFramework.Attibutes.ButtonAttribute>(propertyName);
                var btn = (UIButton)targetHelper.AddButton(description, () => { btnAttr.Action().Invoke(); });

                // set tooltip for button if DescriptionAttribute exists
                var descriptionAttribute = OptionsWrapper<T>.Options.GetAttribute<T, System.ComponentModel.DescriptionAttribute>(propertyName);
                if (descriptionAttribute != null)
                {
                    btn.tooltip = (translator == null || descriptionAttribute is ImprovedPublicTransport.OptionsFramework.Attibutes.DontTranslateDescriptionAttribute) ? descriptionAttribute.Description : translator.Invoke(descriptionAttribute.Description);
                }
            }
        }

        private static void SetTooltip(UIComponent component, PropertyInfo property, Func<string, string> translator)
        {
            if (component == null || property == null) return;
            var descriptionAttribute = (DescriptionAttribute)property.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault();
            if (descriptionAttribute == null) return;
            component.tooltip = (translator == null || descriptionAttribute is ImprovedPublicTransport.OptionsFramework.Attibutes.DontTranslateDescriptionAttribute)
                ? descriptionAttribute.Description
                : translator.Invoke(descriptionAttribute.Description);
        }

        // Stops tab: horizontal label-slider-value rows reading from centralized Settings
        private static void AddStopsAndStationsSliders(UIPanel parentPanel)
        {
            var settings = OptionsWrapper<Settings.Settings>.Options;

            var header = parentPanel.AddUIComponent<UILabel>();
            header.text = Localization.Get("SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_HEADER");
            header.textScale = 0.88f;
            header.autoSize = true;

            // Add 10px spacing between the header and first slider
            var spacingPanel = parentPanel.AddUIComponent<UIPanel>();
            spacingPanel.autoLayout = false;
            spacingPanel.height = 7f;
            spacingPanel.width = parentPanel.width;

            // Store slider references for reset button
            var sliders = new List<SliderResetData>();

            // Add all 13 passenger limit sliders
            AddSasSliderWithRef(parentPanel, "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_BUS", () => settings.MaxWaitingPassengersBus, 
                v => settings.MaxWaitingPassengersBus = v, 10f, 500f, 5f, 50, sliders);
            
            AddSasSliderWithRef(parentPanel, "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TROLLEYBUS", () => settings.MaxWaitingPassengersTrolleybus, 
                v => settings.MaxWaitingPassengersTrolleybus = v, 10f, 500f, 5f, 50, sliders);
            
            AddSasSliderWithRef(parentPanel, "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_EVACUATION_BUS", () => settings.MaxWaitingPassengersEvacuationBus, 
                v => settings.MaxWaitingPassengersEvacuationBus = v, 10f, 500f, 5f, 100, sliders);
            
            AddSasSliderWithRef(parentPanel, "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TOURIST_BUS", () => settings.MaxWaitingPassengersTouristBus, 
                v => settings.MaxWaitingPassengersTouristBus = v, 10f, 500f, 5f, 50, sliders);
            
            AddSasSliderWithRef(parentPanel, "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TRAM", () => settings.MaxWaitingPassengersTram, 
                v => settings.MaxWaitingPassengersTram = v, 10f, 500f, 5f, 80, sliders);
            
            AddSasSliderWithRef(parentPanel, "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_METRO", () => settings.MaxWaitingPassengersMetro, 
                v => settings.MaxWaitingPassengersMetro = v, 50f, 2000f, 25f, 250, sliders);
            
            AddSasSliderWithRef(parentPanel, "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TRAIN", () => settings.MaxWaitingPassengersTrain, 
                v => settings.MaxWaitingPassengersTrain = v, 50f, 2000f, 25f, 250, sliders);
            
            AddSasSliderWithRef(parentPanel, "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_MONORAIL", () => settings.MaxWaitingPassengersMonorail, 
                v => settings.MaxWaitingPassengersMonorail = v, 50f, 2000f, 25f, 250, sliders);
            
            AddSasSliderWithRef(parentPanel, "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_CABLECAR", () => settings.MaxWaitingPassengersCableCar, 
                v => settings.MaxWaitingPassengersCableCar = v, 10f, 500f, 5f, 40, sliders);
            
            AddSasSliderWithRef(parentPanel, "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_SHIP", () => settings.MaxWaitingPassengersShip, 
                v => settings.MaxWaitingPassengersShip = v, 50f, 1000f, 10f, 150, sliders);
            
            AddSasSliderWithRef(parentPanel, "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_AIRPLANE", () => settings.MaxWaitingPassengersAirplane, 
                v => settings.MaxWaitingPassengersAirplane = v, 50f, 1000f, 10f, 250, sliders);
            
            AddSasSliderWithRef(parentPanel, "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_HELICOPTER", () => settings.MaxWaitingPassengersHelicopter, 
                v => settings.MaxWaitingPassengersHelicopter = v, 10f, 500f, 5f, 40, sliders);
            
            AddSasSliderWithRef(parentPanel, "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_HOTAIRBALLOON", () => settings.MaxWaitingPassengersHotAirBalloon, 
                v => settings.MaxWaitingPassengersHotAirBalloon = v, 10f, 500f, 5f, 40, sliders);

            // Reset button
            var resetButtonPanel = parentPanel.AddUIComponent<UIPanel>();
            resetButtonPanel.autoLayout = true;
            resetButtonPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            resetButtonPanel.autoLayoutPadding = new RectOffset(5, 5, 10, 10);
            resetButtonPanel.autoLayoutStart = LayoutStart.TopLeft;
            resetButtonPanel.height = 35f;
            resetButtonPanel.width = parentPanel.width - 10;

            var resetButton = resetButtonPanel.AddUIComponent<UIButton>();
            resetButton.text = "Reset to Default";
            resetButton.tooltip = "Reset all passenger limits to default values";
            resetButton.textScale = 0.9f;
            resetButton.width = 150f;
            resetButton.height = 25f;
            resetButton.normalBgSprite = "ButtonMenu";
            resetButton.hoveredBgSprite = "ButtonMenuHovered";
            resetButton.pressedBgSprite = "ButtonMenuPressed";
            resetButton.disabledBgSprite = "ButtonMenuDisabled";
            resetButton.eventClick += (c, e) =>
            {
                try
                {
                    foreach (var sliderData in sliders)
                    {
                        sliderData.Slider.value = sliderData.DefaultValue;
                        sliderData.ValueLabel.text = sliderData.DefaultValue.ToString();
                        sliderData.Setter(sliderData.DefaultValue);
                    }
                    OptionsWrapper<Settings.Settings>.SaveOptions();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"IPT: Reset SaS error: {ex.Message}");
                }
            };
        }

        private static void AddSasSlider(UIPanel parentPanel, string locKeyLabel, Func<int> getter, Action<int> setter, float min, float max, float step, int defaultValue)
        {
            try
            {
                int current = getter();

                var rowPanel = parentPanel.AddUIComponent<UIPanel>();
                rowPanel.autoLayout = true;
                rowPanel.autoLayoutDirection = LayoutDirection.Horizontal;
                rowPanel.autoLayoutPadding = new RectOffset(5, 5, 0, 0);
                rowPanel.autoLayoutStart = LayoutStart.TopLeft;
                rowPanel.height = 30f;
                rowPanel.width = parentPanel.width - 10;

                var labelCtrl = rowPanel.AddUIComponent<UILabel>();
                labelCtrl.text = Localization.Get(locKeyLabel);
                labelCtrl.textScale = 0.8f;
                labelCtrl.autoSize = false;
                labelCtrl.width = 160f;
                labelCtrl.height = 25f;
                labelCtrl.verticalAlignment = UIVerticalAlignment.Middle;

                var slider = rowPanel.AddUIComponent<UISlider>();
                slider.width = 330f;
                slider.height = 17f;
                slider.minValue = min;
                slider.maxValue = max;
                slider.stepSize = step;
                slider.value = Mathf.Clamp(current, min, max);
                slider.atlas = UITextures.InGameAtlas;
                slider.backgroundSprite = "TextFieldPanel";
                slider.color = new Color32(100, 100, 100, 255);

                // Thumb marker
                var thumb = slider.AddUIComponent<UISlicedSprite>();
                thumb.atlas = UITextures.InGameAtlas;
                thumb.spriteName = "SliderBudget";
                thumb.width = 16f;
                thumb.height = slider.height + 4f;
                slider.thumbObject = thumb;

                var valueLabel = rowPanel.AddUIComponent<UILabel>();
                valueLabel.text = current.ToString();
                valueLabel.textScale = 0.8f;
                valueLabel.autoSize = false;
                valueLabel.width = 50f;
                valueLabel.height = 25f;
                valueLabel.verticalAlignment = UIVerticalAlignment.Middle;
                valueLabel.textAlignment = UIHorizontalAlignment.Right;

                slider.eventValueChanged += (c, val) =>
                {
                    try
                    {
                        int iv = (int)val;
                        valueLabel.text = iv.ToString();
                        setter(iv);
                        OptionsWrapper<Settings.Settings>.SaveOptions();
                    }
                    catch { }
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"IPT: SaS slider error ({locKeyLabel}): {ex.Message}");
            }
        }

        private static void AddSasSliderWithRef(UIPanel parentPanel, string locKeyLabel, Func<int> getter, Action<int> setter, float min, float max, float step, int defaultValue, List<SliderResetData> sliderRefs)
        {
            try
            {
                int current = getter();

                var rowPanel = parentPanel.AddUIComponent<UIPanel>();
                rowPanel.autoLayout = true;
                rowPanel.autoLayoutDirection = LayoutDirection.Horizontal;
                rowPanel.autoLayoutPadding = new RectOffset(5, 5, 0, 0);
                rowPanel.autoLayoutStart = LayoutStart.TopLeft;
                rowPanel.height = 30f;
                rowPanel.width = parentPanel.width - 10;

                var labelCtrl = rowPanel.AddUIComponent<UILabel>();
                labelCtrl.text = Localization.Get(locKeyLabel);
                labelCtrl.textScale = 0.8f;
                labelCtrl.autoSize = false;
                labelCtrl.width = 160f;
                labelCtrl.height = 25f;
                labelCtrl.verticalAlignment = UIVerticalAlignment.Middle;

                var slider = rowPanel.AddUIComponent<UISlider>();
                slider.width = 330f;
                slider.height = 17f;
                slider.minValue = min;
                slider.maxValue = max;
                slider.stepSize = step;
                slider.value = Mathf.Clamp(current, min, max);
                slider.atlas = UITextures.InGameAtlas;
                slider.backgroundSprite = "TextFieldPanel";
                slider.color = new Color32(100, 100, 100, 255);

                // Thumb marker
                var thumb = slider.AddUIComponent<UISlicedSprite>();
                thumb.atlas = UITextures.InGameAtlas;
                thumb.spriteName = "SliderBudget";
                thumb.width = 16f;
                thumb.height = slider.height + 4f;
                slider.thumbObject = thumb;

                var valueLabel = rowPanel.AddUIComponent<UILabel>();
                valueLabel.text = current.ToString();
                valueLabel.textScale = 0.8f;
                valueLabel.autoSize = false;
                valueLabel.width = 50f;
                valueLabel.height = 25f;
                valueLabel.verticalAlignment = UIVerticalAlignment.Middle;
                valueLabel.textAlignment = UIHorizontalAlignment.Right;

                // Store reference for reset button
                sliderRefs.Add(new SliderResetData
                {
                    Slider = slider,
                    ValueLabel = valueLabel,
                    DefaultValue = defaultValue,
                    Setter = setter
                });

                slider.eventValueChanged += (c, val) =>
                {
                    try
                    {
                        int iv = (int)val;
                        valueLabel.text = iv.ToString();
                        setter(iv);
                        OptionsWrapper<Settings.Settings>.SaveOptions();
                    }
                    catch { }
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"IPT: SaS slider error ({locKeyLabel}): {ex.Message}");
            }
        }

        private static void AddAutoLineSection(UIHelperBase helper)
        {
            try
            {
                var group = helper.AddGroup(Localization.Get("SETTINGS_AUTO_LINE"));
                if (group == null) return;

                // Dropdown: Color Strategy
                var colorRow = (UIPanel)group.AddSpace(34);
                var colorDropdown = UI.AlgernonCommons.UIDropDowns.AddLabelledDropDown(colorRow, 0f, 2f, Localization.Get("AUTOLINECOLOR_COLOR_STRATEGY"), 220f, 25f);
                colorDropdown.items = new string[]
                {
                    Localization.Get("AUTOLINECOLOR_STRATEGY_RANDOM_HUE"),
                    Localization.Get("AUTOLINECOLOR_STRATEGY_RANDOM_COLOR"),
                    Localization.Get("AUTOLINECOLOR_STRATEGY_CATEGORISED"),
                    Localization.Get("AUTOLINECOLOR_STRATEGY_NAMED"),
                };
                colorDropdown.selectedIndex = OptionsWrapper<Settings.Settings>.Options.AutoLineColorColorStrategy;
                colorDropdown.eventSelectedIndexChanged += (c, idx) =>
                {
                    OptionsWrapper<Settings.Settings>.Options.AutoLineColorColorStrategy = idx;
                    OptionsWrapper<Settings.Settings>.SaveOptions();
                };
                try { colorDropdown.tooltip = Localization.Get("AUTOLINECOLOR_COLOR_STRATEGY_TOOLTIP"); } catch { }

                // Dropdown: Naming Strategy
                var namingRow = (UIPanel)group.AddSpace(34);
                var namingDropdown = UI.AlgernonCommons.UIDropDowns.AddLabelledDropDown(namingRow, 0f, 2f, Localization.Get("AUTOLINECOLOR_NAMING_STRATEGY"), 220f, 25f);
                namingDropdown.items = new string[]
                {
                    Localization.Get("AUTOLINECOLOR_NAMING_NONE"),
                    Localization.Get("AUTOLINECOLOR_NAMING_DISTRICTS"),
                    Localization.Get("AUTOLINECOLOR_NAMING_LONDON"),
                    Localization.Get("AUTOLINECOLOR_NAMING_ROADS"),
                    Localization.Get("AUTOLINECOLOR_NAMING_COLORS"),
                };
                namingDropdown.selectedIndex = OptionsWrapper<Settings.Settings>.Options.AutoLineColorNamingStrategyMode;
                namingDropdown.eventSelectedIndexChanged += (c, idx) =>
                {
                    OptionsWrapper<Settings.Settings>.Options.AutoLineColorNamingStrategyMode = idx;
                    OptionsWrapper<Settings.Settings>.SaveOptions();
                };
                try { namingDropdown.tooltip = Localization.Get("AUTOLINECOLOR_NAMING_STRATEGY_TOOLTIP"); } catch { }

                // Inline slider: Minimum Color Difference
                var minDiffRow = (UIPanel)group.AddSpace(30);
                PopulateSliderRow(minDiffRow, "AUTOLINECOLOR_MIN_COLOR_DIFF",
                    () => OptionsWrapper<Settings.Settings>.Options.AutoLineColorMinColorDiffPercentage,
                    v => { OptionsWrapper<Settings.Settings>.Options.AutoLineColorMinColorDiffPercentage = v; OptionsWrapper<Settings.Settings>.SaveOptions(); },
                    1f, 50f, 1f, Localization.Get("AUTOLINECOLOR_MIN_COLOR_DIFF_TOOLTIP"));

                // Inline slider: Maximum Color Pick Attempts
                var maxPickRow = (UIPanel)group.AddSpace(30);
                PopulateSliderRow(maxPickRow, "AUTOLINECOLOR_MAX_COLOR_PICK",
                    () => OptionsWrapper<Settings.Settings>.Options.AutoLineColorMaxDiffColorPickAttempt,
                    v => { OptionsWrapper<Settings.Settings>.Options.AutoLineColorMaxDiffColorPickAttempt = v; OptionsWrapper<Settings.Settings>.SaveOptions(); },
                    1f, 50f, 1f, Localization.Get("AUTOLINECOLOR_MAX_COLOR_PICK_TOOLTIP"));

                // Checkbox: Auto show line info (at the bottom)
                var checkbox = group.AddCheckbox(
                    Localization.Get("SETTINGS_AUTOSHOW_LINE_INFO"),
                    OptionsWrapper<Settings.Settings>.Options.ShowLineInfo,
                    v => { OptionsWrapper<Settings.Settings>.Options.ShowLineInfo = v; OptionsWrapper<Settings.Settings>.SaveOptions(); });
                try { if (checkbox is UICheckBox cb) cb.tooltip = Localization.Get("SETTINGS_AUTOSHOW_LINE_INFO_TOOLTIP"); } catch { }
            }
            catch (Exception ex)
            {
                Debug.LogError($"IPT: Failed to build Auto Line section: {ex.Message}");
            }
        }

        private static void PopulateSliderRow(UIPanel rowPanel, string locKeyLabel, Func<int> getter, Action<int> setter, float min, float max, float step, string tooltip = null)
        {
            try
            {
                rowPanel.autoLayout = true;
                rowPanel.autoLayoutDirection = LayoutDirection.Horizontal;
                rowPanel.autoLayoutPadding = new RectOffset(5, 2, 0, 0);
                rowPanel.autoLayoutStart = LayoutStart.TopLeft;

                var labelCtrl = rowPanel.AddUIComponent<UILabel>();
                labelCtrl.text = Localization.Get(locKeyLabel);
                labelCtrl.textScale = 0.8f;
                labelCtrl.autoSize = false;
                labelCtrl.width = 220f;
                labelCtrl.height = 25f;
                labelCtrl.verticalAlignment = UIVerticalAlignment.Middle;
                if (!string.IsNullOrEmpty(tooltip)) labelCtrl.tooltip = tooltip;

                var slider = rowPanel.AddUIComponent<UISlider>();
                slider.width = 180f;
                slider.height = 17f;
                slider.minValue = min;
                slider.maxValue = max;
                slider.stepSize = step;
                slider.value = Mathf.Clamp(getter(), min, max);
                slider.atlas = UITextures.InGameAtlas;
                slider.backgroundSprite = "TextFieldPanel";
                slider.color = new Color32(100, 100, 100, 255);
                if (!string.IsNullOrEmpty(tooltip)) slider.tooltip = tooltip;

                var thumb = slider.AddUIComponent<UISlicedSprite>();
                thumb.atlas = UITextures.InGameAtlas;
                thumb.spriteName = "SliderBudget";
                thumb.width = 16f;
                thumb.height = slider.height + 4f;
                slider.thumbObject = thumb;

                var valLabel = rowPanel.AddUIComponent<UILabel>();
                valLabel.text = getter().ToString();
                valLabel.textScale = 0.8f;
                valLabel.autoSize = false;
                valLabel.width = 50f;
                valLabel.height = 25f;
                valLabel.verticalAlignment = UIVerticalAlignment.Middle;
                valLabel.textAlignment = UIHorizontalAlignment.Right;

                var capturedLabel = valLabel;
                slider.eventValueChanged += (c, val) =>
                {
                    int iv = (int)val;
                    capturedLabel.text = iv.ToString();
                    setter(iv);
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"IPT: Slider row error ({locKeyLabel}): {ex.Message}");
            }
        }

        private static void AddUnbunchingSliders(UIPanel parentPanel)
        {
            try
            {
                // Store slider references for reset button
                UILabel aggressionValueLabel = null;
                UISlider aggressionSlider = null;
                UILabel vehicleCountValueLabel = null;
                UISlider vehicleCountSlider = null;
                UILabel spawnIntervalValueLabel = null;
                UISlider spawnIntervalSlider = null;

                // AC-style inline sliders for the 3 unbunching settings - temporarily store in locals
                AddSliderWithRef(parentPanel, "SETTINGS_UNBUNCHING_AGGRESSION", "SETTINGS_UNBUNCHING_AGGRESSION_TOOLTIP",
                    () => OptionsWrapper<Settings.Settings>.Options.IntervalAggressionFactor,
                    (v) => { OptionsWrapper<Settings.Settings>.Options.IntervalAggressionFactor = (byte)v; OptionsWrapper<Settings.Settings>.SaveOptions(); },
                    0f, 52f, 1f, ref aggressionSlider, ref aggressionValueLabel);

                AddSliderWithRef(parentPanel, "SETTINGS_VEHICLE_COUNT", "SETTINGS_VEHICLE_COUNT_TOOLTIP",
                    () => OptionsWrapper<Settings.Settings>.Options.DefaultVehicleCount,
                    (v) => { OptionsWrapper<Settings.Settings>.Options.DefaultVehicleCount = v; OptionsWrapper<Settings.Settings>.SaveOptions(); },
                    0f, 100f, 1f, ref vehicleCountSlider, ref vehicleCountValueLabel);

                // Store vehicle count slider reference globally for budget control toggling
                SettingsActions.VehicleCountSlider = vehicleCountSlider;
                
                // Apply current budget control state to the vehicle count slider
                var currentBudgetMode = OptionsWrapper<Settings.Settings>.Options.BudgetControl;
                SettingsActions.OnBudgetModeChanged(currentBudgetMode);

                AddSliderWithRef(parentPanel, "SETTINGS_SPAWN_TIME_INTERVAL", "SETTINGS_SPAWN_TIME_INTERVAL_TOOLTIP",
                    () => OptionsWrapper<Settings.Settings>.Options.SpawnTimeInterval,
                    (v) => { OptionsWrapper<Settings.Settings>.Options.SpawnTimeInterval = v; OptionsWrapper<Settings.Settings>.SaveOptions(); },
                    0f, 100f, 1f, ref spawnIntervalSlider, ref spawnIntervalValueLabel);

                // Reset button
                var resetButtonPanel = parentPanel.AddUIComponent<UIPanel>();
                resetButtonPanel.autoLayout = true;
                resetButtonPanel.autoLayoutDirection = LayoutDirection.Horizontal;
                resetButtonPanel.autoLayoutPadding = new RectOffset(5, 5, 10, 10);
                resetButtonPanel.autoLayoutStart = LayoutStart.TopLeft;
                resetButtonPanel.height = 35f;
                resetButtonPanel.width = parentPanel.width - 10;

                var resetButton = resetButtonPanel.AddUIComponent<UIButton>();
                resetButton.text = "Reset to Default";
                resetButton.tooltip = Localization.Get("SETTINGS_UNBUNCHING_RESET_BUTTON_TOOLTIP");
                resetButton.textScale = 0.9f;
                resetButton.width = 150f;
                resetButton.height = 25f;
                resetButton.normalBgSprite = "ButtonMenu";
                resetButton.hoveredBgSprite = "ButtonMenuHovered";
                resetButton.pressedBgSprite = "ButtonMenuPressed";
                resetButton.disabledBgSprite = "ButtonMenuDisabled";
                resetButton.eventClick += (c, e) =>
                {
                    try
                    {
                        OptionsWrapper<Settings.Settings>.Options.IntervalAggressionFactor = 15;
                        OptionsWrapper<Settings.Settings>.Options.DefaultVehicleCount = 50;
                        OptionsWrapper<Settings.Settings>.Options.SpawnTimeInterval = 40;
                        OptionsWrapper<Settings.Settings>.SaveOptions();

                        // Update slider displays
                        if (aggressionSlider != null) aggressionSlider.value = 15;
                        if (aggressionValueLabel != null) aggressionValueLabel.text = "15";
                        if (vehicleCountSlider != null) vehicleCountSlider.value = 50;
                        if (vehicleCountValueLabel != null) vehicleCountValueLabel.text = "50";
                        if (spawnIntervalSlider != null) spawnIntervalSlider.value = 40;
                        if (spawnIntervalValueLabel != null) spawnIntervalValueLabel.text = "40";
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"IPT: Reset unbunching error: {ex.Message}");
                    }
                };

            }
            catch (Exception ex)
            {
                Debug.LogWarning($"IPT: Unbunching sliders error: {ex.Message}");
            }
        }

        private static void AddSliderWithRef(UIPanel parentPanel, string locKeyLabel, string locKeyTooltip, Func<int> getter, Action<int> setter, float min, float max, float step, ref UISlider sliderRef, ref UILabel valueLabelRef)
        {
            try
            {
                int current = getter();

                var rowPanel = parentPanel.AddUIComponent<UIPanel>();
                rowPanel.autoLayout = true;
                rowPanel.autoLayoutDirection = LayoutDirection.Horizontal;
                rowPanel.autoLayoutPadding = new RectOffset(5, 5, 0, 0);
                rowPanel.autoLayoutStart = LayoutStart.TopLeft;
                rowPanel.height = 30f;
                rowPanel.width = parentPanel.width - 10;

                var labelCtrl = rowPanel.AddUIComponent<UILabel>();
                labelCtrl.text = Localization.Get(locKeyLabel);
                labelCtrl.textScale = 0.8f;
                labelCtrl.autoSize = false;
                labelCtrl.width = 160f;
                labelCtrl.height = 25f;
                labelCtrl.verticalAlignment = UIVerticalAlignment.Middle;
                if (!string.IsNullOrEmpty(locKeyTooltip))
                    labelCtrl.tooltip = Localization.Get(locKeyTooltip);

                sliderRef = rowPanel.AddUIComponent<UISlider>();
                sliderRef.width = 330f;
                sliderRef.height = 17f;
                sliderRef.minValue = min;
                sliderRef.maxValue = max;
                sliderRef.stepSize = step;
                sliderRef.value = Mathf.Clamp(current, min, max);
                sliderRef.atlas = UITextures.InGameAtlas;
                sliderRef.backgroundSprite = "TextFieldPanel";
                sliderRef.color = new Color32(100, 100, 100, 255);
                if (!string.IsNullOrEmpty(locKeyTooltip))
                    sliderRef.tooltip = Localization.Get(locKeyTooltip);

                // Thumb marker
                var thumb = sliderRef.AddUIComponent<UISlicedSprite>();
                thumb.atlas = UITextures.InGameAtlas;
                thumb.spriteName = "SliderBudget";
                thumb.width = 16f;
                thumb.height = sliderRef.height + 4f;
                sliderRef.thumbObject = thumb;

                valueLabelRef = rowPanel.AddUIComponent<UILabel>();
                valueLabelRef.text = current.ToString();
                valueLabelRef.textScale = 0.8f;
                valueLabelRef.autoSize = false;
                valueLabelRef.width = 50f;
                valueLabelRef.height = 25f;
                valueLabelRef.verticalAlignment = UIVerticalAlignment.Middle;
                valueLabelRef.textAlignment = UIHorizontalAlignment.Right;

                // Capture the local reference for the lambda
                var localValueLabel = valueLabelRef;
                sliderRef.eventValueChanged += (c, val) =>
                {
                    try
                    {
                        int iv = (int)val;
                        localValueLabel.text = iv.ToString();
                        setter(iv);
                    }
                    catch { }
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"IPT: Unbunching slider error: {ex.Message}");
            }
        }


    }
}
