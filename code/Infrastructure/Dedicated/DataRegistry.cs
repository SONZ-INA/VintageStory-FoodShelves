using System.Linq;

namespace FoodShelves;

public static class DataRegistry {
    /// <summary>
    /// Discovers all restriction files present in the mod.
    /// </summary>
    public static Dictionary<string, string[]> DiscoverRestrictionGroups(ICoreAPI api) {
        var restrictionGroups = new Dictionary<string, string[]>();
        string basePath = "config/restrictions/";

        var restrictionAssets = api.Assets.GetMany("config/restrictions", "foodshelves", false);

        foreach (var asset in restrictionAssets) {
            string fullPath = asset.Location.Path;

            string relativePath = fullPath[basePath.Length..];
            string[] pathParts = relativePath.Split('/');

            if (pathParts.Length >= 2) {
                string folderName = pathParts[0];
                string fileName = pathParts[1];

                if (fileName.EndsWith(".json")) {
                    fileName = fileName[..^5];
                }

                if (!restrictionGroups.TryGetValue(folderName, out string[]? value)) {
                    value = [];
                    restrictionGroups[folderName] = value;
                }

                var currentFiles = value.ToList();
                if (!currentFiles.Contains(fileName)) {
                    currentFiles.Add(fileName);
                    restrictionGroups[folderName] = [.. currentFiles];
                }
            }
        }

        // Remove folders that have no files
        var foldersToRemove = restrictionGroups.Where(kvp => kvp.Value.Length == 0).Select(kvp => kvp.Key).ToList();
        foreach (var folder in foldersToRemove) {
            restrictionGroups.Remove(folder);
        }

        return restrictionGroups;
    }

    /// <summary>
    /// Loads the Restriction and Transformation data from json files.
    /// </summary>
    public static void LoadData(
        ICoreAPI api, 
        Dictionary<string, string[]> restrictionGroups,
        Dictionary<string, RestrictionData> restrictions,
        Dictionary<string, Dictionary<string, ModelTransform>> transformations
    ) {
        foreach (var (category, names) in restrictionGroups) {
            foreach (var name in names) {
                string restrictionPath = $"foodshelves:config/restrictions/{category}/{name}.json".Replace("//", "/");
                string transformationPath = $"foodshelves:config/transformations/{category}/{name}.json".Replace("//", "/");

                restrictions[name] = api.LoadAsset<RestrictionData>(restrictionPath);

                if (api.Assets.Exists(transformationPath)) {
                    transformations[name] = api.LoadAsset<Dictionary<string, ModelTransform>>(transformationPath);
                }
            }
        }
    }
}
