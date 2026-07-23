using System.Linq;

namespace FoodShelves;

public abstract class BaseFSBasket : BaseFSContainer, IContainedInteractable {
    private WorldInteraction[]? interactions;

    protected virtual Dictionary<string, ModelTransform> Transformations { get; set; } = null!;
    protected virtual string InteractionsName => GetType().Name.Replace("Block", "");

    public virtual int InnerSlotCount { get; protected set; } // Separate property since stuff can be put inside when within a Cooling Cabinet

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

    public abstract ExplicitTransform GetTransformationMatrix(string? path = null);
    
    public virtual Action<TransformationData>? GetTransformationModifier() {
        return null;
    }

    public override WorldInteraction[]? GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {
        return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer)
            .Append(interactions);
    }

    public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos) {
        if (neibpos.Equals(pos.UpCopy())) {
            BEBaseFSBasket? be = GetBlockEntity<BEBaseFSBasket>(pos);

            if (be != null) {
                Block upBlock = world.BlockAccessor.GetBlock(pos.UpCopy());

                var areasDict = TryGetAttachmentAreas(this);
                Cuboidi? attachmentArea = GetAreaForFace(areasDict, "up");

                bool currentlyAttached = upBlock.CanAttachBlockAt(world.BlockAccessor, this, pos.UpCopy(), BlockFacing.DOWN, attachmentArea);

                if (currentlyAttached != be.IsCeilingAttached) {
                    be.IsCeilingAttached = currentlyAttached;
                    be.MarkDirty(true);
                }
            }
        }

        base.OnNeighbourBlockChange(world, pos, neibpos);
    }

    // Rotation logic
    public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack) {
        bool val = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
        BEBaseFSBasket? block = world.BlockAccessor.GetBlockEntity<BEBaseFSBasket>(blockSel.Position);
        block?.MeshAngle = GetBlockMeshAngle(byPlayer, blockSel, val);

        return val;
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
        BlockEntity? be = world.BlockAccessor.GetBlockEntity(blockSel.Position);
        bool shift = byPlayer.Entity.Controls.ShiftKey;

        if (shift && be is BEBaseFSBasket frbasket) {
            return frbasket.OnInteract(byPlayer, blockSel);
        }

        return BaseOnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        dsc.Append(Lang.Get("foodshelves:Contents"));

        if (!inSlot.Empty) {
            ItemStack[] contents = GetContents(world, inSlot.Itemstack);
            dsc.Append(PerishableInfoAverageAndSoonest(contents.ToDummySlots(), world));
        }
    }

    public override MeshData? GenMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas, BlockPos? atBlockPos) {
        MeshData? basketMesh = base.GenMesh(slot, targetAtlas, atBlockPos);
        MeshData? contentMesh = GenBasketContents(slot.Itemstack, targetAtlas);

        if (contentMesh != null) {
            contentMesh.Translate(0, 0.02f, 0);
            basketMesh?.AddMeshData(contentMesh);
        }

        return basketMesh;
    }

    protected virtual MeshData? GenBasketContents(ItemStack? itemstack, ITextureAtlasAPI targetAtlas) {
        if (itemstack == null) return null;

        ItemStack[] contents = GetContents(api.World, itemstack);
        MeshData? contentMesh = GenContentMesh(api as ICoreClientAPI, contents, GetTransformationMatrix(), Transformations, GetTransformationModifier());

        return contentMesh;
    }

    protected MeshData? BaseGenMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos) {
        return base.GenMesh(slot, targetAtlas, atBlockPos);
    }

    public override string GetMeshCacheKey(ItemSlot slot) {
        string blockKey = base.GetMeshCacheKey(slot);

        ItemStack[] contents = GetContents(api.World, slot.Itemstack);
        int hashcode = contents.GetStackCacheHashCodeFNV();

        return $"{blockKey}-{hashcode}";
    }

    // Method used for a bit more complex checking, like how the vegetable basket has "groups"
    public virtual bool CanAddToContents(ItemStack[] contents, ItemStack incoming, out int capacity) {
        capacity = InnerSlotCount;
        return contents.Length < capacity;
    }

    public virtual bool OnContainedInteractStart(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) {
        var targetSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
        if (targetSlot == null) return false;

        // Itemslot cleanup
        // When an item rots into nothing within a basket, while it's within the Cabinet / Double Shelf, it will still "occupy" the slot, so a cleanup is needed.
        ItemStack[] contents = InventoryExtensions.GetContents(api.World, slot.Itemstack) ?? [];
        List<ItemStack> validContents = [];
        bool contentsChanged = false;

        foreach (var stack in contents) {
            if (stack == null || stack.StackSize <= 0) {
                contentsChanged = true;
                continue;
            }

            DummySlot dummy = new(stack, be.Inventory);

            stack.Collectible.UpdateAndGetTransitionStates(api.World, dummy);

            if (dummy.Empty || dummy.Itemstack == null || dummy.Itemstack.StackSize <= 0) {
                contentsChanged = true; // It rotted into nothing
            }
            else {
                if (dummy.Itemstack.Collectible.Code != stack.Collectible.Code) {
                    contentsChanged = true;
                }
                validContents.Add(dummy.Itemstack);
            }
        }

        // Save the cleaned up array
        if (contentsChanged) {
            contents = [.. validContents];
            InventoryExtensions.SetContents(slot.Itemstack, contents);
            be.MarkDirty();
        }


        // Putting stuff in
        if (!targetSlot.Empty) {
            if (!targetSlot.CanStoreInSlot("fs" + InteractionsName))
                return false;

            if (!CanAddToContents(contents, targetSlot.Itemstack, out int capacity) || contents.Length >= capacity)
                return false;

            int maxAdd = capacity - contents.Length;
            int amountToMove = byPlayer.Entity.Controls.CtrlKey ? Math.Min(maxAdd, targetSlot.StackSize) : 1;

            int moved = 0;
            for (int i = 0; i < amountToMove; i++) {
                ItemStack one = targetSlot.TakeOut(1);
                if (one == null) break;

                contents = [.. contents, one];
                moved++;
            }

            if (moved == 0) return false;

            InventoryExtensions.SetContents(slot.Itemstack, contents);
            targetSlot.MarkDirty();
            be.MarkDirty();
            return true;
        }


        // Taking stuff out
        if (contents.Length == 0) return false;

        int bestIdx = 0;
        double lowestFreshHours = double.MaxValue;
        float highestRotLevel = -1f;
        bool activelyRotting = false;

        for (int i = 0; i < contents.Length; i++) {
            ItemStack stack = contents[i];

            if (stack.Collectible.Code.Path.StartsWith("rot")) {
                bestIdx = i;
                break;
            }

            DummySlot dummySlot = new(stack, be.Inventory);
            TransitionState[]? states = stack.Collectible.UpdateAndGetTransitionStates(api.World, dummySlot);
            if (states == null) continue;

            foreach (var state in states) {
                if (state.Props.Type != EnumTransitionType.Perish) continue;

                // Actively rotting check
                if (state.TransitionLevel > 0 && state.TransitionLevel > highestRotLevel) {
                    highestRotLevel = state.TransitionLevel;
                    bestIdx = i;
                    activelyRotting = true;
                    continue;
                }

                // Fresh food check
                if (!activelyRotting && state.TransitionLevel <= 0) {
                    float rate = stack.Collectible.GetTransitionRateMul(api.World, dummySlot, EnumTransitionType.Perish);
                    double effectiveFreshness = rate > 0 ? state.FreshHoursLeft / rate : state.FreshHoursLeft;

                    if (effectiveFreshness < lowestFreshHours) {
                        lowestFreshHours = effectiveFreshness;
                        bestIdx = i;
                    }
                }
            }
        }

        ItemStack taken = contents[bestIdx];
        contents = contents.Where((_, index) => index != bestIdx).ToArray();

        if (!byPlayer.InventoryManager.TryGiveItemstack(taken, true))
            api.World.SpawnItemEntity(taken, byPlayer.Entity.Pos.XYZ);

        InventoryExtensions.SetContents(slot.Itemstack, contents);
        be.MarkDirty();

        return true;
    }

    public WorldInteraction[] GetContainedInteractionHelp(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) => [];
    public bool OnContainedInteractStep(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) => false;
    public void OnContainedInteractStop(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) {}
}
