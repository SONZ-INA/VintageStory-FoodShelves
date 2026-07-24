using System.Linq;
using Vintagestory.ServerMods;

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
    public static MeshData? SubstituteItemShape(ICoreAPI Api, ITesselatorAPI tesselator, string shapePath, ItemStack? texturesFromStack = null) {
        if (Api is not ICoreClientAPI capi) return null;

        AssetLocation shapeLocation = new(shapePath);
        Shape? shape = Api.Assets.TryGet(shapeLocation)?.ToObject<Shape>();
        if (shape == null) return null;

        ITexPositionSource texSource;

        if (texturesFromStack?.Item != null) {
            var textures = texturesFromStack.Item.Textures.ShallowClone();

            var keysToRemove = textures
                .Where(t => t.Value.ToString().Contains("transparent"))
                .Select(t => t.Key)
                .ToList();

            foreach (var key in keysToRemove) {
                textures.Remove(key);
            }

            texSource = new ContainerTextureSource(capi, texturesFromStack, textures.Values.FirstOrDefault());
        }
        else {
            texSource = new ShapeTextureSource(capi, shape, "FS-SubstituteItemTexSource");
        }

        tesselator.TesselateShape("FS-TesselateShape", shape, out MeshData mesh, texSource);
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
                ?? contents[i].Block?.Shape?.Base;
            if (shapeLocation == null) continue;

            Shape? shape = capi.TesselatorManager.GetCachedShape(shapeLocation)?.Clone();
            if (shape == null) continue;

            shape.TransferItemtypeTextures(contents[i]);

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
            ? GetTextureOverrideSource(capi, stack)
                ?? GetPieTexture(capi, stack, shape)
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
        ITexPositionSource? texSource = GetTextureOverrideSource(capi, stack)
            ?? GetPieTexture(capi, stack, shape)
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
    /// Generates only the utility shape translated upwards based on the stack size.
    /// </summary>
    public static MeshData? GenFillCubeMesh(ICoreClientAPI? capi, ItemStack? stack, int capacity, float maxHeight, string utilShapeLoc) {
        if (capi == null) 
            return null;
        
        if (stack == null || stack.StackSize == 0)
            return null;
        
        if (string.IsNullOrEmpty(utilShapeLoc))
            return null;

        float shapeHeight = GetFillHeight(stack.StackSize, capacity, maxHeight);

        AssetLocation utilLoc = new(utilShapeLoc);
        Shape utilShape = Shape.TryGet(capi, utilLoc);

        if (utilShape == null) {
            capi.Logger.Warning($"[FoodShelves] non-existent utilShapeLoc '{utilShapeLoc}' passed. No fill mesh will be generated.");
            return null;
        }

        var textures = stack.Item?.Textures ?? stack.Block?.Textures;
        if (textures == null || textures.Count == 0) return null;

        ITexPositionSource texSource = new ContainerTextureSource(capi, stack, textures.Values.FirstOrDefault());

        capi.Tesselator.TesselateShape("FS-TesselateFillCube", utilShape, out MeshData fillMesh, texSource);

        fillMesh.Translate(0, shapeHeight, 0);

        return fillMesh;
    }

    /// <summary>
    /// Generates a paper label + liquid icon mesh shown on the front face of a racked barrel.
    /// Only returns a label if there is a liquid in the barrel.
    /// </summary>
    public static MeshData? GenBarrelLabelMesh(ICoreClientAPI? capi, ItemSlot liquidSlot, string labelShapePath) {
        if (capi == null || liquidSlot == null || liquidSlot.Empty) return null;

        AssetLocation labelLoc = new(labelShapePath);
        Shape? labelShape = capi.Assets.TryGet(labelLoc)?.ToObject<Shape>()?.Clone();
        if (labelShape == null) return null;

        var texSource = new ShapeTextureSource(capi, labelShape, "FS-BarrelLabelTexSource");
        foreach (var kvp in labelShape.Textures) {
            CompositeTexture cTex = new(kvp.Value);
            cTex.Bake(capi.Assets);
            texSource.textures[kvp.Key] = cTex;
        }

        capi.Tesselator.TesselateShape("FS-BarrelLabel", labelShape, out MeshData labelMesh, texSource);

        ItemStack liquidStack = liquidSlot.Itemstack!;
        string? iconShapeLoc = liquidStack.Item?.Shape?.Base?.ToString()
                            ?? liquidStack.ItemAttributes?["displayedShape"]?.Token?.ToObject<CompositeShape>()?.Base?.ToString()
                            ?? liquidStack.Block?.Shape?.Base?.ToString();

        Shape? iconShape = iconShapeLoc != null ? capi.TesselatorManager.GetCachedShape(iconShapeLoc)?.Clone() : null;
        if (iconShape != null) {
            CompositeTexture? liquidTex = liquidStack.Item?.Textures?.Values.FirstOrDefault()
                                       ?? liquidStack.Block?.Textures?.Values.FirstOrDefault();

            iconShape.TransferItemtypeTextures(liquidStack);

            var iconTexSource = new ShapeTextureSource(capi, iconShape, "FS-BarrelLabelIconTex");
            if (liquidTex != null) {
                CompositeTexture iconTex = new(liquidTex.Base);
                iconTex.Bake(capi.Assets);
                foreach (var kvp in iconShape.Textures)
                    iconTexSource.textures[kvp.Key] = iconTex;
                foreach (string key in new[] { "material" })
                    iconTexSource.textures[key] = iconTex;
            }

            capi.Tesselator.TesselateShape("FS-BarrelLabelIcon", iconShape, out MeshData iconMesh, iconTexSource);

            ExplicitTransform iconTransform = new(
                X: [0.003f], Y: [0.541f], Z: [0.375f],
                RX: [-90f],  RY: [0f],   RZ: [180f]
            );
            float[][] matrices = TransformationGenerator.GenerateExplicit(iconTransform,
                td => { td.scaleX = td.scaleY = td.scaleZ = 0.19f; });
            iconMesh.MatrixTransform(matrices[0]);
            labelMesh.AddMeshData(iconMesh);
        }

        return labelMesh;
    }
}
