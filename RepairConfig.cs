using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RepairFullDurability
{
    /// <summary>
    /// Mod配置管理类（读取/写入config.ini）
    /// </summary>
    public static class RepairConfig
    {
        /// <summary>
        /// 配置文件路径：Duckov_Data/Mods/RepairFullDurability/config.ini
        /// （兼容Unity编辑器与打包环境）
        private static string ConfigPath => Path.Combine("Duckov_Data/Mods/RepairFullDurability/config.ini");

        /// <summary>
        /// 是否只过滤装备（读取配置文件中的[General] -> IsOutfit）
        /// </summary>
        public static bool IsOutfit
        {
            get
            {
                string value = ReadValue("General", "IsOutfit", "false");
                return bool.TryParse(value, out bool result) ? result : false;
            }
            set
            {
                WriteValue("General", "IsOutfit", value.ToString().ToLower());
            }
        }

        /// <summary>
        /// 初始化配置：如果文件不存在则创建默认配置
        /// </summary>
        public static bool Initialize()
        {
            if (!File.Exists(ConfigPath))
            {
                // 创建目录
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                // 写入默认配置
                File.WriteAllText(ConfigPath, "[General]\n#为：true时，不修复装备上限，为：false时，修复装备和武器上限。\nIsOutfit = false");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 从INI文件读取指定节和键的值
        /// </summary>
        /// <param name="section">节名称（不区分大小写）</param>
        /// <param name="key">键名称（不区分大小写）</param>
        /// <param name="defaultValue">默认值（节/键不存在时返回）</param>
        /// <returns>读取到的值或默认值</returns>
        private static string ReadValue(string section, string key, string defaultValue)
        {
            if (!File.Exists(ConfigPath)) return defaultValue;

            string[] lines = File.ReadAllLines(ConfigPath, Encoding.UTF8);
            bool isInTargetSection = false;

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                // 1. 检查是否进入目标节（如[General]）
                if (IsSectionLine(trimmedLine))
                {
                    isInTargetSection = GetSectionName(trimmedLine).Equals(section, StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                // 2. 在目标节中查找目标键
                if (isInTargetSection && IsKeyValueLine(trimmedLine))
                {
                    var (currentKey, currentValue) = ParseKeyValueLine(trimmedLine);
                    if (currentKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        return string.IsNullOrEmpty(currentValue) ? defaultValue : currentValue;
                    }
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// 向INI文件写入指定节和键的值（自动修改已有键或新增键）
        /// </summary>
        /// <param name="section">节名称（不区分大小写）</param>
        /// <param name="key">键名称（不区分大小写）</param>
        /// <param name="value">要写入的值</param>
        private static void WriteValue(string section, string key, string value)
        {
            
            string[] lines = File.ReadAllLines(ConfigPath, Encoding.UTF8);
            List<string> newLines = new List<string>();
            bool isInTargetSection = false;
            bool isKeyFound = false;

            foreach (string line in lines)
            {
                string originalLine = line; // 保留原始格式（如缩进、注释）
                string trimmedLine = line.Trim();

                // 1. 更新当前所在节的状态
                if (IsSectionLine(trimmedLine))
                {
                    isInTargetSection = trimmedLine.Equals($"[{section}]", StringComparison.OrdinalIgnoreCase);
                    newLines.Add(originalLine);
                    continue;
                }

                // 2. 在目标节中查找并修改目标键
                if (isInTargetSection && IsKeyValueLine(trimmedLine))
                {
                    var (currentKey, _) = ParseKeyValueLine(trimmedLine);
                    if (currentKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        // 保留原始行的格式（如缩进），仅替换值
                        string indent = originalLine.Substring(0, originalLine.IndexOf(currentKey)).TrimEnd();
                        newLines.Add($"{indent}{key} = {value}");
                        isKeyFound = true;
                        continue;
                    }
                }

                // 3. 非目标节或非目标键：保留原始行
                newLines.Add(originalLine);
            }

            // 3. 目标节中未找到键：在节末尾新增
            if (isInTargetSection && !isKeyFound)
            {
                int lastSectionIndex = newLines.FindLastIndex(l => IsSectionLine(l.Trim()));
                if (lastSectionIndex != -1)
                {
                    newLines.Insert(lastSectionIndex + 1, $"{key} = {value}");
                }
            }

            // 写回文件（覆盖原内容）
            File.WriteAllLines(ConfigPath, newLines, Encoding.UTF8);
        }


        /// <summary>
        /// 判断是否是节行（如"[General]"）
        /// </summary>
        private static bool IsSectionLine(string trimmedLine) =>
            trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]") && trimmedLine.Length >= 2;

        /// <summary>
        /// 从节行提取节名称（如"[General]" → "General"）
        /// </summary>
        private static string GetSectionName(string sectionLine) =>
            sectionLine.Substring(1, sectionLine.Length - 2).Trim();

        /// <summary>
        /// 判断是否是键值对行（如"IsOutfit = false"）
        /// </summary>
        private static bool IsKeyValueLine(string trimmedLine) =>
            trimmedLine.Contains("=") && !trimmedLine.StartsWith(";") && !trimmedLine.StartsWith("#");

        /// <summary>
        /// 解析键值对行（返回键和值，忽略行内注释）
        /// </summary>
        private static (string key, string value) ParseKeyValueLine(string trimmedLine)
        {
            int splitIdx = trimmedLine.IndexOf('=');
            string key = trimmedLine.Substring(0, splitIdx).Trim();
            string value = trimmedLine.Substring(splitIdx + 1).Trim();

            // 去除值前的注释（如"key = value ; comment" → "value"）
            int commentIdx = value.IndexOf(';');
            if (commentIdx != -1) value = value.Substring(0, commentIdx).Trim();

            return (key, value);
        }









    }
}
