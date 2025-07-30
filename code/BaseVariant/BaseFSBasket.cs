namespace FoodShelves;

public abstract class BaseFSBasket : BaseFSContainer {
    private WorldInteraction[] interactions;
    
    protected virtual Dictionary<string, ModelTransform> Transformations { get; set; }
    protected virtual string InteractionsName => GetType().Name.Replace("Block", "");

    public override void OnLoaded(ICoreAPI api) {
        base.OnLoaded(api);

        Transformations ??= api.LoadAsset<Dictionary<string, ModelTransform>>($"foodshelves:config/transformations/baskets/{InteractionsName.ToLower()}.json");

        interactions = ObjectCacheUtil.GetOrCreate(api, InteractionsName + "BlockInteractions", () => {
            List<ItemStack> stackList = [];

            foreach (Item item in api.World.Items) {
                if (item.Code == null) continue;

                if (item.CanStoreInSlot("fs" + InteractionsName)) {
                    stackList.Add(new ItemStack(item));
                }
            }

            return new WorldInteraction[] {
                new() {
                    ActionLangCode = "blockhelp-groundstorage-add",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "shift",
                    Itemstacks = [.. stackList]
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

    public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos) {
        BEBaseFSBasket be = GetBlockEntity<BEBaseFSBasket>(pos);
        if (be != null) {
            BlockBehaviorCanCeilingAttachFalling beh = GetBehavior<BlockBehaviorCanCeilingAttachFalling>();
            beh.CanBlockStay(world, pos, out bool isCeilingAttached);
            be.IsCeilingAttached = isCeilingAttached;
            be.MarkDirty(true);
        }

        base.OnNeighbourBlockChange(world, pos, neibpos);
    }

    // Rotation logic
    public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack) {
        bool val = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
        BEBaseFSBasket block = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEBaseFSBasket;
        block.MeshAngle = GetBlockMeshAngle(byPlayer, blockSel, val);

        return val;
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
        if (byPlayer.Entity.Controls.ShiftKey) {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEBaseFSBasket frbasket)
                return frbasket.OnInteract(byPlayer, blockSel);
        }

        return BaseOnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        dsc.Append(Lang.Get("foodshelves:Contents"));

        if (inSlot.Itemstack == null) {
            dsc.AppendLine(Lang.Get("foodshelves:Empty."));
            return;
        }

        ItemStack[] contents = GetContents(world, inSlot.Itemstack);
        dsc.Append(PerishableInfoAverageAndSoonest(contents.ToDummySlots(), world));
    }

    public abstract float[,] GetTransformationMatrix(string path = null);

    public override MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos) {
        MeshData basketMesh = base.GenMesh(itemstack, targetAtlas, atBlockPos);
        MeshData contentMesh = GenBasketContents(itemstack, targetAtlas);

        if (contentMesh != null) {
            contentMesh.Translate(0, 0.02f, 0);
            basketMesh.AddMeshData(contentMesh);
        }

        return basketMesh;
    }

    protected virtual MeshData GenBasketContents(ItemStack itemstack, ITextureAtlasAPI targetAtlas) {
        ItemStack[] contents = GetContents(api.World, itemstack);
        MeshData contentMesh = GenContentMesh(api as ICoreClientAPI, targetAtlas, contents, GetTransformationMatrix(), 1f, Transformations);

        return contentMesh;
    }

    protected MeshData BaseGenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos) {
        return base.GenMesh(itemstack, targetAtlas, atBlockPos);
    }

    public override string GetMeshCacheKey(ItemStack itemstack) {
        string blockKey = base.GetMeshCacheKey(itemstack);

        ItemStack[] contents = GetContents(api.World, itemstack);
        int hashcode = GetStackCacheHashCodeFNV(contents);

        return $"{blockKey}-{hashcode}";
    }
}
