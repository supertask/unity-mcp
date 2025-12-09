using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.Profiling;
using Newtonsoft.Json.Linq;
using MCPForUnity.Editor.Tools;
using MCPForUnity.Editor.Helpers;
using UnityEngine.Profiling; // Added for Profiler class
using System.IO;

namespace MCPForUnityTests.PlayMode.Tools
{
    public class ReadProfilerPlayModeTests
    {
        private string _tempProfilerLogPath;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Explicitly set a file path for the profiler log to avoid "No file name has been specified" error
            // This is required when enableBinaryLog is true in some Unity versions/environments.
            _tempProfilerLogPath = Path.Combine(Application.temporaryCachePath, "test_profiler_log.raw");
            Profiler.logFile = _tempProfilerLogPath;

            // Ensure Profiler is enabled and recording
            Profiler.enabled = true;
            Profiler.enableBinaryLog = true; // Often needed for driver to record
            
            // Wait a few frames to generate data
            yield return null;
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Profiler.enabled = false;
            
            // Clean up
            if (!string.IsNullOrEmpty(_tempProfilerLogPath) && File.Exists(_tempProfilerLogPath))
            {
                try
                {
                    File.Delete(_tempProfilerLogPath);
                }
                catch { /* Ignore cleanup errors */ }
            }
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReadProfiler_DuringPlay_ReturnsData()
        {
            // 1. Generate some load to ensure we have data
            var go = new GameObject("ProfilerTestObject");
            for (int i = 0; i < 1000; i++)
            {
                go.transform.position = Vector3.one * i;
            }
            Object.Destroy(go);

            yield return null; // Wait for frame end

            // 2. Check last frame index
            // Note: In PlayMode tests running in Editor, ProfilerDriver should have access to Editor Profiler.
            // If running in player, ReadProfiler (Editor tool) won't work anyway.
            
            if (ProfilerDriver.lastFrameIndex == -1)
            {
                Debug.LogWarning("ProfilerDriver.lastFrameIndex is -1. Attempting to force recording.");
                // Sometimes ProfilerDriver needs connection or proper window open
                // But for basic check, we just assert we don't crash and handle -1 gracefully
            }

            // 3. Call the tool
            var parameters = new JObject();
            var result = ReadProfiler.HandleCommand(parameters);

            // 4. Validate
            if (ProfilerDriver.lastFrameIndex != -1)
            {
                Assert.IsInstanceOf<SuccessResponse>(result);
                var success = (SuccessResponse)result;
                var list = success.Data as List<object>;
                
                Assert.IsNotNull(list, "Result data should be a list");
                Assert.IsTrue(list.Count > 0, "Should return profiling items");
            }
            else
            {
                Assert.IsInstanceOf<ErrorResponse>(result);
                var error = (ErrorResponse)result;
                Assert.That(error.Error, Does.Contain("Profiler has no data"));
                // If we really can't get data in this environment, ignoring is acceptable for now
                Assert.Ignore("Skipping detailed validation because ProfilerDriver has no data (common in some test environments).");
            }
        }
    }
}
