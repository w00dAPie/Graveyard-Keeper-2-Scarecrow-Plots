using BepInEx.Logging;
using GK2ScarecrowPlots.Configuration;

namespace GK2ScarecrowPlots.Logging
{
    internal static class ModLog
    {
        private static ManualLogSource logger;
        internal static bool IsDebugEnabled => ModConfig.DebugLogging?.Value == true;

        internal static void Initialize(ManualLogSource source)
        {
            logger = source;
        }

        internal static void Info(string message)
        {
            logger?.LogInfo(message);
        }

        internal static void Warning(string message)
        {
            logger?.LogWarning(message);
        }

        internal static void Debug(string message)
        {
            if (!IsDebugEnabled)
            {
                return;
            }

            logger?.LogInfo($"[DEBUG] {message}");
        }
    }
}
