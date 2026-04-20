namespace FoodShelves;

public class BlockGlassJar : BaseFSContainer {
    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        dsc.Append(Lang.Get("foodshelves:Contents"));

        if (!inSlot.Empty) {
            // ItemStack[] contents = GetContents(world, inSlot.Itemstack);
            // ByBlockMerged(contents.ToDummySlots(), dsc, world);
        }

        dsc.AppendLine(Lang.Get("foodshelves:Empty."));
        return;
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
        return BaseOnBlockInteractStart(world, byPlayer, blockSel); // To handle behaviors
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo) {
        if (api.Side == EnumAppSide.Server) return;

        string meshCacheKey = GetMeshCacheKey(renderinfo.InSlot);
        var meshrefs = GetCacheDictionary(capi, meshCacheKey);

        if (!meshrefs.TryGetValue(meshCacheKey, out MultiTextureMeshRef? meshRef)) {
            MeshData? jarMesh = GenBlockVariantMesh(capi, itemstack, ["Glass1"]); // Glass hides the content in GUI
            MeshData?contentMesh = GenLiquidyMesh(capi, renderinfo.InSlot, ShapeReferences.utilGlassJar, 8.5f);
            
            if (contentMesh != null) jarMesh?.AddMeshData(contentMesh);

            meshrefs[meshCacheKey] = meshRef = capi.Render.UploadMultiTextureMesh(jarMesh);
        }

        renderinfo.ModelRef = meshRef;
    }

    public override MeshData? GenMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas, BlockPos? atBlockPos) {
        MeshData? blockMesh = base.GenMesh(slot, targetAtlas, atBlockPos);
        MeshData? contentMesh = GenLiquidyMesh(api as ICoreClientAPI, slot, ShapeReferences.utilGlassJar, 6f);

        if (contentMesh != null) blockMesh?.AddMeshData(contentMesh);

        return blockMesh;
    }
}
