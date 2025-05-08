namespace FoodShelves;

public static class Patches {
    public static void PatchCollectibleWhitelist(CollectibleObject obj, KeyValuePair<string, RestrictionData> restrictionData, Dictionary<string, ModelTransform> transformationData) {
        bool shouldPatch = false;

        string token = restrictionData.Key;
        RestrictionData data = restrictionData.Value;

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
        obj.Attributes.Token["fs" + token] = JToken.FromObject(true);

        if (transformationData != null) {
            ModelTransform transformation = obj.GetTransformation(transformationData);
            if (transformation != null) {
                obj.Attributes.Token["on" + token + "Transform"] = JToken.FromObject(transformation);
            }
        }
    }
}