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

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel) {
        // Crock sealing interactions
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
        if (!slot.Empty && TryUse(byPlayer, slot, blockSel)) {
            return true;
        }

        return base.OnInteract(byPlayer, blockSel);
    }

    protected bool TryUse(IPlayer player, ItemSlot slot, BlockSelection blockSel) {
        if (blockSel.SelectionBoxIndex > 8) return false; // If it's cabinet or drawer selection box, return

        int segmentIndex = blockSel.SelectionBoxIndex;
        int startIndex = segmentIndex * ItemsPerSegment;
        int endIndex = startIndex + ItemsPerSegment - 20; // Offset of 20 since the crocks can only fit 4 in a segment.

        // If it's empty, shift the check further down - crocks in the back can be reached.
        if (inv[endIndex - 1].Empty) endIndex--;
        if (inv[endIndex - 1].Empty) endIndex--;

        // Only check last 2 slots (visually front crocks)
        for (int i = endIndex - 1; i >= Math.Max(startIndex, endIndex - 2); i--) {
            var stack = inv[i]?.Itemstack;
            var stackSize = slot?.Itemstack?.StackSize ?? 0;
            if (stack?.Collectible is IContainedInteractable ici) {
                if (ici.OnContainedInteractStart(this, inv[i], player, blockSel)) {
                    int afterSize = slot?.Itemstack?.StackSize ?? 0;

                    // If item is consumed for sealing, stop.
                    if (stackSize != afterSize) {
                        MarkDirty();
                        return true;
                    }
                    // Otherwise, keep looping to check the crock behind
                }
            }
        }

        return false;
    }

    protected override bool TryPut(IPlayer byPlayer, ItemSlot slot, BlockSelection blockSel) {
        int startIndex = blockSel.SelectionBoxIndex * ItemsPerSegment;

        if (!inv[startIndex].Empty) {
            ItemStack firstItemInSegment = inv[startIndex].Itemstack;
            if (!firstItemInSegment.IsSolitaryMatch(slot.Itemstack)) return false;
            if (slot.Itemstack.IsLargeItem() || firstItemInSegment.IsLargeItem()) return false;
            if (firstItemInSegment.IsSmallItem() != slot.Itemstack.IsSmallItem()) return false;
        }

        for (int i = 0; i < ItemsPerSegment; i++) {
            int currentIndex = startIndex + i;
            if (currentIndex == startIndex + 4 && !slot.Itemstack.IsSmallItem()) return false;

            if (inv[currentIndex].Empty) {
                int moved = slot.TryPutInto(Api.World, inv[currentIndex]);
                MarkDirty();
                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                return moved > 0;
            }
        }

        return false;
    }

    protected override float[][] genTransformationMatrices() {
        float[][] tfMatrices = new float[SlotCount][];

        for (int shelf = 0; shelf < ShelfCount; shelf++) {
            for (int segment = 0; segment < SegmentsPerShelf; segment++) {
                for (int item = 0; item < ItemsPerSegment; item++) {
                    int index = shelf * (SegmentsPerShelf * ItemsPerSegment) + segment * ItemsPerSegment + item;

                    float x, y = 0f, z;
                    float scale = 0.95f;

                    if (inv[index].Itemstack.IsLargeItem()) {
                        x = segment * 0.65f;
                        z = item * 0.65f;
                    }
                    else if (!inv[index].Itemstack.IsSmallItem()) {
                        x = segment * 0.65f + (index % 2 == 0 ? -0.16f : 0.16f);
                        z = (index / 2) % 2 == 0 ? -0.18f : 0.18f;
                    }
                    else {
                        x = segment * 0.763f + (item % 4) * 0.19f - 0.314f;
                        y = (item / 8) * 0.10f + 0.061f;
                        z = ((item / 4) % 2) * 0.45f - 0.25f;
                        scale = 0.82f;
                    }

                    if (inv[index].Itemstack?.Collectible.Code == "pemmican:pemmican-pack") {
                        y += item / 2 * 0.13f;
                        z = -0.18f;
                    }

                    tfMatrices[index] =
                        new Matrixf()
                        .Translate(0.5f, 0, 0.5f)
                        .RotateYDeg(block.Shape.rotateY)
                        .Scale(scale, scale, scale)
                        .Translate(x - 0.625f, y + 0.395f, z - 0.5325f)
                        .Values;
                }
            }
        }

        return tfMatrices;
    }
}
