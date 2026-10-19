using System;
using System.Drawing;
using System.Numerics;
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
        new TieBreakerTab(rule),
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
        if (ImGuiTweaks.IconButtonWithSize(Service.PluginInterface.UiBuilder.IconFontFixedWidthHandle, FontAwesomeIcon.Plus, "AddNameButton", buttonSize)) {
            if (newRegex.Regex is not null) {
                SortingRule.AllowedNameRegexes.Add(newRegex);
                newRegex = new UserRegex();
            }
        }

        // 說明在這裡自己畫：KamiLib 的 IconButtonWithSize 在停用區段裡打開說明框不推回 Alpha，會跟著變淡。
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
            using var tooltipAlpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha, 1.0f);
            ImGui.SetTooltip("新增名稱");
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
        ImGuiComponents.HelpMarker("用來決定排序順序的主要物品屬性\n依據相同的物品要再怎麼排，在「次要排序」分頁設定");
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

public class TieBreakerTab(SortingRule rule) : IRuleConfigurationTab {
    private static readonly SortOrderMode[] AllSortModes = Enum.GetValues<SortOrderMode>();
    private const string TierNumerals = "一二三四五六七八九十";

    // 「新增條件」那一列的暫存選擇，按下新增才寫進規則。
    private SortOrderMode newTierMode = SortOrderMode.ItemId;
    private SortOrderDirection newTierDirection = SortOrderDirection.Ascending;

    public string Name => "次要排序";

    public bool Disabled => false;

    public SortingRule SortingRule { get; } = rule;

    // TabBar 關了捲動，條件多了會超出視窗，所以這一頁自己包一層可捲動的子視窗。
    public void DrawConfigurationTab() {
        using var scrollChild = ImRaii.Child("##TieBreakerScrollChild", Vector2.Zero);
        if (!scrollChild) return;

        var tiers = SortingRule.AdditionalSortRules;

        ImGui.TextUnformatted("次要排序條件");
        ImGuiComponents.HelpMarker("「排序順序」分頁的物品排序依據是第一層。\n" +
                                   "第一層相同的物品，再依這裡由上到下逐層比較，每一層各自設定遞增或遞減。\n" +
                                   "每一層都相同時照舊依名稱排序（方向跟第一層）。\n\n" +
                                   "沒有設定次要排序條件時，排序結果與以前完全相同。");
        ImGui.Separator();

        var moveUpIndex = -1;
        var moveDownIndex = -1;
        var removeIndex = -1;

        using (var table = ImRaii.Table("##TieBreakerTable", 2, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.BordersInnerV)) {
            if (!table) return;

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextColored(KnownColor.Gray.Vector(), $"第一層：{SortingRule.SortMode.GetDescription()}");
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("第一層在「排序順序」分頁設定");
            }

            ImGui.TableNextColumn();
            ImGui.TextColored(KnownColor.Gray.Vector(), SortingRule.Direction.GetDescription());

            if (tiers.Count is 0) {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextColored(KnownColor.Orange.Vector(), "未設定次要排序條件");
            }

            for (var index = 0; index < tiers.Count; index++) {
                using var id = ImRaii.PushId(index);
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                using (ImRaii.Disabled(index is 0)) {
                    if (ImGuiComponents.IconButton("##TierUp", FontAwesomeIcon.ArrowUp)) moveUpIndex = index;
                }

                ImGui.SameLine();
                using (ImRaii.Disabled(index == tiers.Count - 1)) {
                    if (ImGuiComponents.IconButton("##TierDown", FontAwesomeIcon.ArrowDown)) moveDownIndex = index;
                }

                ImGui.SameLine();
                if (ImGuiComponents.IconButton("##TierRemove", FontAwesomeIcon.Trash)) removeIndex = index;

                ImGui.SameLine();
                DrawTierLabel(index);

                ImGui.TableNextColumn();
                var direction = tiers[index].Direction;
                DrawDirectionPair(ref direction);
                tiers[index].Direction = direction;
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Separator();
            ImGui.TableNextColumn();
            ImGui.Separator();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1.0f);
            using (var combo = ImRaii.Combo("##NewTierMode", newTierMode.GetDescription())) {
                if (combo) {
                    foreach (var mode in AllSortModes) {
                        if (ImGui.Selectable(mode.GetDescription(), mode == newTierMode)) newTierMode = mode;
                    }
                }
            }

            ImGui.TableNextColumn();
            DrawDirectionPair(ref newTierDirection);
            ImGui.SameLine();
            if (ImGui.Button("新增條件")) {
                tiers.Add(new AdditionalSortRule { Mode = newTierMode, Direction = newTierDirection });
            }
        }

        // 畫完整張表才改清單，避免在走訪途中增刪。
        if (moveUpIndex > 0) {
            (tiers[moveUpIndex - 1], tiers[moveUpIndex]) = (tiers[moveUpIndex], tiers[moveUpIndex - 1]);
        }

        if (moveDownIndex >= 0 && moveDownIndex < tiers.Count - 1) {
            (tiers[moveDownIndex], tiers[moveDownIndex + 1]) = (tiers[moveDownIndex + 1], tiers[moveDownIndex]);
        }

        if (removeIndex >= 0 && removeIndex < tiers.Count) {
            tiers.RemoveAt(removeIndex);
        }
    }

    private void DrawTierLabel(int index) {
        var tierNumber = index + 2;
        var tierName = tierNumber <= TierNumerals.Length ? $"第{TierNumerals[tierNumber - 1]}層" : $"第 {tierNumber} 層";
        var label = $"{tierName}：{SortingRule.AdditionalSortRules[index].Mode.GetDescription()}";

        if (IsTierEffective(index)) {
            ImGui.TextUnformatted(label);
            return;
        }

        ImGui.TextColored(KnownColor.Gray.Vector(), label);
        if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip("這一層不會影響排序：前面已有相同的排序依據，或前面已有「物品 ID」（每種物品的 ID 都不同）。");
        }
    }

    // 與比較器同一套規則：前面某層已是同一個依據、或前面有「物品 ID」時，走不到這一層。
    private bool IsTierEffective(int index) {
        var tiers = SortingRule.AdditionalSortRules;
        var mode = tiers[index].Mode;
        if (SortingRule.SortMode is SortOrderMode.ItemId || SortingRule.SortMode == mode) return false;

        for (var earlier = 0; earlier < index; earlier++) {
            if (tiers[earlier].Mode is SortOrderMode.ItemId || tiers[earlier].Mode == mode) return false;
        }

        return true;
    }

    private static void DrawDirectionPair(ref SortOrderDirection direction) {
        if (ImGui.RadioButton($"{SortOrderDirection.Ascending.GetDescription()}##TierAscending", direction is SortOrderDirection.Ascending)) {
            direction = SortOrderDirection.Ascending;
        }

        ImGui.SameLine();
        if (ImGui.RadioButton($"{SortOrderDirection.Descending.GetDescription()}##TierDescending", direction is SortOrderDirection.Descending)) {
            direction = SortOrderDirection.Descending;
        }
    }
}
