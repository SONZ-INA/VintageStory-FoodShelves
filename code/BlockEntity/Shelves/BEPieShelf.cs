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
            td.offsetOriginZ = -0.025f;
            td.y = td.shelf * 0.313f;
            td.offsetY = 0.0525f;
        }, true);
    }
}
