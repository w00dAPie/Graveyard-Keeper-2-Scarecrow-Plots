using System.Collections.Generic;
using System.IO;

namespace GK2ScarecrowPlots.Configuration
{
    internal sealed class LegacyConfig
    {
        internal bool Found;

        internal bool Enabled = true;

        internal bool DebugLogging;

        internal string CreatedPlotIds = string.Empty;
    }

    internal static class ConfigMigration
    {
        internal static LegacyConfig ReadLegacy(string path)
        {
            LegacyConfig result = new LegacyConfig();

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return result;
            }

            string section = string.Empty;

            foreach (string rawLine in File.ReadAllLines(path))
            {
                string line = rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                {
                    continue;
                }

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    section = line.Substring(1, line.Length - 2);

                    continue;
                }

                int separator = line.IndexOf('=');

                if (separator < 0)
                {
                    continue;
                }

                string key = line.Substring(0, separator).Trim();

                string value = line.Substring(separator + 1).Trim();

                if (
                    section == "General"
                    && key == "Enabled"
                    && bool.TryParse(value, out bool enabled)
                )
                {
                    result.Enabled = enabled;

                    result.Found = true;

                    continue;
                }

                if (
                    section == "Debug"
                    && key == "EnableDebugLogging"
                    && bool.TryParse(value, out bool debugLogging)
                )
                {
                    result.DebugLogging = debugLogging;

                    result.Found = true;

                    continue;
                }

                if (section == "Internal" && key == "CreatedPlotIds")
                {
                    result.CreatedPlotIds = value;

                    result.Found = true;
                }
            }

            return result;
        }

        internal static void RemoveLegacySections(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return;
            }

            string[] lines = File.ReadAllLines(path);

            List<string> output = new List<string>();

            string section = string.Empty;

            foreach (string rawLine in lines)
            {
                string trimmed = rawLine.Trim();

                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    section = trimmed.Substring(1, trimmed.Length - 2);
                }

                if (section == "General" || section == "Debug" || section == "Internal")
                {
                    continue;
                }

                output.Add(rawLine);
            }

            File.WriteAllLines(path, output);
        }
    }
}
