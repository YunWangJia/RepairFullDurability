using Duckov.UI;
using HarmonyLib;
using ItemStatsSystem;
using SodaCraft.Localizations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
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

                repairAmount += item.MaxDurability - item.Durability;
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

        //参考自 https://steamcommunity.com/sharedfiles/filedetails/?id=3594997475
        [HarmonyPatch(typeof(ItemRepairView), "RefreshSelectedItemInfo")]
        public class PatchRefreshSelectedItemInfo
        {
            
            private static void Postfix(ItemRepairView __instance, ref TextMeshProUGUI ___willLoseDurabilityText)
            {
                Item selectedItem = ItemUIUtilities.SelectedItem;
                float num = selectedItem.DurabilityLoss;
                if (num > 0|| ___willLoseDurabilityText.text == "")
                {
                    num *= 100;
                    ___willLoseDurabilityText.text = LocalizationManager.ToPlainText("UI_MaxDurability") + " +" + num.ToString("0.#");
                }
            }
        }
        [HarmonyPatch(typeof(ItemRepairView), "CanRepair", MethodType.Getter)]
        public class PatchCanRepairGetter
        {
            [HarmonyPostfix]
            private static void Postfix(ref bool __result)
            {
                Item selectedItem = ItemUIUtilities.SelectedItem;
                if (selectedItem.DurabilityLoss > 0)
                {
                    __result = true;
                }
            }
        }



    }
}
