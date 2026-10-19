using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;
using KamiLib.Classes;
using Lumina.Excel.Sheets;
using SortaKinda.Controllers;
using SortaKinda.ViewComponents;

namespace SortaKinda.Classes;

public unsafe class SortingRule : IComparer<InventorySlot>{
    private readonly SortingRuleTooltipView view;
    private readonly List<SortingFilter> filterRules;

    public SortingRule() {
        view = new SortingRuleTooltipView(this);
        filterRules = new List<SortingFilter> {
            new() {
                Active = () => AllowedNameRegexes.Count != 0,
                IsSlotAllowed = slot => {
                    foreach (var allowedRegex in AllowedNameRegexes) {
                        if (slot is { ExdItem: { Name: var itemName, RowId: not 0 } }) {
                            if (allowedRegex.Match(itemName.ExtractText())) return true;
                        }
                    }

                    return false;
                },
            },
            new() {
                Active = () => AllowedItemTypes.Count != 0,
                IsSlotAllowed = slot => AllowedItemTypes.Any(allowed => slot.ExdItem.ItemUICategory.RowId == allowed),
            },
            new() {
                Active = () => AllowedItemRarities.Count != 0,
                IsSlotAllowed = slot => AllowedItemRarities.Any(allowed => slot.ExdItem.Rarity == (byte) allowed),
            },
            new() {
                Active = () => LevelFilter.Enable,
                IsSlotAllowed = slot => LevelFilter.IsItemSlotAllowed(slot.ExdItem.LevelEquip),
            },
            new() {
                Active = () => ItemLevelFilter.Enable,
                IsSlotAllowed = slot => ItemLevelFilter.IsItemSlotAllowed(slot.ExdItem.LevelItem.RowId),
            },
            new() {
                Active = () => VendorPriceFilter.Enable,
                IsSlotAllowed = slot => VendorPriceFilter.IsItemSlotAllowed(slot.ExdItem.PriceLow),
            },
            new() {
                Active = () => UntradableFilter.State is not ToggleFilterState.Ignored,
                IsSlotAllowed = slot => UntradableFilter.IsItemSlotAllowed(slot),
            },
            new() {
                Active = () => UniqueFilter.State is not ToggleFilterState.Ignored,
                IsSlotAllowed = slot => UniqueFilter.IsItemSlotAllowed(slot),
            },
            new() {
                Active = () => CollectableFilter.State is not ToggleFilterState.Ignored,
                IsSlotAllowed = slot => CollectableFilter.IsItemSlotAllowed(slot),
            },
            new() {
                Active = () => DyeableFilter.State is not ToggleFilterState.Ignored,
                IsSlotAllowed = slot => DyeableFilter.IsItemSlotAllowed(slot),
            },
            new() {
                Active = () => RepairableFilter.State is not ToggleFilterState.Ignored,
                IsSlotAllowed = slot => RepairableFilter.IsItemSlotAllowed(slot),
            }
        };
    }

    public Vector4 Color { get; set; }
    public string Id { get; set; } = SortController.DefaultId;
    public string Name { get; set; } = "新規則";
    public int Index { get; set; }
    public HashSet<UserRegex> AllowedNameRegexes { get; set; } = [];
    public HashSet<uint> AllowedItemTypes { get; set; } = [];
    public HashSet<ItemRarity> AllowedItemRarities { get; set; } = [];
    public RangeFilter LevelFilter { get; set; } = new("Level Filter", 0, 200);
    public RangeFilter ItemLevelFilter { get; set; } = new("Item Level Filter", 0, 1000);
    public RangeFilter VendorPriceFilter { get; set; } = new("Vendor Price Filter", 0, 1_000_000);
    public ToggleFilter UntradableFilter { get; set; } = new(PropertyFilter.Untradable);
    public ToggleFilter UniqueFilter { get; set; } = new(PropertyFilter.Unique);
    public ToggleFilter CollectableFilter { get; set; } = new(PropertyFilter.Collectable);
    public ToggleFilter DyeableFilter { get; set; } = new(PropertyFilter.Dyeable);
    public ToggleFilter RepairableFilter { get; set; } = new(PropertyFilter.Repairable);
    public SortOrderDirection Direction { get; set; } = SortOrderDirection.Ascending;
    public FillMode FillMode { get; set; } = FillMode.Top;
    public SortOrderMode SortMode { get; set; } = SortOrderMode.Alphabetically;

    // 舊設定檔沒有這個鍵＝空清單＝排序與加入此功能前相同。
    // 讀檔時 null 清單與 null 項目在這裡丟掉，比較器與介面因此不判 null。
    public List<AdditionalSortRule> AdditionalSortRules {
        get => additionalSortRules;
        set => additionalSortRules = value is null ? [] : value.FindAll(static tier => tier is not null);
    }
    private List<AdditionalSortRule> additionalSortRules = [];

    public bool InclusiveAnd = false;

    public void ShowTooltip() {
        view.Draw();
    }

    public int Compare(InventorySlot? x, InventorySlot? y) {
        if (x is null) return 0;
        if (y is null) return 0;
        if (x.ExdItem.RowId is 0) return 0;
        if (y.ExdItem.RowId is 0) return 0;
        if (IsSortModeChainMatch(x.ExdItem, y.ExdItem)) return 0;
        if (CompareSlots(x, y)) return 1;
        return -1;
    }

    public bool IsItemSlotAllowed(InventorySlot slot) => InclusiveAnd ? 
        filterRules.Any(rule => rule.Active() && rule.IsSlotAllowed(slot)) : 
        filterRules.All(rule => !rule.Active() || rule.Active() && rule.IsSlotAllowed(slot));

    public bool CompareSlots(InventorySlot a, InventorySlot b) {
        var firstItem = a.ExdItem;
        var secondItem = b.ExdItem;

        switch (a.HasItem, b.HasItem) {
            // If both items are null, don't swap
            case (false, false): return false;

            // first slot empty, second slot full, if Ascending we want to leave justify, move the items left, if Descending right justify, leave the empty slot on the left.
            case (false, true): return FillMode is FillMode.Top;

            // first slot full, second slot empty, if Ascending we want to leave justify, and we have that already, if Descending right justify, move the item right
            case (true, false): return FillMode is FillMode.Bottom;

            case (true, true) when firstItem.RowId is not 0 && secondItem.RowId is not 0:
                
                var shouldSwap = false;
                
                // They are the same item
                if (firstItem.RowId == secondItem.RowId) {
                    // if left is not HQ, and right is HQ, swap
                    if (!a.InventoryItem->Flags.HasFlag(InventoryItem.ItemFlags.HighQuality) && b.InventoryItem->Flags.HasFlag(InventoryItem.ItemFlags.HighQuality)) {
                        shouldSwap = true;
                    }
                    // else if left has lower quantity then right, swap
                    else if (a.InventoryItem->Quantity < b.InventoryItem->Quantity) {
                        shouldSwap = true;
                    }
                }
                // else they are not the same item: each sort tier applies its own direction
                else {
                    return ShouldSwapBySortChain(firstItem, secondItem);
                }
                
                return Direction is SortOrderDirection.Descending ? !shouldSwap : shouldSwap;

            // Something went horribly wrong... best not touch it and walk away.
            default: return false;
        }
    }
    
    // 第一層是 SortMode／Direction，之後依序是 AdditionalSortRules；全部相同時照舊依名稱、方向跟第一層。
    private bool ShouldSwapBySortChain(Item firstItem, Item secondItem) {
        if (!IsSortMatch(firstItem, secondItem, SortMode)) {
            return ApplyDirection(ShouldSwap(firstItem, secondItem, SortMode), Direction);
        }

        foreach (var sortRule in AdditionalSortRules) {
            if (IsSortMatch(firstItem, secondItem, sortRule.Mode)) continue;

            return ApplyDirection(ShouldSwap(firstItem, secondItem, sortRule.Mode), sortRule.Direction);
        }

        return ApplyDirection(ShouldSwap(firstItem, secondItem, SortOrderMode.Alphabetically), Direction);
    }

    private bool IsSortModeChainMatch(Item firstItem, Item secondItem) {
        if (!IsSortMatch(firstItem, secondItem, SortMode)) return false;

        foreach (var sortRule in AdditionalSortRules) {
            if (!IsSortMatch(firstItem, secondItem, sortRule.Mode)) return false;
        }

        return true;
    }

    private static bool ApplyDirection(bool shouldSwap, SortOrderDirection direction)
        => direction is SortOrderDirection.Descending ? !shouldSwap : shouldSwap;

    private static bool IsSortMatch(Item firstItem, Item secondItem, SortOrderMode sortMode) => sortMode switch {
        SortOrderMode.ItemId => firstItem.RowId == secondItem.RowId,
        SortOrderMode.ItemLevel => firstItem.LevelItem.RowId == secondItem.LevelItem.RowId,
        SortOrderMode.Alphabetically => string.Equals(firstItem.Name.ExtractText(), secondItem.Name.ExtractText(), StringComparison.OrdinalIgnoreCase),
        SortOrderMode.SellPrice => firstItem.PriceLow == secondItem.PriceLow,
        SortOrderMode.Rarity => firstItem.Rarity == secondItem.Rarity,
        SortOrderMode.ItemType => firstItem.ItemUICategory.RowId == secondItem.ItemUICategory.RowId,
        SortOrderMode.Level => firstItem.LevelEquip == secondItem.LevelEquip,
        _ => false,
    };

    private static bool ShouldSwap(Item firstItem, Item secondItem, SortOrderMode sortMode) => sortMode switch {
        SortOrderMode.ItemId => firstItem.RowId > secondItem.RowId,
        SortOrderMode.ItemLevel => firstItem.LevelItem.RowId > secondItem.LevelItem.RowId,
        SortOrderMode.Alphabetically => string.Compare(firstItem.Name.ExtractText(), secondItem.Name.ExtractText(), StringComparison.OrdinalIgnoreCase) > 0,
        SortOrderMode.SellPrice => firstItem.PriceLow > secondItem.PriceLow,
        SortOrderMode.Rarity => firstItem.Rarity > secondItem.Rarity,
        SortOrderMode.ItemType => ShouldSwapItemUiCategory(firstItem, secondItem),
        SortOrderMode.Level => firstItem.LevelEquip > secondItem.LevelEquip,
        _ => false,
    };

    private static bool ShouldSwapItemUiCategory(Item firstItem, Item secondItem) {
        // If same category, don't swap, other system handles fallback to alphabetical in this case
        if (firstItem.ItemUICategory.RowId == secondItem.ItemUICategory.RowId) return false;

        if (firstItem is { RowId: not 0, ItemUICategory.Value: var first } && secondItem is { RowId: not 0, ItemUICategory.Value: var second }) {
            if (first.OrderMajor == second.OrderMajor) {
                return first.OrderMinor > second.OrderMinor;
            }

            return first.OrderMajor > second.OrderMajor;
        }

        return false;
    }
}

public enum FillMode {
    [Description("頂端")]
    Top,

    [Description("底端")]
    Bottom,
}

public enum ItemRarity {
    [Description("白色")]
    White = 1,

    [Description("綠色")]
    Green = 2,

    [Description("藍色")]
    Blue = 3,

    [Description("紫色")]
    Purple = 4,

    [Description("粉紅色")]
    Pink = 7,
}

public enum SortOrderDirection {
    [Description("遞增")]
    Ascending,

    [Description("遞減")]
    Descending,
}

public enum SortOrderMode {
    [Description("名稱")]
    Alphabetically,
    
    [Description("品級")]
    ItemLevel,

    [Description("稀有度")]
    Rarity,

    [Description("出售價格")]
    SellPrice,

    [Description("物品 ID")]
    ItemId,
    
    [Description("物品類型")]
    ItemType,
    
    [Description("裝備等級")]
    Level,
}

public class SortingFilter {
    public required Func<bool> Active { get; init; }
    
    public required Func<InventorySlot, bool> IsSlotAllowed { get; init; }
}

// 欄位名與上游 MidoriKami/SortaKinda 2.2.x 相同，匯入上游匯出的規則代碼時才讀得到。
public class AdditionalSortRule {
    public SortOrderMode Mode { get; set; } = SortOrderMode.ItemId;
    public SortOrderDirection Direction { get; set; } = SortOrderDirection.Ascending;
}
