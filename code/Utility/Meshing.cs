using System.Linq;

namespace FoodShelves;

public static class Meshing {
    public static MeshData GenBlockVariantMesh(ICoreAPI api, ItemStack stackWithAttributes) {
        if (api is not ICoreClientAPI capi) return null;

        Block block = stackWithAttributes.Block;

        string shapeLocation = block.Shape.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
        Shape shape = capi.Assets.TryGet(shapeLocation)?.ToObject<Shape>().Clone();
        if (shape == null) return null;

        if (shape.Textures.Count == 0) {
            foreach (var texture in block.Textures) {
                shape.Textures.Add(texture.Key, texture.Value.Base);
            }
        }

        var stexSource = new ShapeTextureSource(capi, shape, "FS-TextureSource");

        // Custom Textures
        if (stackWithAttributes.Attributes[BaseFSContainer.FSAttributes] is ITreeAttribute tree && block.Attributes["variantTextures"].Exists) {
            foreach (var pair in block.Attributes["variantTextures"].AsObject<Dictionary<string, string>>()) {
                string texPath = pair.Value;

                foreach (var attr in tree) {
                    string key = attr.Key;
                    string value = attr.Value.ToString();

                    if (texPath.Contains($"{{{key}}}")) {
                        shape.Textures[key] = pair.Value.Replace($"{{{key}}}", value);

                        var ctex = new CompositeTexture(pair.Value.ToString().Replace($"{{{key}}}", value));

                        //BlendedOverlayTexture overlay = new() {
                        //    Base = new AssetLocation("foodshelves:variants/overlay/shelves/wood")
                        //};

                        //ctex.BlendedOverlays ??= Array.Empty<BlendedOverlayTexture>();
                        //ctex.BlendedOverlays.Append(overlay);

                        ctex.Bake(capi.Assets);
                        stexSource.textures[pair.Key] = ctex;
                    }
                }
            }
        }

        capi.Tesselator.TesselateShape("FS-TesselateShape", shape, out MeshData blockMesh, stexSource);

        float scale = block.Shape.Scale;
        if (scale != 1) blockMesh.Scale(new Vec3f(.5f, 0, .5f), scale, scale, scale);

        return blockMesh.BlockYRotation(block);
    }

    public static MeshData SubstituteBlockShape(ICoreAPI Api, ITesselatorAPI tesselator, string shapePath, Block texturesFromBlock) {
        AssetLocation shapeLocation = new(shapePath);
        ITexPositionSource texSource = tesselator.GetTextureSource(texturesFromBlock);
        Shape shape = Api.Assets.TryGet(shapeLocation)?.ToObject<Shape>();
        if (shape == null) return null;

        tesselator.TesselateShape(null, shape, out MeshData mesh, texSource);
        return mesh;
    }

    public static MeshData SubstituteItemShape(ICoreAPI Api, ITesselatorAPI tesselator, string shapePath, Item texturesFromItem) {
        AssetLocation shapeLocation = new(shapePath);
        ITexPositionSource texSource = tesselator.GetTextureSource(texturesFromItem);
        Shape shape = Api.Assets.TryGet(shapeLocation)?.ToObject<Shape>();
        if (shape == null) return null;

        tesselator.TesselateShape(null, shape, out MeshData mesh, texSource);
        return mesh;
    }

    public static MeshData GenBlockMeshWithoutElements(ICoreClientAPI capi, Block block, string[] elements) {
        if (block == null) return null;

        ITexPositionSource texSource = capi.Tesselator.GetTextureSource(block);
        Shape shape = capi.TesselatorManager.GetCachedShape(block.Shape.Base);
        
        if (shape == null) {
            string shapeLocation = block.Shape.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json").ToString();
            shape = capi.Assets.TryGet(shapeLocation)?.ToObject<Shape>();
            if (shape == null) return null;
        }
        
        Shape shapeClone = shape.Clone();

        ShapeElement[] RemoveElements(ShapeElement[] elementArray) {
            var remainingElements = elementArray.Where(e => !elements.Contains(e.Name)).ToArray();
            foreach (var element in remainingElements) {
                if (element.Children != null && element.Children.Length > 0) {
                    element.Children = RemoveElements(element.Children); // Recursively filter children
                }
            }
            return remainingElements;
        }

        shapeClone.Elements = RemoveElements(shapeClone.Elements);

        capi.Tesselator.TesselateShape("FS-ShapeErased", shapeClone, out MeshData mesh, texSource);
        return mesh;
    }

    public static MeshData GenContentMesh(ICoreClientAPI capi, ITextureAtlasAPI targetAtlas, ItemStack[] contents, float[,] transformationMatrix, float scaleValue = 1f, Dictionary<string, ModelTransform> modelTransformations = null) {
        if (capi == null) return null;

        MeshData nestedContentMesh = null;
        for (int i = 0; i < contents.Length; i++) {
            if (contents[i] == null || contents[i].Item == null) continue;

            string shapeLocation = contents[i].Item.Shape?.Base;
            if (shapeLocation == null) continue;

            Shape shape = capi.TesselatorManager.GetCachedShape(shapeLocation)?.Clone();
            //string shapeLocation = contents[i].Item.Shape?.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json").ToString();
            //Shape shape = capi.Assets.TryGet(shapeLocation)?.ToObject<Shape>().Clone();
            if (shape == null) continue;

            if (shape.Textures.Count == 0) {
                foreach (var texture in contents[i].Item.Textures) {
                    shape.Textures.Add(texture.Key, texture.Value.Base);
                }
            }

            UniversalShapeTextureSource texSource = new(capi, targetAtlas, shape, "FS-ContentTextureSource");

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

    public static MeshData GenLiquidyMesh(ICoreClientAPI capi, ItemStack[] contents, string pathToFillShape, float maxHeight) {
        if (capi == null) return null;
        if (contents == null || contents.Length == 0 || contents[0] == null) return null;
        if (pathToFillShape == null || pathToFillShape == "") return null;

        // Shape location of a simple cube, meant as "filling"
        AssetLocation shapeLocation = new(pathToFillShape);
        Shape shape = Shape.TryGet(capi, shapeLocation);
        if (shape == null) return null;
        Shape shapeClone = shape.Clone();
        string itemPath = contents[0].Item.Code.Path;

        // Handle textureSource
        ITexPositionSource texSource;
        if (contents[0].ItemAttributes?["inPieProperties"].Exists == true) { // First try pie textures
            AssetLocation textureRerouteLocation;

            if (itemPath.EndsWith("-beachalmondwhole")) textureRerouteLocation = new("wildcraftfruit:block/food/pie/fill-beachalmond"); // Fucking exception
            else textureRerouteLocation = new(contents[0].ItemAttributes["inPieProperties"].Token["texture"].ToString());

            shapeClone.Textures.Clear();
            shapeClone.Textures.Add("surface", textureRerouteLocation);

            texSource = new ShapeTextureSource(capi, shapeClone, "FS-LiquidyTextureSource");
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

        // Calculate the total content amount
        float contentAmount = 0;
        foreach (var itemStack in contents) {
            contentAmount += itemStack?.StackSize ?? 0;
        }

        // Calculating new height
        int stackSizeDiv = contents[0].Collectible.MaxStackSize / 32;
        float baseY = (float)shapeClone.Elements[0].From[1];
        float step = maxHeight / (contents.Length * 32 * stackSizeDiv);
        double shapeHeight = contentAmount * step + baseY;

        // Adjusting the "topping" position
        foreach (var child in shapeClone.Elements[0].Children) {
            child.To[1] = shapeHeight - shapeClone.Elements[0].From[1] - (child.From[1] - child.To[1]);
            child.From[1] = shapeHeight - shapeClone.Elements[0].From[1];
        }

        shapeClone.Elements[0].To[1] = shapeHeight;

        // Re-sizing the textures
        for (int i = 0; i < 4; i++) {
            shapeClone.Elements[0].FacesResolved[i].Uv[3] = (float)shapeHeight;
        }

        capi.Tesselator.TesselateShape("FS-TesselateLiquidy", shapeClone, out MeshData contentMesh, texSource);
        return contentMesh;
    }
}
