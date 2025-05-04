using System.Linq;

namespace FoodShelves;

public class BlockVegetableBasket : BlockContainer, IContainedMeshSource {
    private WorldInteraction[] interactions;
    private static RestrictionData VegetableBasketData;

    public override void OnLoaded(ICoreAPI api) {
        base.OnLoaded(api);
        PlacedPriorityInteract = true; // Needed to call OnBlockInteractStart when shifting with an item in hand

        VegetableBasketData = api.LoadAsset<RestrictionData>("foodshelves:config/restrictions/baskets/vegetablebasket.json");

        interactions = ObjectCacheUtil.GetOrCreate(api, "vegetableBasketBlockInteractions", () => {
            List<ItemStack> vegetableStackList = new();

            foreach(Item item in api.World.Items) {
                if (item.Code == null) continue;

                if (item.VegetableBasketCheck()) {
                    vegetableStackList.Add(new ItemStack(item));
                }
            }

            return new WorldInteraction[] {
                new() {
                    ActionLangCode = "blockhelp-groundstorage-add",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "shift",
                    Itemstacks = vegetableStackList.ToArray()
                },
                new() {
                    ActionLangCode = "blockhelp-groundstorage-remove",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "shift"
                }
            };
        });
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {
        return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));      
    }

    public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1) {
        // Prevent duplicating of items inside
        if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Survival) {
            if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityVegetableBasket vgbasket) {
                ItemStack[] contents = vgbasket.GetContentStacks();
                ItemStack emptyVegetableBasket = new(this);
                world.SpawnItemEntity(emptyVegetableBasket, pos.ToVec3d().Add(0.5, 0.5, 0.5));
                for (int i = 0; i < contents.Length; i++) {
                    world.SpawnItemEntity(contents[i], pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }
            }
        }

        world.BlockAccessor.SetBlock(0, pos);
    }

    public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos) {
        BlockEntityVegetableBasket be = GetBlockEntity<BlockEntityVegetableBasket>(pos);
        if (be != null) {
            BlockBehaviorCanCeilingAttachFalling beh = GetBehavior<BlockBehaviorCanCeilingAttachFalling>();
            beh.CanBlockStay(world, pos, out bool isCeilingAttached);
            be.IsCeilingAttached = isCeilingAttached;
            be.MarkDirty(true);
        }

        base.OnNeighbourBlockChange(world, pos, neibpos);
    }

    public override string GetHeldItemName(ItemStack itemStack) {
        string variantName = itemStack.GetMaterialNameLocalized();
        return base.GetHeldItemName(itemStack) + " " + variantName;
    }

    // Rotation logic
    public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack) {
        bool val = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
        BlockEntityVegetableBasket block = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityVegetableBasket;
        block.MeshAngle = GetBlockMeshAngle(byPlayer, blockSel, val);

        return val;
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
        if (byPlayer.Entity.Controls.ShiftKey) {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityVegetableBasket frbasket) 
                return frbasket.OnInteract(byPlayer);
        }

        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        dsc.Append(Lang.Get("foodshelves:Contents"));

        if (inSlot.Itemstack == null) {
            dsc.AppendLine(Lang.Get("foodshelves:Empty."));
            return;
        }

        ItemStack[] contents = GetContents(world, inSlot.Itemstack);
        PerishableInfoAverageAndSoonest(contents.ToDummySlots(), dsc, world);
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo) {
        Dictionary<int, MultiTextureMeshRef> meshrefs;

        string meshCacheKey = GetMeshCacheKey(itemstack);

        if (capi.ObjectCache.TryGetValue(meshCacheKey, out object obj)) {
            meshrefs = obj as Dictionary<int, MultiTextureMeshRef>;
        }
        else {
            capi.ObjectCache[meshCacheKey] = meshrefs = new Dictionary<int, MultiTextureMeshRef>();
        }

        ItemStack[] contents = GetContents(api.World, itemstack);
        int hashcode = GetStackCacheHashCodeFNV(contents);

        if (!meshrefs.TryGetValue(hashcode, out MultiTextureMeshRef meshRef)) {
            MeshData mesh = GenMesh(itemstack, capi.BlockTextureAtlas, null);
            if (mesh != null) meshrefs[hashcode] = meshRef = capi.Render.UploadMultiTextureMesh(mesh);
        }

        renderinfo.ModelRef = meshRef;
    }

    public static float[,] GetTransformationMatrix(string path) {
        float[] x = { .75f }, y = { 0 }, z = { -.03f }, rX = { -2 }, rY = { 4 }, rZ = { 1 };
        if (path == null) return GenTransformationMatrix(x, y, z, rX, rY, rZ);

        foreach (var group in VegetableBasketData.GroupingCodes) {
            if (group.Value.Contains(path)) {
                switch(group.Key) {
                    case "group1":
                        x = new float[] { .75f, .3f, .3f, .3f, .65f, .35f,  .5f, .1f,  .6f, .58f, .2f, .25f };
                        y = new float[] {    0,   0,   0, .25f,   0, .35f,  .2f, .2f,  .4f,  .4f, .5f, .52f };
                        z = new float[] { .05f,   0, .4f, .1f, .45f, .35f, .18f, .1f, .02f,  .4f,  0f, .15f };

                        rX = new float[] {  -2,   0,   0,  -3,   -3,   28,   16,  30,    0,    5,  -8,    8 };
                        rY = new float[] {   4,  -2,  15,  -4,   10,   12,   30,   4,   -5,   -2,  20,   15 };
                        rZ = new float[] {   1,  -1,   0,  45,    1,   41,    5,  17,   -2,  -20,  16,    8 };
                        return GenTransformationMatrix(x, y, z, rX, rY, rZ);
                    case "group2":
                        x = new float[] { .75f, .3f, .19f,  .3f, .51f, .35f,  .05f,  .85f,   .7f,  .9f, .58f,   .4f };
                        y = new float[] {    0,   0,    0, .25f,    0, .35f,   .2f, -.25f, -.35f, .15f,  .4f, -.35f };
                        z = new float[] { .05f,   0,  .3f, .05f,  .4f, .25f, -.05f,  .05f,  .05f, .35f,  .3f,  -.3f };

                        rX = new float[] {  -2,   0,    0,   -3,   -3,   28,    16,    90,    90,   30,    5,    90 };
                        rY = new float[] {   4,  -2,   15,   -4,   10,   12,    30,     0,     0,    4,   -2,     0 };
                        rZ = new float[] {   1,  -1,    0,   45,    1,   41,     5,    12,    83,   17,  -20,    83 };
                        return GenTransformationMatrix(x, y, z, rX, rY, rZ);
                    case "group3":
                        x = new float[] { .75f, .74f,  .73f, .72f,  .71f, .70f, .15f, .15f, .15f, .15f, .15f, .15f, .75f, .74f, .73f, .72f, .71f, .70f, .15f, .15f, .15f, .15f, .15f, .15f, .75f, .74f, .73f, .72f, .71f, .70f, .15f, .15f, .15f, .15f, .15f, .15f };
                        y = new float[] {    0,    0,     0,    0,     0,    0,    0,    0,    0,    0,    0,    0, .15f, .15f, .15f, .15f, .15f, .15f, .15f, .15f, .15f, .15f, .15f, .15f, .30f, .30f, .30f, .30f, .30f, .30f, .30f, .30f, .30f, .30f, .30f, .30f };
                        z = new float[] {-.03f, .12f,  .27f, .42f,  .57f, .72f,-.03f, .12f, .27f, .42f, .57f, .72f,-.03f, .12f, .27f, .42f, .57f, .72f,-.03f, .12f, .27f, .42f, .57f, .72f,-.03f, .12f, .27f, .42f, .57f, .72f,-.03f, .12f, .27f, .42f, .57f, .72f };

                        rX = new float[] {  -2,   -2,    -2,   -2,    -2,   -2,    0,    0,    0,    0,    0,    0,   -2,   -2,   -2,   -2,   -2,   -2,    0,    0,    0,    0,    0,    0,   -2,   -2,   -2,   -2,   -2,   -2,    0,    0,    0,    0,    0,    0 };
                        rY = new float[] {   4,    4,     4,    4,     4,    4,   -2,   -2,   -2,   -2,   -2,   -2,    4,    4,    4,    4,    4,    4,   -2,   -2,   -2,   -2,   -2,   -2,    4,    4,    4,    4,    4,    4,   -2,   -2,   -2,   -2,   -2,   -2 };
                        rZ = new float[] {   1,    1,     1,    1,     1,    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,    1 };
                        return GenTransformationMatrix(x, y, z, rX, rY, rZ);
                }
            }
        }

        return GenTransformationMatrix(x, y, z, rX, rY, rZ);
    }

    public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos) {
        ICoreClientAPI capi = api as ICoreClientAPI;

        capi.Tesselator.TesselateBlock(this, out MeshData basketMesh);

        ItemStack[] contents = GetContents(api.World, itemstack);

        string itemPath = "";
        if (contents != null && contents.Length > 0 && contents[0] != null) {
            itemPath = contents[0].Collectible.Code;
        }

        MeshData contentMesh = GenContentMesh(capi, targetAtlas, contents, GetTransformationMatrix(itemPath), 0.5f, VegetableBasketTransformations);

        if (contentMesh != null) {
            basketMesh.AddMeshData(contentMesh);
        }

        return basketMesh;
    }

    public string GetMeshCacheKey(ItemStack itemstack) {
        ItemStack[] contents = GetContents(api.World, itemstack);
        int hashcode = GetStackCacheHashCodeFNV(contents);

        return $"{itemstack.Collectible.Code}-{hashcode}";
    }
}
