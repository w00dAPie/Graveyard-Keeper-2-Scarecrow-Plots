using System;
using System.Collections.Generic;
using GK2ScarecrowPlots.Configuration;
using GK2ScarecrowPlots.Logging;
using UnityEngine;

namespace GK2ScarecrowPlots.Helpers
{
    internal static class GardenPlotHelper
    {
        internal static WgoData SpawnGardenPlot(GameScene scene, Vector3 position)
        {
            if (scene == null)
            {
                return null;
            }

            ModLog.Debug($"Creating garden plot at {position}.");

            WgoData data = new WgoData("garden_empty", position, scene.Id);

            Wgo spawned = scene.AddWgoData(data);

            if (spawned == null)
            {
                ModLog.Warning($"Failed to create garden plot at {position}.");

                return null;
            }

            RememberCreatedPlot(data);

            ModLog.Debug(
                $"Created garden plot | " + $"UniqueId={data.UniqueId} | " + $"Position={position}"
            );

            return data;
        }

        internal static void RemoveCreatedPlots()
        {
            string storedIds = ModConfig.CreatedPlotIds.Value;

            if (string.IsNullOrWhiteSpace(storedIds))
            {
                return;
            }

            string[] ids = storedIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            List<string> remainingIds = new List<string>();

            foreach (string idString in ids)
            {
                string trimmed = idString.Trim();

                if (!Guid.TryParse(trimmed, out Guid guidValue))
                {
                    ModLog.Warning($"Invalid stored garden plot ID: {trimmed}");

                    remainingIds.Add(trimmed);

                    continue;
                }

                SGuid guid = new SGuid(guidValue);

                WgoData wgo = MainGame.WorldData.GetWgoData(guid);

                if (wgo == null)
                {
                    ModLog.Debug($"Stored garden plot no longer exists: {trimmed}");

                    continue;
                }

                if (wgo.Definition == null || wgo.Definition.wgoGroup != "garden_bed")
                {
                    ModLog.Warning(
                        $"Stored object {trimmed} is not a garden bed. " + "It will not be removed."
                    );

                    remainingIds.Add(trimmed);

                    continue;
                }

                ModLog.Debug(
                    $"Removing garden plot | "
                        + $"UniqueId={wgo.UniqueId} | "
                        + $"ID={wgo.id} | "
                        + $"Position={wgo.Position}"
                );

                MainGame.WorldData.RemoveWgoDataFromGameScene(wgo);
            }

            ModConfig.CreatedPlotIds.Value = string.Join(",", remainingIds);

            ModConfig.Save();
        }

        private static void RememberCreatedPlot(WgoData data)
        {
            if (data == null)
            {
                return;
            }

            string id = data.UniqueId.ToString();

            HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string existing = ModConfig.CreatedPlotIds.Value;

            if (!string.IsNullOrWhiteSpace(existing))
            {
                foreach (string value in existing.Split(','))
                {
                    string trimmed = value.Trim();

                    if (!string.IsNullOrWhiteSpace(trimmed))
                    {
                        ids.Add(trimmed);
                    }
                }
            }

            if (!ids.Add(id))
            {
                return;
            }

            ModConfig.CreatedPlotIds.Value = string.Join(",", ids);

            ModConfig.Save();

            ModLog.Debug($"Remembered garden plot: {id}");
        }
    }
}
