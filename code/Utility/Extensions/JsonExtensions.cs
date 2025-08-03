namespace FoodShelves;

public static class JsonExtensions {
    /// <summary>
    /// Ensures that the <c>Attributes</c> property of the collectible is initialized.<br/>
    /// Prevents null reference errors when accessing or modifying attributes.
    /// </summary>
    public static void EnsureAttributesNotNull(this CollectibleObject obj)
        => obj.Attributes ??= new JsonObject(new JObject());

    /// <summary>
    /// Loads and deserializes an asset from the given path into the specified type.
    /// </summary>
    public static T LoadAsset<T>(this ICoreAPI api, string path)
        => api.Assets.Get(new AssetLocation(path)).ToObject<T>();
}
