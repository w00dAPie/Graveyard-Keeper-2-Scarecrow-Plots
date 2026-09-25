using BepInEx.Configuration;

namespace GK2ScarecrowPlots.Configuration
{
    internal static class ModConfig
    {
        private const string SectionGeneral = "01 - General";

        private const string SectionVisual = "02 - Visual";

        private const string SectionDebug = "90 - Debug";

        private const string SectionInternal = "99 - Internal";

        internal static ConfigEntry<bool> Enabled { get; private set; }

        internal static ConfigEntry<bool> HideScarecrow { get; private set; }

        internal static ConfigEntry<bool> DebugLogging { get; private set; }

        internal static ConfigEntry<string> CreatedPlotIds { get; private set; }

        private static ConfigFile configFile;

        internal static void Initialize(ConfigFile config, LegacyConfig legacy)
        {
            configFile = config;

            Enabled = config.Bind(
                SectionGeneral,
                "Enabled",
                true,
                "Adds the two garden plots occupied by the scarecrow. "
                    + "Set to false to remove plots previously created by this mod. "
                    + "Any crops planted on those plots will also be removed."
            );

            HideScarecrow = config.Bind(
                SectionVisual,
                "HideScarecrow",
                false,
                "Hides the scarecrow visual and its collision area. "
                    + "The scarecrow will return the next time the garden is loaded "
                    + "after this setting is disabled."
            );

            DebugLogging = config.Bind(
                SectionDebug,
                "EnableDebugLogging",
                false,
                "Enables detailed diagnostic logging for garden detection and plot creation."
            );

            CreatedPlotIds = config.Bind(
                SectionInternal,
                "CreatedPlotIds",
                string.Empty,
                "Internal list of garden plots created by this mod. Do not edit manually."
            );

            if (legacy == null || !legacy.Found)
            {
                return;
            }

            Enabled.Value = legacy.Enabled;

            DebugLogging.Value = legacy.DebugLogging;

            CreatedPlotIds.Value = legacy.CreatedPlotIds;

            config.Save();
        }

        internal static void Save()
        {
            configFile?.Save();
        }
    }
}
