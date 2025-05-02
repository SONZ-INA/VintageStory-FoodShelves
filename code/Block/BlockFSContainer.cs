namespace FoodShelves;

public class BlockFSContainer : Block, IContainedMeshSource {
    public const string FSAttributes = "FSAttributes";

    public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos) {
        return true;
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
        if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is IFoodShelvesContainer fscontainer) return fscontainer.OnInteract(byPlayer, blockSel);
        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override string GetHeldItemName(ItemStack itemStack) {
        string variantName = itemStack.GetMaterialName(); // TODO: Change method logic to support coded variants instead.
        return base.GetHeldItemName(itemStack) + " " + variantName;
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos) {
        var stack = base.OnPickBlock(world, pos);

        if (world.BlockAccessor.GetBlockEntity(pos) is IFoodShelvesContainer fscontainer) {
            var attrTree = new TreeAttribute();
            foreach (var attr in fscontainer.VariantAttributes) {
                attrTree.SetAttribute(attr.Key, attr.Value);
            }

            if (attrTree.Count != 0) {
                stack.Attributes[FSAttributes] = attrTree;
            }
        }

        return stack;
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1) {
        return new ItemStack[] { OnPickBlock(world, pos) };
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo) {
        string meshCacheKey = GetMeshCacheKey(itemstack);
        var meshrefs = GetCacheDictionary(capi, meshCacheKey);

        if (!meshrefs.TryGetValue(meshCacheKey, out MultiTextureMeshRef meshRef)) {
            MeshData mesh = GenMesh(itemstack, capi.BlockTextureAtlas, null);
            meshrefs[meshCacheKey] = meshRef = capi.Render.UploadMultiTextureMesh(mesh);
        }

        renderinfo.ModelRef = meshRef;
    }

    public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos) {
        return GenBlockVariantMesh(api, this, itemstack);
    }

    public string GetMeshCacheKey(ItemStack itemstack) {
        if (itemstack.Attributes[FSAttributes] is not ITreeAttribute tree) return Code;

        List<string> parts = new();
        foreach (var pair in tree) {
            parts.Add($"{pair.Key}-{pair.Value}");
        }

        return $"{Code}-{string.Join("-", parts)}";
    }
}
