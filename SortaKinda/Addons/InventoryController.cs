using System;
using System.Diagnostics;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;

namespace SortaKinda.Addons;

public unsafe class InventoryController : AddonController<AddonInventoryExpansion> {

	private CircleButtonNode? sortButton;
	private readonly Stopwatch sortButtonCooldown = new();

	public InventoryController() : base("Inventory") {
		OnAttach += AttachNodes;
		OnDetach += DetachNodes;
		OnUpdate += UpdateNodes;
	}

	private void AttachNodes(AddonInventoryExpansion* addon) {
		if (addon is null) return;

		var targetNode = addon->RootNode;
		if (targetNode is null) return;

		var armoryButton = addon->GetNodeById(17);
		var saddlebagButton = addon->GetNodeById(16);
		if (armoryButton is not null && saddlebagButton is not null) {
			armoryButton->SetXFloat(49.0f);
			saddlebagButton->SetXFloat(49.0f);
		}
		
		sortButton = new CircleButtonNode {
			Size = new Vector2(28.0f, 28.0f),
			Position = new Vector2(19.0f, 414.0f),
			Tooltip = "SortaKinda：排序所有物品欄",
			IsVisible = true,
			Icon = ButtonIcon.Sort,
		};

		sortButton.OnClick = () => {
			System.ModuleController.Sort();
				
			sortButton.HideTooltip();
			sortButton.IsEnabled = false;
			sortButtonCooldown.Restart();
		};

		System.NativeController.AttachNode(sortButton, targetNode, NodePosition.AsLastChild);
	}

	private void DetachNodes(AddonInventoryExpansion* addon) {
		if (addon is null) {
			System.NativeController.DisposeNode(ref sortButton);
			return;
		}

		var armoryButton = addon->GetNodeById(17);
		var saddlebagButton = addon->GetNodeById(16);
		if (armoryButton is not null && saddlebagButton is not null) {
			armoryButton->SetXFloat(33.0f);
			saddlebagButton->SetXFloat(33.0f);
		}

		System.NativeController.DisposeNode(ref sortButton);
	}

	// Re-enabled from the addon's PostUpdate: framework thread, addon alive, nothing scheduled that could
	// outlive the addon or the plugin (a thread-pool timer would touch the native button off-thread).
	private void UpdateNodes(AddonInventoryExpansion* addon) {
		if (sortButton is null || !sortButtonCooldown.IsRunning || sortButtonCooldown.Elapsed < TimeSpan.FromSeconds(5)) return;

		sortButton.IsEnabled = true;
		sortButtonCooldown.Reset();
	}
}
