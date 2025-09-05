namespace FoodShelves;

public static class MeshExtensions {
    /// <summary>
    /// Rotates the mesh around the Y-axis based on the block's predefined <c>rotateY</c> value.<br/>
    /// Useful for aligning meshes with the block's in-world orientation.
    /// </summary>
    public static MeshData BlockYRotation(this MeshData mesh, Block block)
        => mesh?.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, block.Shape.rotateY * GameMath.DEG2RAD, 0);

    /// <summary>
    /// Updates the texture key for all faces in the shape’s root element and its children.
    /// </summary>
    public static void ChangeTextureKey(this Shape shape, string key) {
        foreach (var face in shape.Elements[0].FacesResolved) {
            face.Texture = key;
        }

        foreach (var child in shape.Elements[0].Children) {
            foreach (var face in child.FacesResolved) {
                if (face != null) face.Texture = key;
            }
        }
    }

    /// <summary>
    /// Replaces the texture key of all resolved faces in the first <see cref="ShapeElement"/> and its child elements within the given <see cref="Shape"/>.
    /// </summary>
    public static void ChangeShapeTextureKey(this Shape shape, string key) {
        foreach (var face in shape.Elements[0].FacesResolved) {
            face.Texture = key;
        }

        foreach (var child in shape.Elements[0].Children) {
            foreach (var face in child.FacesResolved) {
                if (face != null) face.Texture = key;
            }
        }
    }

    public static Dictionary<string, AssetLocation> AddOverlays(ICoreClientAPI clientApi, ShapeTextureSource textureSource, Shape shape, CompositeTexture texture) {
        const string prefix = "ov_";
        
        shape.SubclassForStepParenting(prefix);
        Dictionary<string, AssetLocation> prefixedTextureCodes = shape.Textures;
        shape.Textures = new Dictionary<string, AssetLocation>(prefixedTextureCodes.Count);

        foreach (var entry in prefixedTextureCodes) {
            shape.Textures[prefix + entry.Key] = entry.Value;
        }

        Shape resolvedOverlayShape;
        foreach (var overlay in texture.BlendedOverlays) {
            AssetLocation ass = overlay.Base.Clone().WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
            resolvedOverlayShape = clientApi.Assets.TryGet(ass)?.ToObject<Shape>();

            if (resolvedOverlayShape == null) continue;

            resolvedOverlayShape.WalkElements("*", (e) => {
                if (!string.IsNullOrEmpty(e.StepParentName)) {
                    e.StepParentName = prefix + e.StepParentName;
                }
            });

            shape.StepParentShape(resolvedOverlayShape, overlay.Base, overlay.Base, clientApi.Logger, (textureCode, textureLocation) => {
                textureSource.textures[textureCode] = new CompositeTexture(textureLocation);
            });
        }

        return prefixedTextureCodes;
    }
}
