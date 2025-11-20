using System;
using System.Collections.Generic;
using System.Linq;
using MicroDock.Model;
using MicroDock.Utils;
using Serilog;

namespace MicroDock.Service;

/// <summary>
/// 插件依赖解析器
/// 负责：
/// 1. 验证依赖是否满足
/// 2. 检测循环依赖
/// 3. 使用拓扑排序确定加载顺序
/// </summary>
public class DependencyResolver
{
    /// <summary>
    /// 解析结果
    /// </summary>
    public class ResolveResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误信息（如果失败）
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 按依赖顺序排列的插件清单列表（成功时）
        /// </summary>
        public List<PluginManifest>? OrderedManifests { get; set; }
    }

    /// <summary>
    /// 解析插件依赖并确定加载顺序
    /// </summary>
    /// <param name="manifests">所有插件的清单列表</param>
    /// <returns>解析结果</returns>
    public static ResolveResult Resolve(List<PluginManifest> manifests)
    {
        if (manifests == null || manifests.Count == 0)
        {
            return new ResolveResult
            {
                Success = true,
                OrderedManifests = new List<PluginManifest>()
            };
        }

        // 构建插件名称到清单的映射
        var manifestMap = new Dictionary<string, PluginManifest>();
        foreach (var manifest in manifests)
        {
            if (manifestMap.ContainsKey(manifest.Name))
            {
                return new ResolveResult
                {
                    Success = false,
                    ErrorMessage = $"发现重复的插件名称: {manifest.Name}"
                };
            }
            manifestMap[manifest.Name] = manifest;
        }

        // 1. 验证所有依赖是否存在且版本匹配
        foreach (var manifest in manifests)
        {
            if (manifest.Dependencies == null || manifest.Dependencies.Count == 0)
                continue;

            foreach (var dependency in manifest.Dependencies)
            {
                var depName = dependency.Key;
                var versionRange = dependency.Value;

                // 检查依赖的插件是否存在
                if (!manifestMap.TryGetValue(depName, out var depManifest))
                {
                    return new ResolveResult
                    {
                        Success = false,
                        ErrorMessage = $"插件 '{manifest.Name}' 依赖的插件 '{depName}' 未找到"
                    };
                }

                // 检查版本是否匹配
                var depVersion = VersionHelper.ParseVersion(depManifest.Version);
                if (depVersion == null)
                {
                    return new ResolveResult
                    {
                        Success = false,
                        ErrorMessage = $"插件 '{depName}' 的版本号 '{depManifest.Version}' 无法解析"
                    };
                }

                if (!VersionHelper.MatchesRange(depVersion, versionRange))
                {
                    return new ResolveResult
                    {
                        Success = false,
                        ErrorMessage = $"插件 '{manifest.Name}' 需要 '{depName}' 版本 '{versionRange}'，但实际版本为 '{depManifest.Version}'"
                    };
                }
            }
        }

        // 2. 检测循环依赖
        var cycleCheckResult = DetectCycle(manifests, manifestMap);
        if (!cycleCheckResult.Success)
        {
            return cycleCheckResult;
        }

        // 3. 拓扑排序确定加载顺序
        var orderedManifests = TopologicalSort(manifests, manifestMap);
        if (orderedManifests == null)
        {
            return new ResolveResult
            {
                Success = false,
                ErrorMessage = "拓扑排序失败（可能存在循环依赖）"
            };
        }

        return new ResolveResult
        {
            Success = true,
            OrderedManifests = orderedManifests
        };
    }

    /// <summary>
    /// 检测循环依赖
    /// </summary>
    private static ResolveResult DetectCycle(List<PluginManifest> manifests, Dictionary<string, PluginManifest> manifestMap)
    {
        var visiting = new HashSet<string>();
        var visited = new HashSet<string>();

        foreach (var manifest in manifests)
        {
            if (!visited.Contains(manifest.Name))
            {
                var cycle = DetectCycleDFS(manifest.Name, manifestMap, visiting, visited);
                if (cycle != null)
                {
                    return new ResolveResult
                    {
                        Success = false,
                        ErrorMessage = $"检测到循环依赖: {string.Join(" -> ", cycle)}"
                    };
                }
            }
        }

        return new ResolveResult { Success = true };
    }

    /// <summary>
    /// DFS 检测循环依赖
    /// </summary>
    private static List<string>? DetectCycleDFS(
        string pluginName,
        Dictionary<string, PluginManifest> manifestMap,
        HashSet<string> visiting,
        HashSet<string> visited)
    {
        if (visiting.Contains(pluginName))
        {
            // 找到循环
            return new List<string> { pluginName };
        }

        if (visited.Contains(pluginName))
        {
            return null; // 已访问过，没有循环
        }

        visiting.Add(pluginName);

        if (manifestMap.TryGetValue(pluginName, out var manifest) && manifest.Dependencies != null)
        {
            foreach (var dependency in manifest.Dependencies.Keys)
            {
                var cycle = DetectCycleDFS(dependency, manifestMap, visiting, visited);
                if (cycle != null)
                {
                    cycle.Add(pluginName);
                    return cycle;
                }
            }
        }

        visiting.Remove(pluginName);
        visited.Add(pluginName);

        return null;
    }

    /// <summary>
    /// 拓扑排序（Kahn 算法）
    /// </summary>
    private static List<PluginManifest>? TopologicalSort(
        List<PluginManifest> manifests,
        Dictionary<string, PluginManifest> manifestMap)
    {
        // 计算每个插件的入度（被依赖的次数）
        var inDegree = new Dictionary<string, int>();
        var adjacencyList = new Dictionary<string, List<string>>();

        // 初始化
        foreach (var manifest in manifests)
        {
            inDegree[manifest.Name] = 0;
            adjacencyList[manifest.Name] = new List<string>();
        }

        // 构建邻接表和计算入度
        foreach (var manifest in manifests)
        {
            if (manifest.Dependencies == null)
                continue;

            foreach (var depName in manifest.Dependencies.Keys)
            {
                // depName -> manifest.Name (depName 被 manifest 依赖)
                adjacencyList[depName].Add(manifest.Name);
                inDegree[manifest.Name]++;
            }
        }

        // 找出所有入度为 0 的插件（没有依赖的插件）
        var queue = new Queue<string>();
        foreach (var kvp in inDegree)
        {
            if (kvp.Value == 0)
            {
                queue.Enqueue(kvp.Key);
            }
        }

        // 拓扑排序
        var result = new List<PluginManifest>();
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(manifestMap[current]);

            // 减少所有依赖当前插件的插件的入度
            foreach (var dependent in adjacencyList[current])
            {
                inDegree[dependent]--;
                if (inDegree[dependent] == 0)
                {
                    queue.Enqueue(dependent);
                }
            }
        }

        // 如果结果数量不等于输入数量，说明存在循环依赖
        if (result.Count != manifests.Count)
        {
            Log.Error("拓扑排序失败：预期 {Expected} 个插件，实际排序了 {Actual} 个插件", manifests.Count, result.Count);
            return null;
        }

        return result;
    }
}

