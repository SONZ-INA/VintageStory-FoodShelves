using System.Linq;

namespace FoodShelves; 

public static class Extensions {
    #region JSONExtensions

    public static void EnsureAttributesNotNull(this CollectibleObject obj) => obj.Attributes ??= new JsonObject(new JObject());
    public static T LoadAsset<T>(this ICoreAPI api, string path) => api.Assets.Get(new AssetLocation(path)).ToObject<T>();

    public static void SetTreeAttributeContents(this ItemStack stack, InventoryGeneric inv, string attributeName, int index = 1) {
        TreeAttribute stacksTree = new();

        for (; index < inv.Count; index++) {
            if (inv[index].Itemstack == null) break;
            stacksTree[index + ""] = new ItemstackAttribute(inv[index].Itemstack);
        }

        stack.Attributes[$"{attributeName}"] = stacksTree;
    }

    public static ItemStack[] GetTreeAttributeContents(this ItemStack itemStack, ICoreClientAPI capi, string attributeName, int index = 1) {
        ITreeAttribute tree = itemStack?.Attributes?.GetTreeAttribute($"{attributeName}");
        List<ItemStack> contents = new();

        if (tree != null) {
            for (; index < tree.Count + 1; index++) {
                ItemStack stack = tree.GetItemstack(index + "");
                stack?.ResolveBlockOrItem(capi.World);
                contents.Add(stack);
            }
        }

        return contents.ToArray();
    }

    #endregion

    #region StringExtensions

    public static string FirstCharToUpper(this string input) {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (string.IsNullOrEmpty(input)) throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
        return string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1));
    }

    #endregion

    #region MeshExtensions

    public static MeshData BlockYRotation(this MeshData mesh, Block block) {
        return mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, block.Shape.rotateY * GameMath.DEG2RAD, 0);
    }

    public static float GetBlockMeshAngle(IPlayer byPlayer, BlockSelection blockSel, bool val) {
        if (val) {
            BlockPos targetPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position;
            double dx = byPlayer.Entity.Pos.X - (targetPos.X + blockSel.HitPosition.X);
            double dz = byPlayer.Entity.Pos.Z - (targetPos.Z + blockSel.HitPosition.Z);
            float angleHor = (float)Math.Atan2(dx, dz);

            float deg22dot5rad = GameMath.PIHALF / 4;
            float roundRad = ((int)Math.Round(angleHor / deg22dot5rad)) * deg22dot5rad;
            return roundRad;
        }
        return 0;
    }

    public static void ApplyVariantTextures(this Shape shape, BEFSContainer fscontainer) {
        var variantTextures = fscontainer.Block.Attributes?["variantTextures"]?.AsObject<Dictionary<string, string>>();
        if (variantTextures == null) return;

        if (fscontainer.VariantAttributes == null) return;

        foreach (var texture in variantTextures) {
            string textureValue = texture.Value;

            foreach (var attr in fscontainer.VariantAttributes) {
                string paramPlaceholder = "{" + attr.Key + "}";
                string paramValue = attr.Value.ToString();

                textureValue = textureValue.Replace(paramPlaceholder, paramValue);
            }

            if (textureValue.Contains('{') || textureValue.Contains('}')) continue;
            shape.Textures[texture.Key] = textureValue;
        }
    }

    public static void ChangeShapeTextureKey(Shape shape, string key) {
        foreach (var face in shape.Elements[0].FacesResolved) {
            face.Texture = key;
        }

        foreach (var child in shape.Elements[0].Children) {
            foreach (var face in child.FacesResolved) {
                if (face != null) face.Texture = key;
            }
        }
    }

    public static void ApplyModelTransformToMatrixF(this Matrixf mat, ModelTransform transformation) {
        if (transformation == null) return;

        mat.Translate(0.5f, 0, 0.5f);

        if (transformation.Translation != null) {
            mat.Translate(transformation.Translation.X, transformation.Translation.Y, transformation.Translation.Z);
        }
        
        if (transformation.Rotation != null) {
            mat.RotateXDeg(transformation.Rotation.X);
            mat.RotateYDeg(transformation.Rotation.Y);
            mat.RotateZDeg(transformation.Rotation.Z);
        }

        if (transformation.ScaleXYZ != null) {
            mat.Scale(transformation.ScaleXYZ.X, transformation.ScaleXYZ.Y, transformation.ScaleXYZ.Z);
        }

        mat.Translate(-0.5f, 0, -0.5f);
    }

    public static int GetStackCacheHashCodeFNV(ItemStack[] contentStack) {
        if (contentStack == null) return 0;

        unchecked {
            // FNV-1 hash since any other simpler one ends up colliding, fuck data structures & algorithms btw
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

    #endregion

    #region GeneralBlockExtensions

    public static T GetBlockEntityExt<T>(this IBlockAccessor blockAccessor, BlockPos pos) where T : BlockEntity {
        if (blockAccessor.GetBlockEntity<T>(pos) is T blockEntity) {
            return blockEntity;
        }

        if (blockAccessor.GetBlock(pos) is BlockMultiblock multiblock) {
            BlockPos multiblockPos = new(pos.X + multiblock.OffsetInv.X, pos.Y + multiblock.OffsetInv.Y, pos.Z + multiblock.OffsetInv.Z, pos.dimension);
            return blockAccessor.GetBlockEntity<T>(multiblockPos);
        }

        return null;
    }

    public static ModelTransform GetTransformation(this CollectibleObject obj, Dictionary<string, ModelTransform> transformations) {
        foreach (KeyValuePair<string, ModelTransform> transformation in transformations) {
            if (WildcardUtil.Match(transformation.Key, obj.Code.ToString())) return transformation.Value;
        }
        return null;
    }

    public static string GetMaterialName(this ItemStack itemStack, bool includeParenthesis = true) {
        if (itemStack.Attributes["FSAttributes"] is not ITreeAttribute tree)
            return "";

        foreach (var pair in tree) {
            if (pair.Key == "wood") {
                string toReturn = Lang.Get("game:material-" + pair.Value);
                return (includeParenthesis ? "(" : "") + toReturn + (includeParenthesis ? ")" : "");
            }
        }

        return "";
    }

    public static string GetMaterialNameLocalized(this ItemStack itemStack, string[] variantKeys = null, string[] toExclude = null, bool includeParenthesis = true) {
        string material = "";
        string[] materialCheck = { "material-", "rock-", "ore-" };

        if (variantKeys == null) {
            material = itemStack.Collectible.Variant["type"];
        }
        else {
            for (int i = 0; i < variantKeys.Length; i++) {
                if (itemStack.Collectible.Variant.ContainsKey(variantKeys[i])) {
                    material = itemStack.Collectible.Variant[variantKeys[i]];
                    break;
                }
            }
        }

        if (toExclude == null) {
            material = material.Replace("normal", "");
            material = material.Replace("short", "");
            material = material.Replace("very", "");
        }
        else {
            for (int i = 0; i < toExclude.Length; i++) {
                material = material.Replace(toExclude[i], "");
            }
        }

        if (material == "") return "";

        string toReturn = "";
        foreach (string check in materialCheck) {
            toReturn = Lang.Get(check + material);
            if (toReturn != check + material) break;
        }

        return (includeParenthesis ? "(" : "") + toReturn + (includeParenthesis ? ")" : "");
    }

    public static float[,] GenTransformationMatrix(float[] x, float[] y, float[] z, float[] rX, float[] rY, float[] rZ) {
        float[,] transformationMatrix = new float[6, x.Length];

        for (int i = 0; i < x.Length; i++) {
            transformationMatrix[0, i] = x[i];
            transformationMatrix[1, i] = y[i];
            transformationMatrix[2, i] = z[i];
            transformationMatrix[3, i] = rX[i];
            transformationMatrix[4, i] = rY[i];
            transformationMatrix[5, i] = rZ[i];
        }

        return transformationMatrix;
    }

    public static void MBNormalizeSelectionBox(this Cuboidf selectionBox, Vec3i offset) {
        // Make sure that the selection boxes defined in blocktype .json file are defined with the following rotations:
        // { "*-north": 0, "*-east": 270, "*-west": 90, "*-south": 180 }
        // Otherwise, the selection boxes won't correctly normalize
        selectionBox.X1 += offset.X;
        selectionBox.X2 += offset.X;
        selectionBox.Y1 += offset.Y;
        selectionBox.Y2 += offset.Y;
        selectionBox.Z1 += offset.Z;
        selectionBox.Z2 += offset.Z;
    }

    public static int GetRotationAngle(Block block) {
        // This one's more-less hardcoded that these rotations always have to "align" with the ones defined in blocktype but oh well.
        string blockPath = block.Code.Path;
        if (blockPath.EndsWith("-north")) return 0;
        if (blockPath.EndsWith("-south")) return 180;
        if (blockPath.EndsWith("-east")) return 270;
        if (blockPath.EndsWith("-west")) return 90;
        return 0;
    }

    #endregion

    #region BlockInventoryExtensions

    // Must be called before initialize
    public static void RebuildInventory(this BEFSContainer be, ICoreAPI api, int maxSlotStackSize = 1) {
        // Need to save items and transfer it over to new inventory, they disappear otherwise
        ItemStack[] stack = be.inv.Select(slot => slot.Itemstack).ToArray();

        be.inv = new InventoryGeneric(be.SlotCount, be.inv.ClassName + "-0", api, (_, inv) => new ItemSlotFSUniversal(inv, be.AttributeCheck, maxSlotStackSize));

        for (int i = 0; i < be.SlotCount; i++) {
            if (i >= stack.Length) break;
            be.inv[i].Itemstack = stack[i];
        }

        be.inv.LateInitialize(be.inv.InventoryID, api);
    }

    public static ItemStack[] GetContents(IWorldAccessor world, ItemStack itemstack) {
        ITreeAttribute treeAttr = itemstack?.Attributes?.GetTreeAttribute("contents");
        if (treeAttr == null) {
            return ResolveUcontents(world, itemstack);
        }

        ItemStack[] stacks = new ItemStack[treeAttr.Count];
        foreach (var val in treeAttr) {
            ItemStack stack = (val.Value as ItemstackAttribute).value;
            stack?.ResolveBlockOrItem(world);

            if (int.TryParse(val.Key, out int index)) stacks[index] = stack;
        }

        return stacks;
    }

    public static void SetContents(ItemStack containerStack, ItemStack[] stacks) {
        if (stacks == null || stacks.Length == 0) {
            containerStack.Attributes.RemoveAttribute("contents");
            return;
        }

        TreeAttribute stacksTree = new TreeAttribute();
        for (int i = 0; i < stacks.Length; i++) {
            stacksTree[i + ""] = new ItemstackAttribute(stacks[i]);
        }

        containerStack.Attributes["contents"] = stacksTree;
    }

    public static ItemStack[] ResolveUcontents(IWorldAccessor world, ItemStack itemstack) {
        if (itemstack?.Attributes.HasAttribute("ucontents") == true) {
            List<ItemStack> stacks = new();

            var attrs = itemstack.Attributes["ucontents"] as TreeArrayAttribute;

            foreach (ITreeAttribute stackAttr in attrs.value) {
                stacks.Add(CreateItemStackFromJson(stackAttr, world, itemstack.Collectible.Code.Domain));
            }
            ItemStack[] stacksAsArray = stacks.ToArray();
            SetContents(itemstack, stacksAsArray);
            itemstack.Attributes.RemoveAttribute("ucontents");

            return stacksAsArray;
        }
        else {
            return Array.Empty<ItemStack>();
        }
    }

    private static ItemStack CreateItemStackFromJson(ITreeAttribute stackAttr, IWorldAccessor world, string defaultDomain) {
        CollectibleObject collObj;
        var loc = AssetLocation.Create(stackAttr.GetString("code"), defaultDomain);
        if (stackAttr.GetString("type") == "item") {
            collObj = world.GetItem(loc);
        }
        else {
            collObj = world.GetBlock(loc);
        }

        ItemStack stack = new(collObj, (int)stackAttr.GetDecimal("quantity", 1));
        var attr = (stackAttr["attributes"] as TreeAttribute)?.Clone();
        if (attr != null) stack.Attributes = attr;

        return stack;
    }

    #endregion

    #region ItemStackExtensions

    public static DummySlot[] ToDummySlots(this ItemStack[] contents) {
        if (contents == null || contents.Length == 0) return Array.Empty<DummySlot>();

        DummySlot[] dummySlots = new DummySlot[contents.Length];
        for (int i = 0; i < contents.Length; i++) {
            dummySlots[i] = new DummySlot(contents[i]?.Clone());
        }

        return dummySlots;
    }

    #endregion

    #region CheckExtensions

    public static bool CheckTypedRestriction(this CollectibleObject obj, RestrictionData data) => data.CollectibleTypes?.Contains(obj.Code.Domain + ":" + obj.GetType().Name) == true;
    public static bool IsFull(this ItemSlot slot) => slot.StackSize == slot.MaxSlotStackSize;

    public static bool CanStoreInSlot(this ItemSlot slot, string attributeWhitelist) {
        if (slot?.Itemstack?.Collectible?.Attributes?[attributeWhitelist].AsBool() == false) return false;
        if (slot?.Inventory?.ClassName == "hopper") return false;
        return true;
    }

    public static bool CanStoreInSlot(this CollectibleObject obj, string attributeWhitelist) {
        return obj?.Attributes?[attributeWhitelist].AsBool() == true;
    }

    public static bool IsLargeItem(ItemStack stack) {
        if (BakingProperties.ReadFrom(stack)?.LargeItem == true) return true;
        if (stack?.Collectible?.GetType().Name == "ItemCheese") return true;
        if (stack?.Collectible?.GetType().Name == "BlockFruitBasket") return true;
        if (stack?.Collectible?.GetType().Name == "BlockVegetableBasket") return true;
        if (stack?.Collectible?.GetType().Name == "BlockEggBasket") return true;

        return false;
    }

    public static bool IsSmallItem(ItemStack stack) {
        if (stack?.Collectible.Code == "wildcraftfruit:nut-hazelbar") return true;

        return false;
    }

    #endregion
}

#region SyncResolver

// Unused code

//public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data) {
//    base.OnReceivedClientPacket(fromPlayer, packetid, data);

//    if (packetid == (int)CoolingCabinetPacket.CabinetOpen) {
//        data = SerializerUtil.Serialize(true);
//        ((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(Pos, (int)CoolingCabinetPacket.CabinetOpenOthers, data, (IServerPlayer)fromPlayer);
//    }

//    if (packetid == (int)CoolingCabinetPacket.CabinetClose) {
//        data = SerializerUtil.Serialize(false);
//        ((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(Pos, (int)CoolingCabinetPacket.CabinetOpenOthers, data, (IServerPlayer)fromPlayer);
//    }

//    if (packetid == (int)CoolingCabinetPacket.DrawerOpen) {
//        data = SerializerUtil.Serialize(true);
//        ((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(Pos, (int)CoolingCabinetPacket.DrawerOpenOthers, data, (IServerPlayer)fromPlayer);
//    }

//    if (packetid == (int)CoolingCabinetPacket.DrawerClose) {
//        data = SerializerUtil.Serialize(false);
//        ((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(Pos, (int)CoolingCabinetPacket.DrawerOpenOthers, data, (IServerPlayer)fromPlayer);
//    }

//    if (packetid == (int)CoolingCabinetPacket.DrawerInteracted) {
//        data = SerializerUtil.Serialize(new Dictionary<string, int>() {
//            { inv[36].Itemstack?.Collectible.Code ?? "", inv[36].Itemstack?.StackSize ?? 0 } 
//        });
//        ((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(Pos, (int)CoolingCabinetPacket.DrawerInteractedOthers, data, (IServerPlayer)fromPlayer);
//    }
//}

//public override void OnReceivedServerPacket(int packetid, byte[] data) {

//    if (this is BlockEntityCoolingCabinet becc) {
//        if (packetid == (int)CoolingCabinetPacket.CabinetOpenOthers) {
//            bool containerOpened = SerializerUtil.Deserialize<bool>(data);
//            if (containerOpened) becc.OpenCabinet(null);
//            else becc.CloseCabinet(null);
//        }

//        if (packetid == (int)CoolingCabinetPacket.DrawerOpenOthers) {
//            bool containerOpened = SerializerUtil.Deserialize<bool>(data);
//            if (containerOpened) becc.OpenDrawer(null);
//            else becc.CloseDrawer(null);
//        }

//        if (packetid == (int)CoolingCabinetPacket.DrawerInteractedOthers) {
//            var drawerProps = SerializerUtil.Deserialize<Dictionary<string, int>>(data);


//            string collectibleCode = drawerProps.Keys.FirstOrDefault();
//            int stackSize = drawerProps.Values.FirstOrDefault();
//            Api.Logger.Debug($"Got: {drawerProps} - {collectibleCode} - {stackSize}.");

//            if (collectibleCode == "") {
//                IceHeightAllDown();
//                WaterHeightDown();
//            }
//            else {
//                if (WildcardUtil.Match(CoolingOnlyData.CollectibleCodes, collectibleCode)) {
//                    if (stackSize < 20) IceHeight1Up();
//                    else if (stackSize < 40) IceHeight2Up();
//                    else if (stackSize >= 40) IceHeight3Up();
//                }
//                else {
//                    WaterHeightUp();
//                }
//            }
//        }
//    }
//}

#endregion