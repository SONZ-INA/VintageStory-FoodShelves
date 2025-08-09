using System.Linq;
using Vintagestory.ServerMods;

namespace FoodShelves;

public static class Meshing {
    /// <summary>
    /// Generates a mesh for a block that has (or doesn't have) any attributes set to get textures from. Mainly used for many plank textures.
    /// skipElements is used to remove specific cubes from the model (recursive) before meshing.
    /// </summary>
    public static MeshData GenBlockVariantMesh(ICoreAPI api, ItemStack stackWithAttributes, string[] skipElements = null) {
        if (api is not ICoreClientAPI capi) return null;

        Block block = stackWithAttributes.Block;
        var variantData = GetBlockVariantData(capi, stackWithAttributes);

        if (skipElements?.Length > 0) {
            ShapeElement[] RemoveElements(ShapeElement[] elementArray) {
                var remainingElements = elementArray.Where(e => !skipElements.Contains(e.Name)).ToArray();
                foreach (var element in remainingElements) {
                    if (element.Children != null && element.Children.Length > 0) {
                        element.Children = RemoveElements(element.Children); // Recursively filter children
                    }
                }
                return remainingElements;
            }

            variantData.Item1.Elements = RemoveElements(variantData.Item1.Elements);
        }

        capi.Tesselator.TesselateShape("FS-TesselateShape", variantData.Item1, out MeshData blockMesh, variantData.Item2);

        float scale = block.Shape.Scale;
        if (scale != 1) blockMesh.Scale(new Vec3f(.5f, 0, .5f), scale, scale, scale);

        return blockMesh.BlockYRotation(block);
    }

    /// <summary>
    /// Changes the block shape to another shape. Please note that the textures should be the same for the substitute shape.
    /// </summary>
    public static MeshData SubstituteBlockShape(ICoreAPI Api, ITesselatorAPI tesselator, string shapePath, Block texturesFromBlock) {
        AssetLocation shapeLocation = new(shapePath);
        ITexPositionSource texSource = tesselator.GetTextureSource(texturesFromBlock);
        Shape shape = Api.Assets.TryGet(shapeLocation)?.ToObject<Shape>();
        if (shape == null) return null;

        tesselator.TesselateShape(null, shape, out MeshData mesh, texSource);
        return mesh;
    }

    /// <summary>
    /// Changes the item shape to another shape. Please note that the textures should be the same for the substitute shape.
    /// </summary>
    public static MeshData SubstituteItemShape(ICoreAPI Api, ITesselatorAPI tesselator, string shapePath, Item texturesFromItem) {
        AssetLocation shapeLocation = new(shapePath);
        ITexPositionSource texSource = tesselator.GetTextureSource(texturesFromItem);
        Shape shape = Api.Assets.TryGet(shapeLocation)?.ToObject<Shape>();
        if (shape == null) return null;

        tesselator.TesselateShape(null, shape, out MeshData mesh, texSource);
        return mesh;
    }

    /// <summary>
    /// Generates the content mesh of the block's inventory. Mainly used for generating basket contents.
    /// </summary>
    public static MeshData GenContentMesh(ICoreClientAPI capi, ItemStack[] contents, float[,] transformationMatrix, float scaleValue = 1f, Dictionary<string, ModelTransform> modelTransformations = null) {
        if (capi == null) return null;

        MeshData nestedContentMesh = null;
        for (int i = 0; i < contents.Length; i++) {
            if (contents[i] == null || contents[i].Item == null) continue;

            string shapeLocation = contents[i].Item.Shape?.Base;
            if (shapeLocation == null) continue;

            Shape shape = capi.TesselatorManager.GetCachedShape(shapeLocation)?.Clone();
            if (shape == null) continue;

            // Handle overlays if present
            //CompositeShape compositeShape = contents[i].Item.Shape.Clone();
            //if (compositeShape?.Overlays != null && compositeShape.Overlays.Length > 0) {
            //    string overlayPrefix = $"ov_{i}_";
            //    shape.SubclassForStepParenting(overlayPrefix);

            //    // Prefix existing textures
            //    var originalTextures = new Dictionary<string, AssetLocation>(shape.Textures);
            //    shape.Textures.Clear();
            //    foreach (var entry in originalTextures) {
            //        shape.Textures[overlayPrefix + entry.Key] = entry.Value;
            //    }

            //    // Add overlays
            //    foreach (var overlay in compositeShape.Overlays) {
            //        var overlayBase = overlay.Base.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
            //        Shape overlayShape = capi.Assets.TryGet(overlayBase)?.ToObject<Shape>();
            //        if (overlayShape == null) continue;

            //        overlayShape.WalkElements("*", (e) => {
            //            if (!string.IsNullOrEmpty(e.StepParentName)) {
            //                e.StepParentName = overlayPrefix + e.StepParentName;
            //            }
            //        });

            //        shape.StepParentShape(overlayShape, overlayBase.ToString(), compositeShape.Base.ToString(), capi.Logger, null);
            //    }
            //}

            if (shape.Textures.Count == 0) {
                foreach (var texture in contents[i].Item.Textures) {
                    shape.Textures.Add(texture.Key, texture.Value.Base);
                }
            }

            var texSource = new ShapeTextureSource(capi, shape, "FS-ShapeTextureSource");
            foreach (var textureDict in shape.Textures) {
                CompositeTexture cTex = new(textureDict.Value);
                cTex.Bake(capi.Assets);
                texSource.textures[textureDict.Key] = cTex;
            }

            capi.Tesselator.TesselateShape("FS-TesselateContent", shape, out MeshData collectibleMesh, texSource);

            int offset = transformationMatrix.GetLength(1);
            if (i < offset) {
                if (modelTransformations != null) {
                    ModelTransform transformation = contents[i].Item.GetTransformation(modelTransformations);
                    if (transformation != null) collectibleMesh.ModelTransform(transformation);
                }

                float[] matrixTransform =
                    new Matrixf()
                    .Translate(0.5f, 0, 0.5f)
                    .RotateXDeg(transformationMatrix[3, i])
                    .RotateYDeg(transformationMatrix[4, i])
                    .RotateZDeg(transformationMatrix[5, i])
                    .Scale(scaleValue, scaleValue, scaleValue)
                    .Translate(transformationMatrix[0, i] - 0.84375f, transformationMatrix[1, i], transformationMatrix[2, i] - 0.8125f)
                    .Values;

                collectibleMesh.MatrixTransform(matrixTransform);
            }

            if (nestedContentMesh == null) nestedContentMesh = collectibleMesh;
            else nestedContentMesh.AddMeshData(collectibleMesh);
        }

        return nestedContentMesh;
    }

    /// <summary>
    /// Generates a mesh that has a specific "util" shape that resemble liquids or stuff that can behave as liquids.
    /// The parent of the util shape will scale up depending on stacksize passed, and the children of that parent will simply move up.
    /// </summary>
    public static MeshData GenLiquidyMesh(ICoreClientAPI capi, ItemStack[] contents, string pathToFillShape, float maxHeight, bool inheritTextures = true) {
        if (capi == null) return null;
        if (contents == null || contents.Length == 0 || contents[0] == null) return null;
        if (pathToFillShape == null || pathToFillShape == "") return null;

        // Shape location of a simple cube, meant as "filling"
        AssetLocation shapeLocation = new(pathToFillShape);
        Shape shape = Shape.TryGet(capi, shapeLocation)?.Clone();
        if (shape == null) return null;
        string itemPath = contents[0].Item.Code.Path;

        // Handle textureSource
        ITexPositionSource texSource;

        if (inheritTextures) {
            if (contents[0].ItemAttributes?["inPieProperties"].Exists == true) { // First try pie textures
                AssetLocation textureRerouteLocation;

                // Exceptions
                if (itemPath.EndsWith("-beachalmondwhole")) textureRerouteLocation = new("wildcraftfruit:block/food/pie/fill-beachalmond");
                else textureRerouteLocation = new(contents[0].ItemAttributes["inPieProperties"].Token["texture"].ToString());

                if (textureRerouteLocation.ToString().Contains("peanutground")) {
                    textureRerouteLocation = textureRerouteLocation.ToString().Replace("ground", "");
                }

                shape.Textures.Clear();
                shape.Textures.Add("surface", textureRerouteLocation);

                texSource = new ShapeTextureSource(capi, shape, "FS-LiquidyTextureSource");
            }
            else if (contents[0].ItemAttributes?["inContainerTexture"].Exists == true) { // Then try container textures
                var texture = contents[0].ItemAttributes?["inContainerTexture"].AsObject<CompositeTexture>();
                texSource = new ContainerTextureSource(capi, contents[0], texture);
            }
            else { // If all else fails, take the item's own texture
                // For some reason, ITexPositionSource is throwing a null error when getting it with a simple fucking method, so this is needed
                var textures = contents[0].Item.Textures;
                texSource = new ContainerTextureSource(capi, contents[0], textures.Values.FirstOrDefault());
            }
        }
        else {
            texSource = new ShapeTextureSource(capi, shape, "FS-LiquidyTextureSource");
        }

        // Calculate the total content amount
        float contentAmount = 0;
        foreach (var itemStack in contents) {
            contentAmount += itemStack?.StackSize ?? 0;
        }

        // Calculating new height
        int stackSizeDiv = contents[0].Collectible.MaxStackSize / 32;
        float baseY = (float)shape.Elements[0].From[1];
        float step = maxHeight / (contents.Length * 32 * stackSizeDiv);
        double shapeHeight = contentAmount * step + baseY;

        // Adjusting the "topping" position
        foreach (var child in shape.Elements[0].Children) {
            child.To[1] = shapeHeight - shape.Elements[0].From[1] - (child.From[1] - child.To[1]);
            child.From[1] = shapeHeight - shape.Elements[0].From[1];
        }

        shape.Elements[0].To[1] = shapeHeight;

        // Re-sizing the textures
        for (int i = 0; i < 4; i++) {
            shape.Elements[0].FacesResolved[i].Uv[3] = (float)shapeHeight;
        }

        capi.Tesselator.TesselateShape("FS-TesselateLiquidy", shape, out MeshData contentMesh, texSource);
        return contentMesh;
    }
}
