using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game;
using SortaKinda.Classes;
using SortaKinda.Controllers;
using SortaKinda.ViewComponents;

namespace SortaKinda.Modules;

public class ArmoryConfig : IModuleConfig {
    public List<InventoryConfig> InventoryConfigs { get; set; } = [
        new(InventoryType.ArmoryMainHand),
        new(InventoryType.ArmoryOffHand),
        new(InventoryType.ArmoryHead),
        new(InventoryType.ArmoryBody),
        new(InventoryType.ArmoryHands),
        new(InventoryType.ArmoryLegs),
        new(InventoryType.ArmoryFeets),
        new(InventoryType.ArmoryEar),
        new(InventoryType.ArmoryNeck),
        new(InventoryType.ArmoryWrist),
        new(InventoryType.ArmoryRings),
        new(InventoryType.ArmorySoulCrystal),
    ];
}

public class ArmoryInventoryModule : ModuleBase<ArmoryConfig> {
    protected override List<InventoryGrid> Inventories { get; set; } = [];
    
    private ArmoryInventoryGridView? view;
    
    // 分頁圖示第一次畫才載，之後整個插件期間沿用；只有 Dispose 會釋放（不在 Draw 中途換掉）。
    private ArmoryTabTextures? tabTextures;

    // LoadViews 只標記過期，版面等到真的要畫才建（登入時多半根本沒開設定視窗）。
    private bool viewOutdated;

    public override ModuleName ModuleName => ModuleName.ArmoryInventory;
    
    public override ArmoryConfig ModuleConfig { get; set; } = new();

    protected override void LoadViews() {
        Inventories = [];
        foreach (var config in ModuleConfig.InventoryConfigs) {
            Inventories.Add(new InventoryGrid(config.Type, config));
        }

        view = null;
        viewOutdated = true;
    }

    public override void Dispose() {
        view = null;
        tabTextures?.Dispose();
        tabTextures = null;
        base.Dispose();
    }

    public override void Draw() {
        if (viewOutdated) {
            // 先清旗標：建失敗時只擲一次、之後不畫，不每幀重試。
            viewOutdated = false;
            tabTextures ??= new ArmoryTabTextures();
            view = new ArmoryInventoryGridView(Inventories, tabTextures);
        }

        view?.Draw();
    }

    protected override void Sort(params InventoryType[] inventoryTypes) {
        foreach (var type in inventoryTypes) {
            if (Inventories.FirstOrDefault(inventory => inventory.Type == type) is { } targetInventory) {
                if (targetInventory.Inventory.Any(slot => slot.Rule.Id is not SortController.DefaultId)) {
                    System.SortingThreadController.AddSortingTask(targetInventory.Type, targetInventory);
                }
            }
        }
    }
}