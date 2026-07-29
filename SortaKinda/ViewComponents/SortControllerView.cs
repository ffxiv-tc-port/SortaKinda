using System;
using System.Linq;
using System.Text.Json;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Utility;
using KamiLib.Classes;
using KamiLib.Window;
using SortaKinda.Classes;
using SortaKinda.Controllers;
using SortaKinda.Modules;
using SortaKinda.Windows;

namespace SortaKinda.ViewComponents;

public class SortControllerView(SortController sortingController) {
    private readonly SortingRuleListView listView = new(sortingController, sortingController.Rules);

    public void Draw() {
        DrawHeader();
        DrawRules();
    }

    private void DrawHeader() {
        var importExportButtonSize = ImGuiHelpers.ScaledVector2(23.0f, 23.0f);
        var sortButtonSize = ImGuiHelpers.ScaledVector2(100.0f, 23.0f);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3.0f * ImGuiHelpers.GlobalScale);
        ImGui.TextUnformatted("排序規則");

        ImGui.SameLine(ImGui.GetContentRegionAvail().X - importExportButtonSize.X * 3.0f - sortButtonSize.X - ImGui.GetStyle().ItemSpacing.X * 3.0f);

        if (ImGuiTweaks.IconButtonWithSize(Service.PluginInterface.UiBuilder.IconFontFixedWidthHandle, FontAwesomeIcon.Question, "HelpButton", importExportButtonSize, "開啟說明視窗")) {
            System.WindowManager.AddWindow(new TutorialWindow(), WindowFlags.OpenImmediately);
        }

        ImGui.SameLine();
        if (ImGuiTweaks.IconButtonWithSize(Service.PluginInterface.UiBuilder.IconFontFixedWidthHandle, FontAwesomeIcon.Clipboard, "ImportButton", importExportButtonSize, "從剪貼簿匯入規則")) {
            ImportRules();
        }
        
        ImGui.SameLine();
        if (ImGuiTweaks.IconButtonWithSize(Service.PluginInterface.UiBuilder.IconFontFixedWidthHandle, FontAwesomeIcon.ExternalLinkAlt, "ExportButton", importExportButtonSize, "將規則匯出至剪貼簿")) {
            ExportRules();
        }

        ImGui.SameLine();
        if (ImGui.Button("全部排序", sortButtonSize)) {
            sortingController.SortAllInventories();
        }

        ImGui.Separator();
    }

    private record ClipboardRules(SortingRule[] Rules, MainInventoryConfig MainInventory, ArmoryConfig Armory);
    
    private void ImportRules() {
        try {
            var decodedString = Convert.FromBase64String(ImGui.GetClipboardText());
            var uncompressed = Util.DecompressString(decodedString);

            if (uncompressed.IsNullOrEmpty()) {
                Service.ChatGui.PrintError("匯入排序規則時未取得內容，請重新複製代碼後再試。");
                return;
            }

            if (JsonSerializer.Deserialize<ClipboardRules>(uncompressed, SerializerOptions) is { } clipboardData) {
                if (clipboardData.Rules.Length is 0) {
                    Service.ChatGui.PrintError("匯入排序規則時未取得內容，請重新複製代碼後再試。");
                    return;
                }

                var addedCount = 0;
                foreach (var rule in clipboardData.Rules) {
                    if (sortingController.Rules.All(existingRule => existingRule.Id != rule.Id)) {
                        rule.Index = sortingController.Rules.Count;
                        sortingController.Rules.Add(rule);
                        addedCount++;
                    }
                }

                Service.ChatGui.Print($"已從剪貼簿讀取 {clipboardData.Rules.Length} 條排序規則。", "匯入");
                Service.ChatGui.Print($"已新增 {addedCount} 條排序規則。", "匯入");
                sortingController.SaveConfig();

                var mainInventoryModule = System.ModuleController.GetModule<MainInventoryModule>();
                mainInventoryModule.Config = clipboardData.MainInventory;
                mainInventoryModule.Save();
                mainInventoryModule.LoadModule();
                
                var armoryInventoryModule = System.ModuleController.GetModule<ArmoryInventoryModule>();
                armoryInventoryModule.Config = clipboardData.Armory;
                armoryInventoryModule.Save();
                armoryInventoryModule.LoadModule();
            }
        }
        catch (Exception e) {
            Service.ChatGui.PrintError("匯入規則時發生錯誤，請確認複製的代碼是否正確。");
            Service.Log.Error(e, "Handled exception while importing rules.");
        }
    }

    private static readonly JsonSerializerOptions SerializerOptions = new() {
        IncludeFields = true,
    };
    
    private void ExportRules() {
        var data = new ClipboardRules(
            sortingController.Rules.ToArray()[1..],
            (MainInventoryConfig) System.ModuleController.GetModule<MainInventoryModule>().Config,
            (ArmoryConfig) System.ModuleController.GetModule<ArmoryInventoryModule>().Config);
        
        var jsonString = JsonSerializer.Serialize(data, SerializerOptions);
        
        var compressed = Util.CompressString(jsonString);
        ImGui.SetClipboardText(Convert.ToBase64String(compressed));

        Service.ChatGui.Print($"已將 {data.Rules.Length} 條規則匯出至剪貼簿。", "匯出");
    }

    private void DrawRules() 
        => listView.Draw();
}
