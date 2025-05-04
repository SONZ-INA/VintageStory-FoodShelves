namespace FoodShelves;

public class BlockFruitBasket : BlockContainer, IContainedMeshSource {
    WorldInteraction[] interactions;

    public override void OnLoaded(ICoreAPI api) {
        base.OnLoaded(api);
        PlacedPriorityInteract = true; // Needed to call OnBlockInteractStart when shifting with an item in hand

        interactions = ObjectCacheUtil.GetOrCreate(api, "fruitBasketBlockInteractions", () => {
            List<ItemStack> fruitStackList = new();

            foreach(Item item in api.World.Items) {
                if (item.Code == null) continue;

                if (item.FruitBasketCheck()) {
                    fruitStackList.Add(new ItemStack(item));
                }
            }

            return new WorldInteraction[] {
                new() {
                    ActionLangCode = "blockhelp-groundstorage-add",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "shift",
                    Itemstacks = fruitStackList.ToArray()
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
            if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityFruitBasket frbasket) {
                ItemStack emptyFruitBasket = new(this);
                world.SpawnItemEntity(emptyFruitBasket, pos.ToVec3d().Add(0.5, 0.5, 0.5));
                
                ItemStack[] contents = frbasket.GetContentStacks();
                for (int i = 0; i < contents.Length; i++) {
                    world.SpawnItemEntity(contents[i], pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }
            }
        }

        world.BlockAccessor.SetBlock(0, pos);
    }

    public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos) {
        BlockEntityFruitBasket be = GetBlockEntity<BlockEntityFruitBasket>(pos);
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
        BlockEntityFruitBasket block = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityFruitBasket;
        block.MeshAngle = GetBlockMeshAngle(byPlayer, blockSel, val);

        return val;
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
        if (byPlayer.Entity.Controls.ShiftKey) {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityFruitBasket frbasket) 
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

    public static float[,] GetTransformationMatrix() {
        float[] x = { .65f, .3f, .3f,  .3f,  .6f, .35f,  .5f, .65f, .35f, .1f,  .6f, .58f, .3f,   .2f, -.1f,  .1f, .1f, .25f,  .2f, .55f,   .6f, .3f };
        float[] y = {    0,   0,   0, .25f,    0, .35f,  .2f, -.3f,  .3f, .2f,  .4f,  .4f, .4f,   .5f, .57f, .05f, .3f, .52f, .55f, .45f, -.65f, .5f };
        float[] z = { .05f,   0, .4f,  .1f, .45f, .35f, .18f,  .7f, .55f, .1f, .02f,  .3f, .7f, -.15f, .15f, -.2f, .9f, .05f,  .6f, .35f,  -.2f, .6f };

        float[] rX = {  -2,   0,   0,   -3,   -3,   28,   16,   -2,   20,  30,  -20,    5, -75,    -8,   10,   85,   0,    8,   15,   -8,    90, -10 };
        float[] rY = {   4,  -2,  15,   -4,   10,   12,   30,    3,   -2,   4,   -5,   -2,   2,    20,   55,    2,  50,   15,    0,    0,    22,  10 };
        float[] rZ = {   1,  -1,   0,   45,    1,   41,    5,   70,   10,  17,   -2,  -20,   3,    16,    7,    6, -20,    8,  -25,   15,    45, -10 };

        return GenTransformationMatrix(x, y, z, rX, rY, rZ);
    }

    public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos) {
        ICoreClientAPI capi = api as ICoreClientAPI;

        capi.Tesselator.TesselateBlock(this, out MeshData basketMesh);

        ItemStack[] contents = GetContents(api.World, itemstack);
        MeshData contentMesh = GenContentMesh(capi, targetAtlas, contents, GetTransformationMatrix(), 0.5f, FruitBasketTransformations);

        if (contentMesh != null) {
            contentMesh.Translate(0, 0.02f, 0);
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