namespace FoodShelves;

public class BEFoodDisplayBlock : BEBaseFSContainer {
    public override string AttributeTransformCode => "onFoodUniversalTransform";

    public override string AttributeCheck => "fsFoodUniversal";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.BySegment;
    protected override bool RipeningSpot => true;

    protected override float PerishMultiplier => 0.75f;

    private enum SlotNumber {
        BottomSlot = 0,
        TopSlot = 1
    }

    public BEFoodDisplayBlock() {
        ShelfCount = 2;
        ItemsPerSegment = 4;
        inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); 
    }

    public override void Initialize(ICoreAPI api) {
        if (Block.Variant["type"] == "top") {
            ShelfCount = 1;
            this.RebuildInventory(api);
        }

        base.Initialize(api);
    }

    protected override bool TryPut(IPlayer byPlayer, ItemSlot slot, BlockSelection blockSel) {
        int index = blockSel.SelectionBoxIndex;
        if (index >= ShelfCount) return false;

        // Bottom Slot
        if (index == (int)SlotNumber.BottomSlot)
            return TryPlaceInSlots(byPlayer, slot, 0, ItemsPerSegment);

        // Top Slot
        if (Block.Variant["type"] == "top" != true && index == (int)SlotNumber.TopSlot)
            return TryPlaceInSlots(byPlayer, slot, ItemsPerSegment, ShelfCount * ItemsPerSegment);

        return false;
    }

    private bool TryPlaceInSlots(IPlayer byPlayer, ItemSlot slot, int startIndex, int endIndex) {
        if (inv[startIndex].Empty) {
            if (slot.TryPutInto(Api.World, inv[startIndex]) > 0) {
                return this.HandlePlacementEffects(slot.Itemstack, byPlayer);
            }
        }
        else if (!inv[startIndex].Itemstack?.IsLargeItem() == true && slot.Itemstack?.IsLargeItem() == false) {
            for (int i = startIndex + 1; i < endIndex; i++) {
                if (inv[i].Empty) {
                    if (slot.TryPutInto(Api.World, inv[i]) > 0) {
                        return this.HandlePlacementEffects(slot.Itemstack, byPlayer);
                    }
                }
            }
        }

        return false;
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        InitMesh(); // Re-meshing the falling block
        return base.OnTesselation(mesher, tesselator);
    }

    protected override float[][] genTransformationMatrices() {
        float[][] tfMatrices = new float[SlotCount][];

        for (int i = 0; i < SlotCount; i++) {
            if ((i < ItemsPerSegment && inv[i].Itemstack?.IsLargeItem() == true) || (i >= ItemsPerSegment && inv[i].Itemstack?.IsLargeItem() == true)) {
                tfMatrices[i] = new Matrixf()
                    .Translate(0.5f, 0, 0.5f)
                    .RotateYDeg(block?.Shape.rotateY ?? 0)
                    .Translate(-0.5f, i % (ItemsPerSegment - 1) * 0.3725f + 0.2525f, -0.5f)
                    .Values;
            }
            else {
                float x = i % (ItemsPerSegment / 2) == 0 ? 0.18f : -0.18f;
                float z = (i / (ItemsPerSegment / 2)) % 2 == 0 ? 0.18f : -0.18f;

                tfMatrices[i] = new Matrixf()
                    .Translate(0.5f, 0, 0.5f)
                    .RotateYDeg(block?.Shape.rotateY ?? 0)
                    .Translate(x - 0.5f, i / ItemsPerSegment * 0.3725f + 0.2525f, z - 0.5f)
                    .Values;
            }
        }

        return tfMatrices;
    }
}
