using System;
using System.Numerics;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.UI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;

namespace SortaKinda.Addons;

public unsafe class InventoryLargeController : AddonController<AddonInventoryExpansion> {

	private TextButtonNode? sortButton;

	public InventoryLargeController() : base("InventoryLarge") {
		OnAttach += AttachNodes;
		OnDetach += DetachNodes;
	}

	private void AttachNodes(AddonInventoryExpansion* addon) {
		if (addon is null) return;

		var targetNode = addon->RootNode;
		if (targetNode is null) return;
		
		sortButton = new TextButtonNode {
			Label = "排序",
			Size = new Vector2(100.0f, 28.0f),
			Position = new Vector2(19.0f, 412.0f),
			Tooltip = "SortaKinda：排序所有物品欄",
			IsVisible = true,
		};

		sortButton.OnClick = () => {
			System.ModuleController.Sort();

			sortButton.HideTooltip();
			sortButton.IsEnabled = false;
			Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(_ => sortButton.IsEnabled = true);
		};
		
		System.NativeController.AttachNode(sortButton, targetNode, NodePosition.AsLastChild);
	}

	private void DetachNodes(AddonInventoryExpansion* addon) {
		System.NativeController.DisposeNode(ref sortButton);
	}
}
