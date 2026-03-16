namespace FoodShelves;

public class BETableWShelf : BEBaseFSContainer {
    public override string AttributeCheck => "shelvable";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlock;
    protected override bool RipeningSpot => true;

    public override int ItemsPerSegment => 2;

    private enum TableWShelfPart {
        Table = 1,
        Shelf = 0
    }

    public BETableWShelf() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); }

    protected override bool TryPut(IPlayer byPlayer, ItemSlot slot, BlockSelection blockSel) {
        if (blockSel.SelectionBoxIndex != (int)TableWShelfPart.Shelf)
            return false;

        for (int i = 0; i < SlotCount; i++) {
            if (inv[i].Empty) {
                int moved = slot.TryPutInto(Api.World, inv[i]);
                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                MarkDirty();
                return moved > 0;
            }
        }

        return false;
    }

    protected override bool TryTake(IPlayer byPlayer, BlockSelection blockSel) {
        if (blockSel.SelectionBoxIndex != (int)TableWShelfPart.Shelf)
            return false;

        for (int i = SlotCount - 1; i >= 0; i--) {
            if (!inv[i].Empty) {
                ItemStack stack = inv[i].TakeOut(1);
                
                if (byPlayer.InventoryManager.TryGiveItemstack(stack)) {
                    this.HandlePlacementEffects(stack, byPlayer);
                }

                if (stack.StackSize > 0) {
                    Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }

                return true;
            }
        }

        return false;
    }

    protected override float[][] genTransformationMatrices() {
        return TransformationGenerator.Generate(this, td => {
            td.z = td.item * 0.4f - 0.175f;
            td.y = 0.185f;
        });
    }
}
