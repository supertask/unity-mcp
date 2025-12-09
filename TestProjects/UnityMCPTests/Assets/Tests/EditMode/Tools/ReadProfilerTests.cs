using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using MCPForUnity.Editor.Tools;
using MCPForUnity.Editor.Helpers;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor.Profiling;

namespace MCPForUnityTests.Editor.Tools
{
    public class ReadProfilerTests
    {
        [Test]
        public void ReadProfiler_NoData_ReturnsError()
        {
            // Note: ProfilerDriver.lastFrameIndex might be -1 if no profiling data exists.
            var parameters = new JObject();
            var result = ReadProfiler.HandleCommand(parameters);

            if (ProfilerDriver.lastFrameIndex == -1)
            {
                Assert.IsInstanceOf<ErrorResponse>(result);
                var error = (ErrorResponse)result;
                // ErrorResponse uses 'Error' or 'Code' property, 'message' is not defined.
                // Constructor sets both Code and Error to the message string.
                Assert.That(error.Error, Does.Contain("Profiler has no data"));
            }
            else
            {
                // If data exists (e.g. from previous run), it should succeed
                Assert.IsInstanceOf<SuccessResponse>(result);
            }
        }

        [Test]
        public void ReadProfiler_WithInvalidFrame_ReturnsError()
        {
            var parameters = new JObject
            {
                ["frame"] = int.MaxValue
            };

            var result = ReadProfiler.HandleCommand(parameters);

            Assert.IsInstanceOf<ErrorResponse>(result);
            var error = (ErrorResponse)result;
            Assert.That(error.Error, Does.Contain("not valid"));
        }

        [Test]
        public void ReadProfiler_DataStructure_IsCorrect()
        {
            if (ProfilerDriver.lastFrameIndex == -1)
            {
                Assert.Ignore("Profiler has no data, skipping structure test.");
                return;
            }

            var parameters = new JObject();
            var result = ReadProfiler.HandleCommand(parameters);

            Assert.IsInstanceOf<SuccessResponse>(result);
            var success = (SuccessResponse)result;
            
            // SuccessResponse uses 'Data' property for content, not 'content'
            var list = success.Data as List<object>;
            Assert.IsNotNull(list);
            
            if (list.Count > 0)
            {
                // Check first item structure
                // Since result uses anonymous types, we serialize to JObject to check properties easily
                var item = JObject.FromObject(list[0]);
                
                Assert.That(item.ContainsKey("id"), "Should have 'id'");
                Assert.That(item.ContainsKey("name"), "Should have 'name'");
                Assert.That(item.ContainsKey("totalTimeMs"), "Should have 'totalTimeMs'");
                Assert.That(item.ContainsKey("selfTimeMs"), "Should have 'selfTimeMs'");
                Assert.That(item.ContainsKey("calls"), "Should have 'calls'");
            }
        }
    }
}
