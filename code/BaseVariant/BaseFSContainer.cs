namespace FoodShelves;

public class BaseFSContainer : BlockContainer, IContainedMeshSource {
    public BlockHintType hintType = BlockHintType.None;

    protected string? WorldInteractionAttributeCheck = null;
    protected bool globalBlockBuffs = true;
    protected WorldInteraction[]? itemSlottableInteractions;
    
    private string? heldDescEntry;
    private string hintTypeStr = "none";
    private string placingMessage = "";
    private bool preventPlacing = false;

    public override void OnLoaded(ICoreAPI api) {
        base.OnLoaded(api);
        
        PlacedPriorityInteract = true; // Needed to call OnBlockInteractStart when shifting with an item in hand
        
        heldDescEntry = globalBlockBuffs
            ? Attributes["helddescentry"].AsString(Code.FirstCodePart())
            : "";
        WorldInteractionAttributeCheck = Attributes["worldInteractionAttributeCheck"].AsString()
            ?? "fs" + Code.FirstCodePart();

        globalBlockBuffs = api.World.Config.GetBool("FoodShelves.GlobalBlockBuffs", true);
        preventPlacing = Attributes["preventPlacing"].AsBool(false);
        placingMessage = Attributes["placingMessage"].AsString("");
        hintTypeStr = Attributes["hintType"].AsString("none");
        Enum.TryParse(hintTypeStr, true, out hintType);
        
        LoadVariantsCreative(api, this);

        if (hintType == BlockHintType.None)
            return;

        List<ItemStack> stackList = [];
        foreach (var obj in api.World.Collectibles) {
            if (obj.CanStoreInSlot(WorldInteractionAttributeCheck)) {
                stackList.Add(new ItemStack(obj));
            }
        }

        var stackArray = stackList.ToArray();
        
        itemSlottableInteractions = ObjectCacheUtil.GetOrCreate(api, Code.FirstCodePart(), () => {
            return hintType switch {
                BlockHintType.SingleSlot => new WorldInteraction[] {
                        new() {
                            ActionLangCode = "blockhelp-groundstorage-add",
                            MouseButton = EnumMouseButton.Right,
                            Itemstacks = stackArray,
                        },
                        new() {
                            ActionLangCode = "blockhelp-groundstorage-remove",
                            MouseButton = EnumMouseButton.Right
                        }
                    },
                BlockHintType.Bulk => [
                        new() {
                            ActionLangCode = "blockhelp-groundstorage-add",
                            MouseButton = EnumMouseButton.Right,
                            Itemstacks = stackArray,
                            HotKeyCode = "shift"
                        },
                        new() {
                            ActionLangCode = "blockhelp-groundstorage-addbulk",
                            MouseButton = EnumMouseButton.Right,
                            Itemstacks = stackArray,
                            HotKeyCodes = ["shift", "ctrl"]
                        },
                        new() {
                            ActionLangCode = "blockhelp-groundstorage-remove",
                            MouseButton = EnumMouseButton.Right
                        },
                        new() {
                            ActionLangCode = "blockhelp-groundstorage-removebulk",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCode = "ctrl"
                        }
                    ],
                _ => [],
            };
        });
    }

    public override bool DoPartialSelection(IWorldAccessor world, BlockPos pos) {
        return true;
    }
    
    public override int GetRetention(BlockPos pos, BlockFacing facing, EnumRetentionType type) {
        return 0; // To prevent the block reducing the cellar rating
    }

    public WorldInteraction[]? BaseGetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {
        return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
    }

    public override WorldInteraction[]? GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {
        if (itemSlottableInteractions?.Length > 0)
            return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer).Append(itemSlottableInteractions);
        
        return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
    }

    public bool BaseOnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
        if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is IFoodShelvesContainer fsContainer) {
            if (fsContainer.OnInteract(byPlayer, blockSel))
                return true;
        }

        // Handle block behaviors
        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1) {
        if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityContainer bec) { // Split stacks
            Vec3d dropPos = pos.ToVec3d().Add(0.5, 0.5, 0.5);

            foreach (var stack in bec.GetContentStacks()) {
                if (stack == null) continue;

                int max = stack.Collectible.MaxStackSize;

                while (stack.StackSize > 0) {
                    int amount = Math.Min(max, stack.StackSize);

                    ItemStack drop = stack.Clone();
                    drop.StackSize = amount;

                    stack.StackSize -= amount;

                    world.SpawnItemEntity(drop, dropPos);
                }
            }

            bec.Inventory.Clear();
        }

        base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos) {
        var stack = base.OnPickBlock(world, pos);

        if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityContainer bec) {
            if (bec.Inventory.Empty) {
                stack.Attributes.RemoveAttribute("contents"); // To prevent stupid BlockContainer empty attributes
            }
        }

        if (world.BlockAccessor.GetBlockEntity(pos) is IFoodShelvesContainer fscontainer) {
            if (fscontainer.VariantAttributes?.Count > 0) {
                stack.Attributes[FSAttributes] = fscontainer.VariantAttributes;
            }
        }

        return stack;
    }

    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode) {
        if (preventPlacing) {
            (api as ICoreClientAPI)!.TriggerIngameError(this, "cantplace", Lang.Get(placingMessage));
            failureCode = "__ignore__";
            return false;
        }

        return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1) {
        return [OnPickBlock(world, pos)];
    }

    public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer) {
        BlockDropItemStack[] drops = base.GetDropsForHandbook(handbookStack, forPlayer);
        drops[0].ResolvedItemstack?.SetFrom(handbookStack);
        return drops;
    }

    public override string GetHeldItemName(ItemStack itemStack) {
        return base.GetHeldItemName(itemStack) + " " + itemStack.GetMaterialNameLocalized();
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        string entry = "foodshelves:helddesc-" + heldDescEntry;
        string desc = Lang.Get(entry);

        if (desc != entry) {
            dsc.AppendLine();
            dsc.AppendLine(desc);
        }
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo) {
        if (api.Side == EnumAppSide.Server) return;
        
        string meshCacheKey = GetMeshCacheKey(renderinfo.InSlot);
        var meshrefs = GetCacheDictionary(capi, meshCacheKey);

        if (!meshrefs.TryGetValue(meshCacheKey, out MultiTextureMeshRef? meshRef)) {
            MeshData? mesh = GenMesh(renderinfo.InSlot, capi.BlockTextureAtlas, null);
            meshrefs[meshCacheKey] = meshRef = capi.Render.UploadMultiTextureMesh(mesh);
        }

        renderinfo.ModelRef = meshRef;
    }

    public virtual MeshData? GenMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas, BlockPos? atBlockPos) {
        return GenBlockVariantMesh(api, slot.Itemstack);
    }

    public virtual string GetMeshCacheKey(ItemSlot slot) {
        if (slot.Itemstack?.Attributes[FSAttributes] is not ITreeAttribute tree) 
            return Code;

        List<string> parts = [];
        foreach (var pair in tree) {
            parts.Add($"{pair.Key}-{pair.Value}"); // No support for various domains across mods. (eg. cloth from "game:" and "wool:" domains)
        }

        return $"{Code}-{string.Join("-", parts)}";
    }
}
