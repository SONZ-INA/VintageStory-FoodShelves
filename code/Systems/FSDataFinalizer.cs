namespace FoodShelves;

public static class FSDataFinalizer {
    /// <summary>
    /// Applies all defined restrictions and optional transformations to collectible items needed for this mod.
    /// </summary>
    public static void FinalizeAssets(ICoreAPI api, Dictionary<string, RestrictionData> restrictions, Dictionary<string, Dictionary<string, ModelTransform>> transformations) {
        foreach (CollectibleObject obj in api.World.Collectibles) {
            foreach (var restriction in restrictions) {
                transformations.TryGetValue(restriction.Key, out var transformation);
                PatchCollectibleWhitelist(obj, restriction, transformation);
            }
        }
    }

    /// <summary>
    /// Checks if a collectible matches a restriction and applies tags and optional model transformation data.
    /// </summary>
    private static void PatchCollectibleWhitelist(CollectibleObject obj, KeyValuePair<string, RestrictionData> restrictionData, Dictionary<string, ModelTransform>? transformationData) {
        bool shouldPatch = false;

        string token = restrictionData.Key;
        RestrictionData data = restrictionData.Value;

        if (data.BlacklistedCodes != null) {
            if (WildcardUtil.Match(data.BlacklistedCodes, obj.Code.ToString())) {
                return;
            }
        }

        if (data.GroupingCodes != null && data.GroupingCodes.Count > 0) {
            foreach (var group in data.GroupingCodes.Values) {
                foreach (var code in group) {
                    if (WildcardUtil.Match(code, obj.Code.ToString())) {
                        shouldPatch = true;
                        break;
                    }
                }
                if (shouldPatch) break;
            }
        }
        else {
            if (obj.CheckTypedRestriction(data) || WildcardUtil.Match(data.CollectibleCodes, obj.Code.ToString())) {
                shouldPatch = true;
            }
        }

        if (!shouldPatch) return;

        obj.EnsureAttributesNotNull();
        obj.Attributes.Token!["fs" + token] = JToken.FromObject(true);

        if (transformationData != null) {
            ModelTransform? transformation = obj.GetTransformation(transformationData);
            if (transformation != null) {
                obj.Attributes.Token["on" + token + "Transform"] = JToken.FromObject(transformation);
            }
        }
    }
}
