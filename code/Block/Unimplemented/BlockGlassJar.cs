namespace FoodShelves;

public class BlockGlassJar : BlockContainer {
    public override void OnLoaded(ICoreAPI api) {
        base.OnLoaded(api);
        PlacedPriorityInteract = true; // Needed to call OnBlockInteractStart when shifting with an item in hand
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
        if (byPlayer.Entity.Controls.ShiftKey) {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityGlassJar begj) 
                return begj.OnInteract(byPlayer);
        }

        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    private string MeshRefsCacheKey => this.Code.ToShortString() + "meshRefs";
    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo) {
        Dictionary<int, MultiTextureMeshRef> meshrefs;

        if (capi.ObjectCache.TryGetValue(MeshRefsCacheKey, out object obj)) {
            meshrefs = obj as Dictionary<int, MultiTextureMeshRef>;
        }
        else {
            capi.ObjectCache[MeshRefsCacheKey] = meshrefs = new Dictionary<int, MultiTextureMeshRef>();
        }

        ItemStack[] contents = GetContents(api.World, itemstack);
        int hashcode = GetStackCacheHashCodeFNV(contents);

        if (!meshrefs.TryGetValue(hashcode, out MultiTextureMeshRef meshRef)) {
            MeshData jarMesh = GenBlockMeshWithoutElements(capi, this, new[] { "Glass1" }); // Glass hides the content in GUI
            MeshData contentMesh = GenLiquidyMesh(capi, contents, ShapeReferences.utilGlassJar, 8.5f);
            if (contentMesh != null) jarMesh.AddMeshData(contentMesh);

            if (jarMesh != null) {
                meshrefs[hashcode] = meshRef = capi.Render.UploadMultiTextureMesh(jarMesh);
            }
        }

        renderinfo.ModelRef = meshRef;
    }

    //public override string GetHeldItemName(ItemStack itemStack) {
    //    string variantName = itemStack.GetMaterialNameLocalizedOLD();
    //    return base.GetHeldItemName(itemStack) + " " + variantName;
    //}

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        dsc.Append(Lang.Get("foodshelves:Contents"));

        if (inSlot.Itemstack == null) {
            dsc.AppendLine(Lang.Get("foodshelves:Empty."));
            return;
        }

        ItemStack[] contents = GetContents(world, inSlot.Itemstack);
        ByBlockMerged(contents.ToDummySlots(), dsc, world);
    }
}
