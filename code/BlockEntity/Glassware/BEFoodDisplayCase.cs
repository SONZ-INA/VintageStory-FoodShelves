namespace FoodShelves;

public class BEFoodDisplayCase : BEBaseFSContainer {
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
        return TransformationGenerator.Generate(this, td => {
            td.y = td.shelf * 0.3825f + 0.25f;
            td.z = (td.item / 2 == 0 ? -0.05f : 0.05f) + 0.05f;

            td.rotX = td.shelf * 15;
            td.y += td.shelf * td.item / 2 * -0.025f;
        }, true);
    }
}
