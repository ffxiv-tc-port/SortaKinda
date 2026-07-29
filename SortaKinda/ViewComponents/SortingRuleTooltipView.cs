using System;
using System.Drawing;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Utility;
using KamiLib.Extensions;
using Lumina.Excel.Sheets;
using SortaKinda.Classes;
using SortaKinda.Controllers;

namespace SortaKinda.ViewComponents;

public class SortingRuleTooltipView(SortingRule sortingRule) {
    public void Draw() {
        ImGui.BeginTooltip();

        var imGuiColor = sortingRule.Color;
        if (ImGui.ColorEdit4("##ColorTooltip", ref imGuiColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoPicker)) {
            sortingRule.Color = imGuiColor;
        }

        ImGui.SameLine();
        ImGui.Text(sortingRule.Id is SortController.DefaultId ? "未排序" : sortingRule.Name);

        if (sortingRule.Id is not SortController.DefaultId) {
            var itemFiltersString = GetAllowedItemsString();

            ImGui.TextColored(KnownColor.Gray.Vector(), itemFiltersString.IsNullOrEmpty() ? "任何物品" : itemFiltersString);
            ImGui.TextColored(KnownColor.Gray.Vector(), sortingRule.SortMode.GetDescription());
        }

        ImGui.EndTooltip();
    }

    private string GetAllowedItemsString() {
        var strings = new[] {
            sortingRule.AllowedItemTypes.Count > 0 ? string.Join(", ", sortingRule.AllowedItemTypes.Select(type => Service.DataManager.GetExcelSheet<ItemUICategory>().GetRow(type).Name.ExtractText())) : string.Empty,
            sortingRule.AllowedNameRegexes.Count > 0 ? string.Join(", ", sortingRule.AllowedNameRegexes.Select(regex => @$"""{regex.Text}""")) : string.Empty,
            sortingRule.AllowedItemRarities.Count > 0 ? string.Join(", ", sortingRule.AllowedItemRarities.Select(rarity => rarity.GetDescription())) : string.Empty,
            sortingRule.ItemLevelFilter.Enable ? $"品級 {sortingRule.ItemLevelFilter.MinValue} → {sortingRule.ItemLevelFilter.MaxValue}" : string.Empty,
            sortingRule.VendorPriceFilter.Enable ? $"{sortingRule.VendorPriceFilter.MinValue} 金幣 → {sortingRule.VendorPriceFilter.MaxValue} 金幣" : string.Empty,
            sortingRule.LevelFilter.Enable ? $"等級 {sortingRule.LevelFilter.MinValue} → {sortingRule.LevelFilter.MaxValue}" : string.Empty,
        };

        return string.Join("\n", strings
            .Where(eachString => !eachString.IsNullOrEmpty())
            .Select(eachString => eachString[..Math.Min(eachString.Length, 55)]));
    }
}
