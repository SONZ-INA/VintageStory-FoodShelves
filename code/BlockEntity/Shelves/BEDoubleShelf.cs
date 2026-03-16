namespace FoodShelves;

public class BEDoubleShelf : BEBaseFSContainer {
    public override string AttributeTransformCode => "onHolderUniversalTransform";
    public override string AttributeCheck => "fsHolderUniversal";

    protected override string CantPlaceMessage => "foodshelves:This item cannot be placed in this container.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.BySegment;
    protected override bool RipeningSpot => true;

    public BEDoubleShelf() {
        SegmentsPerShelf = 3;
        ItemsPerSegment = 24;

        inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); 
    }

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel, string? overrideAttrCheck = null) {
        bool ctrl = byPlayer.Entity.Controls.CtrlKey;

        // Crock sealing interactions
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
        if ((!slot.Empty || ctrl) && TryUse(byPlayer, slot, blockSel))
            return true;

        return base.OnInteract(byPlayer, blockSel);
    }

    protected bool TryUse(IPlayer player, ItemSlot slot, BlockSelection blockSel) {
        int segmentIndex = blockSel.SelectionBoxIndex;
        int startIndex = segmentIndex * ItemsPerSegment;
        int endIndex = startIndex + ItemsPerSegment - 20; // Offset of 20 since the crocks can only fit 4 in a segment.

        // If it's empty, shift the check further down - crocks in the back can be reached.
        if (inv[endIndex - 1].Empty) endIndex--;
        if (inv[endIndex - 1].Empty) endIndex--;

        if (inv[startIndex].Itemstack?.Collectible is BaseFSBasket && inv[startIndex].Itemstack?.Collectible is IContainedInteractable ic)
            return ic.OnContainedInteractStart(this, inv[startIndex], player, blockSel);

        // Only check last 2 slots (visually front crocks)
        for (int i = endIndex - 1; i >= Math.Max(startIndex, endIndex - 2); i--) {
            var stack = inv[i].Itemstack;
            var stackSize = slot!.Itemstack?.StackSize ?? 0;

            if (stack?.Collectible is IContainedInteractable ici && ici.OnContainedInteractStart(this, inv[i], player, blockSel)) {
                // If it's a meal container, don't check for "sealing" behavior
                if (slot.Itemstack?.ItemAttributes["mealContainer"].AsBool() == true) {
                    MarkDirty();
                    return true;
                }

                // If item is consumed for sealing, stop.
                int afterSize = slot?.Itemstack?.StackSize ?? 0;
                if (stackSize != afterSize) {
                    MarkDirty();
                    return true;
                }
                // Otherwise, keep looping to check the crock behind
            }
        }

        return false;
    }

    protected override bool TryPut(IPlayer byPlayer, ItemSlot slot, BlockSelection blockSel) {
        int startIndex = blockSel.SelectionBoxIndex * ItemsPerSegment;

        if (!inv[startIndex].Empty) {
            ItemStack? firstItemInSegment = inv[startIndex].Itemstack;
            if (!firstItemInSegment.BelongsToSameGroupAs(slot.Itemstack)) return false;
            if (slot.Itemstack?.IsLargeItem() == true || firstItemInSegment?.IsLargeItem() == true) return false;
            if (firstItemInSegment?.IsSmallItem() != slot.Itemstack?.IsSmallItem()) return false;
        }

        for (int i = 0; i < ItemsPerSegment; i++) {
            int currentIndex = startIndex + i;
            if (currentIndex == startIndex + 4 && slot.Itemstack?.IsSmallItem() == false) return false;

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
            td.x = td.segment * 0.625f - 0.125f;
            td.y = 0.395f;
            td.scaleX = td.scaleY = td.scaleZ = 0.95f;
        }, true);
    }
}
