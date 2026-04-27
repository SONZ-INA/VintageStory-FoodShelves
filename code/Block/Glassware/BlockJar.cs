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
            
            if (contents != null && contents.Length > 0) {
                dsc.Append(Lang.Get("foodshelves:Contents"));

                DummySlot dummySlot = new(contents[0]);
                dsc.Append(PerishableInfoCompact(world, dummySlot, 0));
                dsc.Append(TransitionInfoCompact(world, dummySlot, EnumTransitionType.Dry, TransitionDisplayMode.Percentage));
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

    public string GetContainedInfo(ItemSlot inSlot) {
        string jarName = GetContainedName(inSlot, 1);

        ItemStack[] contents = GetContents(api.World, inSlot.Itemstack);
        if (contents != null && contents.Length > 0) {
            return jarName + "<font color=\"#989898\">(" + GetNameAndStackSize(contents[0]) + ")</font>";
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

        // Determine capacity
        int referenceMaxStack = 64;
        if (!hotbarSlot.Empty) referenceMaxStack = hotbarSlot.Itemstack.Collectible.MaxStackSize;
        else if (contents.Length > 0 && contents[0] != null) referenceMaxStack = contents[0].Collectible.MaxStackSize;

        int jarCapacity = referenceMaxStack * InnerStackCount;

        DummySlot internalSlot = new(contents.Length > 0 ? contents[0] : null, be.Inventory) {
            MaxSlotStackSize = jarCapacity
        };

        bool changed = false;

        // Putting stuff in
        if (!hotbarSlot.Empty) {
            if (hotbarSlot.CanStoreInSlot("fsLiquidyStuff")) {
                int moved = hotbarSlot.TryPutIntoBulk(api.World, internalSlot, ctrl ? hotbarSlot.StackSize : 1);
                if (moved > 0) changed = true;
            }
        }
        // Taking stuff out
        else if (ctrl && !internalSlot.Empty) {
            int naturalMax = internalSlot.Itemstack.Collectible.MaxStackSize;
            int amountToTake = shift ? Math.Min(internalSlot.StackSize, naturalMax) : 1;

            ItemStack taken = internalSlot.Itemstack.Clone();
            taken.StackSize = amountToTake;

            internalSlot.Itemstack.StackSize -= amountToTake;

            if (internalSlot.Itemstack.StackSize <= 0) {
                internalSlot.Itemstack = null;
            }

            if (!byPlayer.InventoryManager.TryGiveItemstack(taken)) {
                api.World.SpawnItemEntity(taken, be.Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }
            changed = true;
        }

        if (changed) {
            SetContents(slot.Itemstack, [internalSlot.Itemstack]);

            slot.MarkDirty();
            be.MarkDirty();

            api.World.PlaySoundAt(GlobalConstants.DefaultBuildSound, byPlayer, byPlayer);
        }

        return changed;
    }

    public bool OnContainedInteractStep(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) => false;
    public void OnContainedInteractStop(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) { }
}
