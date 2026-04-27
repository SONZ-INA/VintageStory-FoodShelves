namespace FoodShelves;

public class BlockJar : BaseFSContainer {
    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        dsc.Append(Lang.Get("foodshelves:Contents"));

        if (!inSlot.Empty) {
            ItemStack[] contents = GetContents(api.World, inSlot.Itemstack);
            
            if (contents != null && contents.Length > 0) {
                DummySlot dummySlot = new(contents[0]);
                dsc.Append(PerishableInfoCompact(world, dummySlot, 0));
            }
        }
    }

    public override MeshData? GenMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas, BlockPos? atBlockPos) {
        MeshData? blockMesh = base.GenMesh(slot, targetAtlas, atBlockPos);

        ItemStack[] contents = GetContents(api.World, slot.Itemstack);
        
        if (contents != null && contents.Length > 0) {
            MeshData? contentMesh = GenLiquidyMesh(api as ICoreClientAPI, contents[0], ShapeReferences.utilJar, (contents[0].Item?.MaxStackSize * 2) ?? 128, 7.3f);
            if (contentMesh != null) blockMesh?.AddMeshData(contentMesh);
        }

        return blockMesh;
    }

    public override string GetMeshCacheKey(ItemSlot slot) {
        string blockKey = base.GetMeshCacheKey(slot);

        ItemStack[] contents = GetContents(api.World, slot.Itemstack);
        if (contents.Length == 0) return blockKey;

        string code = contents[0].Item?.Code ?? "unknown";
        float amount = contents[0].StackSize;

        return $"{blockKey}-{code}-{amount}";
    }

    //public string GetContainedInfo(ItemSlot inSlot) {
    //    string jarName = GetContainedName(inSlot, 1);

    //    //ItemStack[] contents = GetContents(api.World, inSlot.Itemstack);
    //    //if (contents != null && contents.Length > 0) {
    //    //    int stackSize = contents[0].StackSize;
    //    //    string amount = stackSize > 0
    //    //        ? " x" + stackSize
    //    //        : "";

    //    //    return GetContainedName(inSlot, 1);
    //    //}

    //    return jarName;
    //}

    //public string GetContainedName(ItemSlot inSlot, int quantity) {
    //    return GetHeldItemName(inSlot.Itemstack!);
    //}
}
