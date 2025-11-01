﻿using Duckov.UI;
using HarmonyLib;
using ItemStatsSystem;
using SodaCraft.Localizations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;


namespace RepairFullDurability
{
    public class PatchModuleCollection
    {
        [HarmonyPatch] // 标记为Harmony补丁类
        internal static class PatchItemRepairView
        {
            private static MethodBase TargetMethod()
            {
                // 1. 获取ItemRepairView类的所有方法（实例/静态、公共/非公共、自身声明）
                MethodInfo[] allMethods = typeof(ItemRepairView).GetMethods((BindingFlags)60);

                // 2. 过滤出名为CalculateRepairPrice的方法
                var calculateMethods = allMethods.Where(m => m.Name == "CalculateRepairPrice");

                // 3. 遍历查找符合参数条件的重载
                foreach (MethodInfo method in calculateMethods)
                {
                    ParameterInfo[] parameters = method.GetParameters();

                    // 核心条件：匹配目标方法的参数签名
                    if (parameters.Length == 4 &&
                        parameters[0].ParameterType == typeof(Item) &&
                        parameters[1].ParameterType == typeof(float).MakeByRefType() &&
                        parameters[2].ParameterType == typeof(float).MakeByRefType() &&
                        parameters[3].ParameterType == typeof(float).MakeByRefType())
                    {
                        return method; // 找到目标方法，返回元数据
                    }
                }

                // 未找到符合要求的方法，抛出异常（Harmony会捕获并提示补丁失败）
                throw new Exception("Failed to find target method: ItemRepairView.CalculateRepairPrice");
            }

            // 前置补丁：修改维修价格计算逻辑
            [HarmonyPrefix]
            private static bool Prefix(Item item, ref float repairAmount, ref float lostAmount, ref float lostPercentage, ref int __result)
            {
                repairAmount = 0f;
                lostAmount = 0f;//去掉这个的赋值，改变显示扣耐久提示
                lostPercentage = 0f;
                if (item == null || !item.UseDurability)
                {
                    __result = 0;
                    return false;
                }
                float maxDurability = item.MaxDurability;
                float durabilityLoss = item.DurabilityLoss;
                float num = maxDurability * (1f - durabilityLoss);
                float durability = item.Durability;
                //原代码
                //repairAmount = num - durability;
                //float repairLossRatio = item.GetRepairLossRatio();
                //lostAmount = repairAmount * repairLossRatio;
                //repairAmount -= lostAmount;

                repairAmount += item.MaxDurability - item.Durability;//加入红条耐久的价格
                if (repairAmount <= 0.0)//​需维修的有效耐久​（扣除维修损耗后的值）
                {
                    __result = 0;
                }
                else
                {
                    lostPercentage = lostAmount / maxDurability;
                    float num2 = repairAmount / maxDurability;
                    
                    __result = Mathf.CeilToInt((float)(item.Value * num2 * 0.5));//维修价格 = 物品价值 × 维修比例 × 0.5（示例逻辑）
                }
                return false;
            }
        }




        [HarmonyPatch(typeof(ItemRepairView), "CanRepair", MethodType.Getter)]
        public class PatchCanRepairGetter
        {
            [HarmonyPrefix]
            private static bool Prefix(ref bool __result)
            {
                // 获取选中物品
                Item selectedItem = ItemUIUtilities.SelectedItem;

                // 空值检查
                if (selectedItem == null)
                {
                    __result = false;
                    return false; // 跳过原方法
                }
                // 基础条件过滤
                if (!selectedItem.UseDurability || selectedItem.MaxDurabilityWithLoss < 1f)
                {
                    __result = false;
                    return false;
                }
                if (!selectedItem.Tags.Contains("Repairable"))
                {
                    Debug.Log(selectedItem.DisplayName + " 不包含tag Repairable");
                    __result = false;
                    return false;
                }
                if (selectedItem.DurabilityLoss > 0 && selectedItem.Tags.Contains("Repairable"))
                {
                    __result = true;
                    return false;
                }

                __result = selectedItem.Durability < selectedItem.MaxDurabilityWithLoss;
                return false; // 表示已处理，不执行原方法

            }
        }
        //参考自 https://steamcommunity.com/sharedfiles/filedetails/?id=3594997475
        [HarmonyPatch(typeof(ItemRepairView), "RefreshSelectedItemInfo")]
        public class PatchRefreshSelectedItemInfo
        {

            private static void Postfix(ref TextMeshProUGUI ___willLoseDurabilityText)
            {
                Item selectedItem = ItemUIUtilities.SelectedItem;
                if(selectedItem != null)
                {
                    float Loss = selectedItem.DurabilityLoss;
                    if (Loss > 0 || ___willLoseDurabilityText.IsUnityNull())
                    {
                        Loss *= 100;
                        ___willLoseDurabilityText.text = LocalizationManager.ToPlainText("UI_MaxDurability") + " +" + Loss.ToString("0.#");
                    }
                }
            }
                
        }

        [HarmonyPatch(typeof(ItemRepairView), "Repair", (new Type[] { typeof(Item), typeof(bool) }))]
        public static class RepairFullDurability
        {

            [HarmonyPostfix]//后置补丁
            private static void Postfix(Item item, bool prepaied)
            {
                // 直接读取配置文件中的值（实时生效）
                bool isOutfit = RepairConfig.IsOutfit;

                if (isOutfit)
                {
                    if (item.Tags.Contains("Weapon"))
                    {
                        item.DurabilityLoss = 0;
                        item.Durability = item.MaxDurability;
                    }
                }
                else
                {
                    item.DurabilityLoss = 0;
                    item.Durability = item.MaxDurability;
                }

            }
        }

        [HarmonyPatch(typeof(ItemRepair_RepairAllPanel), "OnEnable")]
        public static class PatchItemRepair_RepairAllPanel
        {
            [HarmonyPostfix]//后置补丁
            private static void Postfix(ItemRepair_RepairAllPanel __instance)
            {
                //// 获取私有方法信息
                //MethodInfo refreshMethod = AccessTools.Method(
                //    typeof(ItemRepair_RepairAllPanel),
                //    "Refresh",
                //    new Type[] { } // 无参数
                //);

                //// 执行私有方法
                //refreshMethod.Invoke(__instance, null);
                //Traverse.Create(__instance).Method("Refresh").GetValue(); // 调用方法,手动刷新界面。解决批量维修首次打开时，显示不正常的问题

                Traverse.Create(__instance).Field("needsRefresh").SetValue(true); // 设置新值

            }
        }


    }
}
