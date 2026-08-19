using System;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace SortaKinda.Controllers;

public static unsafe class InventoryController {
    public static int GetInventoryPageSize(InventoryType type) {
        var sorter = GetInventorySorter(type);
        return sorter != null ? sorter->ItemsPerPage : 0;
    }

    public static InventoryItem* GetItemForSlot(InventoryType type, int slot) {
        var itemOrderData = GetItemOrderData(type, slot);
        var inventoryManager = InventoryManager.Instance();
        if (itemOrderData is null || inventoryManager is null) return null;

        var container = inventoryManager->GetInventoryContainer(GetAdjustedInventoryType(type) + itemOrderData->Page);
        return container is null ? null : container->GetInventorySlot(itemOrderData->Slot);
    }

    public static ItemOrderModuleSorterItemEntry* GetItemOrderData(InventoryType type, int slot) {
        var sorter = GetInventorySorter(type);
        if (sorter == null || sorter->Items.First == null || slot < 0 || slot >= sorter->ItemsPerPage) return null;

        var adjustedSlot = slot + GetInventoryStartIndex(type);
        return adjustedSlot < sorter->Items.Count ? sorter->Items[adjustedSlot] : null;
    }
    
    private static ItemOrderModuleSorter* GetInventorySorter(InventoryType type) {
        var itemOrderModule = ItemOrderModule.Instance();
        if (itemOrderModule == null) return null;

        return type switch {
            InventoryType.Inventory1 => itemOrderModule->InventorySorter,
            InventoryType.Inventory2 => itemOrderModule->InventorySorter,
            InventoryType.Inventory3 => itemOrderModule->InventorySorter,
            InventoryType.Inventory4 => itemOrderModule->InventorySorter,
            InventoryType.ArmoryMainHand => itemOrderModule->ArmouryMainHandSorter,
            InventoryType.ArmoryOffHand => itemOrderModule->ArmouryOffHandSorter,
            InventoryType.ArmoryHead => itemOrderModule->ArmouryHeadSorter,
            InventoryType.ArmoryBody => itemOrderModule->ArmouryBodySorter,
            InventoryType.ArmoryHands => itemOrderModule->ArmouryHandsSorter,
            InventoryType.ArmoryLegs => itemOrderModule->ArmouryLegsSorter,
            InventoryType.ArmoryFeets => itemOrderModule->ArmouryFeetSorter,
            InventoryType.ArmoryEar => itemOrderModule->ArmouryEarsSorter,
            InventoryType.ArmoryNeck => itemOrderModule->ArmouryNeckSorter,
            InventoryType.ArmoryWrist => itemOrderModule->ArmouryWristsSorter,
            InventoryType.ArmoryRings => itemOrderModule->ArmouryRingsSorter,
            InventoryType.ArmorySoulCrystal => itemOrderModule->ArmourySoulCrystalSorter,
            _ => throw new Exception($"Type Not Implemented: {type}"),
        };
    }

    private static InventoryType GetAdjustedInventoryType(InventoryType type) => type switch {
        InventoryType.Inventory1 => InventoryType.Inventory1,
        InventoryType.Inventory2 => InventoryType.Inventory1,
        InventoryType.Inventory3 => InventoryType.Inventory1,
        InventoryType.Inventory4 => InventoryType.Inventory1,
        _ => type,
    };

    private static int GetInventoryStartIndex(InventoryType type) => type switch {
        InventoryType.Inventory2 => GetInventoryPageSize(type),
        InventoryType.Inventory3 => GetInventoryPageSize(type) * 2,
        InventoryType.Inventory4 => GetInventoryPageSize(type) * 3,
        _ => 0,
    };
}
