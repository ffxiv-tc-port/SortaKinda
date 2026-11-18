using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using SortaKinda.Controllers;
using SortaKinda.Modules;

namespace SortaKinda.Classes;

/// <summary>
/// 登入時第一次讀寫各設定檔，要建 System.Text.Json 的型別中繼資料並 JIT，全部付在登入那一幀。
/// 插件載入時先在背景做掉，登入時就只剩真正的讀寫。
/// </summary>
/// <remarks>
/// 只用 KamiLib 共用的序列化選項（執行緒安全），不讀寫檔。物品欄模組設定的預設建構子會讀遊戲的物品欄
/// 排序資料，所以那兩個型別只取型別中繼資料（GetTypeInfo 不呼叫建構子），不建立實例。
/// 失敗或被取消都只影響第一次登入快慢，不影響功能。
/// </remarks>
public static class SerializerWarmup {
    private static readonly Type[] MetadataOnlyTypes = [typeof(MainInventoryConfig), typeof(ArmoryConfig)];

    private static readonly Type[] RoundTripTypes = [typeof(SystemConfig), typeof(SortingRuleConfig)];

    public static void Run(CancellationToken token) {
        var options = KamiLib.Configuration.Configuration.SerializerOptions;
        var stopwatch = Stopwatch.StartNew();

        try {
            foreach (var type in MetadataOnlyTypes) {
                if (token.IsCancellationRequested) return;

                options.GetTypeInfo(type);
            }

            foreach (var type in RoundTripTypes) {
                if (token.IsCancellationRequested) return;

                // 空物件 → 序列化 → 再反序列化：讀、寫兩條路徑都走過一次。
                var instance = JsonSerializer.Deserialize("{}", type, options);
                var json = JsonSerializer.Serialize(instance, type, options);
                JsonSerializer.Deserialize(json, type, options);
            }
        }
        catch (Exception ex) {
            if (!token.IsCancellationRequested) {
                Service.Log.Warning(ex, "[SerializerWarmup] Failed; the first login will only take longer.");
            }

            return;
        }

        if (!token.IsCancellationRequested) {
            Service.Log.Debug($"[SerializerWarmup] Warmed {MetadataOnlyTypes.Length + RoundTripTypes.Length} config types in {stopwatch.Elapsed.TotalMilliseconds:F0} ms");
        }
    }
}
