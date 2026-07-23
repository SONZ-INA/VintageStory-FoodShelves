namespace FoodShelves;

public class BlockJar : BaseFSContainer, IContainedCustomName, IContainedInteractable {
    private readonly int InnerStackCount = 2; // A copy of the capacity that the jar can hold, for interactions with a Jar when it's on a Jar Stand.

    public override void OnLoaded(ICoreAPI api) {
        base.OnLoaded(api);

        List<ItemStack> stackList = [];
        foreach (var obj in api.World.Collectibles) {
            if (obj.CanStoreInSlot(WorldInteractionAttributeCheck!)) {
                stackList.Add(new ItemStack(obj));
            }
        }

        var stackArray = stackList.ToArray();

        itemSlottableInteractions = [
            new() {
                ActionLangCode = "blockhelp-toolrack-take",
                MouseButton = EnumMouseButton.Right,
                Itemstacks = null
            },
            new() {
                ActionLangCode = "blockhelp-groundstorage-add",
                MouseButton = EnumMouseButton.Right,
                Itemstacks = stackArray,
            },
            new() {
                ActionLangCode = "blockhelp-groundstorage-addbulk",
                MouseButton = EnumMouseButton.Right,
                Itemstacks = stackArray,
                HotKeyCode = "ctrl"
            },
            new() {
                ActionLangCode = "blockhelp-groundstorage-remove",
                MouseButton = EnumMouseButton.Right,
                HotKeyCode = "ctrl"
            },
            new() {
                ActionLangCode = "blockhelp-groundstorage-removebulk",
                MouseButton = EnumMouseButton.Right,
                HotKeyCodes = ["shift", "ctrl"]
            }
        ];
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        if (!inSlot.Empty) {
            ItemStack[] contents = GetContents(api.World, inSlot.Itemstack);
            if (contents.Length == 0) return;

            dsc.Append(Lang.Get("foodshelves:Contents"));

            DummySlot dummySlot = new(contents[0]);
            dsc.Append(PerishableInfoCompact(world, dummySlot, 0));
            dsc.Append(TransitionInfoCompact(world, dummySlot, EnumTransitionType.Dry, TransitionDisplayMode.Percentage));
        }
    }

    public override MeshData? GenMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas, BlockPos? atBlockPos) {
        // Rendered passes aren't accounted for in the hotbar, so remove it if this block is in the hotbar inventory.
        MeshData? blockMesh = slot.Inventory?.ClassName == "hotbar"
            ? GenBlockVariantMesh(api, slot.Itemstack, ["Glass1"])
            : base.GenMesh(slot, targetAtlas, atBlockPos);

        ItemStack[] contents = GetContents(api.World, slot.Itemstack);
        
        if (contents.Length > 0) {
            MeshData? contentMesh = GenLiquidyMesh(api as ICoreClientAPI, contents[0], ShapeReferences.utilJar, (contents[0].Item?.MaxStackSize * 2) ?? 128, 7.3f);
            if (contentMesh != null) blockMesh?.AddMeshData(contentMesh);
        }

        return blockMesh;
    }

    public override string GetMeshCacheKey(ItemSlot slot) {
        string blockKey = base.GetMeshCacheKey(slot);

        int hotbarSlot = slot.Inventory?.ClassName == "hotbar" ? 1 : 0; // To remove the glass from the hotbar - doesn't render.

        ItemStack[] contents = GetContents(api.World, slot.Itemstack);
        if (contents.Length == 0) return blockKey;

        string code = contents[0].Item?.Code ?? "unknown";
        int amount = contents[0].StackSize;

        return $"{blockKey}-{code}-{amount}-{hotbarSlot}";
    }

    public string GetContainedInfo(ItemSlot inSlot) {
        string jarName = GetContainedName(inSlot, 1);

        ItemStack[] contents = GetContents(api.World, inSlot.Itemstack);
        if (contents.Length > 0) {
            return $"{jarName}<font color=\"#989898\">({GetNameAndStackSize(contents[0])})</font>";
        }

        return jarName;
    }

    public string GetContainedName(ItemSlot inSlot, int quantity) {
        return GetHeldItemName(inSlot.Itemstack!);
    }

    public WorldInteraction[] GetContainedInteractionHelp(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) {
        return itemSlottableInteractions!;
    }

    public bool OnContainedInteractStart(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) {
        ICoreAPI api = be.Api;
        ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

        bool ctrl = byPlayer.Entity.Controls.CtrlKey;
        bool shift = byPlayer.Entity.Controls.ShiftKey;

        ItemStack[] contents = GetContents(api.World, slot.Itemstack);
        DummySlot internalSlot = CreateInternalSlot(be, hotbarSlot, contents);

        bool changed = !hotbarSlot.Empty
            ? TryPutIntoJar(api, hotbarSlot, internalSlot, ctrl)
            : TryTakeFromJar(api, be, byPlayer, internalSlot, ctrl, shift);

        if (!changed)
            return false;

        SetContents(slot.Itemstack, internalSlot.Itemstack != null ? [internalSlot.Itemstack] : []);

        slot.MarkDirty();
        be.MarkDirty();

        api.World.PlaySoundAt(GlobalConstants.DefaultBuildSound, byPlayer, byPlayer);

        return true;
    }

    protected DummySlot CreateInternalSlot(BlockEntityContainer be, ItemSlot hotbarSlot, ItemStack[] contents) {
        int referenceMaxStack = !hotbarSlot.Empty
            ? hotbarSlot.Itemstack.Collectible.MaxStackSize
            : contents.Length > 0
                ? contents[0].Collectible.MaxStackSize
                : 64;

        return new DummySlot(contents.Length > 0 ? contents[0] : null, be.Inventory) {
            MaxSlotStackSize = referenceMaxStack * InnerStackCount
        };
    }

    protected bool TryPutIntoJar(ICoreAPI api, ItemSlot hotbarSlot, DummySlot internalSlot, bool ctrl) {
        if (!hotbarSlot.CanStoreInSlot("fsLiquidyStuff"))
            return false;

        int moved = hotbarSlot.TryPutIntoBulk(api.World, internalSlot, ctrl ? hotbarSlot.StackSize : 1);
        return moved > 0;
    }

    protected bool TryTakeFromJar(ICoreAPI api, BlockEntityContainer be, IPlayer byPlayer, DummySlot internalSlot, bool ctrl, bool shift) {
        if (!ctrl || internalSlot.Empty)
            return false;

        int naturalMax = internalSlot.Itemstack.Collectible.MaxStackSize;

        int amount = shift
            ? Math.Min(internalSlot.StackSize, naturalMax)
            : 1;

        ItemStack taken = internalSlot.Itemstack.Clone();
        taken.StackSize = amount;

        internalSlot.Itemstack.StackSize -= amount;

        if (internalSlot.Itemstack.StackSize <= 0)
            internalSlot.Itemstack = null;

        if (!byPlayer.InventoryManager.TryGiveItemstack(taken)) {
            api.World.SpawnItemEntity(taken, be.Pos.ToVec3d().Add(0.5, 0.5, 0.5));
        }

        return true;
    }

    public bool OnContainedInteractStep(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) => false;
    public void OnContainedInteractStop(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) { }
}
