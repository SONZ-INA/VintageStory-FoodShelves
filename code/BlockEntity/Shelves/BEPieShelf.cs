namespace FoodShelves;

public class BEPieShelf : BEBaseFSContainer {
    protected override string CantPlaceMessage => "foodshelves:Only pies or cheese can be placed on this shelf.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.ByBlock;
    protected override bool RipeningSpot => true;

    public override int ShelfCount => 3;
    public override int ItemsPerSegment => 4;

    public BEPieShelf() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); }

    protected override bool TryPut(IPlayer byPlayer, ItemSlot slot, BlockSelection blockSel) {
        int startIndex = blockSel.SelectionBoxIndex;
        if (startIndex > inv.Count) return false;

        ItemStack? stack = slot.Itemstack;
        startIndex *= ItemsPerSegment;

        if (!CanInsertIntoSegment(inv[startIndex], stack))
            return false;

        for (int i = 0; i < ItemsPerSegment; i++) {
            int currentIndex = startIndex + i;

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
        float[][] tfMatrices = new float[SlotCount][];

        for (int shelf = 0; shelf < ShelfCount; shelf++) {
            for (int item = 0; item < ItemsPerSegment; item++) {
                int index = shelf * (SegmentsPerShelf * ItemsPerSegment) + item;

                float x = 0;
                float y = index / ItemsPerSegment * 0.313f + 0.0525f;
                float z = 0; // index % ItemsPerSegment * 0.1f;

                float offsetX = 0;
                float offsetZ = 0;
                float ry = index % ItemsPerSegment;

                if (inv[index].Itemstack?.IsMediumItem() == true) {
                    x -= 0.075f;
                    z = 0.075f;
                    offsetZ = -0.025f;
                    ry *= 180f;
                }

                if (inv[index].Itemstack?.IsStandardItem() == true) {
                    x -= 0.025f;
                    int zzz = block.GetRotationAngle();

                    if (zzz == 0) offsetZ = -0.05f;
                    if (zzz == 90) offsetX = -0.05f; 
                    if (zzz == 180) offsetZ = 0.05f; 
                    if (zzz == 270) offsetX = 0.05f; 

                    ry *= 90f + 180f;
                }

                tfMatrices[index] = new Matrixf()
                    .Translate(0.5f, 0, 0.5f)
                    .Translate(offsetX, 0, offsetZ)
                    .RotateYDeg((block?.Shape.rotateY ?? 0) + ry)
                    .Translate(x - 0.5f, y, z - 0.55f)
                    .Values;
            }
        }

        return tfMatrices;
    }
}
