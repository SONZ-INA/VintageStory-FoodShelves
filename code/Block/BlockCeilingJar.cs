namespace FoodShelves;

public class BlockCeilingJar : BlockContainer, IContainedMeshSource {
    public override void OnLoaded(ICoreAPI api) {
        base.OnLoaded(api);
        PlacedPriorityInteract = true; // Needed to call OnBlockInteractStart when shifting with an item in hand
    }

    public override int GetRetention(BlockPos pos, BlockFacing facing, EnumRetentionType type) {
        return 0; // To prevent the block reducing the cellar rating
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
        if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityCeilingJar becj) return becj.OnInteract(byPlayer);
        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1) {
        // Prevent duplicating of items inside
        if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Survival) {
            if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityCeilingJar becj) {
                ItemStack emptyCeilingJar = new(this);
                world.SpawnItemEntity(emptyCeilingJar, pos.ToVec3d().Add(0.5, 0.5, 0.5));

                ItemStack[] contents = becj.GetContentStacks();
                for (int i = 0; i < contents.Length; i++) {
                    world.SpawnItemEntity(contents[i], pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }
            }
        }

        world.BlockAccessor.SetBlock(0, pos);
    }

    public override string GetHeldItemName(ItemStack itemStack) {
        string variantName = itemStack.GetMaterialNameLocalized();
        return base.GetHeldItemName(itemStack) + " " + variantName;
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        dsc.AppendLine("");
        dsc.AppendLine(Lang.Get("foodshelves:helddesc-ceilingjar"));
    }

    public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos) {
        ICoreClientAPI capi = api as ICoreClientAPI;

        capi.Tesselator.TesselateBlock(this, out MeshData basketMesh);

        ItemStack[] contents = GetContents(api.World, itemstack);
        MeshData contentMesh = GenLiquidyMesh(capi, contents, ShapeReferences.utilCeilingJar);

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
