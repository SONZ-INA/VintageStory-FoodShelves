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

    public override WorldInteraction[]? GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {
        return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer)
            .Append(interactions);
    }

    public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos) {
        BEBaseFSBasket? be = GetBlockEntity<BEBaseFSBasket>(pos);
        if (be != null) {
            Block upBlock = world.BlockAccessor.GetBlock(pos.UpCopy());
            be.IsCeilingAttached = upBlock.SideSolid[BlockFacing.DOWN.Index];

            be.MarkDirty(true);
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
        if (byPlayer.Entity.Controls.ShiftKey) {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEBaseFSBasket frbasket) {
                return frbasket.OnInteract(byPlayer, blockSel);
            }
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

        dsc.AppendLine(Lang.Get("foodshelves:Empty."));
        return;
    }

    public abstract ExplicitTransform GetTransformationMatrix(string? path = null);
    
    public virtual Action<TransformationData>? GetTransformationModifier() { 
        return null; 
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

        // Putting stuff in
        if (!targetSlot.Empty && targetSlot.CanStoreInSlot("fs" + InteractionsName)) {
            ItemStack[] contents = InventoryExtensions.GetContents(api.World, slot.Itemstack) ?? [];

            if (!CanAddToContents(contents, targetSlot.Itemstack, out int capacity) || contents.Length >= capacity)
                return false;

            // CTRL behavior to fill the basket
            int maxAdd = capacity - contents.Length;
            int amountToMove = byPlayer.Entity.Controls.CtrlKey ? Math.Min(maxAdd, targetSlot.StackSize) : 1;

            int moved = 0;
            for (int i = 0; i < amountToMove; i++) {
                ItemStack one = targetSlot.TakeOut(1);
                if (one == null) break;

                contents = [.. contents, one];
                moved++;
            }

            if (moved > 0) {
                InventoryExtensions.SetContents(slot.Itemstack, contents);
                targetSlot.MarkDirty();
                be.MarkDirty();
                return true;
            }

            return false;
        }

        // Taking stuff out
        if (targetSlot.Empty) {
            ItemStack[] contents = InventoryExtensions.GetContents(api.World, slot.Itemstack) ?? [];
            if (contents.Length == 0) return false;

            ItemStack taken = contents[^1];
            Array.Resize(ref contents, contents.Length - 1);

            if (!byPlayer.InventoryManager.TryGiveItemstack(taken, true))
                api.World.SpawnItemEntity(taken, byPlayer.Entity.Pos.XYZ);

            InventoryExtensions.SetContents(slot.Itemstack, contents);
            be.MarkDirty();
            return true;
        }

        return false;
    }

    public bool OnContainedInteractStep(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) {
        return false;
    }

    public void OnContainedInteractStop(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) {
        // Do nothing
    }

    public WorldInteraction[] GetContainedInteractionHelp(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) {
        return [];
    }
}
