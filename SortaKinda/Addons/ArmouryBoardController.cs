using System;
using System.Diagnostics;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;

namespace SortaKinda.Addons;

public unsafe class ArmouryBoardController : AddonController<AddonInventoryExpansion> {

	private TextButtonNode? sortButton;
	private readonly Stopwatch sortButtonCooldown = new();

	public ArmouryBoardController() : base("ArmouryBoard") {
		OnAttach += AttachNodes;
		OnDetach += DetachNodes;
		OnUpdate += UpdateNodes;
	}

	private void AttachNodes(AddonInventoryExpansion* addon) {
		if (addon is null) return;

		var targetNode = addon->RootNode;
		if (targetNode is null) return;

		var inventoryButton = addon->GetNodeById(123);
		if (inventoryButton is not null) {
			inventoryButton->SetXFloat(141.0f);
		}
		
		sortButton = new TextButtonNode {
			Label = "排序",
			Size = new Vector2(100.0f, 28.0f),
			Position = new Vector2(19.0f, 566.0f),
			Tooltip = "SortaKinda：排序所有物品欄",
			IsVisible = true,
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

		var inventoryButton = addon->GetNodeById(17);
		if (inventoryButton is not null) {
			inventoryButton->SetXFloat(80.0f);
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
