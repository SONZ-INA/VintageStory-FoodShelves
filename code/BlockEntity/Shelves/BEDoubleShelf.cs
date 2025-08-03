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
