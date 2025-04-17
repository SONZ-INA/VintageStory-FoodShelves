namespace FoodShelves;

public class BlockEntitySeedShelf : BlockEntityDisplay {
    private InventoryGeneric inv;
    private Block block;
    
    public override InventoryBase Inventory => inv;
    public override string InventoryClassName => Block?.Attributes?["inventoryClassName"].AsString();
    public override string AttributeTransformCode => Block?.Attributes?["attributeTransformCode"].AsString();

    private const int shelfCount = 3;
    private const int segmentsPerShelf = 3;
    private int itemsPerSegment = 4;

    public BlockEntitySeedShelf() { inv = new InventoryGeneric(shelfCount * segmentsPerShelf * itemsPerSegment, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotSeedShelf(inv)); }

    public override void Initialize(ICoreAPI api) {
        block = api.World.BlockAccessor.GetBlock(Pos);

        base.Initialize(api);

        if (block.Code.SecondCodePart().StartsWith("short") || block.Code.SecondCodePart().StartsWith("veryshort")) {
            if (block.Code.SecondCodePart().StartsWith("short")) itemsPerSegment /= 2;
            else itemsPerSegment /= 4;

            // Need to save items and transfer it over to new inventory, they disappear otherwise
            List<ItemStack> stack = new();
            foreach (var slot in inv) stack.Add(slot.Itemstack);
            stack.ToArray();

            inv = new InventoryGeneric(shelfCount * segmentsPerShelf * itemsPerSegment, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotSeedShelf(inv));

            for (int i = 0; i < shelfCount * segmentsPerShelf * itemsPerSegment; i++) {
                if (i >= stack.Count) break;
                inv[i].Itemstack = stack[i];
            }

            Inventory.LateInitialize(Inventory.InventoryID, api);
        }
    }

    internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel) {
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

        if (slot.Empty) {
            return TryTake(byPlayer, blockSel);
        }
        else {
            if (slot.SeedShelfCheck()) {
                AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;

                if (TryPut(byPlayer, slot, blockSel)) {
                    Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                    MarkDirty();
                    return true;
                }
            }

            (Api as ICoreClientAPI)?.TriggerIngameError(this, "cantplace", Lang.Get("foodshelves:Only seeds can be placed on this shelf."));
            return false;
        }
    }

    private bool TryPut(IPlayer byPlayer, ItemSlot slot, BlockSelection blockSel) {
        int startIndex = blockSel.SelectionBoxIndex * itemsPerSegment;

        for (int i = 0; i < itemsPerSegment; i++) {
            int currentIndex = startIndex + i;
            ItemStack currentStack = inv[currentIndex].Itemstack;
            if (inv[currentIndex].Empty || (currentStack.Collectible.Equals(slot.Itemstack.Collectible) && currentStack.StackSize < currentStack.Collectible.MaxStackSize)) {
                int moved;
                if (byPlayer.Entity.Controls.ShiftKey)
                    moved = slot.TryPutInto(Api.World, inv[currentIndex], inv[currentIndex].MaxSlotStackSize - inv[currentIndex].Itemstack?.StackSize ?? 64);
                else
                    moved = slot.TryPutInto(Api.World, inv[currentIndex], 1);
                
                if (moved > 0) {
                    MarkDirty();
                    (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                    return true;
                }
            }

            if (inv[currentIndex].Empty) {
                int moved = slot.TryPutInto(Api.World, inv[currentIndex]);
                MarkDirty();
                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                return moved > 0;
            }
        }

        return false;
    }

    private bool TryTake(IPlayer byPlayer, BlockSelection blockSel) {
        int startIndex = blockSel.SelectionBoxIndex * itemsPerSegment;

        for (int i = itemsPerSegment - 1; i >= 0; i--) {
            int currentIndex = startIndex + i;
            if (!inv[currentIndex].Empty) {
                ItemStack stack;
                if (byPlayer.Entity.Controls.ShiftKey)
                    stack = inv[currentIndex].TakeOutWhole();
                else
                    stack = inv[currentIndex].TakeOut(1);

                if (byPlayer.InventoryManager.TryGiveItemstack(stack)) {
                    AssetLocation sound = stack.Block?.Sounds?.Place;
                    Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                }

                if (stack.StackSize > 0) {
                    Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }

                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                MarkDirty();
                return true;
            }
        }

        return false;
    }

    protected override float[][] genTransformationMatrices() {
        float[][] tfMatrices = new float[shelfCount * segmentsPerShelf * itemsPerSegment][];

        for (int shelf = 0; shelf < shelfCount; shelf++) {
            for (int segment = 0; segment < segmentsPerShelf; segment++) {
                for (int item = 0; item < itemsPerSegment; item++) {
                    int index = shelf * (segmentsPerShelf * itemsPerSegment) + segment * itemsPerSegment + item;

                    float x = segment * 0.575f;
                    float y = shelf * 0.9f;
                    float z = item * 0.4125f;

                    tfMatrices[index] =
                        new Matrixf()
                        .Translate(0.5f, 0, 0.5f)
                        .RotateYDeg(block.Shape.rotateY)
                        .Scale(0.44f, 0.35f, 0.44f)
                        .Translate(x - 1.075f, y + 0.175f, z - 1.225f)
                        .Values;
                }
            }
        }

        return tfMatrices;
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving) {
        base.FromTreeAttributes(tree, worldForResolving);
        RedrawAfterReceivingTreeAttributes(worldForResolving);
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb) {
        base.GetBlockInfo(forPlayer, sb);
        DisplayInfo(forPlayer, sb, inv, InfoDisplayOptions.BySegment, shelfCount * segmentsPerShelf * itemsPerSegment, segmentsPerShelf, itemsPerSegment);
    }
}
