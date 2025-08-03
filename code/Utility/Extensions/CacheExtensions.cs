namespace FoodShelves;

public static class CacheExtensions {
    /// <summary>
    /// Generates a well-distributed hash code for an array of ItemStacks using the FNV-1 algorithm,
    /// typically used to create unique keys for caching meshes of block contents that often collide.
    /// </summary>
    public static int GetStackCacheHashCodeFNV(this ItemStack[] contentStack) {
        if (contentStack == null) return 0;

        unchecked {
            const uint FNV_OFFSET_BASIS = 2166136261;
            const uint FNV_32_PRIME = 16777619;

            uint hash = FNV_OFFSET_BASIS;

            hash = (hash ^ (uint)contentStack.Length.GetHashCode()) * FNV_32_PRIME;

            for (int i = 0; i < contentStack.Length; i++) {
                if (contentStack[i] == null) continue;

                uint collectibleHash = (uint)(contentStack[i].Collectible != null ? contentStack[i].Collectible.Code.GetHashCode() : 0);
                hash = (hash ^ collectibleHash) * FNV_32_PRIME;
            }

            return (int)hash;
        }
    }

    /// <summary>
    /// Retrieves or creates a dictionary for caching mesh references associated with a specific key within the client API's object cache.
    /// </summary>
    public static Dictionary<string, MultiTextureMeshRef> GetCacheDictionary(ICoreClientAPI capi, string meshCacheKey) {
        if (capi.ObjectCache.TryGetValue(meshCacheKey, out object obj)) {
            return obj as Dictionary<string, MultiTextureMeshRef>;
        }
        else {
            var dict = new Dictionary<string, MultiTextureMeshRef>();
            capi.ObjectCache[meshCacheKey] = dict;
            return dict;
        }
    }
}
