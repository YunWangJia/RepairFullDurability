using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RepairFullDurability
{
    /// <summary>
    /// INI文件辅助工具类（简化版，支持基本段/键操作）
    /// </summary>
    public static class IniHelper
    {
        /// <summary>
        /// 读取INI文件中指定段和键的值
        /// </summary>
        /// <param name="filePath">INI文件路径</param>
        /// <param name="section">段名（如[General]）</param>
        /// <param name="key">键名</param>
        /// <param name="defaultValue">默认值（找不到时返回）</param>
        /// <returns>键的值或默认值</returns>
        public static string ReadValue(string filePath, string section, string key, string defaultValue = "")
        {
            if (!File.Exists(filePath)) return defaultValue;

            var lines = File.ReadAllLines(filePath);
            bool inTargetSection = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // 跳过注释（;/#）和空行
                if (trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#") || string.IsNullOrEmpty(trimmedLine))
                    continue;

                // 检查是否进入目标段
                if (trimmedLine.StartsWith($"[{section}]"))
                {
                    inTargetSection = true;
                    continue;
                }

                // 不在目标段则跳过
                if (!inTargetSection) continue;

                // 遇到下一个段，停止查找
                if (trimmedLine.StartsWith("["))
                    break;

                // 匹配目标键
                if (trimmedLine.StartsWith($"{key} = "))
                {
                    return trimmedLine.Substring($"{key} = ".Length).Trim();
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// 写入值到INI文件的指定段和键
        /// </summary>
        /// <param name="filePath">INI文件路径</param>
        /// <param name="section">段名</param>
        /// <param name="key">键名</param>
        /// <param name="value">要写入的值</param>
        public static void WriteValue(string filePath, string section, string key, string value)
        {
            var lines = File.Exists(filePath) ? File.ReadAllLines(filePath).ToList() : new List<string>();
            bool sectionFound = false;
            int targetKeyIndex = -1;

            // 查找目标段和键的位置
            for (int i = 0; i < lines.Count; i++)
            {
                var trimmedLine = lines[i].Trim();

                if (trimmedLine.StartsWith($"[{section}]"))
                {
                    sectionFound = true;
                    // 查找段内的目标键
                    for (int j = i + 1; j < lines.Count; j++)
                    {
                        var l = lines[j].Trim();
                        if (l.StartsWith(";") || l.StartsWith("#") || string.IsNullOrEmpty(l) || l.StartsWith("["))
                            break;

                        if (l.StartsWith($"{key} = "))
                        {
                            targetKeyIndex = j;
                            break;
                        }
                    }
                    break;
                }
            }

            if (sectionFound)
            {
                if (targetKeyIndex != -1)
                {
                    // 修改现有键的值
                    lines[targetKeyIndex] = $"{key} = {value}";
                }
                else
                {
                    // 添加新键到段末尾
                    int sectionEndIndex = -1;
                    for (int i = 0; i < lines.Count; i++)
                    {
                        if (lines[i].Trim().StartsWith($"[{section}]"))
                        {
                            sectionEndIndex = i;
                            while (sectionEndIndex + 1 < lines.Count && !lines[sectionEndIndex + 1].Trim().StartsWith("["))
                            {
                                sectionEndIndex++;
                            }
                            break;
                        }
                    }
                    if (sectionEndIndex != -1)
                        lines.Insert(sectionEndIndex + 1, $"{key} = {value}");
                }
            }
            else
            {
                // 添加新段和键
                lines.Add($"[{section}]");
                lines.Add($"{key} = {value}");
            }

            // 确保目录存在并写入文件
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllLines(filePath, lines);
        }
    }
}
