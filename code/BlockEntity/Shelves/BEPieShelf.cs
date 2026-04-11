namespace FoodShelves;

public class BEPieShelf : BEBaseFSContainer {
    protected override string CantPlaceMessage => "foodshelves:Only pies or cheese can be placed on this shelf.";
    protected override InfoDisplayOptions InfoDisplay => InfoDisplayOptions.BySegment;
    protected override bool RipeningSpot => true;

    public override int ShelfCount => 3;
    public override int ItemsPerSegment => 4;

    public BEPieShelf() { inv = new InventoryGeneric(SlotCount, InventoryClassName + "-0", Api, (_, inv) => new ItemSlotFSUniversal(inv, AttributeCheck)); }

    protected override int GetSegmentLimit(ItemStack? stack) {
        return SegmentLimits.Mixed(this, stack);
    }

    protected override float[][] genTransformationMatrices() {
        return TransformationGenerator.GenerateLayout(this, td => {
            td.x = 0.0375f;
            td.y = td.shelf * 0.313f + 0.0525f;
            td.z = -0.05f;
            td.rotY = 45;
        }, true);
    }
}
