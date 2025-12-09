using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.Profiling;
using Newtonsoft.Json.Linq;
using MCPForUnity.Editor.Helpers;

namespace MCPForUnity.Editor.Tools
{
    [McpForUnityTool("read_profiler", Description = "Reads profiling data for a specific frame.")]
    public static class ReadProfiler
    {
        public static object HandleCommand(JObject @params)
        {
            // Check if Profiler has any data
            if (ProfilerDriver.lastFrameIndex == -1)
            {
                return new ErrorResponse("Profiler has no data. Please ensure the Profiler is recording or has recorded frames.");
            }

            int frameIndex = @params["frame"]?.ToObject<int>() ?? ProfilerDriver.lastFrameIndex;

            var results = new List<object>();

            // ProfilerDriver.GetHierarchyFrameDataView is the correct way to get the view in newer Unity versions.
            // Parameters: frameIndex, threadIndex (0 for main), viewMode, sortColumn, sortAscending
            // Using threadIndex 0 (Main Thread) by default.
            using (var frameData = ProfilerDriver.GetHierarchyFrameDataView(frameIndex, 0, HierarchyFrameDataView.ViewModes.Default, HierarchyFrameDataView.columnTotalTime, false))
            {
                if (frameData == null || !frameData.valid)
                {
                    // Fallback to last available frame if specific frame is invalid
                    if (frameIndex != ProfilerDriver.lastFrameIndex)
                    {
                        return new ErrorResponse($"Frame {frameIndex} is not valid. Last valid frame is {ProfilerDriver.lastFrameIndex}.");
                    }
                    return new ErrorResponse($"Frame {frameIndex} is not valid or unavailable.");
                }

                // In Unity 2022.3+, we use static column IDs directly provided by HierarchyFrameDataView
                // instead of searching by name, as GetColumnName might not be available or stable.
                // Standard columns:
                int totalTimeCol = HierarchyFrameDataView.columnTotalTime;
                int selfTimeCol = HierarchyFrameDataView.columnSelfTime;
                int callsCol = HierarchyFrameDataView.columnCalls;

                int rootId = frameData.GetRootItemID();
                ProcessItem(frameData, rootId, results, totalTimeCol, selfTimeCol, callsCol, 0);
            }

            return new SuccessResponse($"Retrieved profiling data for frame {frameIndex} ({results.Count} items)", results);
        }

        private static void ProcessItem(HierarchyFrameDataView data, int itemId, List<object> results, int totalTimeCol, int selfTimeCol, int callsCol, int depth)
        {
            var name = data.GetItemName(itemId);
            
            // GetItemColumnDataAsFloat works for Time/Percent columns.
            float totalTime = data.GetItemColumnDataAsFloat(itemId, totalTimeCol);
            float selfTime = data.GetItemColumnDataAsFloat(itemId, selfTimeCol);
            
            // Calls is often displayed as string (e.g. "1") but underlying might be different.
            // Using GetItemColumnData with explicit type handling if needed, but AsFloat is usually safe for numeric views.
            // For 'Calls', it returns the count.
            int calls = (int)data.GetItemColumnDataAsFloat(itemId, callsCol);

            results.Add(new
            {
                id = itemId,
                name = name,
                totalTimeMs = totalTime,
                selfTimeMs = selfTime,
                calls = calls,
                depth = depth
            });

            if (data.HasItemChildren(itemId))
            {
                var children = new List<int>();
                data.GetItemChildren(itemId, children);
                foreach (var childId in children)
                {
                    ProcessItem(data, childId, results, totalTimeCol, selfTimeCol, callsCol, depth + 1);
                }
            }
        }
    }
}
