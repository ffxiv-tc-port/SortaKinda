using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using SortaKinda.Classes;

namespace SortaKinda.ViewComponents;

/// <summary>
/// 裝備庫左右兩排分頁圖示。只取決於遊戲的 UI 資產，跟角色與縮放無關，由擁有者建一次、插件卸載時才 Dispose。
/// </summary>
public sealed class ArmoryTabTextures : IDisposable {
    private readonly UldWrapper armouryBoard;

    public Dictionary<InventoryType, IDalamudTextureWrap?> Left { get; } = new();

    public Dictionary<InventoryType, IDalamudTextureWrap?> Right { get; } = new();

    public ArmoryTabTextures() {
        armouryBoard = Service.PluginInterface.UiBuilder.LoadUld("ui/uld/ArmouryBoard.uld");

        Left.Add(InventoryType.ArmoryMainHand, armouryBoard.LoadTexturePart("ui/uld/ArmouryBoard_hr1.tex", 0));
        Left.Add(InventoryType.ArmoryHead, armouryBoard.LoadTexturePart("ui/uld/ArmouryBoard_hr1.tex", 1));
        Left.Add(InventoryType.ArmoryBody, armouryBoard.LoadTexturePart("ui/uld/ArmouryBoard_hr1.tex", 2));
        Left.Add(InventoryType.ArmoryHands, armouryBoard.LoadTexturePart("ui/uld/ArmouryBoard_hr1.tex", 3));
        Left.Add(InventoryType.ArmoryLegs, armouryBoard.LoadTexturePart("ui/uld/ArmouryBoard_hr1.tex", 5));
        Left.Add(InventoryType.ArmoryFeets, armouryBoard.LoadTexturePart("ui/uld/ArmouryBoard_hr1.tex", 6));

        Right.Add(InventoryType.ArmoryOffHand, armouryBoard.LoadTexturePart("ui/uld/ArmouryBoard_hr1.tex", 7));
        Right.Add(InventoryType.ArmoryEar, armouryBoard.LoadTexturePart("ui/uld/ArmouryBoard_hr1.tex", 8));
        Right.Add(InventoryType.ArmoryNeck, armouryBoard.LoadTexturePart("ui/uld/ArmouryBoard_hr1.tex", 9));
        Right.Add(InventoryType.ArmoryWrist, armouryBoard.LoadTexturePart("ui/uld/ArmouryBoard_hr1.tex", 10));
        Right.Add(InventoryType.ArmoryRings, armouryBoard.LoadTexturePart("ui/uld/ArmouryBoard_hr1.tex", 11));
        Right.Add(InventoryType.ArmorySoulCrystal, armouryBoard.LoadTexturePart("ui/uld/ArmouryBoard_hr1.tex", 12));
    }

    public void Dispose() {
        foreach (var loadedTexture in Left) loadedTexture.Value?.Dispose();
        foreach (var loadedTexture in Right) loadedTexture.Value?.Dispose();
        armouryBoard.Dispose();
    }
}

/// <summary>分頁版面（位置依建立當下的縮放）。不擁有圖示，換掉時不必 Dispose。</summary>
public class ArmoryInventoryGridView {
    private readonly Dictionary<InventoryType, IDalamudTextureWrap?> leftTabTextures;
    private readonly Dictionary<InventoryType, IDalamudTextureWrap?> rightTabTextures;

    private readonly Dictionary<InventoryType, InventoryGridView> views = new();
    private InventoryType selectedTab = InventoryType.ArmoryMainHand;

    private static Vector2 ButtonSize 
        => ImGuiHelpers.ScaledVector2(80.0f, 80.0f) * .75f;
    
    private static Vector2 ButtonPadding 
        => ImGuiHelpers.ScaledVector2(5.0f, 5.0f);

    public ArmoryInventoryGridView(List<InventoryGrid> armoryInventories, ArmoryTabTextures tabTextures) {
        var region = ImGuiHelpers.ScaledVector2(404, 565);
        var position = new Vector2(region.X / 2.0f - InventoryGridView.GetGridWidth() / 2.0f, region.Y / 8.0f);

        foreach (var inventory in armoryInventories) {
            views.Add(inventory.Type, new InventoryGridView(inventory, position));
        }

        leftTabTextures = tabTextures.Left;
        rightTabTextures = tabTextures.Right;
    }

    public void Draw() {
        views[selectedTab].Draw();

        var region = ImGuiHelpers.ScaledVector2(404, 565);
        var gridWidth = InventoryGridView.GetGridWidth();
        var position = new Vector2(region.X / 2.0f - gridWidth / 2.0f, region.Y / 8.0f);

        var leftOffset = ButtonSize.X + ButtonPadding.X;
        var leftBarPosition = position - new Vector2(leftOffset, 0.0f);
        DrawTabBar(leftTabTextures, leftBarPosition);

        var rightOffset = ButtonPadding.X;
        var rightBarPosition = position + new Vector2(gridWidth + rightOffset, 0.0f);
        DrawTabBar(rightTabTextures, rightBarPosition);
    }

    private void DrawTabBar(Dictionary<InventoryType, IDalamudTextureWrap?> textures, Vector2 drawPosition) {
        var itemSpacing = new Vector2(0.0f, ButtonSize.Y + ButtonPadding.Y);

        var index = 0;
        foreach (var (tab, texture) in textures) {
            if (texture is null) continue;
            var inactiveColor = Vector4.One with { W = 0.33f };
            var activeColor = Vector4.One;

            if (tab == selectedTab) {
                var windowPosition = ImGui.GetWindowPos();

                var borderColor = ImGui.GetColorU32(KnownColor.White.Vector());
                var backgroundColor = ImGui.GetColorU32(KnownColor.Gray.Vector() with { W = 0.50f });

                var rectStart = windowPosition + drawPosition + itemSpacing * index;
                var rectStop = rectStart + ButtonSize;

                ImGui.GetWindowDrawList().AddRectFilled(rectStart, rectStop, backgroundColor, 5.0f);
                ImGui.GetWindowDrawList().AddRect(rectStart, rectStop, borderColor, 5.0f);
            }

            ImGui.SetCursorPos(drawPosition + itemSpacing * index++);
            ImGui.Image(texture.Handle, ButtonSize, Vector2.Zero, Vector2.One, selectedTab == tab ? activeColor : inactiveColor);

            if (ImGui.IsItemClicked()) {
                selectedTab = tab;
            }
        }
    }
}