using System;
using System.ComponentModel;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using KamiLib.Extensions;
using Lumina.Excel.Sheets;

namespace SortaKinda.Classes;

public class ToggleFilter(PropertyFilter filter, ToggleFilterState state = ToggleFilterState.Ignored) {
    public ToggleFilterState State = state;
    public PropertyFilter Filter = filter;

    public void DrawConfig() {
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3.0f * ImGuiHelpers.GlobalScale);
        ImGui.TextUnformatted(Filter.GetDescription());
        
        ImGui.SameLine(ImGui.GetContentRegionMax().X / 2.0f);
        
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 3.0f * ImGuiHelpers.GlobalScale);
        ImGui.PushItemWidth(ImGui.GetContentRegionMax().X / 2.0f);
        using var combo = ImRaii.Combo($"##{Filter.ToString()}Combo", State.GetDescription());
        if (!combo) return;
        
        foreach(var value in Enum.GetValues<ToggleFilterState>()) {
            if (ImGui.Selectable(value.GetDescription(), value == State)) {
                State = value;
            }
        }
    }

    public bool IsItemSlotAllowed(InventorySlot slot) => State switch {
        ToggleFilterState.Ignored => false,
        ToggleFilterState.Allow => ItemHasProperty(slot.ExdItem),
        ToggleFilterState.Disallow => !ItemHasProperty(slot.ExdItem),
        _ => true,
    };

    private bool ItemHasProperty(Item item) =>  Filter switch {
        PropertyFilter.Collectable when item.IsCollectable => true,
        PropertyFilter.Dyeable when item.DyeCount > 0 => true,
        PropertyFilter.Unique when item.IsUnique => true,
        PropertyFilter.Untradable when item.IsUntradable => true,
        PropertyFilter.Repairable when item.ItemRepair.RowId is not 0 => true,
        _ => false,
    };
}

public enum PropertyFilter {
    [Description("不可交易")]
    Untradable,
    
    [Description("可染色")]
    Dyeable,
    
    [Description("唯一物品")]
    Unique,
    
    [Description("收藏品")]
    Collectable,
    
    [Description("可修理")]
    Repairable,
}

public enum ToggleFilterState {
    [Description("忽略")]
    Ignored,
    
    [Description("允許")]
    Allow,
    
    [Description("不允許")]
    Disallow,
}
