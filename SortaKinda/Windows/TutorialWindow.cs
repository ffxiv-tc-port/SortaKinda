using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using KamiLib.Classes;
using KamiLib.Window;

namespace SortaKinda.Windows;

public class TutorialWindow : Window {
    private readonly TabBar tabBar;

    public TutorialWindow() : base("SortaKinda－使用教學", new Vector2(640.0f, 425.0f)) {

        Flags |= ImGuiWindowFlags.NoScrollbar;
        Flags |= ImGuiWindowFlags.NoScrollWithMouse;

        tabBar = new TabBar("TutorialTabBar", [
            new TutorialAboutTab(),
            new TutorialSortingRules(),
            new TutorialConfiguringInventory(),
            new TutorialAdvancedSorting()
        ]);
    }

    protected override void DrawContents() 
        => tabBar.Draw();

    public override void OnClose() 
        => System.WindowManager.RemoveWindow(this);
}

public class TutorialAboutTab : ITabItem {
    public string Name => "關於";
    
    public bool Disabled => false;
    
    public void Draw() {
        ImGuiHelpers.ScaledDummy(10.0f);
        
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(ImGui.GetStyle().ItemSpacing.X, 10.0f * ImGuiHelpers.GlobalScale));
        
        ImGui.TextWrapped(AboutText);
        
        ImGui.PopStyleVar();
    }

    private const string AboutText = "歡迎使用 SortaKinda！這是一款可高度自訂的物品欄管理工具。\n" +
                                     "你可以精確指定哪些物品應固定整理至物品欄的特定區域。\n\n" +
                                     "SortaKinda 與遊戲內建的「isort」功能無關，也不會與其互動。\n" +
                                     "使用 SortaKinda 排序時，會覆蓋其他排序系統產生的排列結果。\n\n" +
                                     "「一般設定」分頁提供自動排序觸發條件，可在遊玩過程中自動重新整理物品欄。\n\n" +
                                     "SortaKinda 偶爾可能沒有捕捉到某次變更；後續觸發條件通常會很快再次執行排序。";
}

public class TutorialSortingRules : ITabItem {
    public string Name => "排序規則";
    
    public bool Disabled => false;
    
    public void Draw() {
        ImGuiHelpers.ScaledDummy(10.0f);
        
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(ImGui.GetStyle().ItemSpacing.X, 10.0f * ImGuiHelpers.GlobalScale));
        
        ImGui.TextWrapped(SortingRulesHelp);
        
        ImGui.PopStyleVar();
    }

    private const string SortingRulesHelp = "排序規則用來定義哪些物品可以放入指定的物品欄欄位。\n" +
                                            "規則會顯示在設定視窗左側，新規則會加入清單底部。\n\n" +
                                            "若要刪除規則，請同時按住 Shift 與 Ctrl，再點擊垃圾桶圖示。「未排序」規則無法刪除。\n\n" +
                                            "同一規則使用多種類型的篩選時，物品必須符合所有篩選類型才會被允許。\n\n" +
                                            "例如：規則的物品類型設為「雜貨」、物品稀有度設為「綠色」時，只有同時屬於雜貨且為綠色稀有度的物品才能放入這些欄位。\n\n" +
                                            "不符合任何規則的物品會移至物品欄的「未排序」區域，因此必須保留部分欄位並標記為「未排序」。\n\n" +
                                            "若 SortaKinda 無法將物品從已排序區域移至「未排序」區域，會暫時將該物品視為屬於目前區域，並依照該區域的規則排序。";
}

public class TutorialConfiguringInventory : ITabItem {
    public string Name => "使用規則";
    
    public bool Disabled => false;
    
    public void Draw() {
        ImGuiHelpers.ScaledDummy(10.0f);
        
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(ImGui.GetStyle().ItemSpacing.X, 10.0f * ImGuiHelpers.GlobalScale));
        
        ImGui.TextWrapped(UsingRulesHelp);
        
        ImGui.PopStyleVar();
    }

    private const string UsingRulesHelp = "使用規則前，請先在設定視窗左側選取一項規則。\n" +
                                          "目前選取的規則會在其顏色與名稱旁顯示實心圓點。\n\n" +
                                          "選取規則後，即可在設定視窗右側將規則「塗入」物品欄欄位；套用同一規則的欄位不必彼此相鄰。\n\n" +
                                          "每次觸發排序時，符合欄位規則的物品會嘗試移入那些欄位，之後再依規則設定重新排列。\n" +
                                          "已位於規則欄位、但不符合該規則的物品，則會移至「未排序」欄位。\n\n" +
                                          "請務必保留部分物品欄欄位並標記為「未排序」。";
}

public class TutorialAdvancedSorting : ITabItem {
    public string Name => "進階技巧";
    
    public bool Disabled => false;
    
    public void Draw() {
        ImGuiHelpers.ScaledDummy(10.0f);
        
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(ImGui.GetStyle().ItemSpacing.X, 10.0f * ImGuiHelpers.GlobalScale));
        
        ImGui.TextWrapped(AdvancedTech);

        ImGui.PopStyleVar();
    }

    private const string AdvancedTech = "SortaKinda 會依固定順序評估排序規則，" +
                                        "因此可讓同時符合多項規則的物品，最終固定進入物品欄的特定區域。\n\n" +
                                        "規則會從清單頂端（「未排序」所在處）依序評估至底端（新增規則按鈕所在處）。" +
                                        "若物品同時符合多項規則，最終會由清單中位置最低的規則取得該物品。\n\n" +
                                        "你可以利用這項特性，將較通用的排序規則放在清單上方，較具體的規則放在下方；" +
                                        "符合下方具體規則的物品，最後會歸入該規則。\n\n" +
                                        "換句話說，規則順序可視為一種彈性優先權系統，越靠近清單底部的規則優先權越高。";
}
