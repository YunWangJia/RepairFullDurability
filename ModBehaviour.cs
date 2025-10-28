﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Duckov.Economy;
using Duckov.Modding;
using Duckov.UI;
using Duckov.Utilities;
using HarmonyLib;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using SodaCraft.Localizations;
using static CraftView;

namespace RepairFullDurability
{
    //该mod部分代码由AI生成
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        //2025.10.28，更新为1.1版本，加入了ModConfigAPI的界面兼容，玩家现在可以在ModConfig的设置界面直接设置过滤效果。
        public static string MOD_NAME_ch = "维修恢复全耐久";
        // 保存Harmony实例，用于后续撤销补丁
        private Harmony? _harmony;

        static RFDConfig config = new RFDConfig();

        private static string persistentConfigPath => Path.Combine(Application.streamingAssetsPath, "RFDConfig.txt");

        private void OnEnable()
        {
            // 初始化配置（不存在则创建默认）
            RepairConfig.Initialize();

            //config.IsFilteringEquipment = RepairConfig.IsOutfit;
            //HarmonyLoad.Load0Harmony(); // 加载Harmony库
            ModManager.OnModActivated += OnModActivated;

            // 立即检查一次，防止 ModConfig 已经加载但事件错过了
            if (ModConfigAPI.IsAvailable())
            {
                Debug.Log("RepairFullDurability: ModConfig 已可用!");
                SetModConfig();
                IniContrast();
            }

            
            // 初始化Harmony实例并保存
            _harmony = new Harmony("RepairFullDurability");
            _harmony.PatchAll(); // 应用补丁
        }
        private void OnModActivated(ModInfo info, Duckov.Modding.ModBehaviour behaviour)
        {
            if (info.name == ModConfigAPI.ModConfigName)
            {
                Debug.Log("RepairFullDurability: ModConfig 已激活!");
                SetModConfig();
                IniContrast();
            }
        }
        public void IniContrast()
        {
            LoadConfigFromModConfig();
            if (config.IsFilteringEquipment != RepairConfig.IsOutfit)
            {
                config.IsFilteringEquipment = RepairConfig.IsOutfit;
                SaveConfigFromModConfig();
                LoadConfigFromModConfig();
            }
        }


        // 新增：Mod禁用时撤销所有补丁
        private void OnDisable()
        {
            if (_harmony != null)
            {
                _harmony.UnpatchAll(); // 撤销该Harmony实例的所有补丁
                _harmony = null; // 释放引用
            }
            ModManager.OnModActivated -= OnModActivated;
            ModConfigAPI.SafeRemoveOnOptionsChangedDelegate(OnModConfigOptionsChanged);
        }
    

        [HarmonyPatch(typeof(ItemRepairView), "Repair", (new Type[] { typeof(Item), typeof(bool)}))]
        public static class RepairFullDurability
        {
            [HarmonyPostfix]
            public static void Postfix(Item item, bool prepaied)
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


        private void SetModConfig()
        {
            if (!ModConfigAPI.IsAvailable())
            {
                Debug.LogWarning("RepairFullDurability: 与ModConfig通信失败！");
                return;
            }
            // 添加配置变更监听
            ModConfigAPI.SafeAddOnOptionsChangedDelegate(OnModConfigOptionsChanged);
            // 根据当前语言设置描述文字
            SystemLanguage[] chineseLanguages = {
                SystemLanguage.Chinese,
                SystemLanguage.ChineseSimplified,
                SystemLanguage.ChineseTraditional
            };
            bool isChinese = chineseLanguages.Contains(LocalizationManager.CurrentLanguage);
            
            // 添加配置项
            ModConfigAPI.SafeAddBoolDropdownList(
                MOD_NAME_ch,
                "IsFilteringEquipment",
                isChinese ? "维修是否过滤装备？" : "Filter Armor?",
                config.IsFilteringEquipment
            );


            Debug.Log("RepairFullDurability: ModConfig 设置完成！");
        }


        private void SaveConfig(RFDConfig config)
        {
            try
            {
                string json = JsonUtility.ToJson(config, true);
                File.WriteAllText(persistentConfigPath, json);
                Debug.Log("DisplayItemValue: Config saved");
            }
            catch (Exception e)
            {
                Debug.LogError($"DisplayItemValue: Failed to save config: {e}");
            }
        }
        private void OnModConfigOptionsChanged(string key)
        {
            if (!key.StartsWith(MOD_NAME_ch + "_"))
                return;

            // 使用新的 LoadConfig 方法读取配置
            LoadConfigFromModConfig();

            RepairConfig.IsOutfit = config.IsFilteringEquipment;
            // 保存到本地配置文件
            SaveConfig(config);

            Debug.Log($"DisplayItemValue: ModConfig updated - {key}");
        }

        private void LoadConfigFromModConfig()
        {
            // 使用新的 LoadConfig 方法读取所有配置
            config.IsFilteringEquipment = ModConfigAPI.SafeLoad<bool>(MOD_NAME_ch, "IsFilteringEquipment", config.IsFilteringEquipment);
            
        }
        private void SaveConfigFromModConfig()
        {
            ModConfigAPI.SafeSave<bool>(MOD_NAME_ch, "IsFilteringEquipment", config.IsFilteringEquipment);
        }

    }


    [System.Serializable]
    public class RFDConfig//必须使用类，因为储存的时候，是直接将类转为json
    {
        // 是否显过滤装备
        public bool IsFilteringEquipment = false;
    }

}
