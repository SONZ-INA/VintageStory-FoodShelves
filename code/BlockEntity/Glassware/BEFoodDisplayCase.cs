namespace FoodShelves;

public class BEFoodDisplayCase : BEFSContainer {
    public override string AttributeTransformCode => "onFoodUniversalTransform";

    public override string AttributeCheck => "fsFoodUniversal";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.BySegment;
    protected override bool RipeningSpot => true;

    protected override float PerishMultiplier => 0.75f;

    private enum SlotNumber {
        BottomSlot = 0,
        TopSlot = 1
    }

    public BEFoodDisplayCase() {
        ShelfCount = 2;
        ItemsPerSegment = 4;
        inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); 
    }

    public override void Initialize(ICoreAPI api) {
        base.Initialize(api);
        inv.OnAcquireTransitionSpeed += Inventory_OnAcquireTransitionSpeed;
    }

    protected override bool TryPut(IPlayer byPlayer, ItemSlot slot, BlockSelection blockSel) {
        int index = blockSel.SelectionBoxIndex;
        if (index >= ShelfCount) return false;

        // Bottom Slot
        if (index == (int)SlotNumber.BottomSlot) {
            return TryPlaceInSlots(slot, 0, ItemsPerSegment);
        }

        // Top Slot
        if (Block.Variant["type"] == "top" != true && index == (int)SlotNumber.TopSlot) {
            return TryPlaceInSlots(slot, ItemsPerSegment, ShelfCount * ItemsPerSegment);
        }

        return false;
    }

    private bool TryPlaceInSlots(ItemSlot slot, int startIndex, int endIndex) {
        if (inv[startIndex].Empty) {
            int moved = slot.TryPutInto(Api.World, inv[startIndex]);
            if (moved > 0) {
                MarkDirty();
                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                return true;
            }
        }
        else if (!IsLargeItem(inv[startIndex].Itemstack) && !IsLargeItem(slot.Itemstack)) {
            for (int i = startIndex + 1; i < endIndex; i++) {
                if (inv[i].Empty) {
                    int moved = slot.TryPutInto(Api.World, inv[i]);
                    if (moved > 0) {
                        MarkDirty();
                        (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                        return true;
                    }
                }
            }
        }

        return false;
    }

    protected override float[][] genTransformationMatrices() {
        float[][] tfMatrices = new float[SlotCount][];

        for (int i = 0; i < SlotCount; i++) {
            if ((i < ItemsPerSegment && IsLargeItem(inv[i].Itemstack)) || (i >= ItemsPerSegment && IsLargeItem(inv[i].Itemstack))) {
                tfMatrices[i] =
                    new Matrixf()
                    .Translate(0.5f, 0, 0.5f)
                    .RotateYDeg(block.Shape.rotateY)
                    .RotateXDeg(i >= ItemsPerSegment ? 15 : 0)
                    .Translate(-0.5f, i % (ItemsPerSegment - 1) * 0.3725f + 0.24f, i >= ItemsPerSegment ? -0.65f : -0.5f)
                    .Values;
            }
            else {
                float x = i % (ItemsPerSegment / 2) == 0 ? -0.18f : 0.18f;
                float z = i / (ItemsPerSegment / 2) % 2 == 0 ? -0.18f : 0.18f;

                tfMatrices[i] =
                    new Matrixf()
                    .Translate(0.5f, 0, 0.5f)
                    .RotateYDeg(block.Shape.rotateY)
                    .RotateXDeg(i >= ItemsPerSegment ? 15 : 0)
                    .Translate(x - 0.5f, i / ItemsPerSegment * 0.3725f + 0.24f, z - (i >= ItemsPerSegment ? 0.65f : 0.5f))
                    .Values;
            }
        }

        return tfMatrices;
    }
}
