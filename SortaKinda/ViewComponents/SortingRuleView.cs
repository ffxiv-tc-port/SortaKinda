using System;
using System.Drawing;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using KamiLib.Classes;
using KamiLib.Extensions;
using KamiLib.Window;
using KamiLib.Window.SelectionWindows;
using Lumina.Excel.Sheets;
using SortaKinda.Classes;

namespace SortaKinda.ViewComponents;

public class SortingRuleView(SortingRule rule) {
    private readonly TabBar tabBar = new("SortingRuleTabBar", [
        new ItemTypeFilterTab.ItemNameFilterTab(rule),
        new ItemTypeFilterTab(rule),
        new OtherFiltersTab(rule),
        new ToggleFiltersTab(rule),
        new SortOrderTab(rule),
    ], false);

    public void Draw() => tabBar.Draw();
}

public class ItemTypeFilterTab(SortingRule rule) : IOneColumnRuleConfigurationTab {
    public string Name => "物品類型篩選";
    
    public string FirstLabel => "允許的物品類型";

    public bool Disabled => false;

    public SortingRule SortingRule { get; } = rule;

    public void DrawContents() {
        DrawSelectedTypes();
        
        if (ImGuiTweaks.IconButtonWithSize(Service.PluginInterface.UiBuilder.IconFontFixedWidthHandle, FontAwesomeIcon.Plus, "openItemTypeSelect", ImGui.GetContentRegionAvail())) {
            System.WindowManager.AddWindow(new ItemUiCategorySelectionWindow(Service.PluginInterface) {
                MultiSelectionCallback = selections => {
                    foreach (var selected in selections) {
                        SortingRule.AllowedItemTypes.Add(selected.RowId);
                    }
                },
            }, WindowFlags.OpenImmediately);
        }
    }

    public class ItemNameFilterTab(SortingRule rule) : IOneColumnRuleConfigurationTab {
    private UserRegex newRegex = new();
    private bool setNameFocus = true;

    public string Name => "物品名稱篩選";
    
    public bool Disabled => false;
    
    public string FirstLabel => "允許的物品名稱";
    
    public SortingRule SortingRule { get; } = rule;

    public void DrawContents() {
        DrawFilteredNames();
        DrawAddItemNameInput();
    }

    private void DrawFilteredNames() {
        UserRegex? removalRegex = null;

        using var child = ImRaii.Child("##NameFilterChild", ImGuiHelpers.ScaledVector2(0.0f, -50.0f));
        if (!child) return;
        
        if (SortingRule.AllowedNameRegexes.Count is 0) {
            ImGui.TextColored(KnownColor.Orange.Vector(), "未設定篩選條件");
        }

        foreach (var userRegex in SortingRule.AllowedNameRegexes) {
            if (ImGuiComponents.IconButton($"##RemoveNameRegex{userRegex.Text}", FontAwesomeIcon.Trash)) {
                removalRegex = userRegex;
            }
            ImGui.SameLine();
            ImGui.TextUnformatted(userRegex.Text);
        }

        if (removalRegex is { } toRemoveRegex) {
            SortingRule.AllowedNameRegexes.Remove(toRemoveRegex);
        }
    }

    private void DrawAddItemNameInput() {
        var buttonSize = ImGuiHelpers.ScaledVector2(25.0f, 23.0f);

        if (setNameFocus || ImGui.IsWindowAppearing()) {
            ImGui.SetKeyboardFocusHere();
            setNameFocus = false;
        }

        ImGui.TextColored(KnownColor.Gray.Vector(), "物品名稱篩選支援正規表示式");

        if (UserRegex.DrawRegexInput("##NewName", ref newRegex, "物品名稱", null, ImGui.GetContentRegionAvail().X - buttonSize.X - ImGui.GetStyle().ItemSpacing.X, ImGui.GetColorU32(KnownColor.OrangeRed.Vector()))) {
            if (newRegex.Regex is not null) {
                SortingRule.AllowedNameRegexes.Add(newRegex);
                newRegex = new UserRegex();
            }
            setNameFocus = true;
        }

        ImGui.SameLine();

        using var disabled = ImRaii.Disabled(newRegex.Regex is null || newRegex.Text.IsNullOrEmpty());
        if (ImGuiTweaks.IconButtonWithSize(Service.PluginInterface.UiBuilder.IconFontFixedWidthHandle, FontAwesomeIcon.Plus, "AddNameButton", buttonSize, "新增名稱")) {
            if (newRegex.Regex is not null) {
                SortingRule.AllowedNameRegexes.Add(newRegex);
                newRegex = new UserRegex();
            }
        }
    }
}
    
    private void DrawSelectedTypes() {
        uint? removalEntry = null;

        using var itemFilterChild = ImRaii.Child("##ItemFilterChild", ImGuiHelpers.ScaledVector2(0.0f, -30.0f));
        if (!itemFilterChild) return;
        
        if (SortingRule.AllowedItemTypes.Count is 0) {
            ImGui.TextColored(KnownColor.Orange.Vector(), "未設定篩選條件");
        }
        
        foreach (var category in SortingRule.AllowedItemTypes) {
            if (Service.DataManager.GetExcelSheet<ItemUICategory>().GetRow(category) is not { RowId: not 0, Icon: var iconCategory, Name: var entryName }) continue;
            if (Service.TextureProvider.GetFromGameIcon((uint) iconCategory) is not { } iconTexture) continue;

            if (ImGuiComponents.IconButton($"##RemoveButton{category}", FontAwesomeIcon.Trash)) {
                removalEntry = category;
            }

            ImGui.SameLine();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 1.0f * ImGuiHelpers.GlobalScale);
            ImGui.Image(iconTexture.GetWrapOrEmpty().Handle, ImGuiHelpers.ScaledVector2(20.0f, 20.0f));

            ImGui.SameLine();
            ImGui.TextUnformatted(entryName.ExtractText());
        }
        
        if (removalEntry is { } toRemove) {
            SortingRule.AllowedItemTypes.Remove(toRemove);
        }
    }
}

public class OtherFiltersTab(SortingRule rule) : ITwoColumnRuleConfigurationTab {
    public string Name => "其他篩選";
    
    public bool Disabled => false;
    
    public SortingRule SortingRule { get; } = rule;
    
    public string FirstLabel => "範圍篩選";
    
    public string SecondLabel => "物品稀有度篩選";

    public void DrawLeftSideContents() {
        SortingRule.LevelFilter.DrawConfig();
        SortingRule.ItemLevelFilter.DrawConfig();
        SortingRule.VendorPriceFilter.DrawConfig();
    }

    public void DrawRightSideContents() {
        foreach (var enumValue in Enum.GetValues<ItemRarity>()) {
            var enabled = SortingRule.AllowedItemRarities.Contains(enumValue);
            if (ImGuiComponents.ToggleButton($"{enumValue.GetDescription()}", ref enabled)) {
                if (enabled) SortingRule.AllowedItemRarities.Add(enumValue);
                if (!enabled) SortingRule.AllowedItemRarities.Remove(enumValue);
            }

            ImGui.SameLine();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3.0f);
            ImGui.TextUnformatted(enumValue.GetDescription());
        }
    }
}

public class ToggleFiltersTab(SortingRule rule) : IOneColumnRuleConfigurationTab {
    public string Name => "屬性篩選";
    
    public bool Disabled => false;
    
    public SortingRule SortingRule { get; } = rule;
    
    public string FirstLabel => "屬性篩選";
    
    public void DrawContents() {
        SortingRule.UntradableFilter.DrawConfig();
        SortingRule.UniqueFilter.DrawConfig();
        SortingRule.DyeableFilter.DrawConfig();
        SortingRule.CollectableFilter.DrawConfig();
        SortingRule.RepairableFilter.DrawConfig();
    }
}

public class SortOrderTab(SortingRule rule) : ITwoColumnRuleConfigurationTab {
    public string Name => "排序順序";
    
    public bool Disabled => false;
    
    public SortingRule SortingRule { get; } = rule;
    
    public string FirstLabel => "排序依據";
    
    public string SecondLabel => "排序選項";

    public void DrawLeftSideContents() {
        ImGui.Text("物品排序依據");
        ImGuiComponents.HelpMarker("用來決定排序順序的主要物品屬性");
        var sortMode = SortingRule.SortMode;
        DrawRadioEnum(ref sortMode);

        SortingRule.SortMode = sortMode;
    }

    public void DrawRightSideContents() {
        ImGui.Text("排序方向");
        ImGuiComponents.HelpMarker("遞增：A → Z\n遞減：Z → A");
        var sortDirection = SortingRule.Direction;
        DrawRadioEnum(ref sortDirection);

        ImGuiHelpers.ScaledDummy(8.0f);
        ImGui.Text("物品欄填入方向");
        ImGuiComponents.HelpMarker("頂端：物品移至左上方欄位\n底端：物品移至右下方欄位");
        var fillMode = SortingRule.FillMode;
        DrawRadioEnum(ref fillMode);

        SortingRule.Direction = sortDirection;
        SortingRule.FillMode = fillMode;
    }

    private static void DrawRadioEnum<T>(ref T configValue) where T : Enum {
        foreach (Enum value in Enum.GetValues(configValue.GetType())) {
            var isSelected = Convert.ToInt32(configValue);
            if (ImGui.RadioButton($"{value.GetDescription()}##{configValue.GetType()}", ref isSelected, Convert.ToInt32(value))) {
                configValue = (T) value;
            }
        }
    }
}
