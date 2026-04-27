using System.Linq;

namespace FoodShelves;

public static class Meshing {
    /// <summary>
    /// Generates a mesh for a block that has (or doesn't have) any attributes set to get textures from. Mainly used for many plank textures.
    /// skipElements is used to remove specific cubes from the model (recursive) before meshing.
    /// </summary>
    public static MeshData? GenBlockVariantMesh(ICoreAPI? api, ItemStack? stackWithAttributes, string?[]? skipElements = null) {
        if (api is not ICoreClientAPI capi) return null;
        if (stackWithAttributes == null) return null;

        Block block = stackWithAttributes.Block;
        var variantData = GetBlockVariantData(capi, stackWithAttributes);

        if (skipElements?.Length > 0) {
            variantData.Item1?.Elements = RemoveElements(variantData.Item1.Elements, skipElements);
        }

        capi.Tesselator.TesselateShape("FS-TesselateShape", variantData.Item1, out MeshData blockMesh, variantData.Item2);

        float scale = block?.Shape.Scale ?? 0;
        if (scale != 1) blockMesh.Scale(new Vec3f(.5f, 0, .5f), scale, scale, scale);

        return blockMesh.BlockYRotation(block);
    }

    /// <summary>
    /// Changes the block shape to another shape. Please note that the textures should be the same for the substitute shape.
    /// </summary>
    public static MeshData? SubstituteBlockShape(ICoreAPI Api, ITesselatorAPI tesselator, string shapePath, Block texturesFromBlock) {
        AssetLocation shapeLocation = new(shapePath);
        ITexPositionSource texSource = tesselator.GetTextureSource(texturesFromBlock);
        Shape? shape = Api.Assets.TryGet(shapeLocation)?.ToObject<Shape>();
        if (shape == null) return null;

        tesselator.TesselateShape(null, shape, out MeshData mesh, texSource);
        return mesh;
    }

    /// <summary>
    /// Changes the item shape to another shape. Please note that the textures should be the same for the substitute shape.
    /// </summary>
    public static MeshData? SubstituteItemShape(ICoreAPI Api, ITesselatorAPI tesselator, string shapePath, Item texturesFromItem) {
        AssetLocation shapeLocation = new(shapePath);
        ITexPositionSource texSource = tesselator.GetTextureSource(texturesFromItem);
        Shape? shape = Api.Assets.TryGet(shapeLocation)?.ToObject<Shape>();
        if (shape == null) return null;

        tesselator.TesselateShape(null, shape, out MeshData mesh, texSource);
        return mesh;
    }

    /// <summary>
    /// Generates the content mesh of the block's inventory. Mainly used for generating basket contents.
    /// </summary>
    public static MeshData? GenContentMesh(ICoreClientAPI? capi, ItemStack[] contents, ExplicitTransform transformationMatrix, Dictionary<string, ModelTransform>? modelTransformations = null, Action<TransformationData>? contentModifier = null) {
        if (capi == null) return null;

        MeshData? nestedContentMesh = null;
        float[][] contentTransformations = TransformationGenerator.GenerateExplicit(transformationMatrix, contentModifier);

        for (int i = 0; i < contents.Length; i++) {
            if (contents[i] == null || (contents[i].Item == null && contents[i].Block == null)) 
                continue;

            string shapeLocation = contents[i].Item?.Shape?.Base
                ?? contents[i].ItemAttributes?["displayedShape"]?.Token?.ToObject<CompositeShape>()?.Base
                ?? contents[i].Block.Shape.Base;
            if (shapeLocation == null) continue;

            Shape? shape = capi.TesselatorManager.GetCachedShape(shapeLocation)?.Clone();
            if (shape == null) continue;

            if (shape.Textures.Count == 0) {
                bool isItem = contents[i].Item != null;

                if (isItem) {
                    foreach (var texture in contents[i].Item.Textures) {
                        shape.Textures.Add(texture.Key, texture.Value.Base);
                    }
                }
                else {
                    foreach (var texture in contents[i].Block.Textures) {
                        shape.Textures.Add(texture.Key, texture.Value.Base);
                    }
                }
            }

            var texSource = new ShapeTextureSource(capi, shape, "FS-ShapeTextureSource");
            foreach (var textureDict in shape.Textures) {
                CompositeTexture cTex = new(textureDict.Value);
                cTex.Bake(capi.Assets);
                texSource.textures[textureDict.Key] = cTex;
            }

            capi.Tesselator.TesselateShape("FS-TesselateContent", shape, out MeshData collectibleMesh, texSource);

            if (i < transformationMatrix.Length) {
                if (modelTransformations != null) {
                    ModelTransform? transformation = contents[i].Collectible.GetTransformation(modelTransformations);
                    if (transformation != null) collectibleMesh.ModelTransform(transformation);
                }

                collectibleMesh.MatrixTransform(contentTransformations[i]);
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
    public static MeshData? GenLiquidyMesh(ICoreClientAPI? capi, ItemSlot? slot, string pathToFillShape, float maxHeight, bool inheritTextures = true) {
        if (capi == null) return null;
        if (slot == null || slot.Empty) return null;
        if (string.IsNullOrEmpty(pathToFillShape)) return null;

        // Shape location of a simple cube, meant as "filling"
        AssetLocation shapeLocation = new(pathToFillShape);
        Shape? shape = Shape.TryGet(capi, shapeLocation)?.Clone();
        if (shape == null) 
            return null;

        ItemStack stack = slot.Itemstack!;

        // Handle textureSource
        ITexPositionSource? texSource = inheritTextures
            ? GetPieTexture(capi, stack, shape)
                ?? GetContainerTextureSource(capi, stack)
                ?? GetItemTextureSource(capi, stack)
            : new ShapeTextureSource(capi, shape, "FS-LiquidyTextureSource");

        // Content / height calculation
        float contentAmount = slot.StackSize;
        int capacity = slot.StackSize + slot.GetRemainingSlotSpace(stack);

        float baseY = (float)shape.Elements[0].From![1];
        float shapeHeight = baseY + GetFillHeight(contentAmount, capacity, maxHeight);

        // Adjusting the "topping" position
        foreach (var child in shape.Elements[0].Children!) {
            child.To![1] = shapeHeight - shape.Elements[0].From![1] - (child.From![1] - child.To[1]);
            child.From[1] = shapeHeight - shape.Elements[0].From![1];
        }

        shape.Elements[0].To![1] = shapeHeight;

        // Re-sizing the textures
        for (int i = 0; i < 4; i++) {
            shape.Elements[0].FacesResolved![i].Uv[3] = shapeHeight;
        }

        capi.Tesselator.TesselateShape("FS-TesselateLiquidy", shape, out MeshData contentMesh, texSource);
        return contentMesh;
    }

    /// <summary>
    /// Generates a mesh that has a specific "util" shape that resemble liquids or stuff that can behave as liquids.
    /// The parent of the util shape will scale up depending on stacksize passed, and the children of that parent will simply move up.
    /// </summary>
    public static MeshData? GenLiquidyMesh(ICoreClientAPI? capi, ItemStack? stack, string pathToFillShape, float capacity, float maxHeight) {
        if (capi == null) return null;
        if (stack == null) return null;
        if (string.IsNullOrEmpty(pathToFillShape)) return null;

        // Shape location of a simple cube, meant as "filling"
        AssetLocation shapeLocation = new(pathToFillShape);
        Shape? shape = Shape.TryGet(capi, shapeLocation)?.Clone();
        if (shape == null)
            return null;

        // Handle textureSource
        ITexPositionSource? texSource = GetPieTexture(capi, stack, shape)
                ?? GetContainerTextureSource(capi, stack)
                ?? GetItemTextureSource(capi, stack);

        // Height calculation
        float contentAmount = stack.StackSize;

        float baseY = (float)shape.Elements[0].From![1];
        float shapeHeight = baseY + GetFillHeight(contentAmount, capacity, maxHeight);

        // Adjusting the "topping" position
        foreach (var child in shape.Elements[0].Children!) {
            child.To![1] = shapeHeight - shape.Elements[0].From![1] - (child.From![1] - child.To[1]);
            child.From[1] = shapeHeight - shape.Elements[0].From![1];
        }

        shape.Elements[0].To![1] = shapeHeight;

        // Re-sizing the textures
        for (int i = 0; i < 4; i++) {
            shape.Elements[0].FacesResolved![i].Uv[3] = shapeHeight;
        }

        capi.Tesselator.TesselateShape("FS-TesselateLiquidy", shape, out MeshData contentMesh, texSource);
        return contentMesh;
    }

    /// <summary>
    /// Generates a mesh that will generate some item shapes, and then move them up as the stack size increases.
    /// Do note that generally, the item shapes should "cover" up the whole top so you can't see the bottom of the block, giving the illusion that it's full.
    /// </summary>
    public static MeshData? GenPartialContentMesh(ICoreClientAPI capi, ItemSlot slot, float[][]? transformationMatrices, float maxHeight, string? utilShapeLoc = null) {
        if (capi == null) return null;
        if (slot == null || slot.Empty) return null;

        ItemStack stack = slot.Itemstack!;
        if (stack.Item?.Shape?.Base == null) return null;

        string shapeLocation = stack.Item.Shape.Base;

        Shape? shape = capi.TesselatorManager.GetCachedShape(shapeLocation)?.Clone();
        if (shape == null) return null;

        ITexPositionSource texSource = new ShapeTextureSource(capi, shape, "FS-PartialContentMesh");
        capi.Tesselator.TesselateShape("FS-TesselatePartial", shape, out MeshData itemMesh, texSource);

        MeshData contentMesh = itemMesh.Clone();

        if (transformationMatrices?[0] != null)
            contentMesh.MatrixTransform(transformationMatrices[0]);

        int stackSize = stack.StackSize;
        int matrixCount = transformationMatrices?.Length ?? 0;

        // Mesh some shapes
        for (int i = 1; i < Math.Min(stackSize, matrixCount); i++) {
            MeshData currentMesh = itemMesh.Clone();

            if (transformationMatrices?[i] != null)
                currentMesh.MatrixTransform(transformationMatrices[i]);

            contentMesh.AddMeshData(currentMesh);
        }

        // If more items are added, move content up
        if (stackSize > matrixCount) {
            float contentAmount = stackSize;
            int capacity = stackSize + slot.GetRemainingSlotSpace(stack);

            float shapeHeight = GetFillHeight(contentAmount, capacity, maxHeight);

            contentMesh.Translate(0, shapeHeight, 0);

            if (utilShapeLoc != null) {
                AssetLocation utilLoc = new(utilShapeLoc);
                Shape utilShape = Shape.TryGet(capi, utilLoc);

                if (utilShape == null) {
                    capi.Logger.Warning("[FoodShelves] non-existent utilShapeLoc passed. No content mesh will be generated.");
                    return null;
                }

                // Get util textures
                var textures = stack.Item.Textures;
                texSource = new ContainerTextureSource(capi, stack, textures.Values.FirstOrDefault());

                capi.Tesselator.TesselateShape("FS-TesselatePartialUtil", utilShape, out MeshData utilMesh, texSource);

                utilMesh.Translate(0, shapeHeight, 0);
                contentMesh.AddMeshData(utilMesh);
            }
        }

        return contentMesh;
    }
}
