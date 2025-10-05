using System.Linq;

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

    /// <summary>
    /// Recursively removes elements and their children whose names are in skipElements.
    /// </summary>
    public static ShapeElement[] RemoveElements(ShapeElement[] elementArray, string[] skipElements) {
        var remainingElements = elementArray.Where(e => !skipElements.Contains(e.Name)).ToArray();
        foreach (var element in remainingElements) {
            if (element.Children != null && element.Children.Length > 0) {
                element.Children = RemoveElements(element.Children, skipElements); // Recursively filter children
            }
        }

        return remainingElements;
    }

    /// <summary>
    /// Returns a pie texture source based on the 'inPieProperties' attribute.
    /// </summary>
    public static ITexPositionSource GetPieTexture(ICoreClientAPI capi, ItemStack[] contents, Shape shape, string itemPath) {
        if (capi == null || shape == null || contents == null || contents.Length == 0) 
            return null;

        var item = contents[0];
        var pieProps = item.ItemAttributes?["inPieProperties"];
        if (pieProps?.Exists != true)
            return null;

        // Exception for WC:FN
        var texturePath = itemPath.EndsWith("-beachalmondwhole")
            ? "wildcraftfruit:block/food/pie/fill-beachalmond"
            : pieProps["texture"]?.ToString();

        if (string.IsNullOrEmpty(texturePath))
            return null;

        var textureLoc = new AssetLocation(texturePath);

        // Special case: remove "ground" from peanut textures
        if (textureLoc.ToString().Contains("peanutground"))
            textureLoc = new AssetLocation(textureLoc.ToString().Replace("ground", ""));

        // Apply to shape
        shape.Textures.Clear();
        shape.Textures["surface"] = textureLoc;

        return new ShapeTextureSource(capi, shape, "FS-LiquidyTextureSource");
    }

    /// <summary>
    /// Returns a texture source defined by the item's 'inContainerTexture' attribute.
    /// </summary>
    public static ITexPositionSource GetContainerTextureSource(ICoreClientAPI capi, ItemStack[] contents) {
        if (capi == null || contents == null || contents.Length == 0) 
            return null;

        var item = contents[0];
        var texAttr = item.ItemAttributes?["inContainerTexture"];
        if (texAttr?.Exists != true) 
            return null;

        var texture = texAttr.AsObject<CompositeTexture>();
        return new ContainerTextureSource(capi, item, texture);
    }

    /// <summary>
    /// Returns a texture source using the item's first available texture.
    /// </summary>
    public static ITexPositionSource GetItemTextureSource(ICoreClientAPI capi, ItemStack[] contents) {
        if (capi == null || contents == null || contents.Length == 0) 
            return null;

        var item = contents[0];
        var firstTexture = contents[0].Item?.Textures?.Values?.FirstOrDefault();
        if (firstTexture == null) 
            return null;

        return new ContainerTextureSource(capi, item, firstTexture);
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
