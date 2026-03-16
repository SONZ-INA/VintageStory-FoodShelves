namespace FoodShelves;

public class BEPieShelf : BEBaseFSContainer {
    protected override string CantPlaceMessage => "foodshelves:Only pies or cheese can be placed on this shelf.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.BySegment;
    protected override bool RipeningSpot => true;

    public override int ShelfCount => 3;
    public override int ItemsPerSegment => 4;

    public BEPieShelf() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); }

    protected override bool TryPut(IPlayer byPlayer, ItemSlot slot, BlockSelection blockSel) {
        int startIndex = blockSel.SelectionBoxIndex;
        if (startIndex > inv.Count) return false;

        ItemStack? stack = slot.Itemstack;
        startIndex *= ItemsPerSegment;

        for (int i = 0; i < ItemsPerSegment; i++) {
            int currentIndex = startIndex + i;

            if (!CanInsertIntoSegment(inv[currentIndex].Itemstack, stack))
                return false;

            if (currentIndex == startIndex + 4 && stack?.IsSmallItem() == false)
                return false;

            if (inv[currentIndex].Empty) {
                int moved = slot.TryPutInto(Api.World, inv[currentIndex]);
                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                MarkDirty();
                return moved > 0;
            }
        }

        return false;
    }

    protected override float[][] genTransformationMatrices() {
        return TransformationGenerator.Generate(this, td => {
            td.x = 0.0375f;
            td.y = td.shelf * 0.313f + 0.0525f;
            td.z = -0.05f;
            td.rotY = 45;
        }, true);
    }
}
