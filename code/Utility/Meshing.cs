﻿using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.Common;

namespace FoodShelves;

public static class Meshing {
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

        capi.Tesselator.TesselateShape("erasedelementsshape", shapeClone, out MeshData mesh, texSource);
        return mesh;
    }

    public static MeshData GenContentMesh(ICoreClientAPI capi, ItemStack[] contents, float[,] transformationMatrix, float scaleValue = 1f, Dictionary<string, ModelTransform> modelTransformations = null) {
        MeshData contentMesh = null;
        
        if (contents != null) {
            int offset = transformationMatrix.GetLength(1);

            for (int i = 0; i < contents.Length; i++) {
                if (contents[i] != null) {
                    if (contents[i].Item == null) continue; // To fix the damn pumpkin bug on existing worlds
                    capi.Tesselator.TesselateItem(contents[i].Item, out MeshData itemMesh);

                    if (i < offset) {
                        if (modelTransformations != null) {
                            ModelTransform transformation = contents[i].Item.GetTransformation(modelTransformations);
                            if (transformation != null) itemMesh.ModelTransform(transformation);
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

                        itemMesh.MatrixTransform(matrixTransform);
                    }

                    if (contentMesh == null) contentMesh = itemMesh;
                    else contentMesh.AddMeshData(itemMesh);
                }
            }
        }

        return contentMesh;
    }

    public static MeshData GenLiquidyMesh(ICoreClientAPI capi, ItemStack[] contents, string pathToFillShape) {
        if (contents == null || contents.Length == 0 || contents[0] == null) return null;
        if (pathToFillShape == null || pathToFillShape == "") return null;

        // Shape location of a simple cube, meant to "fill" the Glass Jar
        AssetLocation shapeLocation = new(pathToFillShape);
        Shape shape = capi.Assets.TryGet(shapeLocation)?.ToObject<Shape>();
        if (shape == null) return null;
        Shape shapeClone = shape.Clone();
        string itemPath = contents[0].Item.Code.Path;

        // Handle textureSource
        ITexPositionSource texSource;

        if (contents[0].ItemAttributes?["inPieProperties"].Exists == true) {
            AssetLocation textureRerouteLocation;

            if (itemPath.EndsWith("-beachalmondwhole")) textureRerouteLocation = new("wildcraftfruit:block/food/pie/fill-beachalmond"); // Fucking exception
            else textureRerouteLocation = new(contents[0].ItemAttributes["inPieProperties"].Token["texture"].ToString());

            shapeClone.Textures.Clear();
            shapeClone.Textures.Add("surface", textureRerouteLocation);

            texSource = new ShapeTextureSource(capi, shapeClone, "jarcontentshape");
        }
        else {
            // For some reason, ITexPositionSource is throwing a null error when simply getting it with a simple fucking method, so this is needed
            var textures = contents[0].Item.Textures;
            texSource = new ContainerTextureSource(capi, contents[0], textures.Values.FirstOrDefault());

            // Modifying the texture key of the shape to fit the key of the item
            string textureKey = textures.Keys.FirstOrDefault();
            ChangeShapeTextureKey(shapeClone, textureKey);
        }

        // Adjusting the cube height
        float contentHeight = 0;
        foreach (var itemStack in contents) {
            contentHeight += itemStack?.StackSize ?? 0;
        }

        int stackSizeDiv = contents[0].Collectible.MaxStackSize / 32;
        float multiplier = contents.Length == 2 ? 0.11f / stackSizeDiv : 0.022f / stackSizeDiv; // Hardcoded for now
        double shapeHeight = contentHeight * multiplier + shapeClone.Elements[0].From[1];
        shapeClone.Elements[0].To[1] = shapeHeight;

        // Adjusting the "topping" position
        foreach(var child in shapeClone.Elements[0].Children) {
            child.From[1] = shapeHeight - 0.5;
            child.To[1] += shapeHeight - 1;
        }

        // Re-sizing the textures
        if (itemPath == "beeswax") { // Hardcoded stuff for beeswax
            if (pathToFillShape == ShapeReferences.CeilingJarUtil) {
                for (int i = 0; i < 6; i++) {
                    shapeClone.Elements[0].FacesResolved[i].Uv[0] = 6f;
                }
            }
        }

        float textureOffset = 0;
        if (itemPath == "fat") { // Hardcoded stuff for animal fat
            textureOffset = -1.8f;
            shapeClone.Elements[0].FacesResolved[5].Uv[3] = 8f;
        }

        for (int i = 0; i < 4; i++) {
            float offset = 0; // Another hardcode for beeswax texture height
            if (pathToFillShape == ShapeReferences.GlassJarUtil && contents[0].Collectible.Code.Path == "beeswax") {
                offset = -1.5f;
            }

            shapeClone.Elements[0].FacesResolved[i].Uv[3] = (float)shapeHeight + textureOffset + offset;
        }

        capi.Tesselator.TesselateShape("liquidymesh", shapeClone, out MeshData contentMesh, texSource);
        return contentMesh;
    }

    // Old GenLiquidyMesh, it's here coz i might need it
    public static MeshData GenLiquidyMeshOLD(ICoreClientAPI capi, InventoryGeneric inventory) {
        if (inventory == null || inventory.Count == 0) return null;

        List<ItemStack> contentList = new();
        foreach (ItemSlot itemSlot in inventory) {
            if (itemSlot.Itemstack != null)
                contentList.Add(itemSlot.Itemstack);
        }
        if (contentList.Count == 0) return null; // Empty
        ItemStack[] contents = contentList.ToArray();
        if (contents[0].Item == null) return null; // Isn't intended for block use

        // Shape location of a simple cube, meant to "fill" the Glass Jar
        AssetLocation shapeLocation = new("foodshelves:shapes/util/glassjarcontentcube.json");
        Shape shape = capi.Assets.TryGet(shapeLocation)?.ToObject<Shape>();
        if (shape == null) return null;

        Shape shapeClone = shape.Clone();

        // Handle textureSource
        ShapeTextureSource texSource;

        // Rerouting of some textures is needed
        string itemCodePath = contents[0].Item.Code.Path;
        string[] texturesToReroute = { "currant", "berry", "saguaro", "apple", "cherry", "peach", "pear", "orange", "mango", "breadfruit", "lychee", "pomegranate" };
        bool reroute = false;

        if (itemCodePath.StartsWith("wilddehydratedfruit") || itemCodePath.StartsWith("wilddryfruit")) { // Wildcraft: Fruits and Nuts have their own textures
            reroute = true;
        }
        else if (!itemCodePath.StartsWith("dryfruit")) { // Most dehydrated/dry fruit
            foreach (var texture in texturesToReroute) {
                if (itemCodePath.EndsWith(texture)) {
                    reroute = true;
                    break;
                }
            }
        }
        else if (itemCodePath.StartsWith("dryfruit")) { // Some dry fruit that don't have good textures
            if (itemCodePath.EndsWith("blueberry")) reroute = true;
            else if (itemCodePath.EndsWith("currant")) reroute = true;
            else if (itemCodePath.EndsWith("pineapple")) reroute = true;
        }

        if (reroute) { // Handle currant specific cases
            int index = itemCodePath.LastIndexOf('-');

            if (index >= 0) {
                // Getting an end part of an item, and then rerouting it to pie fillings to show properly in-game
                string suffix = itemCodePath.Substring(index);

                if (suffix.EndsWith("apple") && !suffix.Contains("pine") && !suffix.Contains("cashew")) suffix = "-apple";
                if (suffix.EndsWith("lillypillyblue")) suffix = suffix.Replace("blue", "pink");
                if (suffix.EndsWith("lemon")) suffix = suffix.Replace("lemon", "citron");
                if (suffix.Contains("pitted")) suffix = suffix.Replace("pitted", "");

                string domain = "game";
                if (itemCodePath.StartsWith("wilddehydratedfruit") || itemCodePath.StartsWith("wilddryfruit")) domain = "wildcraftfruit";
                if (suffix == "-cherry" || suffix == "-breadfruit") domain = "game"; // Fucking hell with these inconsistencies

                AssetLocation textureRerouteLocation = new($"{domain}:block/food/pie/fill{suffix}");

                shapeClone.Textures.Clear();
                shapeClone.Textures.Add("surface", textureRerouteLocation);

                texSource = new(capi, shapeClone, "jarcontentshape");
            }
            else {
                capi.Logger.Warning("[FoodShelves] Indexing for item code path failed, item will not be meshed correctly. Report this to the mod author.");
                return null;
            }
        }
        else {
            // For some reason, ITexPositionSource is throwing a null error when simply getting it with a simple fucking method, so this is needed
            AssetLocation contentShapeLocation = contents[0].Item.Shape.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
            Shape contentShape = capi.Assets.TryGet(contentShapeLocation)?.ToObject<Shape>();
            if (contentShape == null) return null;
            texSource = new(capi, contentShape, "jarcontentshape");

            // Modifying the texture key of the shape to fit the key of the item
            string textureKey = contentShape.Textures.Keys.FirstOrDefault();
            ChangeShapeTextureKey(shapeClone, textureKey);
        }

        // Adjusting the cube height
        float contentHeight = 0;
        foreach (var itemStack in contents) {
            contentHeight += itemStack.StackSize;
        }

        double shapeHeight = contentHeight * 0.11 + shapeClone.Elements[0].From[1];
        shapeClone.Elements[0].To[1] = shapeHeight;

        // Adjusting the "topping" position
        foreach (var child in shapeClone.Elements[0].Children) {
            child.From[1] = shapeHeight - 0.5;
            child.To[1] += shapeHeight - 1;
        }

        // Re-sizing the textures
        shapeClone.Elements[0].FacesResolved[0].Uv[3] = (float)shapeHeight;
        shapeClone.Elements[0].FacesResolved[1].Uv[3] = (float)shapeHeight;
        shapeClone.Elements[0].FacesResolved[2].Uv[3] = (float)shapeHeight;
        shapeClone.Elements[0].FacesResolved[3].Uv[3] = (float)shapeHeight;

        capi.Tesselator.TesselateShape(null, shapeClone, out MeshData contentMesh, texSource);

        return contentMesh;
    }

    public static MeshData GenNestedContentMesh(ICoreClientAPI capi, ItemStack[] stack) {
        if (capi == null) return null;

        MeshData nestedContentMesh = null;
        foreach (ItemStack itemStack in stack) {
            if (itemStack == null || itemStack.Item == null) continue;

            Shape shape = capi.TesselatorManager.GetCachedShape(itemStack.Item.Shape.Base).Clone();
            if (shape == null) return null;

            UniversalShapeTextureSource texSource = new(capi, capi.ItemTextureAtlas, shape, "inContainerTexSource");

            foreach (var textureDict in shape.Textures) {
                CompositeTexture cTex = new(textureDict.Value);
                cTex.Bake(capi.Assets);
                texSource.textures[textureDict.Key] = cTex;
            }

            capi.Tesselator.TesselateShape("InContainerTesselate", shape, out MeshData collectibleMesh, texSource);

            if (nestedContentMesh == null) nestedContentMesh = collectibleMesh;
            else nestedContentMesh.AddMeshData(collectibleMesh);
        }

        return nestedContentMesh;
    }

    // GeneralizedTexturedGenMesh written specifically for expanded foods, i might need it so it's here
    public static MeshData GeneralizedTexturedGenMesh(ICoreClientAPI capi, Item item) { // third passed attribute would be a Dictionary of keys and texture paths and
                                                                                        // then iterate through them after Textures.Clear()
        AssetLocation shapeLocation = item.Shape.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json"); // A generalized shape would go here, like a berrybread for example

        Shape shape = capi.Assets.TryGet(shapeLocation)?.ToObject<Shape>();
        if (shape == null) return null;

        var keys = new List<string>(shape.Textures.Keys); // we can get all available keys here. IT PASSES A REFERENCE SO DON'T REMOVE THE 'List<string>' PART!!
        Shape shapeClone = shape.Clone(); // has to be cloned to work

        shapeClone.Textures.Clear(); // remove all keys and values

        // shapeClone.Textures.Remove("sides"); // Remove or .Clear() Textures to add new ones, 
                                                // These textures are contained within `shapes` .json file.
                                                // If it doesn't work, remove the "textures" {} from `blocktypes` .json files, i haven't tested it
        AssetLocation ass = new("game:item/food/fruit/cherry"); // path to desired texture
        foreach (var x in keys) {
            shapeClone.Textures.Add(x, ass); // apply desired texture to the key, make sure to add *all* keys as it might crash
        }

        ITexPositionSource texSource = new ShapeTextureSource(capi, shapeClone, null); // get texture source of the newly modified shape
        capi.Tesselator.TesselateShape(null, shapeClone, out MeshData block2, texSource); // tesselate shape here.
                                                                                          // this will use the texSource to apply textures, that's why we got it as a ShapeTextureSource

        return block2;
    }
}
