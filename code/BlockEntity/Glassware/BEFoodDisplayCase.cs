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

    protected override int GetSegmentLimit(ItemStack? stack) {
        return SegmentLimits.Mixed(this, stack);
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator) {
        InitMesh(); // Re-meshing the falling block
        return base.OnTesselation(mesher, tesselator);
    }

    protected override float[][] genTransformationMatrices() {
        return TransformationGenerator.GenerateLayout(this, td => {
            td.y = td.shelf * 0.3825f + 0.25f;
            td.z = (td.item / 2 == 0 ? -0.05f : 0.05f) + 0.05f;

            td.rotX = td.shelf * 15;
            td.y += td.shelf * td.item / 2 * -0.025f;
        }, true);
    }
}
