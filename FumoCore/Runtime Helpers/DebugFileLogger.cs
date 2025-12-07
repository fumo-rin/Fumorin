using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace FumoCore.Tools
{
    public static class DebugFileLogger
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static string logDirectory;
        private static string logFilePath;
        private static bool initialized = false;
        private static bool isRunning = false;

        private static UnityEngine.UI.Text uiLogText;

        private static readonly ConcurrentQueue<string> logQueue = new();
        private static Thread logThread;
        private static ManualResetEvent logSignal = new(false);

        /// <summary>
        /// Whether to write log messages to file.
        /// </summary>
        public static bool EnableFileLogging { get; set; } = true;

        /// <summary>
        /// How many recent logs to keep.
        /// Older ones beyond this count are deleted automatically.
        /// </summary>
        public static int MaxLogFiles { get; set; } = 10;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize() => Initialize();

        public static void Initialize(UnityEngine.UI.Text uiText = null)
        {
            if (initialized) return;
            initialized = true;

            uiLogText = uiText;

            logDirectory = Path.Combine(Application.persistentDataPath, "DebugLogs");
            if (EnableFileLogging)
            {
                if (!Directory.Exists(logDirectory))
                    Directory.CreateDirectory(logDirectory);

                CleanupOldLogs(); // 🔥 delete old logs before starting a new one

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filename = $"DebugLog_{timestamp}.txt";
                logFilePath = Path.Combine(logDirectory, filename);
                File.WriteAllText(logFilePath, "---- Debug Log Start ----\n");
            }

            Application.logMessageReceived += HandleLog;
            Application.quitting += Cleanup;

            if (EnableFileLogging)
            {
                isRunning = true;
                logThread = new Thread(ProcessLogQueue)
                {
                    IsBackground = true,
                    Name = "DebugFileLoggerThread"
                };
                logThread.Start();

                Log("DebugFileLogger initialized and file logging enabled.");
            }
            else
            {
                Log("DebugFileLogger initialized, but file logging disabled.");
            }
        }

        private static void Cleanup()
        {
            Log("DebugFileLogger stopping...");
            if (EnableFileLogging)
            {
                isRunning = false;
                logSignal.Set(); // wake thread
                logThread?.Join();
            }

            Application.logMessageReceived -= HandleLog;
            Application.quitting -= Cleanup;

            initialized = false;
        }

        private static void HandleLog(string logString, string stackTrace, LogType type)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string entry = $"[{timestamp}][{type}] {logString}";

            if (type == LogType.Exception || type == LogType.Error)
                entry += $"\n{stackTrace}";

            if (EnableFileLogging && isRunning)
            {
                logQueue.Enqueue(entry);
                logSignal.Set();
            }

            if (uiLogText != null)
                uiLogText.text += entry + "\n";
        }

        private static void ProcessLogQueue()
        {
            while (isRunning || !logQueue.IsEmpty)
            {
                logSignal.WaitOne();

                while (logQueue.TryDequeue(out string logEntry))
                {
                    try
                    {
                        File.AppendAllText(logFilePath, logEntry + "\n");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"DebugFileLogger failed to write: {ex}");
                    }
                }

                logSignal.Reset();
            }
        }

        /// <summary>
        /// Deletes older log files beyond MaxLogFiles in the DebugLogs directory.
        /// </summary>
        private static void CleanupOldLogs()
        {
            try
            {
                var files = new DirectoryInfo(logDirectory)
                    .GetFiles("DebugLog_*.txt")
                    .OrderByDescending(f => f.CreationTimeUtc)
                    .ToList();

                if (files.Count <= MaxLogFiles) return;

                // Delete older ones beyond the limit
                foreach (var oldFile in files.Skip(MaxLogFiles))
                {
                    try
                    {
                        oldFile.Delete();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to delete old log file: {oldFile.Name} - {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"CleanupOldLogs failed: {ex.Message}");
            }
        }

        public static void Log(string message) => Debug.Log(message);
        public static void LogWarning(string message) => Debug.LogWarning(message);
        public static void LogError(string message) => Debug.LogError(message);
#endif
    }
}
