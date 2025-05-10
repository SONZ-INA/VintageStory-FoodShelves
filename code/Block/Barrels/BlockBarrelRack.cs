namespace FoodShelves;

public class BlockBarrelRack : BlockLiquidContainerBase, IContainedMeshSource {
    public override bool AllowHeldLiquidTransfer => false;
    public override int GetContainerSlotId(BlockPos pos) => 1;
    public override int GetContainerSlotId(ItemStack containerStack) => 1;

    public override void OnLoaded(ICoreAPI api) {
        base.OnLoaded(api);
        PlacedPriorityInteract = true; // Needed to call OnBlockInteractStart when shifting with an item in hand

        LoadVariantsCreative(api, this);
    }

    public override int GetRetention(BlockPos pos, BlockFacing facing, EnumRetentionType type) {
        return 0; // To prevent the block reducing the cellar rating
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
        if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEBarrelRack ifsc) return ifsc.OnInteract(byPlayer, blockSel);
        else return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public bool BaseOnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {
        if (world.BlockAccessor.GetBlockEntity(selection.Position) is BEBarrelRack be && be.Inventory.Empty) return null;
        else return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
    }

    public override string GetHeldItemName(ItemStack itemStack) {
        string itemName = base.GetHeldItemName(itemStack);
        return itemName + " " + itemStack.GetMaterialNameLocalized();
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        dsc.AppendLine("");
        dsc.AppendLine(Lang.Get("foodshelves:helddesc-barrelrack"));
    }

    public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1) {
        // First, check for behaviors preventing default, for example Reinforcement system
        bool preventDefault = false;
        foreach (BlockBehavior behavior in BlockBehaviors) {
            EnumHandling handled = EnumHandling.PassThrough;

            behavior.OnBlockBroken(world, pos, byPlayer, ref handled);
            if (handled == EnumHandling.PreventDefault) preventDefault = true;
            if (handled == EnumHandling.PreventSubsequent) return;
        }

        if (preventDefault) return;

        // Drop barrel
        BEBarrelRack be = GetBlockEntity<BEBarrelRack>(pos);
        be?.Inventory.DropAll(pos.ToVec3d());

        // Spawn liquid particles
        if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)) {
            ItemStack[] array = new ItemStack[1] { OnPickBlock(world, pos) };
            for (int j = 0; j < array.Length; j++) {
                world.SpawnItemEntity(array[j], new Vec3d(pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5));
            }

            world.PlaySoundAt(Sounds.GetBreakSound(byPlayer), pos.X, pos.Y, pos.Z, byPlayer);
        }

        world.BlockAccessor.SetBlock(0, pos);
    }

    // Dynamic change of collision boxes, when there's a barrel inside
    public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos) {
        if (Variant["type"] == "top") {
            if (blockAccessor.GetBlockEntity(pos) is BEBarrelRack be && be.Inventory.Empty) {
                return new Cuboidf[] { new(0, 0, 0, 1f, 0.3f, 1f) };
            }
        }

        return base.GetCollisionBoxes(blockAccessor, pos);
    }

    public override void TryFillFromBlock(EntityItem byEntityItem, BlockPos pos) {
        // Don't fill when dropped as item in water
    }

    public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer) {
        StringBuilder dsc = new();

        BEBarrelRack be = GetBlockEntity<BEBarrelRack>(pos);
        if (be?.Inventory.Empty == true) dsc.Append(Lang.Get("foodshelves:Missing barrel."));
        else {
            dsc.Append(base.GetPlacedBlockInfo(world, pos, forPlayer));

            if (!be?.inv[1].Empty == true) {
                dsc.Append(TransitionInfoCompact(world, be.inv[1], EnumTransitionType.Cure));
            }
        }

        return dsc.ToString();
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos) {
        var stack = base.OnPickBlock(world, pos);

        if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityContainer bec) {
            if (bec.Inventory.Empty) {
                stack.Attributes.RemoveAttribute("contents"); // To prevent stupid BlockContainer empty attributes
            }
        }

        if (world.BlockAccessor.GetBlockEntity(pos) is IFoodShelvesContainer fscontainer) {
            if (fscontainer?.VariantAttributes?.Count != 0) {
                stack.Attributes[BaseFSContainer.FSAttributes] = fscontainer.VariantAttributes;
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

    public virtual MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos) {
        return GenBlockVariantMesh(api, itemstack);
    }

    public virtual string GetMeshCacheKey(ItemStack itemstack) {
        if (itemstack.Attributes[BaseFSContainer.FSAttributes] is not ITreeAttribute tree) return Code;

        List<string> parts = new();
        foreach (var pair in tree) {
            parts.Add($"{pair.Key}-{pair.Value}");
        }

        return $"{Code}-{string.Join("-", parts)}";
    }
}
