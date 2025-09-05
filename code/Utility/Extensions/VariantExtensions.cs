using System.Linq;

namespace FoodShelves;

public static class VariantExtensions {
    /// <summary>
    /// Retrieves the block variant data needed for properly meshing variant textures on blocks.<br/>
    /// Avoid using this method during animation mesh generation.
    /// </summary>
    public static (Shape, ITexPositionSource) GetBlockVariantData(ICoreClientAPI capi, ItemStack stackWithAttributes) {
        Block block = stackWithAttributes.Block;

        AssetLocation assetLoc = block.Shape.Base.Clone();
        string shapeLocation = assetLoc.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
        Shape shape = capi.Assets.TryGet(shapeLocation)?.ToObject<Shape>().Clone();
        if (shape == null) return (null, null);

        if (shape.Textures.Count == 0) {
            foreach (var texture in block.Textures) {
                shape.Textures.Add(texture.Key, texture.Value.Base);
            }
        }

        var stexSource = new ShapeTextureSource(capi, shape, "FS-TextureSource");

        // Custom Textures
        if (stackWithAttributes.Attributes[BaseFSContainer.FSAttributes] is ITreeAttribute tree && block.Attributes["variantTextures"].Exists) {
            foreach (var pair in block.Attributes["variantTextures"].AsObject<Dictionary<string, string[]>>()) {
                string[] texPaths = pair.Value;

                foreach (var attr in tree) {
                    string key = attr.Key;
                    string value = attr.Value?.GetValue()?.ToString() ?? ""; // Null safety when removing a mod from a world.

                    foreach (string texPath in texPaths.Reverse()) { // Reverse to start from the end texture paths (patched textures), first one serving as default
                        if (texPath.Contains($"{{{key}}}")) {
                            string fullTexPath = texPath.Replace($"{{{key}}}", value);

                            if (capi.Assets.TryGet(new AssetLocation(fullTexPath).WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png")) == null) {
                                continue;
                            }

                            shape.Textures[key] = fullTexPath;

                            var ctex = new CompositeTexture(fullTexPath);

                            ctex.Bake(capi.Assets);
                            stexSource.textures[pair.Key] = ctex;
                            break;
                        }
                    }
                }
            }
        }

        return (shape, stexSource);
    }

    /// <summary>
    /// Applies variant textures to a shape based on block attributes. <br/>
    /// Intended for use in animations; do not use GetBlockVariantData here.
    /// </summary>
    public static void ApplyVariantTextures(this Shape shape, BEBaseFSContainer fscontainer) {
        var variantTextures = fscontainer.Block.Attributes?["variantTextures"]?.AsObject<Dictionary<string, string[]>>();

        if (variantTextures == null) return;
        if (fscontainer.VariantAttributes == null) return;

        foreach (var texture in variantTextures) {
            string[] textureValues = texture.Value;

            foreach (string originalTexVal in textureValues.Reverse()) {
                string textureValue = originalTexVal;

                foreach (var attr in fscontainer.VariantAttributes) {
                    string paramPlaceholder = "{" + attr.Key + "}";
                    string paramValue = attr.Value.ToString();
                    textureValue = textureValue.Replace(paramPlaceholder, paramValue);
                }

                if (textureValue.Contains('{') || textureValue.Contains('}')) continue;

                if ((fscontainer.Api as ICoreClientAPI)?.Assets.TryGet(new AssetLocation(textureValue).WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png")) == null) {
                    continue;
                }

                shape.Textures[texture.Key] = textureValue;
                break;
            }
        }
    }

    /// <summary>
    /// Creates an ItemStack from the block entity containing variant attributes
    /// required for correct texture meshing.<br/> Use this in GetBlockVariantMesh().
    /// </summary>
    public static ItemStack GetVariantStack(this BEBaseFSContainer entity) {
        var stack = new ItemStack(entity.Block);
        if (entity.VariantAttributes.Count != 0) {
            stack.Attributes[BaseFSContainer.FSAttributes] = entity.VariantAttributes;
        }

        return stack;
    }
}
