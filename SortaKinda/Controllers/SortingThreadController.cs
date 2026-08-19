using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using SortaKinda.Classes;

namespace SortaKinda.Controllers;

public unsafe class SortingThreadController : IDisposable {
    private readonly List<SortingRequest> pendingRequests = [];
    private bool disposed;

    public void Dispose() {
        disposed = true;
        pendingRequests.Clear();
    }

    public void AddSortingTask(InventoryType type, params InventoryGrid[] grids) {
        if (disposed) return;

        // InventoryChanged may fire several times for one operation. Only the latest
        // request for a sorter is needed before the next framework update.
        pendingRequests.RemoveAll(request => request.Type == type);
        pendingRequests.Add(new SortingRequest(type, grids));
    }

    public void Update() {
        if (disposed || pendingRequests.Count == 0) return;

        var requests = pendingRequests.ToArray();
        pendingRequests.Clear();
        Service.Log.Verbose($"Running {requests.Length} pending sorting requests on the framework thread.");

        // Native inventory state is owned by the game/framework thread. Running sorts
        // concurrently can race both the game and other armoury sorting requests.
        foreach (var request in requests) {
            InventorySorter.SortInventory(request.Type, request.Grids);
        }

        Service.Framework.RunOnTick(() => {
            var itemOrderModule = ItemOrderModule.Instance();
            if (disposed || itemOrderModule == null) return;

            Service.Log.Debug("Marked ItemODR as changed.");
            itemOrderModule->UserFileEvent.HasChanges = true;
        }, delayTicks: 5);
    }

    private sealed record SortingRequest(InventoryType Type, InventoryGrid[] Grids);
}
